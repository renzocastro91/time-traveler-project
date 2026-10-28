using System.Collections.Generic;
using System.Linq;

namespace TimeTraveler.Core
{
    /// <summary>IA de enemigos: elige entre sus acciones por peso, filtrando por porcentaje de vida y PM.</summary>
    public static class EnemyAI
    {
        public static BattleAction Choose(BattleEngine engine, BattleUnit unit)
        {
            var db = engine.Database;
            var options = new List<EnemyActionDef>();
            int totalWeight = 0;
            foreach (var action in unit.EnemyDef.Actions)
            {
                if (unit.HpPercent < action.MinHpPercent || unit.HpPercent > action.MaxHpPercent) continue;
                if (action.TechniqueId != null)
                {
                    var tech = db.Technique(action.TechniqueId);
                    if (unit.Mp < tech.MpCost) continue;
                    if (tech.Kind == TechniqueKind.Magica && unit.HasStatus(StatusType.Silencio)) continue;
                    if (unit.HasStatus(StatusType.Olvido)) continue;
                }
                options.Add(action);
                totalWeight += action.Weight;
            }

            var opponents = engine.Opponents(unit).ToList();
            BattleUnit randomOpponent = opponents.Count > 0 ? opponents[engine.Random.Range(0, opponents.Count)] : null;
            if (options.Count == 0) return BattleAction.Attack(unit, randomOpponent);

            int roll = engine.Random.Range(0, totalWeight);
            EnemyActionDef chosen = options[0];
            foreach (var option in options)
            {
                if (roll < option.Weight) { chosen = option; break; }
                roll -= option.Weight;
            }

            if (chosen.TechniqueId == null) return BattleAction.Attack(unit, randomOpponent);

            var technique = db.Technique(chosen.TechniqueId);
            BattleUnit target = technique.IsOffensive
                ? randomOpponent
                : engine.Alive(unit.Team).OrderBy(u => u.HpPercent).First();
            return BattleAction.Technique(unit, chosen.TechniqueId, target);
        }
    }

    /// <summary>
    /// IA de la mascota del grupo. Actúa sola según su postura (elegida en el menú del Santuario):
    /// Ofensiva = técnicas de daño sobre el enemigo más débil; Defensiva = protege y se defiende
    /// con poca vida; Apoyo = cura a aliados heridos antes de atacar.
    /// </summary>
    public static class PetAI
    {
        public static BattleAction Choose(BattleEngine engine, BattleUnit pet)
        {
            var db = engine.Database;
            var enemies = engine.Opponents(pet).ToList();
            var weakestEnemy = enemies.OrderBy(u => u.Hp).FirstOrDefault();
            var usable = pet.Techniques.Where(t => engine.CanUseTechnique(pet, t)).Select(db.Technique).ToList();

            var woundedAlly = engine.Alive(pet.Team).Where(u => u.HpPercent < 0.5f).OrderBy(u => u.HpPercent).FirstOrDefault();
            var heal = usable.FirstOrDefault(t => t.Healing);
            var support = usable.FirstOrDefault(t => !t.Healing && !t.IsOffensive);
            var strongest = usable.Where(t => t.IsOffensive).OrderByDescending(t => t.Power).FirstOrDefault();

            switch (pet.Stance)
            {
                case PetStance.Apoyo:
                    if (woundedAlly != null && heal != null) return BattleAction.Technique(pet, heal.Id, woundedAlly);
                    if (support != null && engine.Random.Value() < 0.4f) return BattleAction.Technique(pet, support.Id, pet);
                    break;
                case PetStance.Defensiva:
                    if (pet.HpPercent < 0.4f) return BattleAction.Defend(pet);
                    if (support != null && engine.Random.Value() < 0.3f) return BattleAction.Technique(pet, support.Id, pet);
                    break;
                default:
                    if (strongest != null && engine.Random.Value() < 0.6f) return BattleAction.Technique(pet, strongest.Id, weakestEnemy);
                    break;
            }
            return BattleAction.Attack(pet, weakestEnemy);
        }
    }
}
