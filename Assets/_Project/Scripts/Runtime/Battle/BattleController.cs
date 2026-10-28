using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TimeTraveler.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TimeTraveler
{
    /// <summary>
    /// Capa visual del combate. El combate ocurre sobre el propio mapa: el grupo se coloca en
    /// formación frente a los enemigos, sin transición de escena. Toda la lógica la decide
    /// BattleEngine (Core); aquí sólo se muestran menús y se animan los BattleEvents.
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        private sealed class Row
        {
            public BattleUnit Unit;
            public Text Name;
            public Text Hp;
            public Text Mp;
            public RectTransform AtbFill;
        }

        private GameManager gm;
        private BattleEngine engine;
        private Action<BattleOutcome> onFinished;
        private MapEnemy source;
        private Vector3 center, forward, right;

        private readonly Dictionary<BattleUnit, Transform> views = new Dictionary<BattleUnit, Transform>();
        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly Queue<BattleEvent> pending = new Queue<BattleEvent>();
        private readonly List<Row> rows = new List<Row>();
        private readonly Stack<Action> backStack = new Stack<Action>();

        private Canvas canvas;
        private RectTransform commandWindow, commandList, captionWindow, targetMarker;
        private Text commandTitle, infoText, captionText;
        private BattleUnit menuActor;
        private BattleUnit markedTarget;
        private bool animating;
        private bool finishing;

        public bool InBattle { get { return engine != null; } }
        /// <summary>Pruebas automáticas: usa la orden especial si existe, si no ataca; cierra los resultados solo.</summary>
        public bool AutoPlay;

        private void Awake()
        {
            gm = GetComponent<GameManager>();
        }

        // ================================================================== inicio y fin

        public void Begin(string encounterId, Vector3 battleCenter, MapEnemy sourceEnemy, Action<BattleOutcome> finished)
        {
            onFinished = finished;
            source = sourceEnemy;
            finishing = false;
            engine = BattleFactory.Create(gm.Db, gm.State, encounterId, new SystemRandom());

            var party = PartyVisuals.Instance;
            var leader = party.Leader;
            center = new Vector3(battleCenter.x, leader.position.y, battleCenter.z);
            forward = center - leader.position;
            forward.y = 0;
            if (forward.sqrMagnitude < 0.5f) forward = leader.forward;
            forward.Normalize();
            right = Vector3.Cross(Vector3.up, forward);
            // El centro del combate queda a media distancia: grupo detrás, enemigos delante.
            center = leader.position + forward * 3.5f;

            party.FollowEnabled = false;
            party.Player.enabled = false;
            var cc = leader.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            if (source != null) source.gameObject.SetActive(false);

            foreach (var unit in engine.Units)
            {
                Transform view;
                if (unit.Team == Team.Jugador)
                {
                    view = party.ViewFor(unit.SourceId);
                }
                else
                {
                    var go = Placeholders.Enemy(unit.EnemyDef);
                    spawned.Add(go);
                    view = go.transform;
                    view.position = ToWorld(unit.Position);
                }
                if (view == null) continue;
                views[unit] = view;
                StartCoroutine(MoveTo(view, ToWorld(unit.Position), unit.Team == Team.Jugador ? forward : -forward, 0.35f));
            }

            CameraRig.Instance.FocusBattle(center);
            BuildHud();
            foreach (var e in engine.DrainEvents()) pending.Enqueue(e);
        }

        private Vector3 ToWorld(Pos p)
        {
            return center + right * p.X + forward * p.Y;
        }

        private void Finish()
        {
            finishing = true;
            CloseMenu();
            var outcome = engine.Outcome;
            var summary = BattleResultService.Apply(gm.Db, gm.State, engine);
            if (outcome == BattleOutcome.Victoria) ShowResults(summary, () => Cleanup(outcome));
            else if (outcome == BattleOutcome.Huida) StartCoroutine(DelayThen(0.6f, () => Cleanup(outcome)));
            else StartCoroutine(DelayThen(1.5f, () => Cleanup(outcome)));
        }

        private void Cleanup(BattleOutcome outcome)
        {
            // Detiene animaciones en curso (caídas, números flotantes) antes de destruir los modelos.
            StopAllCoroutines();
            foreach (var go in spawned) Destroy(go);
            spawned.Clear();
            foreach (var pair in views)
                if (pair.Key.Team == Team.Jugador && pair.Value != null) pair.Value.localScale = Vector3.one;
            views.Clear();
            pending.Clear();
            if (source != null && outcome != BattleOutcome.Victoria) source.gameObject.SetActive(true);

            var party = PartyVisuals.Instance;
            var cc = party.Leader.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
            party.Player.enabled = true;
            party.RegroupAfterBattle();
            CameraRig.Instance.ReleaseBattle();

            if (canvas != null) Destroy(canvas.gameObject);
            rows.Clear();
            engine = null;
            animating = false;
            onFinished?.Invoke(outcome);
        }

        // ================================================================== bucle

        private void Update()
        {
            if (engine == null || finishing) return;
            UpdateHud();

            if (!animating)
            {
                foreach (var e in engine.DrainEvents()) pending.Enqueue(e);
                if (pending.Count > 0)
                {
                    StartCoroutine(PlayPending());
                    return;
                }
                if (engine.Outcome != BattleOutcome.EnCurso)
                {
                    Finish();
                    return;
                }
            }

            if (menuActor != null && !engine.ReadyPlayers.Contains(menuActor)) CloseMenu();
            if (menuActor == null && !animating && engine.ReadyPlayers.Count > 0) OpenCommands(engine.ReadyPlayers[0]);
            if (AutoPlay && menuActor != null && !animating)
            {
                var actor = menuActor;
                var rule = engine.Rule != null ? engine.Rule.GetCommands(engine, actor).FirstOrDefault() : null;
                var target = engine.Opponents(actor).FirstOrDefault();
                Submit(rule != null ? BattleAction.Rule(actor, rule.Id, target) : BattleAction.Attack(actor, target));
            }

            engine.MenuOpen = menuActor != null;
            engine.Paused = animating;
            engine.Tick(Time.deltaTime);

            if (menuActor != null && GameInput.Instance.Cancel.WasPressedThisFrame()) Back();
            UpdateMarker();
        }

        // ================================================================== interfaz

        private void BuildHud()
        {
            canvas = UIKit.CreateCanvas("BattleCanvas", 20);
            var root = canvas.transform;

            var partyWindow = UIKit.Window(root, "Party", new Vector2(0.02f, 0.02f), new Vector2(0.58f, 0.3f));
            var list = UIKit.VerticalList(partyWindow, false, 2f);
            rows.Clear();
            foreach (var unit in engine.Units.Where(u => u.Team == Team.Jugador))
            {
                var row = UIKit.Rect(list, "Row", Vector2.zero, Vector2.one);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.childControlWidth = true; h.childControlHeight = true; h.spacing = 12; h.childForceExpandWidth = true;
                var r = new Row { Unit = unit };
                r.Name = UIKit.Label(row, GameManager.T(unit.NameKey) + (unit.Kind == UnitKind.Mascota ? " ♦" : ""), 30);
                r.Hp = UIKit.Label(row, "", 28, TextAnchor.MiddleRight);
                r.Mp = UIKit.Label(row, "", 28, TextAnchor.MiddleRight, UIKit.MpColor);
                var atbHolder = UIKit.Rect(row, "Atb", Vector2.zero, Vector2.one);
                atbHolder.gameObject.AddComponent<LayoutElement>().preferredWidth = 160;
                r.AtbFill = UIKit.Bar(atbHolder, UIKit.AtbColor, 14f);
                ((RectTransform)r.AtbFill.parent).anchorMin = new Vector2(0, 0.5f);
                ((RectTransform)r.AtbFill.parent).anchorMax = new Vector2(1, 0.5f);
                rows.Add(r);
            }

            commandWindow = UIKit.Window(root, "Commands", new Vector2(0.6f, 0.02f), new Vector2(0.98f, 0.46f));
            commandTitle = UIKit.Label(commandWindow, "", 28, TextAnchor.UpperLeft, UIKit.ButtonSelected);
            commandTitle.rectTransform.anchorMin = new Vector2(0, 1);
            commandTitle.rectTransform.anchorMax = new Vector2(1, 1);
            commandTitle.rectTransform.offsetMin = new Vector2(16, -50);
            commandTitle.rectTransform.offsetMax = new Vector2(-16, -8);
            var listHolder = UIKit.Rect(commandWindow, "ListHolder", Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(0, -52));
            commandList = UIKit.VerticalList(listHolder, true, 4f);
            commandWindow.gameObject.SetActive(false);

            var info = UIKit.Window(root, "Info", new Vector2(0.02f, 0.31f), new Vector2(0.58f, 0.38f));
            infoText = UIKit.Stretch(UIKit.Label(info, "", 26), 10f);
            info.gameObject.SetActive(false);

            captionWindow = UIKit.Window(root, "Caption", new Vector2(0.2f, 0.86f), new Vector2(0.8f, 0.96f));
            captionText = UIKit.Stretch(UIKit.Label(captionWindow, "", 32, TextAnchor.MiddleCenter), 10f);
            captionWindow.gameObject.SetActive(false);

            targetMarker = UIKit.Rect(root, "TargetMarker", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-24, -24), new Vector2(24, 24));
            var arrow = UIKit.Stretch(UIKit.Label(targetMarker, "▼", 48, TextAnchor.MiddleCenter, UIKit.ButtonSelected), 0f);
            arrow.horizontalOverflow = HorizontalWrapMode.Overflow;
            targetMarker.gameObject.SetActive(false);
        }

        private void UpdateHud()
        {
            foreach (var r in rows)
            {
                var u = r.Unit;
                r.Hp.text = u.Hp + "/" + u.MaxHp;
                r.Hp.color = u.Hp == 0 ? Color.red : u.HpPercent < 0.25f ? new Color(1f, 0.75f, 0.3f) : UIKit.TextColor;
                r.Mp.text = u.Mp + " PM";
                UIKit.SetBar(r.AtbFill, u.IsAlive ? u.Atb / BattleEngine.AtbMax : 0f);
                r.Name.color = u == menuActor ? UIKit.ButtonSelected : UIKit.TextColor;
            }
        }

        private void SetInfo(string text)
        {
            infoText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(text));
            infoText.text = text;
        }

        private void ShowList(string title, List<Button> buttons)
        {
            commandWindow.gameObject.SetActive(true);
            commandTitle.text = title;
            UIKit.FocusFirst(commandList);
        }

        private void ResetList()
        {
            UIKit.Clear(commandList);
            markedTarget = null;
            SetInfo(null);
        }

        // ------------------------------------------------------------------ menú de órdenes

        private void OpenCommands(BattleUnit actor)
        {
            menuActor = actor;
            backStack.Clear();
            ShowCommands();
        }

        private void ShowCommands()
        {
            ResetList();
            var a = menuActor;
            var buttons = new List<Button>
            {
                UIKit.Button(commandList, GameManager.T("battle.attack"), () => Push(ShowCommands, () => ChooseTarget(TargetType.EnemigoUno, false, t => Submit(BattleAction.Attack(a, t))))),
                UIKit.Button(commandList, GameManager.T("battle.technique"), () => Push(ShowCommands, ShowTechniques), null, a.Techniques.Count > 0),
            };
            var combos = engine.AvailableCombos(a);
            buttons.Add(UIKit.Button(commandList, GameManager.T("battle.combo"), () => Push(ShowCommands, ShowCombos), () => SetInfo(combos.Count == 0 ? GameManager.T("battle.no_combos") : null), combos.Count > 0));
            buttons.Add(UIKit.Button(commandList, GameManager.T("battle.item"), () => Push(ShowCommands, ShowItems)));
            if (engine.Rule != null)
            {
                var ruleCommands = engine.Rule.GetCommands(engine, a).ToList();
                if (ruleCommands.Count > 0) buttons.Add(UIKit.Button(commandList, GameManager.T("battle.rule"), () => Push(ShowCommands, () => ShowRuleCommands(ruleCommands))));
            }
            buttons.Add(UIKit.Button(commandList, GameManager.T("battle.defend"), () => Submit(BattleAction.Defend(a))));
            buttons.Add(UIKit.Button(commandList, GameManager.T("battle.flee"), () => Submit(BattleAction.Flee(a))));
            ShowList(GameManager.T("battle.ready", GameManager.T(a.NameKey)), buttons);
        }

        private void ShowTechniques()
        {
            ResetList();
            var a = menuActor;
            var buttons = new List<Button>();
            foreach (var id in a.Techniques)
            {
                var tech = gm.Db.Technique(id);
                string label = GameManager.T(tech.NameKey) + "   " + GameManager.T("tech.cost", tech.MpCost);
                string desc = GameManager.T(tech.DescKey) + "  [" + GameManager.T("element." + tech.Element) + "]";
                buttons.Add(UIKit.Button(commandList, label, () => Push(ShowTechniques, () =>
                    ChooseTarget(tech.Target, tech.Revive > 0f, t => Submit(BattleAction.Technique(a, id, t)))),
                    () => SetInfo(desc), engine.CanUseTechnique(a, id)));
            }
            ShowList(GameManager.T("battle.technique"), buttons);
        }

        private void ShowCombos()
        {
            ResetList();
            var a = menuActor;
            var buttons = new List<Button>();
            foreach (var combo in engine.AvailableCombos(a))
            {
                var c = combo;
                List<BattleUnit> members;
                engine.TryGetComboParticipants(c, out members);
                string who = string.Join(" + ", members.Select(m => GameManager.T(m.NameKey)));
                buttons.Add(UIKit.Button(commandList, GameManager.T(c.NameKey), () => Push(ShowCombos, () =>
                    ChooseTarget(c.Target, c.Revive > 0f, t => Submit(BattleAction.Combo(a, c.Id, t)))),
                    () => SetInfo(who + " — " + GameManager.T(c.DescKey))));
            }
            ShowList(GameManager.T("battle.combo"), buttons);
        }

        private void ShowItems()
        {
            ResetList();
            var a = menuActor;
            var buttons = new List<Button>();
            foreach (var pair in gm.State.Inventory.Items.OrderBy(p => p.Key))
            {
                ItemDef item;
                if (!gm.Db.Items.TryGetValue(pair.Key, out item) || !item.UsableInBattle || item.Effect == null) continue;
                var it = item;
                var target = it.Effect.AllAllies ? TargetType.AliadosTodos : TargetType.AliadoUno;
                buttons.Add(UIKit.Button(commandList, GameManager.T(it.NameKey) + "  ×" + pair.Value, () => Push(ShowItems, () =>
                    ChooseTarget(target, it.Effect.Revive > 0f, t => Submit(BattleAction.Item(a, it.Id, t)))),
                    () => SetInfo(GameManager.T(it.DescKey))));
            }
            if (buttons.Count == 0) UIKit.Label(commandList, GameManager.T("menu.nothing"), 26, TextAnchor.MiddleLeft, UIKit.DimText);
            ShowList(GameManager.T("battle.item"), buttons);
        }

        private void ShowRuleCommands(List<RuleCommand> commands)
        {
            ResetList();
            var a = menuActor;
            var buttons = new List<Button>();
            foreach (var cmd in commands)
            {
                var c = cmd;
                buttons.Add(UIKit.Button(commandList, GameManager.T(c.NameKey), () =>
                {
                    if (c.TargetsEnemy) Push(() => ShowRuleCommands(commands), () => ChooseTarget(TargetType.EnemigoUno, false, t => Submit(BattleAction.Rule(a, c.Id, t))));
                    else Submit(BattleAction.Rule(a, c.Id, null));
                }, () => SetInfo(GameManager.T(c.DescKey))));
            }
            ShowList(GameManager.T("battle.rule"), buttons);
        }

        private void ChooseTarget(TargetType type, bool allowDead, Action<BattleUnit> onChosen)
        {
            if (type == TargetType.EnemigosTodos || type == TargetType.AliadosTodos) { onChosen(null); return; }
            if (type == TargetType.Propio) { onChosen(menuActor); return; }

            ResetList();
            var buttons = new List<Button>();
            foreach (var unit in engine.ValidTargets(menuActor, type, allowDead))
            {
                var u = unit;
                string label = GameManager.T(u.NameKey) + (u.Team == Team.Jugador ? "   " + u.Hp + "/" + u.MaxHp : "");
                buttons.Add(UIKit.Button(commandList, label, () => onChosen(u), () => markedTarget = u));
            }
            ShowList(GameManager.T("battle.target"), buttons);
        }

        private void Push(Action current, Action next)
        {
            backStack.Push(current);
            next();
        }

        private void Back()
        {
            if (backStack.Count > 0)
            {
                backStack.Pop()();
                return;
            }
            // En la raíz, Cancelar pasa el turno al siguiente personaje listo (como en los JRPG clásicos).
            if (engine.ReadyPlayers.Count > 1)
            {
                int index = (engine.ReadyPlayers.ToList().IndexOf(menuActor) + 1) % engine.ReadyPlayers.Count;
                OpenCommands(engine.ReadyPlayers[index]);
            }
        }

        private void Submit(BattleAction action)
        {
            if (!engine.Submit(action)) return;
            CloseMenu();
        }

        private void CloseMenu()
        {
            menuActor = null;
            markedTarget = null;
            backStack.Clear();
            if (commandWindow != null)
            {
                UIKit.Clear(commandList);
                commandWindow.gameObject.SetActive(false);
                SetInfo(null);
            }
            if (engine != null) engine.MenuOpen = false;
        }

        private void UpdateMarker()
        {
            Transform view;
            if (markedTarget == null || !views.TryGetValue(markedTarget, out view) || view == null)
            {
                targetMarker.gameObject.SetActive(false);
                return;
            }
            targetMarker.gameObject.SetActive(true);
            targetMarker.anchoredPosition = WorldToCanvas(view.position + Vector3.up * (markedTarget.IsBoss ? 5f : 2.6f));
        }

        private Vector2 WorldToCanvas(Vector3 world)
        {
            var cam = Camera.main;
            Vector2 screen = cam.WorldToScreenPoint(world);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen, null, out local);
            return local;
        }

        // ================================================================== animación de eventos

        private IEnumerator PlayPending()
        {
            animating = true;
            engine.Paused = true;
            while (pending.Count > 0) yield return Animate(pending.Dequeue());
            animating = false;
            if (engine != null) engine.Paused = false;
        }

        private IEnumerator Animate(BattleEvent e)
        {
            Transform target = e.Target != null && views.ContainsKey(e.Target) ? views[e.Target] : null;
            Transform actor = e.Actor != null && views.ContainsKey(e.Actor) ? views[e.Actor] : null;

            switch (e.Type)
            {
                case BattleEventType.ActionStart:
                    string caption = ActionCaption(e);
                    if (caption != null) Caption(caption);
                    var actors = e.Participants ?? (e.Actor != null ? new List<BattleUnit> { e.Actor } : new List<BattleUnit>());
                    Vector3 aim = target != null ? target.position : center;
                    foreach (var p in actors)
                        if (views.ContainsKey(p)) StartCoroutine(Lunge(views[p], aim));
                    yield return new WaitForSeconds(e.Participants != null ? 0.55f : 0.3f);
                    HideCaption();
                    break;
                case BattleEventType.Damage:
                    string extra = e.Critical ? GameManager.T("battle.critical") + " " : e.ElementMultiplier > 1f ? GameManager.T("battle.weak") + " " : e.ElementMultiplier < 1f ? GameManager.T("battle.resist") + " " : "";
                    Popup(target, extra + e.Amount, e.Critical ? new Color(1f, 0.85f, 0.2f) : Color.white);
                    if (target != null) StartCoroutine(Shake(target));
                    yield return new WaitForSeconds(0.28f);
                    break;
                case BattleEventType.Heal:
                    Popup(target, "+" + e.Amount, UIKit.HpColor);
                    yield return new WaitForSeconds(0.25f);
                    break;
                case BattleEventType.MpRestore:
                    Popup(target, "+" + e.Amount + " PM", UIKit.MpColor);
                    yield return new WaitForSeconds(0.25f);
                    break;
                case BattleEventType.Miss:
                    Popup(target, GameManager.T("battle.miss"), UIKit.DimText);
                    yield return new WaitForSeconds(0.25f);
                    break;
                case BattleEventType.Immune:
                    Popup(target, GameManager.T("battle.immune"), UIKit.DimText);
                    yield return new WaitForSeconds(0.25f);
                    break;
                case BattleEventType.StatusApplied:
                    Popup(target, GameManager.T("status." + e.Status), new Color(0.9f, 0.6f, 1f));
                    yield return new WaitForSeconds(0.25f);
                    break;
                case BattleEventType.TurnSkipped:
                    Popup(actor, "…", UIKit.DimText);
                    yield return new WaitForSeconds(0.3f);
                    break;
                case BattleEventType.Defend:
                    Popup(actor, GameManager.T("battle.defend"), UIKit.MpColor);
                    yield return new WaitForSeconds(0.2f);
                    break;
                case BattleEventType.Death:
                    if (target != null) StartCoroutine(Fall(target, e.Target.Team == Team.Enemigo));
                    yield return new WaitForSeconds(0.35f);
                    break;
                case BattleEventType.Revive:
                    if (target != null) { target.localScale = Vector3.one; target.rotation = Quaternion.LookRotation(forward); }
                    Popup(target, "+" + e.Amount, UIKit.HpColor);
                    yield return new WaitForSeconds(0.3f);
                    break;
                case BattleEventType.RuleMessage:
                    Caption(gm.Loc.Format(e.TextKey, e.Args ?? new object[0]));
                    yield return new WaitForSeconds(1.6f);
                    HideCaption();
                    break;
                case BattleEventType.FleeFailed:
                    Caption(GameManager.T(e.TextKey ?? "battle.flee.failed"));
                    yield return new WaitForSeconds(0.9f);
                    HideCaption();
                    break;
                case BattleEventType.Fled:
                    Caption(GameManager.T("battle.fled"));
                    yield return new WaitForSeconds(0.6f);
                    break;
                case BattleEventType.Defeat:
                    Caption(GameManager.T("battle.defeat"));
                    yield return new WaitForSeconds(1f);
                    break;
            }
        }

        private string ActionCaption(BattleEvent e)
        {
            switch (e.ActionType)
            {
                case ActionType.Tecnica: return GameManager.T(gm.Db.Technique(e.TechniqueId).NameKey);
                case ActionType.Combo: return GameManager.T(gm.Db.Combos[e.ComboId].NameKey);
                case ActionType.Objeto: return GameManager.T(gm.Db.Item(e.ItemId).NameKey);
                default: return null;
            }
        }

        private void Caption(string text)
        {
            captionWindow.gameObject.SetActive(true);
            captionText.text = text;
        }

        private void HideCaption()
        {
            captionWindow.gameObject.SetActive(false);
        }

        private void Popup(Transform at, string text, Color color)
        {
            if (at == null || canvas == null) return;
            var label = UIKit.Label(canvas.transform, text, 40, TextAnchor.MiddleCenter, color);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.rectTransform.sizeDelta = new Vector2(400, 60);
            StartCoroutine(FloatUp(label, at.position + Vector3.up * 2.2f));
        }

        private IEnumerator FloatUp(Text label, Vector3 world)
        {
            float t = 0f;
            while (t < 0.9f && label != null && canvas != null)
            {
                t += Time.deltaTime;
                label.rectTransform.anchoredPosition = WorldToCanvas(world) + new Vector2(0, 70f * t);
                var c = label.color;
                c.a = 1f - Mathf.Clamp01((t - 0.5f) / 0.4f);
                label.color = c;
                yield return null;
            }
            if (label != null) Destroy(label.gameObject);
        }

        private static IEnumerator MoveTo(Transform t, Vector3 position, Vector3 face, float duration)
        {
            Vector3 start = t.position;
            Quaternion startRot = t.rotation, endRot = Quaternion.LookRotation(face);
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, time / duration);
                t.position = Vector3.Lerp(start, position, k);
                t.rotation = Quaternion.Slerp(startRot, endRot, k);
                yield return null;
            }
            t.position = position;
            t.rotation = endRot;
        }

        private static IEnumerator Lunge(Transform t, Vector3 target)
        {
            Vector3 home = t.position;
            Vector3 dir = target - home;
            dir.y = 0;
            Vector3 peak = home + Vector3.ClampMagnitude(dir, Mathf.Max(0f, dir.magnitude - 1.2f));
            for (float time = 0f; time < 0.12f; time += Time.deltaTime) { t.position = Vector3.Lerp(home, peak, time / 0.12f); yield return null; }
            for (float time = 0f; time < 0.15f; time += Time.deltaTime) { t.position = Vector3.Lerp(peak, home, time / 0.15f); yield return null; }
            t.position = home;
        }

        private static IEnumerator Shake(Transform t)
        {
            Vector3 home = t.position;
            for (float time = 0f; time < 0.2f; time += Time.deltaTime)
            {
                t.position = home + new Vector3(Mathf.Sin(time * 90f) * 0.12f, 0, 0);
                yield return null;
            }
            t.position = home;
        }

        private static IEnumerator Fall(Transform t, bool vanish)
        {
            for (float time = 0f; time < 0.4f; time += Time.deltaTime)
            {
                if (t == null) yield break;
                float k = time / 0.4f;
                if (vanish) t.localScale = Vector3.one * (1f - k);
                else t.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.35f, k), 1f);
                yield return null;
            }
            if (vanish && t != null) t.gameObject.SetActive(false);
        }

        private static IEnumerator DelayThen(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }

        // ================================================================== resultados

        private void ShowResults(BattleSummary summary, Action onClose)
        {
            var window = UIKit.Window(canvas.transform, "Results", new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.8f));
            var list = UIKit.VerticalList(window, false, 4f);
            UIKit.Label(list, GameManager.T("battle.victory"), 44, TextAnchor.MiddleCenter, UIKit.ButtonSelected);
            UIKit.Label(list, GameManager.T("battle.xp", summary.Rewards.Xp), 30);
            UIKit.Label(list, GameManager.T("battle.money_gained", summary.Rewards.Money), 30);
            foreach (var item in summary.Rewards.Items)
                UIKit.Label(list, GameManager.T("battle.item_gained", GameManager.T(gm.Db.Item(item).NameKey)), 28);
            foreach (var up in summary.LevelUps)
            {
                UIKit.Label(list, GameManager.T("battle.level_up", GameManager.T(up.ActorName), up.NewLevel), 28, TextAnchor.MiddleLeft, UIKit.HpColor);
                foreach (var tech in up.NewTechniques)
                    UIKit.Label(list, GameManager.T("battle.learned", GameManager.T(up.ActorName), GameManager.T(gm.Db.Technique(tech).NameKey)), 26, TextAnchor.MiddleLeft, UIKit.AtbColor);
            }
            var pet = gm.State.ActivePet;
            if (summary.PetAffinityGained > 0 && pet != null)
                UIKit.Label(list, GameManager.T("battle.affinity", GameManager.T(gm.Db.Species(pet.SpeciesId).NameKey), summary.PetAffinityGained), 26, TextAnchor.MiddleLeft, UIKit.MpColor);
            var button = UIKit.Button(list, GameManager.T("battle.continue"), onClose);
            UIKit.Focus(button);
            if (AutoPlay) onClose();
        }
    }
}
