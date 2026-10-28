using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeTraveler.Core
{
    /// <summary>Construye combates desde el estado de la partida y aplica sus resultados al terminar.</summary>
    public static class BattleFactory
    {
        /// <summary>Formación del grupo (relativa al centro del combate): tres humanos y la mascota delante.</summary>
        public static readonly Pos[] PartyPositions = { new Pos(0f, -3.5f), new Pos(-2.2f, -3f), new Pos(2.2f, -3f) };
        public static readonly Pos PetPosition = new Pos(0f, -1.8f);

        public static BattleEngine Create(GameDatabase db, GameState state, string encounterId, IRandom rng)
        {
            var encounter = db.Encounter(encounterId);
            var units = new List<BattleUnit>();

            for (int i = 0; i < state.Party.Active.Count; i++)
                units.Add(FromCharacter(db, state.Characters[state.Party.Active[i]], PartyPositions[i % PartyPositions.Length]));

            var pet = state.ActivePet;
            if (pet != null && pet.Hp > 0) units.Add(FromCreature(db, pet, PetPosition));

            foreach (var slot in encounter.Enemies)
                units.Add(FromEnemy(db.Enemy(slot.EnemyId), new Pos(slot.X, slot.Y)));

            return new BattleEngine(db, rng, units, encounter, state.Inventory)
            {
                Mode = state.BattleMode,
                Speed = state.BattleSpeed
            };
        }

        public static BattleUnit FromCharacter(GameDatabase db, CharacterState c, Pos position)
        {
            var def = db.Character(c.Id);
            return new BattleUnit
            {
                Team = Team.Jugador,
                Kind = UnitKind.Humano,
                SourceId = c.Id,
                NameKey = def.NameKey,
                Level = c.Level,
                Element = def.Element,
                WeaponElement = StatCalculator.WeaponElement(db, c.Equipment),
                Stats = StatCalculator.ForCharacter(db, c),
                Hp = c.Hp,
                Mp = c.Mp,
                Position = position,
                Techniques = new List<string>(c.LearnedTechniques),
                Resistances = StatCalculator.Resistances(db, c.Equipment),
                Immunities = StatCalculator.Immunities(db, c.Equipment)
            };
        }

        public static BattleUnit FromCreature(GameDatabase db, CreatureInstance creature, Pos position)
        {
            var species = db.Species(creature.SpeciesId);
            return new BattleUnit
            {
                Team = Team.Jugador,
                Kind = UnitKind.Mascota,
                SourceId = creature.Uid,
                NameKey = species.NameKey,
                Level = creature.Level,
                Element = species.Element,
                WeaponElement = species.Element,
                Stats = StatCalculator.ForCreature(db, creature),
                Hp = creature.Hp,
                Mp = creature.Mp,
                Position = position,
                Stance = creature.Stance,
                Techniques = new List<string>(creature.LearnedTechniques),
                Resistances = StatCalculator.Resistances(db, creature.Equipment),
                Immunities = StatCalculator.Immunities(db, creature.Equipment)
            };
        }

        public static BattleUnit FromEnemy(EnemyDef def, Pos position)
        {
            return new BattleUnit
            {
                Team = Team.Enemigo,
                Kind = UnitKind.Enemigo,
                SourceId = def.Id,
                NameKey = def.NameKey,
                Level = def.Level,
                Element = def.Element,
                WeaponElement = def.Element,
                Stats = def.Stats.Clone(),
                Hp = def.Stats.Get(StatType.MaxHp),
                Mp = def.Stats.Get(StatType.MaxMp),
                Position = position,
                IsBoss = def.IsBoss,
                EnemyDef = def,
                Techniques = def.Actions.Where(a => a.TechniqueId != null).Select(a => a.TechniqueId).ToList(),
                Resistances = new Dictionary<Element, float>(def.Resistances),
                Immunities = new HashSet<StatusType>(def.StatusImmunities)
            };
        }
    }

    public sealed class BattleSummary
    {
        public BattleOutcome Outcome;
        public BattleRewards Rewards;
        public List<LevelUpInfo> LevelUps = new List<LevelUpInfo>();
        public int PetAffinityGained;
    }

    public static class BattleResultService
    {
        /// <summary>Fracción de experiencia que reciben los reclutados que esperan en reserva.</summary>
        public const float ReserveXpShare = 0.5f;

        /// <summary>
        /// Devuelve vida/PM a la partida y reparte recompensas. Tras una victoria, quien cayó vuelve con
        /// 1 de vida (toque moderno: no obliga a buscar un objeto para seguir explorando).
        /// </summary>
        public static BattleSummary Apply(GameDatabase db, GameState state, BattleEngine engine)
        {
            var summary = new BattleSummary { Outcome = engine.Outcome, Rewards = engine.Rewards };
            bool victory = engine.Outcome == BattleOutcome.Victoria;

            foreach (var unit in engine.Units.Where(u => u.Team == Team.Jugador))
            {
                int hp = victory ? Math.Max(1, unit.Hp) : unit.Hp;
                if (unit.Kind == UnitKind.Humano)
                {
                    var c = state.Characters[unit.SourceId];
                    c.Hp = hp;
                    c.Mp = unit.Mp;
                }
                else
                {
                    var pet = state.Sanctuary.Find(unit.SourceId);
                    if (pet != null) { pet.Hp = hp; pet.Mp = unit.Mp; }
                }
            }

            if (!victory || engine.Rewards == null) return summary;

            var rewards = engine.Rewards;
            state.Inventory.Money += rewards.Money;
            foreach (var item in rewards.Items) state.Inventory.Add(item);

            foreach (var c in state.Characters.Values.Where(c => c.Recruited))
            {
                int xp = state.Party.IsActive(c.Id) ? rewards.Xp : (int)(rewards.Xp * ReserveXpShare);
                summary.LevelUps.AddRange(ProgressionService.GainXp(db, c, xp));
            }

            var petUnit = engine.Pet;
            var activePet = state.ActivePet;
            if (petUnit != null && activePet != null)
            {
                summary.LevelUps.AddRange(ProgressionService.GainXp(db, activePet, rewards.Xp));
                summary.PetAffinityGained = Math.Min(100 - activePet.Affinity, 1 + engine.PetCombos * 2);
                activePet.Affinity += summary.PetAffinityGained;
            }
            return summary;
        }
    }
}
