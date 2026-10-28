using System.Collections.Generic;
using Newtonsoft.Json;

namespace TimeTraveler.Core
{
    // Definiciones estáticas cargadas desde StreamingAssets/Data/*.json.
    // Los textos visibles nunca se guardan aquí: se derivan claves de localización a partir del Id
    // (por ejemplo "tech.tajo.name"), así el juego queda listo para traducirse.

    public sealed class LearnEntry
    {
        public int Level;
        public string TechniqueId;
    }

    public sealed class CharacterDef
    {
        public string Id;
        public string Era;
        public int Age;
        public Element Element;
        public string WeaponType;
        public StatBlock BaseStats = new StatBlock();
        public StatGrowth Growth = new StatGrowth();
        public List<LearnEntry> Learnset = new List<LearnEntry>();
        public Dictionary<EquipSlot, string> StartingEquipment = new Dictionary<EquipSlot, string>();

        [JsonIgnore] public string NameKey { get { return "char." + Id + ".name"; } }
        [JsonIgnore] public string BioKey { get { return "char." + Id + ".bio"; } }
    }

    public sealed class TechniqueDef
    {
        public string Id;
        public TechniqueKind Kind;
        public Element Element;
        public int MpCost;
        /// <summary>Potencia: 10 equivale a un ataque básico.</summary>
        public int Power;
        public TargetType Target;
        public float AreaRadius = 2.5f;
        public bool Healing;
        /// <summary>Fracción de Vida máxima con que revive (0 = no revive).</summary>
        public float Revive;
        public StatusType? ApplyStatus;
        public float StatusChance;
        public int StatusDuration = 3;
        public List<StatusType> CureStatuses = new List<StatusType>();

        [JsonIgnore] public string NameKey { get { return "tech." + Id + ".name"; } }
        [JsonIgnore] public string DescKey { get { return "tech." + Id + ".desc"; } }
        [JsonIgnore] public bool IsOffensive { get { return Target == TargetType.EnemigoUno || Target == TargetType.EnemigoArea || Target == TargetType.EnemigosTodos; } }
    }

    public sealed class ComboParticipant
    {
        /// <summary>Id de personaje, o "mascota" para la mascota del grupo.</summary>
        public string Actor;
        /// <summary>Técnica que el participante debe conocer (y cuyo PM paga). Para la mascota puede ser null.</summary>
        public string TechniqueId;
        /// <summary>Sólo para la mascota: elemento requerido (null = cualquiera).</summary>
        public Element? PetElement;

        [JsonIgnore] public bool IsPet { get { return Actor == "mascota"; } }
    }

    public sealed class ComboDef
    {
        public string Id;
        public List<ComboParticipant> Participants = new List<ComboParticipant>();
        public TechniqueKind Kind;
        public Element Element;
        public int Power;
        public TargetType Target;
        public float AreaRadius = 3f;
        public bool Healing;
        public float Revive;
        public StatusType? ApplyStatus;
        public float StatusChance;
        public int StatusDuration = 3;
        /// <summary>Costo de PM de la mascota (las mascotas no tienen técnica fija en todos los combos).</summary>
        public int PetMpCost;

        [JsonIgnore] public string NameKey { get { return "combo." + Id + ".name"; } }
        [JsonIgnore] public string DescKey { get { return "combo." + Id + ".desc"; } }
    }

    public sealed class ConsumableEffect
    {
        public int HealHp;
        public int HealMp;
        public float HealHpPercent;
        public float Revive;
        public List<StatusType> CureStatuses = new List<StatusType>();
        public bool AllAllies;
    }

