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
        /// <summary>Cierres confirmados (la Hebra actualizada), en orden.</summary>
        public List<CierreRecord> Cierres = new List<CierreRecord>();
        /// <summary>Nudo abierto cuyas decisiones aún pueden reconsiderarse.</summary>
        public NudoSnapshot PendingNudo;
        /// <summary>Fichas de ausentes conservadas (personas borradas de la Hebra).</summary>
        public List<string> Absents = new List<string>();
        public List<string> VisitedMaps = new List<string>();
        /// <summary>Acto completado más alto (para el cartel de fin de acto).</summary>
        public int ActsCompleted;
    }

    public sealed class LocationState
    {
        public string Era = "2000";
        public string MapId = "instituto_2000";
        public string Spawn = "default";
        /// <summary>True si X/Y/Z guardan una posición exacta (partida guardada); si no, se usa Spawn.</summary>
        public bool HasPosition;
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
        /// inventario), "recruited.id", "scene.E05" (completada), "current.E05" (en curso), "cierre.id"
        /// (confirmado), "visited.mapa", "absent.id", "resolved.id" (objeto de mapa resuelto) y "money".
        /// </summary>
        public int ResolveVariable(string name)
        {
            if (name == "money") return Inventory.Money;
            if (name.StartsWith("current.", StringComparison.Ordinal)) return Story.CurrentScene == name.Substring(8) ? 1 : 0;
            if (name.StartsWith("cierre.", StringComparison.Ordinal)) return Story.Cierres.Exists(c => c.Id == name.Substring(7)) ? 1 : 0;
            if (name.StartsWith("visited.", StringComparison.Ordinal)) return Story.VisitedMaps.Contains(name.Substring(8)) ? 1 : 0;
            if (name.StartsWith("absent.", StringComparison.Ordinal)) return Story.Absents.Contains(name.Substring(7)) ? 1 : 0;
            if (name.StartsWith("resolved.", StringComparison.Ordinal)) return Story.ClearedEncounters.Contains(name.Substring(9)) ? 1 : 0;
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
            state.Location.MapId = "instituto_2000";
            state.Location.Era = "2000";
            state.Location.Spawn = "default";
            return state;
        }

        /// <summary>
        /// Partida del modo de pruebas: grupo de nivel 8 con Lía, Diapasón, ofrendas y los objetos de
        /// testimonio, en el Nudo de pruebas. Sirve para probar combate y criaturas sin jugar la historia.
        /// </summary>
        public static GameState NewTestGame(GameDatabase db)
        {
            var state = NewGame(db);
            state.Recruit(db, "lia", 8);
            foreach (var c in state.Characters.Values)
            {
                ProgressionService.GainXp(db, c, ExperienceCurve.TotalXpForLevel(8) - c.Xp);
                var stats = StatCalculator.ForCharacter(db, c);
                c.Hp = stats.Get(StatType.MaxHp);
                c.Mp = stats.Get(StatType.MaxMp);
            }
            foreach (var item in new[] { "diapason_de_arcilla", "casco_de_operario", "foto_de_turno", "informe_de_julian", "ficha_gabriel", "collar_de_cuero", "manta_acolchada" })
                state.Inventory.Add(item);
            state.Inventory.Add("bizcocho", 5);
            state.Inventory.Add("pescado_seco", 3);
            state.Inventory.Add("carbon_dulce", 3);
            state.Inventory.Add("tuerca_brillante", 3);
            state.Inventory.Money = 2000;
            state.Story.CurrentScene = "PRUEBAS";
            state.Location.MapId = "nudo_pruebas";
            state.Location.Era = "orillas";
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
