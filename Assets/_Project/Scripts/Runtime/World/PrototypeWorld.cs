using System.Collections.Generic;
using TimeTraveler.Core;
using UnityEngine;

namespace TimeTraveler
{
    /// <summary>
    /// Mapa de PRUEBA del Acto I construido por código, para jugar el núcleo sin escenas ni arte:
    ///  · Sur:   Instituto Público del Muelle (2000) — inicio, profesora (E02), cachorros callejeros.
    ///  · Oeste: Estación de bombeo N.º 4 (2000) — Marta (E03), Huecos y el jefe «El Hueco».
    ///  · Este:  Puerto Niebla (2026) — Lía, drones, vendedor y el jefe «La Fe de Erratas».
    ///  · Norte: Nudo de pruebas (Otras Orillas) — Diapasón y criaturas de varias épocas para el Vínculo.
    /// Cuando existan escenas reales, este constructor deja de usarse (no se ejecuta si la escena
    /// ya contiene un objeto llamado "WorldRoot").
    /// </summary>
    public sealed class PrototypeWorld : MonoBehaviour
    {
        public static PrototypeWorld Instance { get; private set; }
        public static readonly Vector3 SpawnPoint = new Vector3(0, 0.1f, -30f);

        private struct EraRegion
        {
            public Rect Area;
            public string Era;
        }

        private readonly List<EraRegion> regions = new List<EraRegion>();
        private Transform dynamicRoot;

        public static void EnsureBuilt()
        {
            if (GameObject.Find("WorldRoot") != null || Instance != null) return;
            var root = new GameObject("WorldRoot");
            root.AddComponent<PrototypeWorld>().Build();
        }

        private void Build()
        {
            Instance = this;
            SetupCameraAndLight();

            var partyRoot = new GameObject("Party");
            partyRoot.AddComponent<PartyVisuals>();

            Floor(new Vector3(0, -0.5f, 0), new Vector3(90, 1, 90), new Color(0.32f, 0.42f, 0.3f));
            // Instituto 2000 (sur)
            Region(new Rect(-15, -40, 30, 25), "2000", new Color(0.62f, 0.55f, 0.45f), "Instituto Público del Muelle · 2000");
            Block(new Vector3(0, 3, -38), new Vector3(24, 6, 3), new Color(0.75f, 0.68f, 0.55f));
            Block(new Vector3(-12, 2, -28), new Vector3(3, 4, 14), new Color(0.75f, 0.68f, 0.55f));
            Block(new Vector3(8, 0.05f, -26), new Vector3(10, 0.1f, 6), new Color(0.35f, 0.38f, 0.45f)); // cancha agrietada
            // Estación de bombeo (oeste)
            Region(new Rect(-40, -15, 25, 30), "2000", new Color(0.45f, 0.47f, 0.5f), "Estación de bombeo N.º 4 · 2000");
            Block(new Vector3(-38, 2.5f, 0), new Vector3(3, 5, 28), new Color(0.4f, 0.42f, 0.46f));
            Block(new Vector3(-24, 1, -13), new Vector3(8, 2, 2), new Color(0.3f, 0.32f, 0.35f));
            Block(new Vector3(-20, 0.6f, 10), new Vector3(1.5f, 1.2f, 1.5f), new Color(0.5f, 0.35f, 0.25f));
            // Puerto Niebla 2026 (este)
            Region(new Rect(12, -15, 28, 32), "2026", new Color(0.3f, 0.35f, 0.45f), "Puerto Niebla · 28 de octubre de 2026");
            for (int i = 0; i < 4; i++) Block(new Vector3(38, 4 + i, -10 + i * 7), new Vector3(3, 8 + i * 2, 4), new Color(0.2f, 0.25f, 0.35f));
            Block(new Vector3(26, 2.5f, 15), new Vector3(10, 5, 2), new Color(0.15f, 0.5f, 0.6f)); // pantallas
            // Nudo de pruebas (norte)
            Region(new Rect(-15, 10, 30, 30), "orillas", new Color(0.4f, 0.32f, 0.5f), "Nudo de pruebas · Las Otras Orillas");

            dynamicRoot = new GameObject("Dynamic").transform;
            dynamicRoot.SetParent(transform, false);
            BuildDynamic();
        }

        /// <summary>Recrea PNJ, cofres, enemigos y criaturas (al empezar o cargar partida).</summary>
        public void ResetDynamic()
        {
            foreach (Transform child in dynamicRoot) Destroy(child.gameObject);
            BuildDynamic();
        }

