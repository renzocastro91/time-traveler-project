using System;

namespace TimeTraveler.Core
{
    /// <summary>
    /// Acciones que disparan los diálogos. Las que sólo cambian la partida se ejecutan aquí; las que
    /// necesitan la capa visual (viajar, combatir, puzzles, ficha de Cierre, fin de acto, encadenar
    /// diálogos) se devuelven como "diferidas" para que Unity las ejecute al terminar el diálogo.
    ///
    ///   give:objeto[:n]  take:objeto[:n]  money:±n  recruit:personaje[:nivel]  scene:E05
    ///   absent:id  heal  nudo:id  resolve:id
    ///   (diferidas) travel:mapa[:punto]  battle:encuentro  puzzle:id  cierre:id  dialogue:id  endact:n
    /// </summary>
    public static class StoryActions
    {
        public static bool IsDeferred(string action)
        {
            string verb = Verb(action);
            return verb == "travel" || verb == "battle" || verb == "puzzle" || verb == "cierre" || verb == "dialogue" || verb == "endact";
        }

        private static string Verb(string action)
        {
            int colon = action.IndexOf(':');
            return colon < 0 ? action : action.Substring(0, colon);
        }

        /// <summary>Ejecuta una acción inmediata. Devuelve false si es diferida o desconocida.</summary>
        public static bool Execute(GameDatabase db, GameState state, string action)
        {
            string[] p = action.Split(':');
            switch (p[0])
            {
                case "give":
                    state.Inventory.Add(p[1], p.Length > 2 ? int.Parse(p[2]) : 1);
                    return true;
                case "take":
                    state.Inventory.Remove(p[1], Math.Min(state.Inventory.Count(p[1]), p.Length > 2 ? int.Parse(p[2]) : 1));
                    return true;
                case "money":
                    state.Inventory.Money = Math.Max(0, state.Inventory.Money + int.Parse(p[1]));
                    return true;
                case "recruit":
                    state.Recruit(db, p[1], p.Length > 2 ? int.Parse(p[2]) : 1);
                    return true;
                case "scene":
                    StoryDirector.AdvanceTo(state, p[1]);
                    return true;
                case "absent":
                    if (!state.Story.Absents.Contains(p[1])) state.Story.Absents.Add(p[1]);
                    return true;
                case "resolve":
                    if (!state.Story.ClearedEncounters.Contains(p[1])) state.Story.ClearedEncounters.Add(p[1]);
                    return true;
                case "nudo":
                    CierreService.BeginNudo(state, p[1]);
                    return true;
                case "heal":
                    HealAll(db, state);
                    return true;
                default:
                    return false;
            }
        }

        public static void HealAll(GameDatabase db, GameState state)
        {
            foreach (var c in state.Characters.Values)
            {
                var stats = StatCalculator.ForCharacter(db, c);
                c.Hp = stats.Get(StatType.MaxHp);
                c.Mp = stats.Get(StatType.MaxMp);
            }
            foreach (var cr in state.Sanctuary.Creatures)
            {
                var stats = StatCalculator.ForCreature(db, cr);
                cr.Hp = stats.Get(StatType.MaxHp);
                cr.Mp = stats.Get(StatType.MaxMp);
            }
        }
    }
}
