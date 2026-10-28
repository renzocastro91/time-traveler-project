using System;
using System.Collections.Generic;

namespace TimeTraveler.Core
{
    /// <summary>Una criatura vinculada al grupo.</summary>
    public sealed class CreatureInstance
    {
        public string Uid;
        public string SpeciesId;
        /// <summary>Apodo opcional; vacío = nombre de la especie.</summary>
        public string Nickname;
        public int Level = 1;
        public int Xp;
        /// <summary>Afinidad 0-100: sube combatiendo juntos y con ofrendas; necesaria para evolucionar.</summary>
        public int Affinity;
        public int Hp;
        public int Mp;
        public PetStance Stance = PetStance.Ofensiva;
        public Dictionary<EquipSlot, string> Equipment = new Dictionary<EquipSlot, string>();
        public List<string> LearnedTechniques = new List<string>();
    }

    /// <summary>
    /// El Santuario guarda las criaturas vinculadas (vive en el Museo, la base móvil del guion).
    /// Desde el menú de pausa se elige cuál acompaña al grupo.
    /// </summary>
    public sealed class Sanctuary
    {
        public List<CreatureInstance> Creatures = new List<CreatureInstance>();
        public int Capacity = 12;

        public bool IsFull { get { return Creatures.Count >= Capacity; } }

        public CreatureInstance Find(string uid)
        {
            if (uid == null) return null;
            foreach (var c in Creatures)
                if (c.Uid == uid) return c;
            return null;
        }

        /// <summary>Libera una criatura (vuelve a su hábitat). No se puede liberar a la mascota activa.</summary>
        public bool Release(string uid, PartyState party)
        {
            if (uid == party.ActivePetUid) return false;
            var creature = Find(uid);
            return creature != null && Creatures.Remove(creature);
        }
    }

    public static class CreatureFactory
    {
        public static CreatureInstance Create(GameDatabase db, string speciesId, int level, string uid)
        {
            var species = db.Species(speciesId);
            var creature = new CreatureInstance
            {
                Uid = uid,
                SpeciesId = speciesId,
                Level = Math.Max(1, level),
                Xp = ExperienceCurve.TotalXpForLevel(level),
            };
            foreach (var entry in species.Learnset)
                if (entry.Level <= creature.Level) creature.LearnedTechniques.Add(entry.TechniqueId);
            var stats = StatCalculator.ForCreature(db, creature);
            creature.Hp = stats.Get(StatType.MaxHp);
            creature.Mp = stats.Get(StatType.MaxMp);
            return creature;
        }
    }

    public static class EvolutionSystem
    {
        public static List<EvolutionDef> Available(GameDatabase db, GameState state, CreatureInstance creature, string currentEra)
        {
            var result = new List<EvolutionDef>();
            foreach (var evo in db.Species(creature.SpeciesId).Evolutions)
                if (CanEvolve(state, creature, evo, currentEra)) result.Add(evo);
            return result;
        }

        public static bool CanEvolve(GameState state, CreatureInstance creature, EvolutionDef evo, string currentEra)
        {
            if (creature.Level < evo.MinLevel || creature.Affinity < evo.MinAffinity) return false;
            if (evo.RequiredItemId != null && state.Inventory.Count(evo.RequiredItemId) <= 0) return false;
            if (evo.RequiredEra != null && evo.RequiredEra != currentEra) return false;
            if (evo.RequiredFlag != null && state.Flags.Get(evo.RequiredFlag) == 0) return false;
            return true;
        }

        /// <summary>
        /// Evoluciona la criatura conservando nivel, afinidad y equipo; aprende las técnicas de la nueva
        /// especie hasta su nivel actual. Devuelve las técnicas nuevas.
        /// </summary>
        public static List<string> Evolve(GameDatabase db, GameState state, CreatureInstance creature, EvolutionDef evo, string currentEra)
        {
            if (!CanEvolve(state, creature, evo, currentEra)) throw new InvalidOperationException("La criatura no cumple las condiciones de evolución");
            if (evo.RequiredItemId != null && evo.ConsumeItem) state.Inventory.Remove(evo.RequiredItemId);

            var before = StatCalculator.ForCreature(db, creature);
            creature.SpeciesId = evo.ToSpeciesId;
            var learned = new List<string>();
            foreach (var entry in db.Species(evo.ToSpeciesId).Learnset)
            {
                if (entry.Level <= creature.Level && !creature.LearnedTechniques.Contains(entry.TechniqueId))
                {
                    creature.LearnedTechniques.Add(entry.TechniqueId);
                    learned.Add(entry.TechniqueId);
                }
            }
            // Conserva la proporción de vida/maná al cambiar de forma.
            var after = StatCalculator.ForCreature(db, creature);
            creature.Hp = Scale(creature.Hp, before.Get(StatType.MaxHp), after.Get(StatType.MaxHp));
            creature.Mp = Scale(creature.Mp, before.Get(StatType.MaxMp), after.Get(StatType.MaxMp));
            return learned;
        }

