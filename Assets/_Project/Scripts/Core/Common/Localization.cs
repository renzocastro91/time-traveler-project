using System.Collections.Generic;
using System.IO;

namespace TimeTraveler.Core
{
    /// <summary>
    /// Tabla de textos por idioma (StreamingAssets/Localization/{idioma}.json, objeto plano clave→texto).
    /// Si falta una clave en el idioma activo se usa el idioma de respaldo (español); si tampoco existe,
    /// se muestra la clave entre corchetes para detectarla enseguida.
    /// </summary>
    public sealed class Localization
    {
        public const string DefaultLanguage = "es";

        private Dictionary<string, string> table = new Dictionary<string, string>();
        private Dictionary<string, string> fallback = new Dictionary<string, string>();

        public string Language { get; private set; }

        public static Localization LoadFromDirectory(string directory, string language)
        {
            var loc = new Localization();
            loc.fallback = ReadTable(Path.Combine(directory, DefaultLanguage + ".json"));
            loc.SetLanguage(directory, language);
            return loc;
        }

        public static Localization FromTables(string language, Dictionary<string, string> table)
        {
            return new Localization { Language = language, table = table, fallback = table };
        }

        public void SetLanguage(string directory, string language)
        {
            Language = language;
            table = language == DefaultLanguage ? fallback : ReadTable(Path.Combine(directory, language + ".json"));
        }

        private static Dictionary<string, string> ReadTable(string path)
        {
            if (!File.Exists(path)) return new Dictionary<string, string>();
            return Json.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new Dictionary<string, string>();
        }

        public bool Has(string key)
        {
            return key != null && (table.ContainsKey(key) || fallback.ContainsKey(key));
        }

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            string value;
            if (table.TryGetValue(key, out value)) return value;
            if (fallback.TryGetValue(key, out value)) return value;
            return "[" + key + "]";
        }

        public string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        public IEnumerable<string> Keys { get { return fallback.Keys; } }
    }
}