    public sealed class ItemDef
    {
        public string Id;
        public ItemCategory Category;
        public EquipSlot? Slot;
        public string WeaponType;
        public StatBlock Stats = new StatBlock();
        public Element Element;
        public Dictionary<Element, float> Resistances = new Dictionary<Element, float>();
        public List<StatusType> StatusImmunities = new List<StatusType>();
        /// <summary>Vacío = cualquiera que pueda usar el espacio. Ids de personaje o "mascota".</summary>
        public List<string> AllowedActors = new List<string>();
        public int Price;
        public ConsumableEffect Effect;
        public bool UsableInBattle;
        public bool UsableInField;
        /// <summary>Para Ofrendas: bonificación a la probabilidad de Vínculo.</summary>
        public float BondingBonus;

        [JsonIgnore] public string NameKey { get { return "item." + Id + ".name"; } }
        [JsonIgnore] public string DescKey { get { return "item." + Id + ".desc"; } }
        [JsonIgnore] public bool IsPetEquipment { get { return Slot == EquipSlot.Collar || Slot == EquipSlot.ArmaduraMascota; } }
    }

    public sealed class EvolutionDef
    {
        public string ToSpeciesId;
        public int MinLevel;
        public int MinAffinity;
        public string RequiredItemId;
        public bool ConsumeItem = true;
        /// <summary>Si se indica, sólo puede evolucionar estando en esa época.</summary>
        public string RequiredEra;
        public string RequiredFlag;
    }

    public sealed class CreatureSpeciesDef
    {
        public string Id;
        public string Family;
        public int Stage = 1;
        public string Era;
        public Element Element;
        public Temperament Temperament;
        public StatBlock BaseStats = new StatBlock();
        public StatGrowth Growth = new StatGrowth();
        public List<LearnEntry> Learnset = new List<LearnEntry>();
        public List<EvolutionDef> Evolutions = new List<EvolutionDef>();
        /// <summary>False para especies que nunca pueden vincularse (p. ej. fauna del Cretácico).</summary>
        public bool Bondable = true;
        /// <summary>1 (fácil) a 5 (muy difícil).</summary>
        public int BondDifficulty = 2;
        public string FavoriteOfferingId;
        /// <summary>Bandera narrativa que debe cumplirse antes de poder vincularla (opcional).</summary>
        public string BondCondition;

        [JsonIgnore] public string NameKey { get { return "creature." + Id + ".name"; } }
        [JsonIgnore] public string DescKey { get { return "creature." + Id + ".desc"; } }
    }

    public sealed class EnemyActionDef
    {
        /// <summary>null = ataque básico.</summary>
        public string TechniqueId;
        public int Weight = 1;
        public float MinHpPercent;
        public float MaxHpPercent = 1f;
    }

    public sealed class DropDef
    {
        public string ItemId;
        public float Chance;
    }

    public sealed class EnemyDef
    {
        public string Id;
        public int Level = 1;
        public Element Element;
        public StatBlock Stats = new StatBlock();
        public Dictionary<Element, float> Resistances = new Dictionary<Element, float>();
        public List<StatusType> StatusImmunities = new List<StatusType>();
        public List<EnemyActionDef> Actions = new List<EnemyActionDef>();
        public int Xp;
        public int Money;
        public List<DropDef> Drops = new List<DropDef>();
        public bool IsBoss;
        /// <summary>Pista visual para el prototipo (forma/color provisional).</summary>
        public string ModelHint;

        [JsonIgnore] public string NameKey { get { return "enemy." + Id + ".name"; } }
    }

    public sealed class EncounterSlot
    {
        public string EnemyId;
        public float X;
        public float Y;
    }

    public sealed class EncounterDef
    {
        public string Id;
        public List<EncounterSlot> Enemies = new List<EncounterSlot>();
        public bool CanFlee = true;
        /// <summary>Regla dramática del jefe (ver DramaticRuleRegistry).</summary>
        public string DramaticRule;
    }

    public sealed class FlagDef
    {
        public string Id;
        public int Min;
        public int Max = 1;
        public int Initial;

        [JsonIgnore] public string DescKey { get { return "flag." + Id + ".desc"; } }
    }
}
