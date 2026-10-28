using System;
using System.Collections.Generic;
using System.IO;
using TimeTraveler.Core;
using UnityEngine;

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
        GameOver
    }

    /// <summary>
    /// Punto central del juego en Unity: carga datos y textos, mantiene la partida (GameState),
    /// coordina los modos (exploración, diálogo, menú, combate, resonancia) y ejecuta las acciones
    /// que piden los diálogos. Se crea solo al pulsar Play en cualquier escena.
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

        /// <summary>Se dispara cuando cambia el grupo o la mascota activa (para reconstruir los modelos).</summary>
        public event Action PartyChanged;

        public DialogueUI Dialogue { get; private set; }
        public PauseMenu Pause { get; private set; }
        public BattleController Battle { get; private set; }
        public ResonanceMinigame Resonance { get; private set; }
        public HudUI Hud { get; private set; }
        public TitleScreen Title { get; private set; }

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
            var errors = Db.Validate();
            foreach (var e in errors) Debug.LogError("[Datos] " + e);
            Loc = Localization.LoadFromDirectory(LocalizationDirectory, Localization.DefaultLanguage);

            Hud = gameObject.AddComponent<HudUI>();
            Dialogue = gameObject.AddComponent<DialogueUI>();
            Pause = gameObject.AddComponent<PauseMenu>();
            Battle = gameObject.AddComponent<BattleController>();
            Resonance = gameObject.AddComponent<ResonanceMinigame>();
            Title = gameObject.AddComponent<TitleScreen>();
        }

        private void Start()
        {
            PrototypeWorld.EnsureBuilt();
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

        // ------------------------------------------------------------ partida

        public void NewGame()
        {
            State = GameState.NewGame(Db);
            PrototypeWorld.Instance.ResetDynamic();
            PartyVisuals.Instance.Rebuild();
            PartyVisuals.Instance.PlaceLeader(PrototypeWorld.SpawnPoint, Quaternion.identity);
            CameraRig.Instance.SnapToTarget();
            SetMode(GameMode.Exploration);
            StartDialogue("e01_aula", () => Hud.Toast(T("proto.welcome"), 6f));
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
            PrototypeWorld.Instance.ApplyClearedEncounters(State);
            PartyVisuals.Instance.Rebuild();
            PartyVisuals.Instance.PlaceLeader(new Vector3(State.Location.X, State.Location.Y, State.Location.Z));
            CameraRig.Instance.SnapToTarget();
            SetMode(GameMode.Exploration);
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
            }
            SaveService.Save(State, SaveDirectory, slot);
            LastSaveSlot = slot;
        }

        public void NotifyPartyChanged()
        {
            PartyChanged?.Invoke();
        }

        // ------------------------------------------------------------ diálogos y acciones

        public void StartDialogue(string dialogueId, Action onFinished = null)
        {
            var deferred = new List<string>();
            SetMode(GameMode.Dialogue);
            Dialogue.Play(dialogueId, action => HandleDialogueAction(action, deferred), () =>
            {
                SetMode(GameMode.Exploration);
                foreach (var action in deferred) RunDeferredAction(action);
                onFinished?.Invoke();
            });
        }

        /// <summary>Acciones inmediatas de diálogo. Combates y diálogos encadenados esperan a que termine el actual.</summary>
        private void HandleDialogueAction(string action, List<string> deferred)
        {
            string[] parts = action.Split(':');
            switch (parts[0])
            {
                case "give":
                    int qty = parts.Length > 2 ? int.Parse(parts[2]) : 1;
                    State.Inventory.Add(parts[1], qty);
                    Hud.Toast(T("interact.chest_got", Loc.Get(Db.Item(parts[1]).NameKey), qty));
                    break;
                case "money":
                    State.Inventory.Money = Math.Max(0, State.Inventory.Money + int.Parse(parts[1]));
                    break;
                case "recruit":
                    int level = parts.Length > 2 ? int.Parse(parts[2]) : 1;
                    State.Recruit(Db, parts[1], level);
                    NotifyPartyChanged();
                    break;
                case "scene":
                    if (!State.Story.CompletedScenes.Contains(State.Story.CurrentScene))
                        State.Story.CompletedScenes.Add(State.Story.CurrentScene);
                    State.Story.CurrentScene = parts[1];
                    break;
                case "battle":
                case "dialogue":
                    deferred.Add(action);
                    break;
                default:
                    Debug.LogWarning("Acción de diálogo desconocida: " + action);
                    break;
            }
        }

        private void RunDeferredAction(string action)
        {
            string[] parts = action.Split(':');
            if (parts[0] == "battle") StartBattle(parts[1], PartyVisuals.Instance.Leader.position + PartyVisuals.Instance.Leader.forward * 4f, null);
            else if (parts[0] == "dialogue") StartDialogue(parts[1]);
        }

        // ------------------------------------------------------------ combate

        public void StartBattle(string encounterId, Vector3 center, MapEnemy source)
        {
            if (Mode == GameMode.Battle) return;
            SetMode(GameMode.Battle);
            Battle.Begin(encounterId, center, source, outcome =>
            {
                if (outcome == BattleOutcome.Derrota)
                {
                    SetMode(GameMode.GameOver);
                    Title.ShowGameOver();
                    return;
                }
                if (outcome == BattleOutcome.Victoria && source != null)
                {
                    if (!State.Story.ClearedEncounters.Contains(source.UniqueId)) State.Story.ClearedEncounters.Add(source.UniqueId);
                    Destroy(source.gameObject);
                }
                else if (source != null)
                {
                    source.StartCooldown();
                }
                SetMode(GameMode.Exploration);
                NotifyPartyChanged();
            });
        }
    }
}
