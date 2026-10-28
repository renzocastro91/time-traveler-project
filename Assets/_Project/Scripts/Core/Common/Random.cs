namespace TimeTraveler.Core
{
    /// <summary>Fuente de azar inyectable, para que el combate sea reproducible en pruebas.</summary>
    public interface IRandom
    {
        /// <summary>Valor en [0, 1).</summary>
        float Value();

        /// <summary>Entero en [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);
    }

    public sealed class SystemRandom : IRandom
    {
        private readonly System.Random random;

        public SystemRandom() { random = new System.Random(); }
        public SystemRandom(int seed) { random = new System.Random(seed); }

        public float Value() { return (float)random.NextDouble(); }
        public int Range(int minInclusive, int maxExclusive) { return random.Next(minInclusive, maxExclusive); }
    }
}
