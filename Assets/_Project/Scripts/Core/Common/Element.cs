using System.Collections.Generic;

namespace TimeTraveler.Core
{
    /// <summary>
    /// Elementos del juego. Violeta=Viento (sonido), Ángel=Rayo, Sura=Tierra, Teo=Fuego,
    /// Lía=Luz, Anouk=Agua, Imen=Metal, Anida=Vacío.
    /// </summary>
    public enum Element
    {
        Neutro,
        Viento,
        Rayo,
        Tierra,
        Fuego,
        Luz,
        Agua,
        Metal,
        Vacio
    }

    /// <summary>
    /// Tabla de afinidades. Ciclo de seis: Fuego > Metal > Viento > Tierra > Rayo > Agua > Fuego.
    /// Luz y Vacío son mutuamente fuertes entre sí. Mismo elemento: resistencia leve.
    /// </summary>
    public static class ElementChart
    {
        public const float Strong = 1.5f;
        public const float Weak = 0.5f;
        public const float Same = 0.75f;
        public const float Normal = 1f;

        private static readonly Dictionary<Element, Element> Beats = new Dictionary<Element, Element>
        {
            { Element.Fuego, Element.Metal },
            { Element.Metal, Element.Viento },
            { Element.Viento, Element.Tierra },
            { Element.Tierra, Element.Rayo },
            { Element.Rayo, Element.Agua },
            { Element.Agua, Element.Fuego },
        };

        public static float Multiplier(Element attack, Element defender)
        {
            if (attack == Element.Neutro || defender == Element.Neutro) return Normal;
            if (attack == defender) return Same;
            if ((attack == Element.Luz && defender == Element.Vacio) || (attack == Element.Vacio && defender == Element.Luz))
                return Strong;

            Element beaten;
            if (Beats.TryGetValue(attack, out beaten) && beaten == defender) return Strong;
            if (Beats.TryGetValue(defender, out beaten) && beaten == attack) return Weak;
            return Normal;
        }
    }
}
