using System.Collections.Generic;
using TimeTraveler.Core;
using UnityEngine;

namespace TimeTraveler
{
    /// <summary>
    /// Construye el mapa actual a partir de su MapDef (StreamingAssets/Data/maps) con modelos
    /// provisionales, y gestiona lo que depende de la historia: objetos que aparecen o desaparecen
    /// según condiciones, salidas, zonas que disparan diálogos y la superposición ANTES/AHORA.
    /// </summary>
    public sealed class WorldBuilder : MonoBehaviour
    {
        public static WorldBuilder Instance { get; private set; }

        private sealed class Conditional
        {
            public GameObject Go;
            public string Condition;
            /// <summary>Id resuelto que oculta el objeto para siempre (enemigos vencidos, criaturas vinculadas).</summary>
            public string ResolvedId;
        }

        private sealed class Zone
        {
            public Rect Area;
            public MapExit Exit;
            public MapTrigger Trigger;
            public Renderer Marker;
        }

        public MapDef Current { get; private set; }

        private Transform mapRoot;
        private readonly List<Conditional> conditionals = new List<Conditional>();
        private readonly List<Zone> zones = new List<Zone>();
        private readonly List<GameObject> antesLayer = new List<GameObject>();
        private Zone lockedZoneShown;
        private bool exitsArmed;

        public bool HasAntesLayer { get { return antesLayer.Count > 0; } }
        public bool OverlayActive { get; private set; }

        public static void EnsureCreated()
        {
            if (Instance != null) return;
            var root = new GameObject("WorldRoot");
            root.AddComponent<WorldBuilder>();
        }

        private void Awake()
        {
            Instance = this;
            SetupCameraAndLight();
            new GameObject("Party").AddComponent<PartyVisuals>();
        }

        private static GameManager Gm { get { return GameManager.Instance; } }
        private bool Check(string condition) { return ConditionEvaluator.Evaluate(condition, Gm.State.ResolveVariable); }

        // ================================================================== carga

        public void Load(string mapId)
        {
            if (mapRoot != null) Destroy(mapRoot.gameObject);
            conditionals.Clear();
            zones.Clear();
            antesLayer.Clear();
            lockedZoneShown = null;
            exitsArmed = false;
            OverlayActive = false;

            Current = Gm.Db.Map(mapId);
            mapRoot = new GameObject("Map_" + mapId).transform;
            mapRoot.SetParent(transform, false);

            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = Hex(Current.SkyColor);
            var floor = Placeholders.Primitive(PrimitiveType.Cube, mapRoot, new Vector3(0, -0.5f, 0), new Vector3(Current.Size[0] + 20f, 1f, Current.Size[1] + 20f), Hex(Current.FloorColor), true);
            floor.name = "Floor";

            foreach (var b in Current.Blocks) BuildBlock(b);
            foreach (var n in Current.Npcs) BuildNpc(n);
            foreach (var c in Current.Chests) BuildChest(c);
            foreach (var e in Current.Enemies) BuildEnemy(e);
            foreach (var w in Current.Wild) BuildWild(w);
            foreach (var p in Current.SavePoints) BuildSavePoint(p);
            foreach (var p in Current.NudoPoints) BuildNudoPoint(p);
            foreach (var x in Current.Exits) zones.Add(new Zone { Area = AreaOf(x.Pos, x.Size), Exit = x, Marker = BuildZoneMarker(x.Pos, x.Size) });
            foreach (var t in Current.Triggers) zones.Add(new Zone { Area = AreaOf(t.Pos, t.Size), Trigger = t });

            Refresh();
        }

        public Vector3 SpawnPosition(string spawn, out Quaternion rotation)
        {
            float[] s;
            if (spawn == null || !Current.Spawns.TryGetValue(spawn, out s)) s = Current.Spawns["default"];
            rotation = Quaternion.Euler(0, s.Length > 2 ? s[2] : 0f, 0);
            return new Vector3(s[0], 0.1f, s[1]);
        }

