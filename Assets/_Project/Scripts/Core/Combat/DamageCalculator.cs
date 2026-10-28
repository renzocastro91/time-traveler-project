using System;
using System.Collections.Generic;

namespace TimeTraveler.Core
{
    public struct DamageResult
    {
        public bool Hit;
        /// <summary>Positivo = daño; negativo = el objetivo absorbe y se cura.</summary>
        public int Amount;
        public bool Critical;
        public float ElementMultiplier;
    }

    /// <summary>
    /// Fórmulas de combate. Todas las constantes están aquí para equilibrar en un solo lugar.
    /// Daño base = (Potencia/10) · Ataque · 2 · 100 / (100 + Defensa · 2)
    /// luego × elemento × crítico × variación (±10 %) × defensa/estados.
    /// </summary>
    public static class DamageCalculator
    {
        public const float Variance = 0.1f;
        public const float CritMultiplier = 1.75f;
        public const float DefendMultiplier = 0.5f;
        public const float ShieldMultiplier = 0.67f;
        public const float BlindHitPenalty = 0.4f;
        public const float HealScale = 1.5f;

        public static float ElementMultiplier(Element attack, BattleUnit target)
        {
            float resist;
            if (target.Resistances != null && target.Resistances.TryGetValue(attack, out resist)) return resist;
            return ElementChart.Multiplier(attack, target.Element);
        }

        public static float HitChance(int precision, int evasion, bool blind)
        {
            float chance = 0.95f + (precision - evasion) * 0.01f;
            if (blind) chance -= BlindHitPenalty;
            return Math.Max(0.3f, Math.Min(1f, chance));
        }

        public static DamageResult Compute(EffectSpec spec, int attackStat, int precision, int luck, bool attackerBlind, float outgoingMultiplier, BattleUnit target, IRandom rng)
        {
            bool physical = spec.Kind == TechniqueKind.Fisica;
            var result = new DamageResult { Hit = true, ElementMultiplier = ElementMultiplier(spec.Element, target) };

            if (physical && rng.Value() > HitChance(precision, target.Stats.Get(StatType.Evasion), attackerBlind))
            {
                result.Hit = false;
                return result;
            }

            int defense = physical ? target.Stats.Get(StatType.Defensa) : target.Stats.Get(StatType.DefensaMagica);
            double amount = spec.Power / 10.0 * attackStat * 2.0 * 100.0 / (100.0 + defense * 2.0);
            amount *= result.ElementMultiplier * outgoingMultiplier;

            if (physical && rng.Value() < 0.04f + luck * 0.002f)
            {
                result.Critical = true;
                amount *= CritMultiplier;
            }

            amount *= 1.0 - Variance + rng.Value() * Variance * 2.0;

            if (target.IsDefending) amount *= DefendMultiplier;
            if (physical && target.HasStatus(StatusType.Proteccion)) amount *= ShieldMultiplier;
            if (!physical && target.HasStatus(StatusType.Coraza)) amount *= ShieldMultiplier;

            if (result.ElementMultiplier > 0f) result.Amount = Math.Max(1, (int)Math.Round(amount));
            else if (result.ElementMultiplier < 0f) result.Amount = Math.Min(-1, (int)Math.Round(amount));
            else result.Amount = 0;
            return result;
        }

        public static int Healing(int power, int magic, IRandom rng)
        {
            double amount = power / 10.0 * magic * HealScale * (1.0 - Variance + rng.Value() * Variance * 2.0);
            return Math.Max(1, (int)Math.Round(amount));
        }

        /// <summary>
        /// Estadística de ataque de varios participantes (combos): cada uno aporta el 60 % de la suya.
        /// Con un solo atacante es su estadística completa.
        /// </summary>
        public static int CombinedStat(IList<BattleUnit> attackers, StatType stat)
        {
            if (attackers.Count == 1) return attackers[0].Stats.Get(stat);
            double sum = 0;
            foreach (var a in attackers) sum += a.Stats.Get(stat) * 0.6;
            return (int)Math.Round(sum);
        }
    }
}
