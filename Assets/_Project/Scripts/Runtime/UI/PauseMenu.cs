using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TimeTraveler.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TimeTraveler
{
    /// <summary>
    /// Menú de pausa (Start / Tab). Pestañas a la izquierda, contenido a la derecha y un panel de
    /// detalle que describe lo seleccionado. Cancelar retrocede un nivel; Start cierra.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        private GameManager gm;
        private Canvas canvas;
        private RectTransform tabWindow, tabList, contentList;
        private Text contentTitle, detailText, footerText;
        private readonly Stack<Action> backStack = new Stack<Action>();
        private bool fromTitle;
        private int openedFrame;

        public bool IsOpen { get { return canvas != null && canvas.gameObject.activeSelf; } }

        private GameState State { get { return gm.State; } }
        private GameDatabase Db { get { return gm.Db; } }
        private static string T(string key) { return GameManager.T(key); }
        private static string T(string key, params object[] args) { return GameManager.T(key, args); }

        private void Awake()
        {
            gm = GetComponent<GameManager>();
            canvas = UIKit.CreateCanvas("PauseCanvas", 40);
            tabWindow = UIKit.Window(canvas.transform, "Tabs", new Vector2(0.02f, 0.14f), new Vector2(0.22f, 0.96f));
            tabList = UIKit.VerticalList(tabWindow, false, 6f);

            var content = UIKit.Window(canvas.transform, "Content", new Vector2(0.24f, 0.32f), new Vector2(0.98f, 0.96f));
            contentTitle = UIKit.Label(content, "", 36, TextAnchor.UpperLeft, UIKit.ButtonSelected);
            contentTitle.rectTransform.anchorMin = new Vector2(0, 1);
            contentTitle.rectTransform.anchorMax = new Vector2(1, 1);
            contentTitle.rectTransform.offsetMin = new Vector2(20, -60);
            contentTitle.rectTransform.offsetMax = new Vector2(-20, -10);
            var holder = UIKit.Rect(content, "Holder", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -64));
            contentList = UIKit.VerticalList(holder, true, 5f);

            var detail = UIKit.Window(canvas.transform, "Detail", new Vector2(0.24f, 0.02f), new Vector2(0.98f, 0.3f));
            detailText = UIKit.Stretch(UIKit.Label(detail, "", 28, TextAnchor.UpperLeft), 20f);

            var footer = UIKit.Window(canvas.transform, "Footer", new Vector2(0.02f, 0.02f), new Vector2(0.22f, 0.12f));
            footerText = UIKit.Stretch(UIKit.Label(footer, "", 24, TextAnchor.MiddleLeft), 12f);
            canvas.gameObject.SetActive(false);
        }

        // ================================================================== abrir / cerrar

        public void Open()
        {
            fromTitle = false;
            gm.SetMode(GameMode.Menu);
            openedFrame = Time.frameCount;
            canvas.gameObject.SetActive(true);
            tabWindow.gameObject.SetActive(true);
            BuildTabs();
            ShowParty();
            UIKit.FocusFirst(tabList);
        }

        public void OpenSaveScreen()
        {
            Open();
            Enter(ShowSaveSlots);
        }

        public void OpenLoadScreen(bool fromTitle)
        {
            if (!fromTitle) { Open(); Enter(ShowLoadSlots); return; }
            this.fromTitle = true;
            gm.Title.Hide();
            openedFrame = Time.frameCount;
            canvas.gameObject.SetActive(true);
            tabWindow.gameObject.SetActive(false);
            backStack.Clear();
            ShowLoadSlots();
            UIKit.FocusFirst(contentList);
        }

        public void Close()
        {
            canvas.gameObject.SetActive(false);
            backStack.Clear();
            if (fromTitle) { fromTitle = false; gm.Title.Show(); return; }
            gm.ReturnToExploration();
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (State != null) footerText.text = T("menu.money", State.Inventory.Money) + "\n" + T("menu.playtime", FormatTime(State.PlayTimeSeconds));
            else footerText.text = "";

            if (Time.frameCount == openedFrame) return;
            var input = GameInput.Instance;
            if (input.Cancel.WasPressedThisFrame()) Back();
            else if (!fromTitle && input.Menu.WasPressedThisFrame()) Close();
        }

        private void Back()
        {
            if (backStack.Count > 0) { backStack.Pop()(); return; }
            Close();
        }

        /// <summary>Entra a una pantalla de contenido; Cancelar vuelve a las pestañas.</summary>
        private void Enter(Action show)
        {
            backStack.Clear();
            backStack.Push(() => UIKit.FocusFirst(tabList));
            show();
            UIKit.FocusFirst(contentList);
        }

        /// <summary>Pasa a una sub-pantalla recordando cómo volver a la actual.</summary>
        private void Go(Action current, Action next)
        {
            backStack.Push(() => { current(); UIKit.FocusFirst(contentList); });
            next();
            UIKit.FocusFirst(contentList);
        }

        private void BuildTabs()
        {
            UIKit.Clear(tabList);
            Tab("menu.party", ShowParty);
            Tab("menu.equip", ShowEquipCharacters);
            Tab("menu.techniques", ShowTechniqueCharacters);
            Tab("menu.items", ShowItems);
            Tab("menu.sanctuary", ShowSanctuary);
            Tab("menu.notebook", ShowNotebook);
            Tab("menu.save", ShowSaveSlots);
            Tab("menu.load", ShowLoadSlots);
            Tab("menu.options", ShowOptions);
            UIKit.Button(tabList, T("menu.close"), Close);
        }

        private void Tab(string key, Action show)
        {
            UIKit.Button(tabList, T(key), () => Enter(show), () => { show(); });
        }

        private void Page(string title)
        {
            UIKit.Clear(contentList);
            contentTitle.text = title;
            detailText.text = "";
        }

        private Button Item(string label, Action onClick, string detail = null, bool interactable = true)
        {
            return UIKit.Button(contentList, label, onClick, () => detailText.text = detail ?? "", interactable);
        }

        private void Info(string text)
        {
            UIKit.Label(contentList, text, 26, TextAnchor.MiddleLeft, UIKit.DimText);
        }

        // ================================================================== grupo

        private void ShowParty()
        {
            Page(T("menu.party"));
            foreach (var c in RecruitedOrdered())
            {
                var ch = c;
                bool active = State.Party.IsActive(ch.Id);
                bool locked = State.Party.Locked.Contains(ch.Id);
                string tag = locked ? " " + T("menu.locked") : active ? "" : " · " + T("menu.reserve");
                Item(CharacterLine(ch) + tag, () => { if (!locked) Go(ShowParty, () => ShowSwapTargets(ch.Id)); }, CharacterDetail(ch), true);
            }
            var pet = State.ActivePet;
            if (pet != null) Item("♦ " + CreatureLine(pet), () => Go(ShowParty, () => ShowCreature(pet.Uid)), CreatureDetail(pet));
        }

        private void ShowSwapTargets(string id)
        {
            Page(T("menu.swap_member"));
            bool active = State.Party.IsActive(id);
            foreach (var c in RecruitedOrdered())
            {
                var other = c;
                if (other.Id == id || State.Party.IsActive(other.Id) == active) continue;
                if (State.Party.Locked.Contains(other.Id)) continue;
                Item(CharacterLine(other), () =>
                {
                    bool ok = active ? State.Party.Swap(id, other.Id, State.Characters) : State.Party.Swap(other.Id, id, State.Characters);
                    if (ok) gm.NotifyPartyChanged();
                    Back();
                }, CharacterDetail(other));
            }
            if (contentList.childCount == 0) Info(T("menu.nothing"));
        }

        private IEnumerable<CharacterState> RecruitedOrdered()
        {
            var active = State.Party.Active.Select(id => State.Characters[id]);
            var reserve = State.Characters.Values.Where(c => c.Recruited && !State.Party.IsActive(c.Id)).OrderBy(c => c.Id);
            return active.Concat(reserve);
        }

        private string CharacterLine(CharacterState c)
        {
            var stats = StatCalculator.ForCharacter(Db, c);
            return T(Db.Character(c.Id).NameKey) + "   " + T("menu.level", c.Level) + "   " + T("menu.hp", c.Hp, stats.Get(StatType.MaxHp)) + "   " + T("menu.mp", c.Mp, stats.Get(StatType.MaxMp));
        }

        private string CharacterDetail(CharacterState c)
        {
            var def = Db.Character(c.Id);
            var stats = StatCalculator.ForCharacter(Db, c);
            var sb = new StringBuilder();
            sb.Append(T("element." + def.Element)).Append(" · ").AppendLine(T("menu.xp_next", ExperienceCurve.XpToNextLevel(c.Level, c.Xp)));
            sb.AppendLine(FormatStats(stats, false));
            sb.Append(T(def.BioKey));
            return sb.ToString();
        }

        // ================================================================== equipo

        private void ShowEquipCharacters()
        {
            Page(T("menu.equip"));
            foreach (var c in RecruitedOrdered())
            {
                var ch = c;
                Item(T(Db.Character(ch.Id).NameKey), () => Go(ShowEquipCharacters, () => ShowEquipSlots(ch.Id)), CharacterDetail(ch));
            }
        }

        private void ShowEquipSlots(string id)
        {
            var c = State.Characters[id];
            Page(T("menu.equip") + " · " + T(Db.Character(id).NameKey));
            foreach (var slot in EquipmentRules.HumanSlots)
            {
                var s = slot;
                string current;
                c.Equipment.TryGetValue(s, out current);
                string label = T("slot." + s) + ":  " + (current != null ? T(Db.Item(current).NameKey) : T("menu.empty_slot"));
                Item(label, () => Go(() => ShowEquipSlots(id), () => ShowEquipChoices(id, s)), current != null ? ItemDetail(Db.Item(current)) : null);
            }
            Info(FormatStats(StatCalculator.ForCharacter(Db, c), false));
        }

        private void ShowEquipChoices(string id, EquipSlot slot)
        {
            var def = Db.Character(id);
            Page(T("slot." + slot) + " · " + T(def.NameKey));
            if (slot != EquipSlot.Arma)
                Item(T("menu.unequip"), () => { EquipmentRules.EquipCharacter(Db, State, id, slot, null); Back(); });
            foreach (var pair in State.Inventory.Items.OrderBy(p => p.Key))
            {
                ItemDef item;
                if (!Db.Items.TryGetValue(pair.Key, out item) || !EquipmentRules.CanEquip(item, slot, def)) continue;
                var it = item;
                Item(T(it.NameKey) + "  ×" + pair.Value, () => { EquipmentRules.EquipCharacter(Db, State, id, slot, it.Id); Back(); }, ItemDetail(it));
            }
            if (contentList.childCount <= 1) Info(T("menu.nothing"));
        }

        // ================================================================== técnicas

        private void ShowTechniqueCharacters()
        {
            Page(T("menu.techniques"));
            foreach (var c in RecruitedOrdered())
            {
                var ch = c;
                Item(T(Db.Character(ch.Id).NameKey), () => Go(ShowTechniqueCharacters, () => ShowTechniques(ch.Id)), CharacterDetail(ch));
            }
        }

        private void ShowTechniques(string id)
        {
            var c = State.Characters[id];
            var def = Db.Character(id);
            Page(T("menu.techniques") + " · " + T(def.NameKey));
            foreach (var entry in def.Learnset)
            {
                var tech = Db.Technique(entry.TechniqueId);
                bool learned = c.LearnedTechniques.Contains(tech.Id);
                string label = (learned ? T(tech.NameKey) : "???") + "   " + (learned ? T("tech.cost", tech.MpCost) : T("menu.learns_at", entry.Level));
                Item(label, null, learned ? TechniqueDetail(tech) : T("menu.learns_at", entry.Level), true);
            }
            foreach (var combo in Db.Combos.Values.Where(cb => cb.Participants.Any(p => p.Actor == id)))
            {
                string partners = string.Join(" + ", combo.Participants.Select(p => p.IsPet ? T("sanctuary.active") : T(Db.Character(p.Actor).NameKey)));
                Item("+ " + T(combo.NameKey), null, partners + "\n" + T(combo.DescKey));
            }
        }

        private string TechniqueDetail(TechniqueDef tech)
        {
            return T("kind." + tech.Kind) + " · " + T("element." + tech.Element) + " · " + T("tech.cost", tech.MpCost) + "\n" + T(tech.DescKey);
        }

        // ================================================================== objetos

        private void ShowItems()
        {
            Page(T("menu.items"));
            var ordered = State.Inventory.Items.Keys.Where(Db.Items.ContainsKey).Select(Db.Item).OrderBy(i => i.Category).ThenBy(i => i.Id);
            foreach (var item in ordered)
            {
                var it = item;
                bool usable = it.Category == ItemCategory.Consumible && it.UsableInField;
                Item(T(it.NameKey) + "  ×" + State.Inventory.Count(it.Id), () => { if (usable) Go(ShowItems, () => ShowItemTargets(it)); }, ItemDetail(it));
            }
            if (contentList.childCount == 0) Info(T("menu.nothing"));
        }

        private void ShowItemTargets(ItemDef item)
        {
            Page(T("menu.on_whom") + " · " + T(item.NameKey));
            if (item.Effect.AllAllies)
            {
                Item(T("menu.use"), () => UseItem(item, null, null));
                return;
            }
            foreach (var id in State.Party.Active)
            {
                var c = State.Characters[id];
                Item(CharacterLine(c), () => UseItem(item, id, null));
            }
            var pet = State.ActivePet;
            if (pet != null) Item("♦ " + CreatureLine(pet), () => UseItem(item, null, pet.Uid));
        }

        private void UseItem(ItemDef item, string characterId, string creatureUid)
        {
            bool used = FieldItemService.Use(Db, State, item.Id, characterId, creatureUid);
            if (!used) { gm.Hud.Toast(T("menu.nothing")); return; }
            if (State.Inventory.Count(item.Id) > 0) ShowItemTargets(item);
            else Back();
            UIKit.FocusFirst(contentList);
        }

        private string ItemDetail(ItemDef item)
        {
            var sb = new StringBuilder(T(item.DescKey));
            if (item.Stats != null && item.Stats.Count > 0) sb.Append("\n").Append(FormatStats(item.Stats, true));
            if (item.Element != Element.Neutro) sb.Append("\n").Append(T("element." + item.Element));
            return sb.ToString();
        }

        // ================================================================== santuario

        private void ShowSanctuary()
        {
            Page(T("menu.sanctuary") + " · " + T("sanctuary.capacity", State.Sanctuary.Creatures.Count, State.Sanctuary.Capacity));
            if (State.Sanctuary.Creatures.Count == 0) { Info(T("sanctuary.no_creatures")); return; }
            foreach (var cr in State.Sanctuary.Creatures)
            {
                var c = cr;
                bool active = c.Uid == State.Party.ActivePetUid;
                bool canEvolve = EvolutionSystem.Available(Db, State, c, State.Location.Era).Count > 0;
                Item((active ? "♦ " : "   ") + CreatureLine(c) + (canEvolve ? "   ★" : ""), () => Go(ShowSanctuary, () => ShowCreature(c.Uid)), CreatureDetail(c));
            }
        }

        private void ShowCreature(string uid)
        {
            var c = State.Sanctuary.Find(uid);
            if (c == null) { Back(); return; }
            var species = Db.Species(c.SpeciesId);
            Page(T(species.NameKey));
            string detail = CreatureDetail(c);
            Action refresh = () => { ShowCreature(uid); UIKit.FocusFirst(contentList); };

            if (State.Party.ActivePetUid != uid)
                Item(T("sanctuary.set_active"), () => { State.Party.ActivePetUid = uid; gm.NotifyPartyChanged(); refresh(); }, detail);

            foreach (var evo in EvolutionSystem.Available(Db, State, c, State.Location.Era))
            {
                var e = evo;
                Item(T("sanctuary.evolve") + " → " + T(Db.Species(e.ToSpeciesId).NameKey), () =>
                {
                    string before = T(species.NameKey);
                    EvolutionSystem.Evolve(Db, State, c, e, State.Location.Era);
                    gm.Hud.Toast(T("sanctuary.evolved", before, T(Db.Species(c.SpeciesId).NameKey)));
                    gm.NotifyPartyChanged();
                    refresh();
                }, T("sanctuary.can_evolve"));
            }

            Item(T("sanctuary.offering"), () => Go(refresh, () => ShowOfferings(uid)), detail);
            Item(T("sanctuary.stance", T("stance." + c.Stance)), () =>
            {
                c.Stance = (PetStance)(((int)c.Stance + 1) % Enum.GetValues(typeof(PetStance)).Length);
                refresh();
            }, detail);
            foreach (var slot in EquipmentRules.PetSlots)
            {
                var s = slot;
                string current;
                c.Equipment.TryGetValue(s, out current);
                Item(T("slot." + s) + ":  " + (current != null ? T(Db.Item(current).NameKey) : T("menu.empty_slot")), () => Go(refresh, () => ShowPetEquipChoices(uid, s)), current != null ? ItemDetail(Db.Item(current)) : detail);
            }
            if (State.Party.ActivePetUid != uid)
                Item(T("sanctuary.release"), () => Go(refresh, () => ConfirmRelease(uid)), detail);
        }

        private void ShowOfferings(string uid)
        {
            var c = State.Sanctuary.Find(uid);
            Page(T("sanctuary.offering"));
            foreach (var item in State.Inventory.Items.Keys.Where(Db.Items.ContainsKey).Select(Db.Item).Where(i => i.Category == ItemCategory.Ofrenda).ToList())
            {
                var it = item;
                Item(T(it.NameKey) + "  ×" + State.Inventory.Count(it.Id), () =>
                {
                    int gain = BondingSystem.GiveOffering(Db, State, c, it.Id);
                    gm.Hud.Toast(T("battle.affinity", T(Db.Species(c.SpeciesId).NameKey), gain));
                    Back();
                }, ItemDetail(it));
            }
            if (contentList.childCount == 0) Info(T("menu.nothing"));
        }

        private void ShowPetEquipChoices(string uid, EquipSlot slot)
        {
            var c = State.Sanctuary.Find(uid);
            Page(T("slot." + slot));
            Item(T("menu.unequip"), () => { EquipmentRules.EquipCreature(Db, State, c, slot, null); Back(); });
            foreach (var pair in State.Inventory.Items.OrderBy(p => p.Key))
            {
                ItemDef item;
                if (!Db.Items.TryGetValue(pair.Key, out item) || !EquipmentRules.CanEquipPet(item, slot)) continue;
                var it = item;
                Item(T(it.NameKey) + "  ×" + pair.Value, () => { EquipmentRules.EquipCreature(Db, State, c, slot, it.Id); Back(); }, ItemDetail(it));
            }
        }

        private void ConfirmRelease(string uid)
        {
            var c = State.Sanctuary.Find(uid);
            Page(T("sanctuary.release"));
            Info(T("sanctuary.release_confirm", T(Db.Species(c.SpeciesId).NameKey)));
            Item(T("sanctuary.release"), () =>
            {
                State.Sanctuary.Release(uid, State.Party);
                backStack.Clear();
                Enter(ShowSanctuary);
            });
            Item(T("menu.back"), Back);
        }

        private string CreatureLine(CreatureInstance c)
        {
            var species = Db.Species(c.SpeciesId);
            var stats = StatCalculator.ForCreature(Db, c);
            return T(species.NameKey) + "   " + T("menu.level", c.Level) + "   " + T("element." + species.Element) + "   " + T("menu.hp", c.Hp, stats.Get(StatType.MaxHp));
        }

        private string CreatureDetail(CreatureInstance c)
        {
            var species = Db.Species(c.SpeciesId);
            var sb = new StringBuilder();
            sb.Append(T("sanctuary.affinity", c.Affinity)).Append(" · ").Append(T("temperament." + species.Temperament)).Append(" · ").AppendLine(T("sanctuary.stance", T("stance." + c.Stance)));
            sb.AppendLine(FormatStats(StatCalculator.ForCreature(Db, c), false));
            sb.Append(T(species.DescKey));
            return sb.ToString();
        }

        // ================================================================== cuaderno

        private void ShowNotebook()
        {
            Page(T("notebook.title"));
            var scene = StoryDirector.CurrentScene(Db, State);
            var objective = StoryDirector.CurrentObjective(Db, State);
            if (scene != null) Info(T(scene.TitleKey));
            if (objective != null) Info(T("notebook.objective", T(scene.ObjectiveKey(objective))));
            Info(T("notebook.scenes", State.Story.CompletedScenes.Count == 0 ? "—" : string.Join(", ", State.Story.CompletedScenes)));

            if (State.Story.Cierres.Count > 0)
            {
                Info(T("notebook.cierres"));
                foreach (var record in State.Story.Cierres)
                {
                    var def = Db.Cierres[record.Id];
                    string detail = T("cierre.before") + ": " + T(def.BeforeKey) + "\n" + T("cierre.now") + ": " + T(def.NowKey(record.Value)) + "\n" + T(def.PersonKey(record.Value));
                    Item(T(def.CauseKey), null, detail);
                }
            }

            if (State.Story.Absents.Count > 0)
            {
                Info(T("notebook.absents"));
                foreach (var id in State.Story.Absents) Item(T("absent." + id + ".name"), null, T("absent." + id + ".desc"));
            }

            Info(T("notebook.flags"));
            foreach (var flag in Db.Flags.Values)
            {
                int value = State.Flags.Get(flag.Id);
                if (value == flag.Initial) continue;
                Item(T(flag.DescKey) + "   " + value, null, flag.Id);
            }
        }

        // ================================================================== guardar / cargar

        private void ShowSaveSlots()
        {
            Page(T("menu.save"));
            for (int slot = 1; slot <= SaveService.SlotCount; slot++)
            {
                int s = slot;
                Item(SlotLabel(s), () =>
                {
                    gm.SaveGame(s);
                    gm.Hud.Toast(T("menu.saved", s));
                    ShowSaveSlots();
                    UIKit.FocusFirst(contentList);
                });
            }
        }

        private void ShowLoadSlots()
        {
            Page(T("menu.load"));
            for (int slot = 1; slot <= SaveService.SlotCount; slot++)
            {
                int s = slot;
                bool exists = File.Exists(SaveService.SlotPath(gm.SaveDirectory, s));
                Item(SlotLabel(s), () =>
                {
                    if (!gm.LoadGame(s)) return;
                    fromTitle = false;
                    canvas.gameObject.SetActive(false);
                    backStack.Clear();
                    gm.Hud.Toast(T("menu.loaded"));
                }, null, exists);
            }
        }

        private string SlotLabel(int slot)
        {
            SaveSlotInfo info = null;
            try { info = SaveService.Peek(gm.SaveDirectory, slot); }
            catch (Exception e) { Debug.LogWarning("Ranura " + slot + " ilegible: " + e.Message); }
            if (info == null) return T("menu.slot_empty", slot);
            return T("menu.slot_info", slot, T("era." + info.Era), info.LeaderLevel, FormatTime(info.PlayTimeSeconds));
        }

        // ================================================================== opciones

        private void ShowOptions()
        {
            Page(T("menu.options"));
            Item(T("options.battle_mode", T("battlemode." + State.BattleMode)), () =>
            {
                State.BattleMode = State.BattleMode == BattleMode.Activa ? BattleMode.Espera : BattleMode.Activa;
                ShowOptions();
                UIKit.FocusFirst(contentList);
            });
            Item(T("options.battle_speed", State.BattleSpeed.ToString("0.##")), () =>
            {
                float[] speeds = { 0.75f, 1f, 1.5f, 2f };
                int index = Array.FindIndex(speeds, s => Mathf.Approximately(s, State.BattleSpeed));
                State.BattleSpeed = speeds[(index + 1) % speeds.Length];
                ShowOptions();
                UIKit.FocusFirst(contentList);
            });
            Item(T("options.language", State.Language.ToUpperInvariant()), () =>
            {
                string dir = Path.Combine(Application.streamingAssetsPath, "Localization");
                var languages = Directory.GetFiles(dir, "*.json").Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).ToList();
                int index = languages.IndexOf(State.Language);
                State.Language = languages[(index + 1) % languages.Count];
                gm.Loc.SetLanguage(dir, State.Language);
                BuildTabs();
                ShowOptions();
                UIKit.FocusFirst(contentList);
            });
        }

        // ================================================================== utilidades

        private static string FormatStats(StatBlock stats, bool signed)
        {
            var parts = new List<string>();
            foreach (var stat in StatMath.All)
            {
                int v = stats.Get(stat);
                if (signed && v == 0) continue;
                parts.Add(T("stat." + stat) + " " + (signed && v > 0 ? "+" : "") + v);
            }
            return string.Join("   ", parts);
        }

        private static string FormatTime(double seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            return ((int)t.TotalHours).ToString("00") + ":" + t.Minutes.ToString("00");
        }
    }
}