        /// <summary>Muestra u oculta los objetos según las condiciones de la historia.</summary>
        public void Refresh()
        {
            if (Current == null || Gm.State == null) return;
            var resolved = Gm.State.Story.ClearedEncounters;
            foreach (var c in conditionals)
            {
                if (c.Go == null) continue;
                bool visible = Check(c.Condition) && (c.ResolvedId == null || !resolved.Contains(c.ResolvedId));
                if (c.Go.activeSelf != visible) c.Go.SetActive(visible);
            }
            foreach (var z in zones)
            {
                if (z.Marker == null) continue;
                bool open = Check(z.Exit.Condition);
                z.Marker.gameObject.SetActive(open || z.Exit.LockedTextKey != null);
                z.Marker.sharedMaterial = Placeholders.Mat(open ? new Color(0.5f, 0.9f, 1f, 0.45f) : new Color(1f, 0.35f, 0.3f, 0.35f));
            }
            foreach (var chest in mapRoot.GetComponentsInChildren<ChestInteractable>(true)) chest.Refresh();
        }

        // ================================================================== zonas y superposición

        private void Update()
        {
            var gm = Gm;
            if (gm == null || gm.State == null || Current == null) return;
            var leader = PartyVisuals.Instance != null ? PartyVisuals.Instance.Leader : null;
            if (leader == null) return;

            bool exploring = gm.IsSettledIn(GameMode.Exploration);
            SetOverlay(exploring && HasAntesLayer && GameInput.Instance.Overlay.IsPressed());
            if (!exploring) return;

            var p = new Vector2(leader.position.x, leader.position.z);
            Zone inside = null;
            foreach (var z in zones)
            {
                if (!z.Area.Contains(p)) continue;
                if (z.Trigger != null)
                {
                    string key = "trigger:" + Current.Id + ":" + z.Trigger.Id;
                    if (!Check(z.Trigger.Condition) || (z.Trigger.Once && gm.State.Story.ClearedEncounters.Contains(key))) continue;
                    if (z.Trigger.Once) gm.State.Story.ClearedEncounters.Add(key);
                    gm.StartDialogue(z.Trigger.Dialogue);
                    return;
                }
                inside = z;
            }

            // Las salidas se activan sólo después de haber salido de todas (evita rebotar al llegar).
            if (inside == null) { exitsArmed = true; lockedZoneShown = null; return; }
            if (!exitsArmed) return;
            if (Check(inside.Exit.Condition))
            {
                exitsArmed = false;
                gm.TravelTo(inside.Exit.ToMap, inside.Exit.ToSpawn, null);
            }
            else if (inside.Exit.LockedTextKey != null && lockedZoneShown != inside)
            {
                lockedZoneShown = inside;
                gm.Hud.Toast(GameManager.T(inside.Exit.LockedTextKey));
            }
        }

        private void SetOverlay(bool active)
        {
            if (OverlayActive == active) return;
            OverlayActive = active;
            foreach (var go in antesLayer) if (go != null) go.SetActive(active);
        }

        // ================================================================== construcción

        private void Register(GameObject go, string condition, string resolvedId = null)
        {
            conditionals.Add(new Conditional { Go = go, Condition = condition, ResolvedId = resolvedId });
        }

        private void BuildBlock(MapBlock b)
        {
            var color = Hex(b.Color);
            bool antes = b.Layer == "antes";
            if (antes) color.a = 0.45f;
            var go = Placeholders.Primitive(PrimitiveType.Cube, mapRoot, new Vector3(b.Pos[0], b.Pos[1], b.Pos[2]), new Vector3(b.Size[0], b.Size[1], b.Size[2]), color, !antes);
            go.name = antes ? "Antes" : "Block";
            if (antes) { go.SetActive(false); antesLayer.Add(go); }
        }

        private T Place<T>(GameObject model, float[] pos, float rotation) where T : Component
        {
            model.transform.SetParent(mapRoot, false);
            model.transform.position = new Vector3(pos[0], 0f, pos[1]);
            model.transform.rotation = Quaternion.Euler(0, rotation, 0);
            var col = model.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.9f, 0);
            col.height = 1.8f;
            col.radius = 0.5f;
            return model.AddComponent<T>();
        }

        private void BuildNpc(MapNpc n)
        {
            var npc = Place<NpcInteractable>(NpcModel(n), n.Pos, n.Rotation);
            npc.name = "NPC_" + n.Id;
            npc.DialogueId = n.Dialogue;
            if (n.PromptKey != null) npc.PromptKey = n.PromptKey;
            npc.FaceOnTalk = n.Model == "person" || n.Model == "personaje";
            Register(npc.gameObject, n.Condition);
        }