        private void BuildDynamic()
        {
            // Instituto
            Npc("profesora", new Vector3(4, 0, -22), new Color(0.5f, 0.3f, 0.3f), "e02_profesora");
            Chest("cofre_aula", new Vector3(-6, 0, -24), "venda", 3);
            Chest("cofre_cancha", new Vector3(10, 0, -24), "gorra_de_lana", 1);
            Wild("wild_tropel_1", "tropel", 3, new Vector3(-4, 0, -18));
            Wild("wild_tropel_2", "tropel", 4, new Vector3(6, 0, -16));
            // Estación de bombeo
            Npc("marta", new Vector3(-16, 0, -8), new Color(0.3f, 0.45f, 0.35f), "e03_marta");
            Save(new Vector3(-14, 0, 4));
            Enemy("map_huecos_1", "enc_estacion_huecos", new Vector3(-22, 0, 2));
            Enemy("map_mixto_1", "enc_estacion_mixto", new Vector3(-28, 0, -6));
            Enemy("map_huecos_2", "enc_estacion_huecos", new Vector3(-32, 0, 4));
            Enemy("map_boss_hueco", "enc_boss_hueco", new Vector3(-30, 0, 11));
            Chest("cofre_galeria", new Vector3(-34, 0, -11), "collar_de_resonancia", 1);
            // 2026
            Npc("lia", new Vector3(16, 0, -6), Placeholders.CharacterColors["lia"], "proto_lia");
            Npc("vendedor", new Vector3(15, 0, 8), new Color(0.55f, 0.5f, 0.35f), "proto_vendedor");
            Save(new Vector3(18, 0, -12));
            Enemy("map_drones", "enc_2026_drones", new Vector3(24, 0, 4));
            Enemy("map_archivo", "enc_2026_archivo", new Vector3(30, 0, -4));
            Enemy("map_boss_erratas", "enc_boss_erratas", new Vector3(32, 0, 10));
            Chest("cofre_2026", new Vector3(20, 0, 13), "espada_de_bronce", 1);
            Wild("wild_pipistrelo", "pipistrelo", 6, new Vector3(28, 0, -10));
            // Nudo de pruebas
            Npc("diapason", new Vector3(0, 0, 14), new Color(0.8f, 0.6f, 0.4f), "proto_diapason");
            Chest("cofre_nudo", new Vector3(-10, 0, 14), "cable_trenzado", 1);
            Chest("cofre_nudo_2", new Vector3(10, 0, 14), "malla_ligera", 1);
            Wild("wild_galapago", "galapago_olivar", 4, new Vector3(-8, 0, 22));
            Wild("wild_nutrilla", "nutrilla", 4, new Vector3(-2, 0, 30));
            Wild("wild_ascuela", "ascuela", 5, new Vector3(6, 0, 24));
            Wild("wild_grillo", "grillo_de_laton", 5, new Vector3(9, 0, 32));
            Wild("wild_lumedusa", "lumedusa", 5, new Vector3(-10, 0, 34));
            Wild("wild_cria", "cria_emplumada", 1, new Vector3(2, 0, 36));
            Enemy("map_lira_jabali", "enc_lira_jabali", new Vector3(12, 0, 20));
            Enemy("map_lira_sellados", "enc_lira_sellados", new Vector3(-12, 0, 28));
        }

        public void ApplyClearedEncounters(GameState state)
        {
            ResetDynamic();
            var cleared = new HashSet<string>(state.Story.ClearedEncounters);
            foreach (var enemy in dynamicRoot.GetComponentsInChildren<MapEnemy>(true))
                if (cleared.Contains(enemy.UniqueId)) Destroy(enemy.gameObject);
            foreach (var wild in dynamicRoot.GetComponentsInChildren<WildCreature>(true))
                if (cleared.Contains(wild.UniqueId)) Destroy(wild.gameObject);
            foreach (var chest in dynamicRoot.GetComponentsInChildren<ChestInteractable>(true))
                chest.Refresh();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            var leader = PartyVisuals.Instance != null ? PartyVisuals.Instance.Leader : null;
            if (gm == null || gm.State == null || leader == null) return;
            var p = new Vector2(leader.position.x, leader.position.z);
            foreach (var r in regions)
                if (r.Area.Contains(p)) { gm.State.Location.Era = r.Era; return; }
        }

        // ------------------------------------------------------------------ construcción

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
            cam.backgroundColor = new Color(0.08f, 0.1f, 0.18f);
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

        private void Floor(Vector3 pos, Vector3 size, Color color)
        {
            var go = Placeholders.Primitive(PrimitiveType.Cube, transform, pos, size, color, true);
            go.name = "Floor";
        }

        private void Region(Rect area, string era, Color color, string sign)
        {
            regions.Add(new EraRegion { Area = area, Era = era });
            var tile = Placeholders.Primitive(PrimitiveType.Cube, transform, new Vector3(area.center.x, 0.01f, area.center.y), new Vector3(area.width, 0.02f, area.height), color);
            tile.name = "Region_" + era;
            var label = new GameObject("Sign", typeof(TextMesh));
            label.transform.SetParent(transform, false);
            label.transform.position = new Vector3(area.center.x, 0.05f, area.yMin + 2f);
            label.transform.rotation = Quaternion.Euler(90, 0, 0);
            var text = label.GetComponent<TextMesh>();
            text.text = sign;
            text.font = UIKit.Font;
            text.characterSize = 0.25f;
            text.fontSize = 48;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 1f, 1f, 0.75f);
            label.GetComponent<MeshRenderer>().sharedMaterial = UIKit.Font.material;
        }

