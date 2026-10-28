using System.Collections.Generic;
using Newtonsoft.Json;

namespace TimeTraveler.Core
{
    // Mapas descritos en datos (StreamingAssets/Data/maps/*.json). La capa Unity los construye con
    // modelos provisionales; cuando existan escenas con arte, cada mapa puede apuntar a su escena
    // y conservar aquí sólo la lógica (PNJ, condiciones, salidas, encuentros).
    // Todas las posiciones son [x, z] en metros; "condition" usa la sintaxis de ConditionEvaluator.

    public sealed class MapBlock
    {
        /// <summary>Centro [x, y, z] (los bloques son 3D, a diferencia del resto de posiciones).</summary>
        public float[] Pos;
        /// <summary>Tamaño [ancho, alto, profundidad].</summary>
        public float[] Size;
        public string Color = "#808080";
        /// <summary>"antes" = sólo visible con la superposición ANTES/AHORA (E04).</summary>
        public string Layer;
    }

    public sealed class MapNpc
    {
        public string Id;
        public float[] Pos;
        public float Rotation = 180f;
        public string Dialogue;
        public string Condition;
        /// <summary>person | object | sign | magnetofono | personaje (usa el modelo del personaje Id).</summary>
        public string Model = "person";
        public string Color = "#a08070";
        public string PromptKey;
    }

    public sealed class MapChest
    {
        public string Id;
        public float[] Pos;
        public string ItemId;
        public int Quantity = 1;
        public string Condition;
    }

    public sealed class MapEnemyPlacement
    {
        public string Id;
        public float[] Pos;
        public string Encounter;
        public string Condition;
        /// <summary>Se puede volver a combatir al regresar al mapa (no queda resuelto).</summary>
        public bool Respawns;
    }

    public sealed class MapWild
    {
        public string Id;
        public float[] Pos;
        public string Species;
        public int Level = 3;
        public string Condition;
    }

    public sealed class MapExit
    {
        public float[] Pos;
        public float[] Size = { 2f, 2f };
        public string ToMap;
        public string ToSpawn = "default";
        public string Condition;
        /// <summary>Aviso cuando la salida aún no está disponible (null = la salida no se muestra).</summary>
        public string LockedTextKey;
    }

    public sealed class MapTrigger
    {
        public string Id;
        public float[] Pos;
        public float[] Size = { 3f, 3f };
        public string Dialogue;
        public string Condition;
        public bool Once = true;
    }

    public sealed class MapPoint
    {
        public float[] Pos;
        public string Condition;
    }

    public sealed class MapDef
    {
        public string Id;
        public string Era;
        public float[] Size = { 40f, 40f };
        public string FloorColor = "#556655";
        public string SkyColor = "#141a2e";
        /// <summary>Nombre del punto → [x, z, rotaciónY].</summary>
        public Dictionary<string, float[]> Spawns = new Dictionary<string, float[]>();
        public List<MapBlock> Blocks = new List<MapBlock>();
        public List<MapNpc> Npcs = new List<MapNpc>();
        public List<MapChest> Chests = new List<MapChest>();
        public List<MapEnemyPlacement> Enemies = new List<MapEnemyPlacement>();
        public List<MapWild> Wild = new List<MapWild>();
        public List<MapExit> Exits = new List<MapExit>();
        public List<MapTrigger> Triggers = new List<MapTrigger>();
        public List<MapPoint> SavePoints = new List<MapPoint>();
        /// <summary>Puntos de viaje entre Nudos (requieren el Diapasón).</summary>
        public List<MapPoint> NudoPoints = new List<MapPoint>();
        /// <summary>Si es un Nudo, aparece como destino de viaje una vez visitado y cumplida esta condición.</summary>
        public bool IsNudo;
        public string NudoCondition;

        [JsonIgnore] public string NameKey { get { return "map." + Id + ".name"; } }
    }
}