        private GameObject NpcModel(MapNpc n)
        {
            var color = Hex(n.Color);
            var root = new GameObject();
            switch (n.Model)
            {
                case "personaje":
                    Destroy(root);
                    return Placeholders.Character(n.Id);
                case "object":
                    root.AddComponent<ModelSlot>().ModelId = "prop/" + n.Id;
                    Placeholders.Primitive(PrimitiveType.Cube, root.transform, new Vector3(0, 0.45f, 0), new Vector3(0.9f, 0.9f, 0.9f), color);
                    Placeholders.Primitive(PrimitiveType.Sphere, root.transform, new Vector3(0, 1.3f, 0), Vector3.one * 0.25f, new Color(1f, 0.9f, 0.4f)).AddComponent<Spin>();
                    break;
                case "sign":
                    root.AddComponent<ModelSlot>().ModelId = "prop/" + n.Id;
                    Placeholders.Primitive(PrimitiveType.Cube, root.transform, new Vector3(0, 1.2f, 0), new Vector3(2.4f, 1.6f, 0.2f), color);
                    break;
                case "magnetofono":
                    root.AddComponent<ModelSlot>().ModelId = "prop/magnetofono";
                    Placeholders.Primitive(PrimitiveType.Cube, root.transform, new Vector3(0, 0.5f, 0), new Vector3(1.2f, 1f, 0.8f), new Color(0.35f, 0.32f, 0.3f));
                    Placeholders.Primitive(PrimitiveType.Cylinder, root.transform, new Vector3(-0.3f, 1.02f, 0), new Vector3(0.4f, 0.02f, 0.4f), new Color(0.15f, 0.15f, 0.15f)).AddComponent<Spin>();
                    Placeholders.Primitive(PrimitiveType.Cylinder, root.transform, new Vector3(0.3f, 1.02f, 0), new Vector3(0.4f, 0.02f, 0.4f), new Color(0.15f, 0.15f, 0.15f)).AddComponent<Spin>();
                    break;
                default:
                    root.AddComponent<ModelSlot>().ModelId = "npc/" + n.Id;
                    Placeholders.Primitive(PrimitiveType.Capsule, root.transform, new Vector3(0, 0.8f, 0), new Vector3(0.7f, 0.8f, 0.7f), color);
                    Placeholders.Primitive(PrimitiveType.Sphere, root.transform, new Vector3(0, 1.8f, 0), Vector3.one * 0.5f, new Color(0.9f, 0.78f, 0.66f));
                    break;
            }
            return root;
        }

        private void BuildChest(MapChest c)
        {
            var model = new GameObject();
            model.AddComponent<ModelSlot>().ModelId = "prop/chest";
            Placeholders.Primitive(PrimitiveType.Cube, model.transform, new Vector3(0, 0.35f, 0), new Vector3(1f, 0.7f, 0.7f), new Color(0.55f, 0.35f, 0.15f));
            var lid = new GameObject("Lid").transform;
            lid.SetParent(model.transform, false);
            lid.localPosition = new Vector3(0, 0.7f, -0.35f);
            Placeholders.Primitive(PrimitiveType.Cube, lid, new Vector3(0, 0.08f, 0.35f), new Vector3(1.05f, 0.16f, 0.75f), new Color(0.75f, 0.55f, 0.2f));
            var chest = Place<ChestInteractable>(model, c.Pos, 180f);
            chest.name = "Chest_" + c.Id;
            chest.PromptKey = "interact.open";
            chest.UniqueId = c.Id;
            chest.ItemId = c.ItemId;
            chest.Quantity = c.Quantity;
            Register(chest.gameObject, c.Condition);
        }

        private void BuildEnemy(MapEnemyPlacement e)
        {
            var db = Gm.Db;
            var encounter = db.Encounter(e.Encounter);
            var enemy = Place<MapEnemy>(Placeholders.Enemy(db.Enemy(encounter.Enemies[0].EnemyId)), e.Pos, 180f);
            enemy.name = "Enemy_" + e.Id;
            enemy.UniqueId = e.Id;
            enemy.EncounterId = e.Encounter;
            enemy.Respawns = e.Respawns;
            // Los jefes no persiguen: esperan a que el grupo decida enfrentarlos.
            if (encounter.DramaticRule != null) enemy.ChaseRadius = 0f;
            Register(enemy.gameObject, e.Condition, e.Respawns ? null : e.Id);
        }