        private static int Scale(int value, int oldMax, int newMax)
        {
            if (oldMax <= 0) return newMax;
            return (int)Math.Round((double)value / oldMax * newMax);
        }
    }

    public sealed class BondResult
    {
        public bool Accepted;
        public float Chance;
        /// <summary>Clave de localización del motivo (para mostrar al jugador).</summary>
        public string ReasonKey;
        public CreatureInstance Creature;
    }

    /// <summary>
    /// Vínculo por Resonancia: el grupo sintoniza con una criatura mediante el Diapasón de Arcilla
    /// (minijuego rítmico del motivo de tres notas). No hay combate previo ni dispositivos de captura:
    /// la criatura decide si acepta acompañarlos. La calidad de la sintonía, la dificultad de la especie
    /// y una ofrenda adecuada determinan la probabilidad.
    /// </summary>
    public static class BondingSystem
    {
        public const float MinScore = 0.4f;
        public const float MaxChance = 0.95f;

        public static float AcceptanceChance(CreatureSpeciesDef species, float resonanceScore, ItemDef offering)
        {
            if (!species.Bondable || resonanceScore < MinScore) return 0f;
            float score = Math.Max(0f, Math.Min(1f, resonanceScore));
            float baseChance = (float)Math.Pow(score, 1.5) * (1.25f - 0.15f * species.BondDifficulty);
            float bonus = 0f;
            if (offering != null)
            {
                bonus += offering.BondingBonus;
                if (offering.Id == species.FavoriteOfferingId) bonus += 0.15f;
            }
            return Math.Max(0f, Math.Min(MaxChance, baseChance + bonus));
        }

        public static BondResult Attempt(GameDatabase db, GameState state, string speciesId, int level, float resonanceScore, string offeringId, IRandom rng)
        {
            var species = db.Species(speciesId);
            if (!species.Bondable) return new BondResult { ReasonKey = "bond.reason.not_bondable" };
            if (species.BondCondition != null && !ConditionEvaluator.Evaluate(species.BondCondition, state.ResolveVariable))
                return new BondResult { ReasonKey = "bond.reason.condition" };
            if (state.Sanctuary.IsFull) return new BondResult { ReasonKey = "bond.reason.full" };

            ItemDef offering = null;
            if (offeringId != null)
            {
                if (!state.Inventory.Remove(offeringId)) return new BondResult { ReasonKey = "bond.reason.no_offering" };
                offering = db.Item(offeringId);
            }

            float chance = AcceptanceChance(species, resonanceScore, offering);
            var result = new BondResult { Chance = chance };
            if (resonanceScore < MinScore) { result.ReasonKey = "bond.reason.no_tune"; return result; }

            result.Accepted = rng.Value() < chance;
            if (!result.Accepted) { result.ReasonKey = "bond.reason.refused"; return result; }

            var creature = CreatureFactory.Create(db, speciesId, level, state.NewCreatureUid());
            creature.Affinity = offering != null && offering.Id == species.FavoriteOfferingId ? 15 : 5;
            state.Sanctuary.Creatures.Add(creature);
            if (state.Party.ActivePetUid == null) state.Party.ActivePetUid = creature.Uid;
            result.Creature = creature;
            result.ReasonKey = "bond.reason.accepted";
            return result;
        }

        /// <summary>Dar una ofrenda fuera de combate sube la afinidad.</summary>
        public static int GiveOffering(GameDatabase db, GameState state, CreatureInstance creature, string offeringId)
        {
            if (!state.Inventory.Remove(offeringId)) return 0;
            var species = db.Species(creature.SpeciesId);
            int gain = offeringId == species.FavoriteOfferingId ? 10 : 4;
            creature.Affinity = Math.Min(100, creature.Affinity + gain);
            return gain;
        }
    }
}