        private void Block(Vector3 pos, Vector3 size, Color color)
        {
            Placeholders.Primitive(PrimitiveType.Cube, transform, pos, size, color, true).name = "Block";
        }

        private T Spawn<T>(string name, Vector3 pos, GameObject model) where T : Component
        {
            model.name = name;
            model.transform.SetParent(dynamicRoot, false);
            model.transform.position = pos;
            var col = model.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.9f, 0);
            col.height = 1.8f;
            col.radius = 0.5f;
            return model.AddComponent<T>();
        }

        private void Npc(string id, Vector3 pos, Color color, string dialogueId)
        {
            GameObject model;
            if (Placeholders.CharacterColors.ContainsKey(id)) model = Placeholders.Character(id);
            else
            {
                model = new GameObject();
                model.AddComponent<ModelSlot>().ModelId = "npc/" + id;
                Placeholders.Primitive(PrimitiveType.Capsule, model.transform, new Vector3(0, 0.8f, 0), new Vector3(0.7f, 0.8f, 0.7f), color);
                Placeholders.Primitive(PrimitiveType.Sphere, model.transform, new Vector3(0, 1.8f, 0), Vector3.one * 0.5f, new Color(0.9f, 0.78f, 0.66f));
            }
            var npc = Spawn<NpcInteractable>("NPC_" + id, pos, model);
            npc.DialogueId = dialogueId;
            npc.transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        private void Chest(string uniqueId, Vector3 pos, string itemId, int qty)
        {
            var model = new GameObject();
            model.AddComponent<ModelSlot>().ModelId = "prop/chest";
            Placeholders.Primitive(PrimitiveType.Cube, model.transform, new Vector3(0, 0.35f, 0), new Vector3(1f, 0.7f, 0.7f), new Color(0.55f, 0.35f, 0.15f));
            var lidPivot = new GameObject("Lid").transform;
            lidPivot.SetParent(model.transform, false);
            lidPivot.localPosition = new Vector3(0, 0.7f, -0.35f);
            Placeholders.Primitive(PrimitiveType.Cube, lidPivot, new Vector3(0, 0.08f, 0.35f), new Vector3(1.05f, 0.16f, 0.75f), new Color(0.75f, 0.55f, 0.2f));
            var chest = Spawn<ChestInteractable>("Chest_" + uniqueId, pos, model);
            chest.PromptKey = "interact.open";
            chest.UniqueId = uniqueId;
            chest.ItemId = itemId;
            chest.Quantity = qty;
            chest.Refresh();
        }

        private void Save(Vector3 pos)
        {
            var model = new GameObject();
            model.AddComponent<ModelSlot>().ModelId = "prop/save_point";
            Placeholders.Primitive(PrimitiveType.Cylinder, model.transform, new Vector3(0, 0.1f, 0), new Vector3(1.4f, 0.1f, 1.4f), new Color(0.3f, 0.3f, 0.4f));
            var crystal = Placeholders.Primitive(PrimitiveType.Cube, model.transform, new Vector3(0, 1.3f, 0), new Vector3(0.5f, 0.9f, 0.5f), new Color(0.5f, 0.9f, 1f));
            crystal.transform.localRotation = Quaternion.Euler(45, 0, 45);
            crystal.AddComponent<Spin>();
            Spawn<SavePoint>("SavePoint", pos, model).PromptKey = "explore.saved_point";
        }

        private void Wild(string uniqueId, string speciesId, int level, Vector3 pos)
        {
            var species = GameManager.Instance.Db.Species(speciesId);
            var wild = Spawn<WildCreature>("Wild_" + uniqueId, pos, Placeholders.Creature(species));
            wild.UniqueId = uniqueId;
            wild.SpeciesId = speciesId;
            wild.Level = level;
            wild.PromptKey = "bond.title";
        }

        private void Enemy(string uniqueId, string encounterId, Vector3 pos)
        {
            var db = GameManager.Instance.Db;
            var encounter = db.Encounter(encounterId);
            var model = Placeholders.Enemy(db.Enemy(encounter.Enemies[0].EnemyId));
            var enemy = Spawn<MapEnemy>("Enemy_" + uniqueId, pos, model);
            enemy.UniqueId = uniqueId;
            enemy.EncounterId = encounterId;
            // Los jefes no persiguen: esperan a que el grupo decida enfrentarlos.
            if (encounter.DramaticRule != null) enemy.ChaseRadius = 0f;
        }
    }

    /// <summary>Rotación decorativa (cristales de guardado).</summary>
    public sealed class Spin : MonoBehaviour
    {
        public float Speed = 60f;
        private void Update() { transform.Rotate(0, Speed * Time.deltaTime, 0, Space.World); }
    }
}