        private void BuildWild(MapWild w)
        {
            var species = Gm.Db.Species(w.Species);
            var wild = Place<WildCreature>(Placeholders.Creature(species), w.Pos, 180f);
            wild.name = "Wild_" + w.Id;
            wild.UniqueId = w.Id;
            wild.SpeciesId = w.Species;
            wild.Level = w.Level;
            wild.PromptKey = "bond.title";
            Register(wild.gameObject, w.Condition, w.Id);
        }

        private void BuildSavePoint(MapPoint p)
        {
            var model = new GameObject();
            model.AddComponent<ModelSlot>().ModelId = "prop/save_point";
            Placeholders.Primitive(PrimitiveType.Cylinder, model.transform, new Vector3(0, 0.1f, 0), new Vector3(1.4f, 0.1f, 1.4f), new Color(0.3f, 0.3f, 0.4f));
            var crystal = Placeholders.Primitive(PrimitiveType.Cube, model.transform, new Vector3(0, 1.3f, 0), new Vector3(0.5f, 0.9f, 0.5f), new Color(0.5f, 0.9f, 1f));
            crystal.transform.localRotation = Quaternion.Euler(45, 0, 45);
            crystal.AddComponent<Spin>();
            var save = Place<SavePoint>(model, p.Pos, 0f);
            save.PromptKey = "explore.saved_point";
            Register(save.gameObject, p.Condition);
        }

        private void BuildNudoPoint(MapPoint p)
        {
            var model = new GameObject();
            model.AddComponent<ModelSlot>().ModelId = "prop/nudo";
            for (int i = 0; i < 3; i++)
            {
                var ring = Placeholders.Primitive(PrimitiveType.Cylinder, model.transform, new Vector3(0, 0.02f + i * 0.01f, 0), new Vector3(1f + i * 0.7f, 0.01f, 1f + i * 0.7f), new Color(0.75f, 0.6f, 1f, 0.5f - i * 0.12f));
                ring.AddComponent<Spin>().Speed = 20f + i * 25f;
            }
            var nudo = Place<NudoPoint>(model, p.Pos, 0f);
            nudo.PromptKey = "nudo.prompt";
            Register(nudo.gameObject, p.Condition);
        }

        private Renderer BuildZoneMarker(float[] pos, float[] size)
        {
            var go = Placeholders.Primitive(PrimitiveType.Cube, mapRoot, new Vector3(pos[0], 0.03f, pos[1]), new Vector3(size[0], 0.04f, size[1]), new Color(0.5f, 0.9f, 1f, 0.45f));
            go.name = "Exit";
            return go.GetComponent<Renderer>();
        }

        private static Rect AreaOf(float[] pos, float[] size)
        {
            return new Rect(pos[0] - size[0] / 2f, pos[1] - size[1] / 2f, size[0], size[1]);
        }

        public static Color Hex(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.gray;
        }

        private static void SetupCameraAndLight()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            if (cam.GetComponent<CameraRig>() == null) cam.gameObject.AddComponent<CameraRig>();

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var light = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.shadows = LightShadows.Soft;
                light.color = new Color(1f, 0.95f, 0.88f);
                light.transform.rotation = Quaternion.Euler(50, -35, 0);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.55f);
        }
    }

    /// <summary>Rotación decorativa (cristales de guardado, Nudos, objetos examinables).</summary>
    public sealed class Spin : MonoBehaviour
    {
        public float Speed = 60f;
        private void Update() { transform.Rotate(0, Speed * Time.deltaTime, 0, Space.World); }
    }

    /// <summary>Punto de viaje entre Nudos: con el Diapasón abre la lista de destinos.</summary>
    public sealed class NudoPoint : Interactable
    {
        public override void Interact(PlayerController player)
        {
            if (Gm.State.Inventory.Count("diapason_de_arcilla") <= 0)
            {
                Gm.Hud.Toast(GameManager.T("nudo.need"));
                return;
            }
            Gm.Story.ShowNudoTravel();
        }
    }
}
