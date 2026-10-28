using System;
using System.Collections.Generic;

namespace TimeTraveler.Core
{
    /// <summary>Estado persistente de un personaje humano.</summary>
    public sealed class CharacterState
    {
        public string Id;
        public int Level = 1;
        public int Xp;
        public int Hp;
        public int Mp;
        public bool Recruited;
        public Dictionary<EquipSlot, string> Equipment = new Dictionary<EquipSlot, string>();
        public List<string> LearnedTechniques = new List<string>();
    }

    public sealed class LevelUpInfo
    {
        public string ActorName;
        public int NewLevel;
        public List<string> NewTechniques = new List<string>();
    }

    public static class ExperienceCurve
    {
        public const int MaxLevel = 99;

        /// <summary>Experiencia total necesaria para alcanzar un nivel.</summary>
        public static int TotalXpForLevel(int level)
        {
            if (level <= 1) return 0;
            return (int)Math.Round(12.0 * Math.Pow(level - 1, 2.35));
        }

        public static int XpToNextLevel(int level, int currentXp)
        {
            if (level >= MaxLevel) return 0;
            return Math.Max(0, TotalXpForLevel(level + 1) - currentXp);
        }
    }

    /// <summary>Cálculo de estadísticas finales (nivel + equipo) para humanos y criaturas.</summary>
    public static class StatCalculator
    {
        public static StatBlock ForCharacter(GameDatabase db, CharacterState state)
        {
            var def = db.Character(state.Id);
            var stats = StatMath.AtLevel(def.BaseStats, def.Growth, state.Level);
            AddEquipment(db, stats, state.Equipment);
            return stats;
        }

        public static StatBlock ForCreature(GameDatabase db, CreatureInstance creature)
        {
            var species = db.Species(creature.SpeciesId);
            var stats = StatMath.AtLevel(species.BaseStats, species.Growth, creature.Level);
            // La afinidad fortalece levemente a la mascota: hasta +10 % con afinidad 100.
            float bond = 1f + creature.Affinity / 1000f;
            foreach (var stat in StatMath.All) stats[stat] = (int)Math.Round(stats.Get(stat) * bond);
            AddEquipment(db, stats, creature.Equipment);
            return stats;
        }

        private static void AddEquipment(GameDatabase db, StatBlock stats, Dictionary<EquipSlot, string> equipment)
        {
            foreach (var itemId in equipment.Values)
                if (itemId != null) stats.AddAll(db.Item(itemId).Stats);
        }

        public static Element WeaponElement(GameDatabase db, Dictionary<EquipSlot, string> equipment)
        {
            string weaponId;
            if (equipment.TryGetValue(EquipSlot.Arma, out weaponId) && weaponId != null)
                return db.Item(weaponId).Element;
            return Element.Neutro;
        }

        public static Dictionary<Element, float> Resistances(GameDatabase db, Dictionary<EquipSlot, string> equipment)
        {
            var result = new Dictionary<Element, float>();
            foreach (var itemId in equipment.Values)
            {
                if (itemId == null) continue;
                foreach (var r in db.Item(itemId).Resistances)
                {
                    // Se queda con la resistencia más favorable de todo el equipo.
                    float current;
                    if (!result.TryGetValue(r.Key, out current) || r.Value < current) result[r.Key] = r.Value;
                }
            }
            return result;
        }

        public static HashSet<StatusType> Immunities(GameDatabase db, Dictionary<EquipSlot, string> equipment)
        {
            var result = new HashSet<StatusType>();
            foreach (var itemId in equipment.Values)
                if (itemId != null)
                    foreach (var s in db.Item(itemId).StatusImmunities) result.Add(s);
            return result;
        }
    }

    public static class ProgressionService
    {
        /// <summary>Crea el estado inicial de un personaje con su equipo y técnicas de nivel inicial.</summary>
        public static CharacterState CreateCharacter(GameDatabase db, string id, int level)
        {
            var def = db.Character(id);
            var state = new CharacterState { Id = id, Level = Math.Max(1, level), Xp = ExperienceCurve.TotalXpForLevel(level) };
            foreach (var eq in def.StartingEquipment) state.Equipment[eq.Key] = eq.Value;
            foreach (var entry in def.Learnset)
                if (entry.Level <= state.Level) state.LearnedTechniques.Add(entry.TechniqueId);
            var stats = StatCalculator.ForCharacter(db, state);
            state.Hp = stats.Get(StatType.MaxHp);
            state.Mp = stats.Get(StatType.MaxMp);
            return state;
        }

        public static List<LevelUpInfo> GainXp(GameDatabase db, CharacterState state, int amount)
        {
            var def = db.Character(state.Id);
            return GainXp(amount, ref state.Level, ref state.Xp, def.Learnset, state.LearnedTechniques, def.NameKey);
        }

        public static List<LevelUpInfo> GainXp(GameDatabase db, CreatureInstance creature, int amount)
        {
            var species = db.Species(creature.SpeciesId);
            return GainXp(amount, ref creature.Level, ref creature.Xp, species.Learnset, creature.LearnedTechniques, species.NameKey);
        }

        private static List<LevelUpInfo> GainXp(int amount, ref int level, ref int xp, List<LearnEntry> learnset, List<string> learned, string nameKey)
        {
            var result = new List<LevelUpInfo>();
            if (amount <= 0 || level >= ExperienceCurve.MaxLevel) return result;
            xp += amount;
            while (level < ExperienceCurve.MaxLevel && xp >= ExperienceCurve.TotalXpForLevel(level + 1))
            {
                level++;
                var info = new LevelUpInfo { ActorName = nameKey, NewLevel = level };
                foreach (var entry in learnset)
                {
                    if (entry.Level <= level && !learned.Contains(entry.TechniqueId))
                    {
                        learned.Add(entry.TechniqueId);
                        info.NewTechniques.Add(entry.TechniqueId);
                    }
                }
                result.Add(info);
            }
            return result;
        }
    }
}
