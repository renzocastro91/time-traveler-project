using System;
using System.Collections.Generic;

namespace TimeTraveler.Core
{
    public enum StatType
    {
        MaxHp,
        MaxMp,
        Fuerza,
        Magia,
        Defensa,
        DefensaMagica,
        Velocidad,
        Precision,
        Evasion,
        Suerte
    }

    /// <summary>Bloque de estadísticas enteras. Una estadística ausente vale 0.</summary>
    public sealed class StatBlock : Dictionary<StatType, int>
    {
        public int Get(StatType stat)
        {
            int value;
            return TryGetValue(stat, out value) ? value : 0;
        }

        public void AddTo(StatType stat, int amount)
        {
            this[stat] = Get(stat) + amount;
        }

        public void AddAll(StatBlock other)
        {
            if (other == null) return;
            foreach (var pair in other) AddTo(pair.Key, pair.Value);
        }

        public StatBlock Clone()
        {
            var copy = new StatBlock();
            copy.AddAll(this);
            return copy;
        }
    }

    /// <summary>Crecimiento por nivel (puede ser fraccionario: 1.5 = +3 cada 2 niveles).</summary>
    public sealed class StatGrowth : Dictionary<StatType, float>
    {
        public float Get(StatType stat)
        {
            float value;
            return TryGetValue(stat, out value) ? value : 0f;
        }
    }

    public static class StatMath
    {
        public static readonly StatType[] All = (StatType[])Enum.GetValues(typeof(StatType));

        /// <summary>Estadísticas base de nivel 1 más el crecimiento acumulado hasta <paramref name="level"/>.</summary>
        public static StatBlock AtLevel(StatBlock baseStats, StatGrowth growth, int level)
        {
            var result = new StatBlock();
            foreach (var stat in All)
            {
                int value = baseStats.Get(stat);
                if (growth != null) value += (int)Math.Floor(growth.Get(stat) * (level - 1));
                result[stat] = value;
            }
            return result;
        }
    }
}
