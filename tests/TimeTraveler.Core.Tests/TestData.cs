using System.IO;
using TimeTraveler.Core;

namespace TimeTraveler.Core.Tests
{
    /// <summary>Carga los datos reales del juego (StreamingAssets) una sola vez para todas las pruebas.</summary>
    public static class TestData
    {
        private static GameDatabase database;
        private static Localization localization;

        public static string RepoRoot
        {
            get
            {
                var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets", "StreamingAssets")))
                    dir = dir.Parent;
                return dir.FullName;
            }
        }

        public static string DataDir { get { return Path.Combine(RepoRoot, "Assets", "StreamingAssets", "Data"); } }
        public static string LocDir { get { return Path.Combine(RepoRoot, "Assets", "StreamingAssets", "Localization"); } }

        public static GameDatabase Db
        {
            get { return database ?? (database = GameDatabase.LoadFromDirectory(DataDir)); }
        }

        public static Localization Loc
        {
            get { return localization ?? (localization = Localization.LoadFromDirectory(LocDir, "es")); }
        }

        /// <summary>Azar fijo: siempre devuelve el mismo valor (0.5 = sin crítico, acierta, variación neutra).</summary>
        public sealed class FixedRandom : IRandom
        {
            private readonly float value;
            public FixedRandom(float value) { this.value = value; }
            public float Value() { return value; }
            public int Range(int minInclusive, int maxExclusive) { return minInclusive; }
        }
    }
}
