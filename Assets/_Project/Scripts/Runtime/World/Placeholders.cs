using System.Collections.Generic;
using TimeTraveler.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TimeTraveler
{
    /// <summary>
    /// Modelos PROVISIONALES hechos con primitivas. Cada uno lleva un componente ModelSlot con el id
    /// que debe tener el modelo definitivo; cuando lleguen los modelos 3D se reemplaza esta fábrica
    /// (por ejemplo cargando prefabs desde Resources/Models/{id}) sin tocar el resto del juego.
    /// </summary>
    public static class Placeholders
    {
        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        public static Material Mat(Color color)
        {
            Material m;
            if (Materials.TryGetValue(color, out m)) return m;
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            var shader = Shader.Find(srp ? "Universal Render Pipeline/Lit" : "Standard");
            m = new Material(shader) { color = color };
            if (color.a < 1f && !srp)
            {
                // Modo transparente del shader Standard.
                m.SetFloat("_Mode", 3);
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_ALPHABLEND_ON");
                m.renderQueue = 3000;
            }
            Materials[color] = m;
            return m;
        }

        public static GameObject Primitive(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            if (!keepCollider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static Color Darken(Color c, float factor)
        {
            return new Color(c.r * factor, c.g * factor, c.b * factor, 1f);
        }

        public static readonly Dictionary<string, Color> CharacterColors = new Dictionary<string, Color>
        {
            { "violeta", new Color(0.56f, 0.32f, 0.82f) },
            { "angel", new Color(0.22f, 0.42f, 0.86f) },
            { "sura", new Color(0.78f, 0.62f, 0.4f) },
            { "teo", new Color(0.8f, 0.36f, 0.2f) },
            { "lia", new Color(0.92f, 0.86f, 0.5f) },
            { "anouk", new Color(0.3f, 0.72f, 0.8f) },
            { "imen", new Color(0.62f, 0.62f, 0.68f) },
            { "anida", new Color(0.38f, 0.22f, 0.5f) },
        };

        public static Color ElementColor(Element e)
        {
            switch (e)
            {
                case Element.Viento: return new Color(0.6f, 0.95f, 0.75f);
                case Element.Rayo: return new Color(1f, 0.92f, 0.3f);
                case Element.Tierra: return new Color(0.6f, 0.45f, 0.25f);
                case Element.Fuego: return new Color(1f, 0.45f, 0.2f);
                case Element.Luz: return new Color(1f, 1f, 0.85f);
                case Element.Agua: return new Color(0.25f, 0.55f, 1f);
                case Element.Metal: return new Color(0.7f, 0.72f, 0.78f);
                case Element.Vacio: return new Color(0.25f, 0.15f, 0.35f);
                default: return Color.gray;
            }
        }

        /// <summary>Humano provisional: cuerpo, cabeza y una "nariz" que indica hacia dónde mira.</summary>
        public static GameObject Character(string id)
        {
            var root = new GameObject("PJ_" + id);
            root.AddComponent<ModelSlot>().ModelId = "char/" + id;
            Color c;
            if (!CharacterColors.TryGetValue(id, out c)) c = Color.white;
            Primitive(PrimitiveType.Capsule, root.transform, new Vector3(0, 0.75f, 0), new Vector3(0.7f, 0.75f, 0.7f), c);
            Primitive(PrimitiveType.Sphere, root.transform, new Vector3(0, 1.75f, 0), Vector3.one * 0.55f, new Color(0.96f, 0.82f, 0.7f));
            Primitive(PrimitiveType.Cube, root.transform, new Vector3(0, 1.72f, 0.28f), new Vector3(0.12f, 0.12f, 0.12f), Darken(c, 0.6f));
            return root;
        }

        public static GameObject Creature(CreatureSpeciesDef species)
        {
            var root = new GameObject("CR_" + species.Id);
            root.AddComponent<ModelSlot>().ModelId = "creature/" + species.Id;
            var color = ElementColor(species.Element);
            float s = 0.45f + 0.15f * species.Stage;
            Primitive(PrimitiveType.Sphere, root.transform, new Vector3(0, s * 0.6f, 0), new Vector3(s, s * 0.8f, s * 1.2f), color);
            Primitive(PrimitiveType.Sphere, root.transform, new Vector3(0, s * 0.95f, s * 0.55f), Vector3.one * s * 0.5f, Darken(color, 0.85f));
            return root;
        }

        public static GameObject Enemy(EnemyDef def)
        {
            var root = new GameObject("EN_" + def.Id);
            root.AddComponent<ModelSlot>().ModelId = "enemy/" + def.Id;
            var color = ElementColor(def.Element);
            float size = def.IsBoss ? 2.4f : 1f;
            switch (def.ModelHint)
            {
                case "hueco":
                case "boss_hueco":
                    Primitive(PrimitiveType.Sphere, root.transform, new Vector3(0, size, 0), Vector3.one * size * 1.2f, new Color(0.1f, 0.05f, 0.15f, 0.75f));
                    Primitive(PrimitiveType.Sphere, root.transform, new Vector3(0, size, size * 0.45f), Vector3.one * size * 0.3f, new Color(0.9f, 0.9f, 1f));
                    break;
                case "eel":
                    Primitive(PrimitiveType.Capsule, root.transform, new Vector3(0, 0.4f, 0), new Vector3(0.4f, 1f, 0.4f), color).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case "drone":
                    Primitive(PrimitiveType.Cube, root.transform, new Vector3(0, 1.6f, 0), new Vector3(0.9f, 0.25f, 0.9f), color);
                    Primitive(PrimitiveType.Cylinder, root.transform, new Vector3(0, 1.8f, 0), new Vector3(1.2f, 0.02f, 1.2f), Color.gray);
                    break;
                case "robot":
                case "boss_erratas":
                    Primitive(PrimitiveType.Cube, root.transform, new Vector3(0, 0.9f * size, 0), new Vector3(0.9f, 1.8f, 0.7f) * size, color);
                    Primitive(PrimitiveType.Cube, root.transform, new Vector3(0, 1.95f * size, 0), new Vector3(0.6f, 0.4f, 0.5f) * size, Darken(color, 0.7f));
                    break;
                case "boar":
                    Primitive(PrimitiveType.Cube, root.transform, new Vector3(0, 0.55f, 0), new Vector3(0.9f, 0.8f, 1.6f), new Color(0.35f, 0.25f, 0.18f));
                    break;
                default:
                    Primitive(PrimitiveType.Capsule, root.transform, new Vector3(0, 0.9f * size, 0), new Vector3(0.7f, 0.9f, 0.7f) * size, color);
                    break;
            }
            return root;
        }
    }

    /// <summary>Marca qué modelo definitivo debe ir en este lugar (para el reemplazo por arte final).</summary>
    public sealed class ModelSlot : MonoBehaviour
    {
        public string ModelId;
    }
}
