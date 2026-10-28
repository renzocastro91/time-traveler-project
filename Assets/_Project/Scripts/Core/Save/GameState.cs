using System;
using System.Collections.Generic;
using System.IO;

namespace TimeTraveler.Core
{
    public sealed class StoryState
    {
        /// <summary>Escena del guion en curso (E01…E42).</summary>
        public string CurrentScene = "E01";
        public List<string> CompletedScenes = new List<string>();
        public List<string> EndingsSeen = new List<string>();
        /// <summary>Objetos de mapa ya resueltos por su id único: enemigos y jefes vencidos, cofres abiertos, criaturas vinculadas.</summary>
        public List<string> ClearedEncounters = new List<string>();
        /// <summary>Criaturas salvajes que rechazaron el vínculo recientemente, con tiempo de espera.</summary>
        public Dictionary<string, double> BondCooldowns = new Dictionary<string, double>();
    }

    public sealed class LocationState
    {
        public string Era = "2000";
        public string MapId = "prototipo_acto1";
        public float X;
        public float Y;
        public float Z;
    }

    /// <summary>Todo lo que se guarda en una partida.</summary>
    public sealed class GameState
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public DateTime SavedAt;
        public double PlayTimeSeconds;
        public string Language = Localization.DefaultLanguage;
        public BattleMode BattleMode = BattleMode.Espera;
        public float BattleSpeed = 1f;
        public int NextCreatureNumber = 1;

        public Dictionary<string, CharacterState> Characters = new Dictionary<string, CharacterState>();
        public PartyState Party = new PartyState();
        public Inventory Inventory = new Inventory();
        public Sanctuary Sanctuary = new Sanctuary();
        public FlagStore Flags = new FlagStore();
        public StoryState Story = new StoryState();
        public LocationState Location = new LocationState();

        public string NewCreatureUid()
        {
            return "cr" + (NextCreatureNumber++).ToString("D4");
        }

        public CreatureInstance ActivePet { get { return Sanctuary.Find(Party.ActivePetUid); } }

        /// <summary>
        /// Resuelve identificadores usados en condiciones: banderas narrativas, "item.id" (cantidad en
        /// inventario), "recruited.id" (1 si está reclutado), "scene.E05" (1 si se completó) y "money".
        /// </summary>
        public int ResolveVariable(string name)
        {
            if (name == "money") return Inventory.Money;
            if (name.StartsWith("item.", StringComparison.Ordinal)) return Inventory.Count(name.Substring(5));
            if (name.StartsWith("recruited.", StringComparison.Ordinal))
            {
                CharacterState c;
                return Characters.TryGetValue(name.Substring(10), out c) && c.Recruited ? 1 : 0;
            }
            if (name.StartsWith("scene.", StringComparison.Ordinal)) return Story.CompletedScenes.Contains(name.Substring(6)) ? 1 : 0;
            return Flags.Get(name);
        }

        public void Recruit(GameDatabase db, string characterId, int level)
        {
            CharacterState state;
            if (!Characters.TryGetValue(characterId, out state))
            {
                state = ProgressionService.CreateCharacter(db, characterId, level);
                Characters[characterId] = state;
            }
            state.Recruited = true;
            Party.AddToActive(characterId);
        }

        /// <summary>Partida nueva al comienzo de E01: Violeta y Ángel en el año 2000.</summary>
        public static GameState NewGame(GameDatabase db)
        {
            var state = new GameState();
            foreach (var flag in db.Flags.Values) state.Flags.Values[flag.Id] = flag.Initial;
            state.Recruit(db, "violeta", 1);
            state.Recruit(db, "angel", 1);
            state.Party.Locked.Add("violeta");
            state.Party.Locked.Add("angel");
            state.Inventory.Money = 150;
            state.Inventory.Add("venda", 5);
            state.Inventory.Add("agua_manantial", 2);
            state.Inventory.Add("cinta_2026");
            return state;
        }
    }

    public sealed class SaveSlotInfo
    {
        public int Slot;
        public DateTime SavedAt;
        public double PlayTimeSeconds;
        public string Scene;
        public string Era;
        public int LeaderLevel;
    }

    public static class SaveService
    {
        public const int SlotCount = 3;

        public static string SlotPath(string directory, int slot)
        {
            return Path.Combine(directory, "partida" + slot + ".json");
        }

        public static void Save(GameState state, string directory, int slot)
        {
            Directory.CreateDirectory(directory);
            state.SavedAt = DateTime.Now;
            state.Version = GameState.CurrentVersion;
            string path = SlotPath(directory, slot);
            // Escritura atómica: primero a un temporal, luego se reemplaza, para no corromper la partida.
            string temp = path + ".tmp";
            File.WriteAllText(temp, Json.Serialize(state));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }

        public static GameState Load(string directory, int slot)
        {
            string path = SlotPath(directory, slot);
            if (!File.Exists(path)) return null;
            var state = Json.Deserialize<GameState>(File.ReadAllText(path));
            if (state.Version > GameState.CurrentVersion)
                throw new InvalidDataException("La partida pertenece a una versión más nueva del juego");
            return state;
        }

        public static SaveSlotInfo Peek(string directory, int slot)
        {
            var state = Load(directory, slot);
            if (state == null) return null;
            int level = 0;
            CharacterState leader;
            if (state.Party.Active.Count > 0 && state.Characters.TryGetValue(state.Party.Active[0], out leader)) level = leader.Level;
            return new SaveSlotInfo
            {
                Slot = slot,
                SavedAt = state.SavedAt,
                PlayTimeSeconds = state.PlayTimeSeconds,
                Scene = state.Story.CurrentScene,
                Era = state.Location.Era,
                LeaderLevel = level
            };
        }
    }
}
