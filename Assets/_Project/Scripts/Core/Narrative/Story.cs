using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace TimeTraveler.Core
{
    // ------------------------------------------------------------------ escenas y objetivos

    public sealed class ObjectiveDef
    {
        public string Id;
        /// <summary>El objetivo se cumple cuando esta condición es verdadera.</summary>
        public string Condition;
    }

    /// <summary>Una escena del guion (E01…E42) con sus objetivos jugables.</summary>
    public sealed class SceneDef
    {
        public string Id;
        public int Act = 1;
        public List<ObjectiveDef> Objectives = new List<ObjectiveDef>();
        /// <summary>Diálogo que se reproduce solo al cumplir todos los objetivos (opcional).</summary>
        public string OnObjectivesComplete;

        [JsonIgnore] public string TitleKey { get { return "scene." + Id + ".title"; } }
        public string ObjectiveKey(ObjectiveDef o) { return "scene." + Id + ".obj." + o.Id; }
    }

    /// <summary>Consultas sobre el avance de la historia. No guarda estado propio: todo vive en GameState.</summary>
    public static class StoryDirector
    {
        public static SceneDef CurrentScene(GameDatabase db, GameState state)
        {
            SceneDef scene;
            return db.Scenes.TryGetValue(state.Story.CurrentScene, out scene) ? scene : null;
        }

        /// <summary>Primer objetivo aún no cumplido de la escena actual (null si no hay o están todos).</summary>
        public static ObjectiveDef CurrentObjective(GameDatabase db, GameState state)
        {
            var scene = CurrentScene(db, state);
            if (scene == null) return null;
            return scene.Objectives.FirstOrDefault(o => !ConditionEvaluator.Evaluate(o.Condition, state.ResolveVariable));
        }

        /// <summary>
        /// Diálogo automático pendiente: la escena tiene objetivos, están todos cumplidos y su diálogo
        /// final aún no se reprodujo. Al devolverlo queda marcado como reproducido.
        /// </summary>
        public static string TakePendingDialogue(GameDatabase db, GameState state)
        {
            var scene = CurrentScene(db, state);
            if (scene == null || scene.OnObjectivesComplete == null || scene.Objectives.Count == 0) return null;
            if (CurrentObjective(db, state) != null) return null;
            string key = "auto:" + scene.Id;
            if (state.Story.ClearedEncounters.Contains(key)) return null;
            state.Story.ClearedEncounters.Add(key);
            return scene.OnObjectivesComplete;
        }

        public static void AdvanceTo(GameState state, string nextScene)
        {
            if (!state.Story.CompletedScenes.Contains(state.Story.CurrentScene))
                state.Story.CompletedScenes.Add(state.Story.CurrentScene);
            state.Story.CurrentScene = nextScene;
        }
    }

    // ------------------------------------------------------------------ Cierres (ANTES / AHORA)

    /// <summary>
    /// Un Cierre del guion (1.1): al abandonar un Nudo tras una decisión irreversible se actualiza
    /// la Hebra. La ficha muestra ANTES / AHORA, una frase humana y la causa. La variante depende
    /// del valor de la bandera de decisión. Textos: cierre.{id}.cause, .before, .now.{valor}, .person.{valor}.
    /// </summary>
    public sealed class CierreDef
    {
        public string Id;
        public string Scene;
        /// <summary>Época donde se ve el cambio (no la del Nudo).</summary>
        public string EchoEra;
        public string Flag;
        public List<int> Values = new List<int>();
        /// <summary>Diálogo de la decisión: se repite si el jugador elige reconsiderar.</summary>
        public string DecisionDialogue;
        /// <summary>Diálogo tras confirmar (opcional).</summary>
        public string OnConfirmDialogue;
        /// <summary>Escena a la que se avanza al confirmar (opcional).</summary>
        public string NextScene;

        [JsonIgnore] public string CauseKey { get { return "cierre." + Id + ".cause"; } }
        [JsonIgnore] public string BeforeKey { get { return "cierre." + Id + ".before"; } }
        public string NowKey(int value) { return "cierre." + Id + ".now." + value; }
        public string PersonKey(int value) { return "cierre." + Id + ".person." + value; }
    }

    public sealed class CierreRecord
    {
        public string Id;
        public int Value;
        public string Scene;
        public double PlayTimeSeconds;
    }

    /// <summary>Instantánea al entrar en un Nudo: permite reconsiderar decisiones antes de confirmar el Cierre.</summary>
    public sealed class NudoSnapshot
    {
        public string NudoId;
        public Dictionary<string, int> Flags = new Dictionary<string, int>();
        public Dictionary<string, int> Items = new Dictionary<string, int>();
        public int Money;
    }

    public static class CierreService
    {
        public static void BeginNudo(GameState state, string nudoId)
        {
            state.Story.PendingNudo = new NudoSnapshot
            {
                NudoId = nudoId,
                Flags = new Dictionary<string, int>(state.Flags.Values),
                Items = new Dictionary<string, int>(state.Inventory.Items),
                Money = state.Inventory.Money
            };
        }

        /// <summary>
        /// Deshace las decisiones tomadas desde que se entró al Nudo (banderas, objetos entregados).
        /// Sólo posible antes de confirmar el Cierre. Devuelve false si no hay Nudo abierto.
        /// </summary>
        public static bool Reconsider(GameState state)
        {
            var snap = state.Story.PendingNudo;
            if (snap == null) return false;
            state.Flags.Values = new Dictionary<string, int>(snap.Flags);
            state.Inventory.Items = new Dictionary<string, int>(snap.Items);
            state.Inventory.Money = snap.Money;
            return true;
        }

        public static CierreRecord Preview(GameDatabase db, GameState state, string cierreId)
        {
            var def = db.Cierres[cierreId];
            return new CierreRecord { Id = def.Id, Value = state.Flags.Get(def.Flag), Scene = def.Scene, PlayTimeSeconds = state.PlayTimeSeconds };
        }

        /// <summary>
        /// Confirma el Cierre: la decisión queda fijada en la Hebra (salvo en Nueva Hebra+) y se avanza
        /// a la escena siguiente si el Cierre la define. El diálogo de confirmación lo lanza la interfaz.
        /// </summary>
        public static CierreRecord Confirm(GameDatabase db, GameState state, string cierreId)
        {
            var def = db.Cierres[cierreId];
            var record = Preview(db, state, cierreId);
            state.Story.Cierres.RemoveAll(c => c.Id == cierreId);
            state.Story.Cierres.Add(record);
            state.Story.PendingNudo = null;
            if (def.NextScene != null) StoryDirector.AdvanceTo(state, def.NextScene);
            return record;
        }
    }

    // ------------------------------------------------------------------ puzzles

    /// <summary>Qué ocurre al resolver cada puzzle: se fija una bandera y se reproduce un diálogo.</summary>
    public static class Puzzles
    {
        public sealed class Outcome
        {
            public string Flag;
            public string Dialogue;
        }

        public static readonly Dictionary<string, Outcome> OnSolved = new Dictionary<string, Outcome>
        {
            { "cinta", new Outcome { Flag = "CINTA_RESUELTA", Dialogue = "e02_resuelto" } },
            { "signos", new Outcome { Flag = "E07_SIGNOS", Dialogue = "e07_bifurcacion" } },
        };

        public static void MarkSolved(GameDatabase db, GameState state, string puzzleId)
        {
            state.Flags.Set(OnSolved[puzzleId].Flag, 1, db);
        }
    }

    /// <summary>
    /// E02 · La cinta. Hay que ajustar cabezal, frecuencia y velocidad (1-5). Tres recuerdos
    /// encontrados en E01 dan valores contradictorios: cada uno acierta en dos parámetros y falla
    /// en uno. La señal verdadera es la que comparten al menos dos recuerdos ("tres recuerdos
    /// contradictorios pueden formar una señal verdadera").
    /// </summary>
    public static class TapePuzzle
    {
        public const int Dials = 3;
        public const int MinValue = 1;
        public const int MaxValue = 5;

        /// <summary>Casete de 1986, maqueta del puerto y lista del personal: [cabezal, frecuencia, velocidad].</summary>
        public static readonly int[][] Memories =
        {
            new[] { 2, 4, 3 },
            new[] { 2, 1, 5 },
            new[] { 4, 4, 5 },
        };

        public static readonly string[] MemoryFlags = { "INSP_CASETE", "INSP_MAQUETA", "INSP_LISTA" };

        public static int[] Solution()
        {
            var result = new int[Dials];
            for (int d = 0; d < Dials; d++)
            {
                result[d] = Memories.Select(m => m[d]).GroupBy(v => v).OrderByDescending(g => g.Count()).First().Key;
            }
            return result;
        }

        /// <summary>Cuántos parámetros coinciden con la señal verdadera (0-3).</summary>
        public static int Signal(int[] dials)
        {
            var solution = Solution();
            int matches = 0;
            for (int d = 0; d < Dials; d++) if (dials[d] == solution[d]) matches++;
            return matches;
        }

        public static bool IsSolved(int[] dials) { return Signal(dials) == Dials; }
    }

    /// <summary>
    /// E07 · Alfabeto acústico de Sura: ella muestra una secuencia de signos (mano, gesto, huella en
    /// barro, golpe) y el grupo la repite. Se aprende por observación: equivocarse sólo repite la ronda.
    /// </summary>
    public sealed class SignSequencePuzzle
    {
        public const int SignCount = 4;
        public static readonly int[] RoundLengths = { 3, 4, 5 };

        private readonly System.Random random;
        public int Round { get; private set; }
        public int Mistakes { get; private set; }
        public List<int> Sequence { get; private set; }
        public bool IsComplete { get { return Round >= RoundLengths.Length; } }

        public SignSequencePuzzle(int seed)
        {
            random = new System.Random(seed);
            NewSequence();
        }

        private void NewSequence()
        {
            if (IsComplete) { Sequence = new List<int>(); return; }
            Sequence = Enumerable.Range(0, RoundLengths[Round]).Select(_ => random.Next(SignCount)).ToList();
        }

        /// <summary>Comprueba la respuesta completa de la ronda. Acierto → siguiente ronda; error → nueva secuencia de la misma longitud.</summary>
        public bool Submit(IList<int> answer)
        {
            if (IsComplete) return true;
            bool ok = answer.Count == Sequence.Count && !answer.Where((v, i) => v != Sequence[i]).Any();
            if (ok) Round++;
            else Mistakes++;
            NewSequence();
            return ok;
        }
    }
}
