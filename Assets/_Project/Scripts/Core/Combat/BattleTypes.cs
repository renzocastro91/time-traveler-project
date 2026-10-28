using System;
using System.Collections.Generic;

namespace TimeTraveler.Core
{
    /// <summary>Posición en el plano de batalla (metros, relativa al centro del combate en el mapa).</summary>
    public struct Pos
    {
        public float X;
        public float Y;

        public Pos(float x, float y) { X = x; Y = y; }

        public float DistanceTo(Pos other)
        {
            float dx = X - other.X, dy = Y - other.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public sealed class StatusInstance
    {
        public StatusType Type;
        /// <summary>Turnos restantes del afectado.</summary>
        public int Remaining;
    }

    public sealed class BattleUnit
    {
        public int Index;
        public Team Team;
        public UnitKind Kind;
        /// <summary>Id de personaje, uid de criatura o id de enemigo.</summary>
        public string SourceId;
        public string NameKey;
        public int Level;
        public Element Element;
        public Element WeaponElement;
        public StatBlock Stats = new StatBlock();
        public int Hp;
        public int Mp;
        public float Atb;
        public Pos Position;
        public bool IsBoss;
        public bool IsDefending;
        public bool IsReady;
        public float PetHoldTimer;
        public PetStance Stance;
        public EnemyDef EnemyDef;
        public List<string> Techniques = new List<string>();
        public List<StatusInstance> Statuses = new List<StatusInstance>();
        public Dictionary<Element, float> Resistances = new Dictionary<Element, float>();
        public HashSet<StatusType> Immunities = new HashSet<StatusType>();
        /// <summary>Multiplicador de daño infligido que pueden modificar las reglas dramáticas.</summary>
        public float OutgoingDamageMultiplier = 1f;

        public bool IsAlive { get { return Hp > 0; } }
        public int MaxHp { get { return Stats.Get(StatType.MaxHp); } }
        public int MaxMp { get { return Stats.Get(StatType.MaxMp); } }
        public float HpPercent { get { return MaxHp > 0 ? (float)Hp / MaxHp : 0f; } }

        public bool HasStatus(StatusType type)
        {
            foreach (var s in Statuses)
                if (s.Type == type) return true;
            return false;
        }

        public void RemoveStatus(StatusType type)
        {
            Statuses.RemoveAll(s => s.Type == type);
        }
    }

    public enum ActionType
    {
        Atacar,
        Tecnica,
        Combo,
        Objeto,
        Defender,
        Huir,
        ReglaDramatica
    }

    public sealed class BattleAction
    {
        public ActionType Type;
        public BattleUnit Actor;
        public string TechniqueId;
        public string ComboId;
        public string ItemId;
        public string RuleCommandId;
        public BattleUnit Target;

        public static BattleAction Attack(BattleUnit actor, BattleUnit target)
        {
            return new BattleAction { Type = ActionType.Atacar, Actor = actor, Target = target };
        }

        public static BattleAction Technique(BattleUnit actor, string techniqueId, BattleUnit target)
        {
            return new BattleAction { Type = ActionType.Tecnica, Actor = actor, TechniqueId = techniqueId, Target = target };
        }

        public static BattleAction Combo(BattleUnit actor, string comboId, BattleUnit target)
        {
            return new BattleAction { Type = ActionType.Combo, Actor = actor, ComboId = comboId, Target = target };
        }

        public static BattleAction Item(BattleUnit actor, string itemId, BattleUnit target)
        {
            return new BattleAction { Type = ActionType.Objeto, Actor = actor, ItemId = itemId, Target = target };
        }

        public static BattleAction Defend(BattleUnit actor)
        {
            return new BattleAction { Type = ActionType.Defender, Actor = actor };
        }

        public static BattleAction Flee(BattleUnit actor)
        {
            return new BattleAction { Type = ActionType.Huir, Actor = actor };
        }

        public static BattleAction Rule(BattleUnit actor, string commandId, BattleUnit target)
        {
            return new BattleAction { Type = ActionType.ReglaDramatica, Actor = actor, RuleCommandId = commandId, Target = target };
        }
    }

    public enum BattleEventType
    {
        TurnReady,
        ActionStart,
        Damage,
        Heal,
        MpRestore,
        Miss,
        Immune,
        StatusApplied,
        StatusRemoved,
        TurnSkipped,
        Death,
        Revive,
        Defend,
        RuleMessage,
        FleeFailed,
        Fled,
        Victory,
        Defeat
    }

    /// <summary>
    /// Lo que pasó en la batalla. La capa visual consume estos eventos en orden y los anima;
    /// mientras anima, pausa el motor (BattleEngine.Paused).
    /// </summary>
    public sealed class BattleEvent
    {
        public BattleEventType Type;
        public BattleUnit Actor;
        public BattleUnit Target;
        /// <summary>Participantes de un combo (incluye al actor).</summary>
        public List<BattleUnit> Participants;
        public ActionType ActionType;
        public int Amount;
        public bool Critical;
        public float ElementMultiplier = 1f;
        public string TechniqueId;
        public string ComboId;
        public string ItemId;
        public StatusType Status;
        public string TextKey;
        public object[] Args;
    }

    public enum BattleOutcome
    {
        EnCurso,
        Victoria,
        Derrota,
        Huida
    }

    public sealed class BattleRewards
    {
        public int Xp;
        public int Money;
        public List<string> Items = new List<string>();
    }

    /// <summary>Parámetros comunes de un efecto (técnica, combo o ataque básico).</summary>
    public sealed class EffectSpec
    {
        public TechniqueKind Kind;
        public Element Element;
        public int Power;
        public TargetType Target;
        public float AreaRadius;
        public bool Healing;
        public float Revive;
        public StatusType? ApplyStatus;
        public float StatusChance;
        public int StatusDuration;
        public List<StatusType> CureStatuses = new List<StatusType>();

        public bool IsOffensive { get { return Target == TargetType.EnemigoUno || Target == TargetType.EnemigoArea || Target == TargetType.EnemigosTodos; } }

        public static EffectSpec BasicAttack(BattleUnit actor)
        {
            return new EffectSpec { Kind = TechniqueKind.Fisica, Element = actor.WeaponElement, Power = 10, Target = TargetType.EnemigoUno };
        }

        public static EffectSpec From(TechniqueDef t)
        {
            return new EffectSpec
            {
                Kind = t.Kind, Element = t.Element, Power = t.Power, Target = t.Target, AreaRadius = t.AreaRadius,
                Healing = t.Healing, Revive = t.Revive, ApplyStatus = t.ApplyStatus, StatusChance = t.StatusChance,
                StatusDuration = t.StatusDuration, CureStatuses = t.CureStatuses ?? new List<StatusType>()
            };
        }

        public static EffectSpec From(ComboDef c)
        {
            return new EffectSpec
            {
                Kind = c.Kind, Element = c.Element, Power = c.Power, Target = c.Target, AreaRadius = c.AreaRadius,
                Healing = c.Healing, Revive = c.Revive, ApplyStatus = c.ApplyStatus, StatusChance = c.StatusChance,
                StatusDuration = c.StatusDuration
            };
        }
    }
}
