using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TimeTraveler.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TimeTraveler
{
    public enum GameMode
    {
        Title,
        Exploration,
        Dialogue,
        Menu,
        Battle,
        Resonance,
        /// <summary>Puzzles, ficha de Cierre, viajes, fin de acto.</summary>
        Story,
        GameOver
    }

    /// <summary>
    /// Punto central del juego en Unity: carga datos y textos, mantiene la partida (GameState),
    /// coordina los modos y ejecuta la historia. Las acciones de los diálogos que sólo cambian la
    /// partida las resuelve Core (StoryActions); las visuales (viajar, combatir, puzzles, Cierres,
    /// fin de acto) se encolan y se ejecutan en orden al terminar cada diálogo.
    /// Se crea solo al pulsar Play en cualquier escena.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameDatabase Db { get; private set; }
        public Localization Loc { get; private set; }
        public GameState State { get; private set; }
        public GameMode Mode { get; private set; }
        /// <summary>Frame del último cambio de modo: la entrada de ese frame ya se consumió.</summary>
        public int ModeChangedFrame { get; private set; }

        /// <summary>True si el modo es el indicado y no acaba de cambiar en este frame.</summary>
        public bool IsSettledIn(GameMode mode) { return Mode == mode && Time.frameCount > ModeChangedFrame; }
        public int LastSaveSlot { get; private set; } = 1;

        public string SaveDirectory { get { return Path.Combine(Application.persistentDataPath, "saves"); } }
        private string DataDirectory { get { return Path.Combine(Application.streamingAssetsPath, "Data"); } }
        private string LocalizationDirectory { get { return Path.Combine(Application.streamingAssetsPath, "Localization"); } }

        /// <summary>Cambió el grupo o la mascota activa (reconstruir modelos).</summary>
        public event Action PartyChanged;
        /// <summary>Cambió algo de la historia (objetivos, objetos visibles en el mapa).</summary>
        public event Action StoryChanged;

        public DialogueUI Dialogue { get; private set; }
        public PauseMenu Pause { get; private set; }
        public BattleController Battle { get; private set; }
        public ResonanceMinigame Resonance { get; private set; }
        public HudUI Hud { get; private set; }
        public TitleScreen Title { get; private set; }
        public StoryUI Story { get; private set; }
        public TapePuzzleUI TapePuzzle { get; private set; }
        public SignPuzzleUI SignPuzzle { get; private set; }

        /// <summary>Acciones diferidas pendientes (se ejecutan una tras otra).</summary>
        private readonly List<string> pending = new List<string>();
        private Image fade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("GameManager");
            DontDestroyOnLoad(go);
            go.AddComponent<GameInput>();
            go.AddComponent<GameManager>();
        }

        public static string T(string key) { return Instance != null ? Instance.Loc.Get(key) : key; }
        public static string T(string key, params object[] args) { return Instance != null ? Instance.Loc.Format(key, args) : key; }

        private void Awake()
        {
            Instance = this;
#if !ENABLE_INPUT_SYSTEM
            Debug.LogError("Las Horas que Faltan: activa el Input System (Edit > Project Settings > Player > Active Input Handling = Input System Package (New) o Both).");
#endif
            Db = GameDatabase.LoadFromDirectory(DataDirectory);
            foreach (var e in Db.Validate()) Debug.LogError("[Datos] " + e);
            Loc = Localization.LoadFromDirectory(LocalizationDirectory, Localization.DefaultLanguage);

            Hud = gameObject.AddComponent<HudUI>();
            Dialogue = gameObject.AddComponent<DialogueUI>();
            Pause = gameObject.AddComponent<PauseMenu>();
            Battle = gameObject.AddComponent<BattleController>();
            Resonance = gameObject.AddComponent<ResonanceMinigame>();
            Title = gameObject.AddComponent<TitleScreen>();
            Story = gameObject.AddComponent<StoryUI>();
            TapePuzzle = gameObject.AddComponent<TapePuzzleUI>();
            SignPuzzle = gameObject.AddComponent<SignPuzzleUI>();

            var fadeCanvas = UIKit.CreateCanvas("FadeCanvas", 100);
            fade = UIKit.Rect(fadeCanvas.transform, "Fade", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            fade.color = new Color(0, 0, 0, 0);
            fade.raycastTarget = false;
        }

        private void Start()
        {
            WorldBuilder.EnsureCreated();
            SetMode(GameMode.Title);
            Title.Show();
        }

        private void Update()
        {
            if (State != null && Mode != GameMode.Title && Mode != GameMode.GameOver)
                State.PlayTimeSeconds += Time.unscaledDeltaTime;

            if (IsSettledIn(GameMode.Exploration) && GameInput.Instance.Menu.WasPressedThisFrame())
                Pause.Open();
        }

        public void SetMode(GameMode mode)
        {
            Mode = mode;
            ModeChangedFrame = Time.frameCount;
            Hud.SetVisible(mode == GameMode.Exploration);
        }

        public void NotifyPartyChanged() { PartyChanged?.Invoke(); }

        public void NotifyStoryChanged()
        {
            if (WorldBuilder.Instance != null) WorldBuilder.Instance.Refresh();
            StoryChanged?.Invoke();
        }

        // ================================================================== partida

        public void NewGame() { BeginGame(GameState.NewGame(Db)); }
        public void NewTestGame() { BeginGame(GameState.NewTestGame(Db)); }

        private void BeginGame(GameState state)
        {
            State = state;
            pending.Clear();
            EnterMap(State.Location.MapId, State.Location.Spawn, null);
            ReturnToExploration();
            Hud.Toast(T("proto.welcome"), 6f);
        }

        public bool LoadGame(int slot)
        {
            GameState loaded;
            try
            {
                loaded = SaveService.Load(SaveDirectory, slot);
            }
            catch (Exception e)
            {
                Debug.LogError("No se pudo cargar la partida: " + e);
                return false;
            }
            if (loaded == null) return false;
            State = loaded;
            LastSaveSlot = slot;
            pending.Clear();
            Vector3? exact = State.Location.HasPosition ? new Vector3(State.Location.X, State.Location.Y, State.Location.Z) : (Vector3?)null;
            EnterMap(State.Location.MapId, State.Location.Spawn, exact);
            ReturnToExploration();
            return true;
        }

        public void SaveGame(int slot)
        {
            var leader = PartyVisuals.Instance.Leader;
            if (leader != null)
            {
                State.Location.X = leader.position.x;
                State.Location.Y = leader.position.y;
                State.Location.Z = leader.position.z;
                State.Location.HasPosition = true;
            }
            SaveService.Save(State, SaveDirectory, slot);
            LastSaveSlot = slot;
        }

        // ================================================================== mapas y viajes

        private void EnterMap(string mapId, string spawn, Vector3? exactPosition)
        {
            var world = WorldBuilder.Instance;
            world.Load(mapId);
            State.Location.MapId = mapId;
            State.Location.Era = world.Current.Era;
            State.Location.Spawn = spawn ?? "default";
            if (!State.Story.VisitedMaps.Contains(mapId)) State.Story.VisitedMaps.Add(mapId);

            Quaternion rotation;
            Vector3 position = world.SpawnPosition(spawn, out rotation);
            PartyVisuals.Instance.Rebuild();
            PartyVisuals.Instance.PlaceLeader(exactPosition ?? position, rotation);
            CameraRig.Instance.SnapToTarget();
        }

        /// <summary>Viaja a otro mapa con fundido a negro. onArrived null = volver a explorar.</summary>
        public void TravelTo(string mapId, string spawn, Action onArrived)
        {
            StartCoroutine(TravelRoutine(mapId, spawn, onArrived));
        }

        private IEnumerator TravelRoutine(string mapId, string spawn, Action onArrived)
        {
            SetMode(GameMode.Story);
            yield return Fade(1f);
            EnterMap(mapId, spawn, null);
            yield return Fade(0f);
            if (onArrived != null) onArrived();
            else ReturnToExploration();
        }

        private IEnumerator Fade(float target)
        {
            float start = fade.color.a;
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                fade.color = new Color(0, 0, 0, Mathf.Lerp(start, target, t / 0.35f));
                yield return null;
            }
            fade.color = new Color(0, 0, 0, target);
        }

        // ================================================================== historia

        /// <summary>
        /// Vuelve a explorar: refresca el mapa y, si la escena cumplió sus objetivos, reproduce su
        /// diálogo automático. Es el punto al que regresan diálogos, combates, menús y puzzles.
        /// </summary>
        public void ReturnToExploration()
        {
            if (pending.Count > 0) { RunNextPending(); return; }
            SetMode(GameMode.Exploration);
            NotifyStoryChanged();
            string auto = State != null ? StoryDirector.TakePendingDialogue(Db, State) : null;
            if (auto != null) StartDialogue(auto);
        }

        public void StartDialogue(string dialogueId, Action onFinished = null)
        {
            var deferred = new List<string>();
            SetMode(GameMode.Dialogue);
            Dialogue.Play(dialogueId, action => HandleDialogueAction(action, deferred), () =>
            {
                // Las acciones del diálogo nuevo van antes que las que ya esperaban.
                pending.InsertRange(0, deferred);
                onFinished?.Invoke();
                ReturnToExploration();
            });
        }

        private void HandleDialogueAction(string action, List<string> deferred)
        {
            if (StoryActions.IsDeferred(action)) { deferred.Add(action); return; }
            if (!StoryActions.Execute(Db, State, action)) { Debug.LogWarning("Acción de diálogo desconocida: " + action); return; }

            string[] p = action.Split(':');
            if (p[0] == "give")
                Hud.Toast(T("interact.chest_got", T(Db.Item(p[1]).NameKey), p.Length > 2 ? p[2] : "1"));
            else if (p[0] == "recruit")
                NotifyPartyChanged();
            else if (p[0] == "scene")
                Hud.ShowSceneTitle();
        }

        private void RunNextPending()
        {
            string action = pending[0];
            pending.RemoveAt(0);
            string[] p = action.Split(':');
            string arg = p.Length > 1 ? p[1] : null;
            switch (p[0])
            {
                case "travel":
                    TravelTo(arg, p.Length > 2 ? p[2] : "default", ReturnToExploration);
                    break;
                case "battle":
                    var leader = PartyVisuals.Instance.Leader;
                    StartBattle(arg, leader.position + leader.forward * 4f, null, ReturnToExploration);
                    break;
                case "dialogue":
                    StartDialogue(arg);
                    break;
                case "puzzle":
                    SetMode(GameMode.Story);
                    Action<bool> done = solved =>
                    {
                        if (!solved) { ReturnToExploration(); return; }
                        Puzzles.MarkSolved(Db, State, arg);
                        StartDialogue(Puzzles.OnSolved[arg].Dialogue);
                    };
                    if (arg == "cinta") TapePuzzle.Open(done);
                    else SignPuzzle.Open(done);
                    break;
                case "cierre":
                    SetMode(GameMode.Story);
                    Story.ShowCierre(arg);
                    break;
                case "endact":
                    SetMode(GameMode.Story);
                    State.Story.ActsCompleted = Math.Max(State.Story.ActsCompleted, int.Parse(arg));
                    Story.ShowActEnd(int.Parse(arg), ReturnToExploration);
                    break;
                default:
                    Debug.LogWarning("Acción diferida desconocida: " + action);
                    ReturnToExploration();
                    break;
            }
        }

        /// <summary>Llamado por la ficha de Cierre al confirmar.</summary>
        public void ConfirmCierre(string cierreId)
        {
            var def = Db.Cierres[cierreId];
            CierreService.Confirm(Db, State, cierreId);
            Hud.ShowSceneTitle();
            if (def.OnConfirmDialogue != null) StartDialogue(def.OnConfirmDialogue);
            else ReturnToExploration();
        }

        /// <summary>Llamado por la ficha de Cierre al reconsiderar: deshace y repite la decisión.</summary>
        public void ReconsiderCierre(string cierreId)
        {
            CierreService.Reconsider(State);
            NotifyPartyChanged();
            StartDialogue(Db.Cierres[cierreId].DecisionDialogue);
        }

        // ================================================================== combate

        public void StartBattle(string encounterId, Vector3 center, MapEnemy source, Action onWon)
        {
            if (Mode == GameMode.Battle) return;
            SetMode(GameMode.Battle);
            Battle.Begin(encounterId, center, source, outcome =>
            {
                if (outcome == BattleOutcome.Derrota)
                {
                    pending.Clear();
                    SetMode(GameMode.GameOver);
                    Title.ShowGameOver();
                    return;
                }
                if (outcome == BattleOutcome.Victoria && source != null)
                {
                    if (!source.Respawns && !State.Story.ClearedEncounters.Contains(source.UniqueId))
                        State.Story.ClearedEncounters.Add(source.UniqueId);
                    if (source.Respawns) source.gameObject.SetActive(false);
                    else Destroy(source.gameObject);
                }
                else if (source != null)
                {
                    source.StartCooldown();
                }
                NotifyPartyChanged();
                if (outcome == BattleOutcome.Victoria && onWon != null) onWon();
                else ReturnToExploration();
            });
        }
    }
}
