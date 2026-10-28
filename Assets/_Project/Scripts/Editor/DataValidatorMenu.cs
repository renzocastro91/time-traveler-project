using System.IO;
using System.Linq;
using TimeTraveler.Core;
using UnityEditor;
using UnityEngine;

namespace TimeTraveler.EditorTools
{
    /// <summary>Menú «Time Traveler» del editor: validación de datos y accesos rápidos.</summary>
    public static class DataValidatorMenu
    {
        [MenuItem("Time Traveler/Validar datos y textos")]
        public static void Validate()
        {
            string data = Path.Combine(Application.streamingAssetsPath, "Data");
            string loc = Path.Combine(Application.streamingAssetsPath, "Localization");
            GameDatabase db;
            try
            {
                db = GameDatabase.LoadFromDirectory(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError("No se pudieron leer los datos: " + e.Message);
                return;
            }

            var errors = db.Validate();
            var texts = Localization.LoadFromDirectory(loc, Localization.DefaultLanguage);
            var missing = db.RequiredLocalizationKeys().Where(k => !texts.Has(k)).Distinct().ToList();

            foreach (var e in errors) Debug.LogError("[Datos] " + e);
            foreach (var k in missing) Debug.LogWarning("[Texto faltante] " + k);

            string summary = string.Format("{0} personajes · {1} técnicas · {2} combos · {3} objetos · {4} criaturas · {5} enemigos\n{6} errores · {7} textos faltantes",
                db.Characters.Count, db.Techniques.Count, db.Combos.Count, db.Items.Count, db.Creatures.Count, db.Enemies.Count, errors.Count, missing.Count);
            Debug.Log("[Time Traveler] " + summary);
            EditorUtility.DisplayDialog("Validación de datos", summary, "OK");
        }

        [MenuItem("Time Traveler/Abrir carpeta de partidas guardadas")]
        public static void OpenSaves()
        {
            string dir = Path.Combine(Application.persistentDataPath, "saves");
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
        }
    }
}
