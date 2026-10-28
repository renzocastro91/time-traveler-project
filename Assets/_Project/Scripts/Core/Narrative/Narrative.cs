using System;
using System.Collections.Generic;

namespace TimeTraveler.Core
{
    /// <summary>
    /// Banderas narrativas del guion (TESTIMONIOS, CONSENTIMIENTO, PLURALIDAD, DEUDA, CONFIANZA_VA,
    /// HUELGA_1891, etc.). Los rangos vienen de flags.json y se respetan al modificarlas.
    /// </summary>
    public sealed class FlagStore
    {
        public Dictionary<string, int> Values = new Dictionary<string, int>();

        public int Get(string flag)
        {
            int value;
            return flag != null && Values.TryGetValue(flag, out value) ? value : 0;
        }

        public void Set(string flag, int value, GameDatabase db = null)
        {
            FlagDef def;
            if (db != null && db.Flags.TryGetValue(flag, out def)) value = Math.Max(def.Min, Math.Min(def.Max, value));
            Values[flag] = value;
        }

        public void Apply(FlagEffect effect, GameDatabase db = null)
        {
            int value = effect.Op == FlagOp.Set ? effect.Value : Get(effect.Flag) + effect.Value;
            Set(effect.Flag, value, db);
        }

        public void Apply(IEnumerable<FlagEffect> effects, GameDatabase db = null)
        {
            if (effects == null) return;
            foreach (var e in effects) Apply(e, db);
        }
    }

    /// <summary>
    /// Evalúa condiciones simples escritas en los datos: "CONFIANZA_VA>=2 && !PACTO_SURA || item.ficha_gabriel".
    /// Admite &&, ||, !, ==, !=, >=, <=, >, &lt;. Sin paréntesis (&& tiene prioridad sobre ||).
    /// Un identificador sin operador es verdadero si vale distinto de cero.
    /// </summary>
    public static class ConditionEvaluator
    {
        private static readonly string[] Operators = { ">=", "<=", "==", "!=", ">", "<" };

        public static bool Evaluate(string condition, Func<string, int> resolve)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;
            foreach (string orPart in condition.Split(new[] { "||" }, StringSplitOptions.None))
            {
                bool all = true;
                foreach (string andPart in orPart.Split(new[] { "&&" }, StringSplitOptions.None))
                {
                    if (!EvaluateTerm(andPart.Trim(), resolve)) { all = false; break; }
                }
                if (all) return true;
            }
            return false;
        }

        /// <summary>Todos los identificadores que usa una condición (para validar datos).</summary>
        public static List<string> Identifiers(string condition)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(condition)) return result;
            foreach (string orPart in condition.Split(new[] { "||" }, StringSplitOptions.None))
            {
                foreach (string andPart in orPart.Split(new[] { "&&" }, StringSplitOptions.None))
                {
                    string term = andPart.Trim();
                    if (term.Length == 0) throw new FormatException("Condición vacía");
                    string name = term.TrimStart('!').Trim();
                    foreach (string op in Operators)
                    {
                        int index = term.IndexOf(op, StringComparison.Ordinal);
                        if (index <= 0) continue;
                        name = term.Substring(0, index).Trim();
                        int.Parse(term.Substring(index + op.Length).Trim());
                        break;
                    }
                    result.Add(name);
                }
            }
            return result;
        }

        private static bool EvaluateTerm(string term, Func<string, int> resolve)
        {
            if (term.Length == 0) throw new FormatException("Condición vacía");
            foreach (string op in Operators)
            {
                int index = term.IndexOf(op, StringComparison.Ordinal);
                if (index <= 0) continue;
                int left = resolve(term.Substring(0, index).Trim());
                int right = int.Parse(term.Substring(index + op.Length).Trim());
                switch (op)
                {
                    case ">=": return left >= right;
                    case "<=": return left <= right;
                    case "==": return left == right;
                    case "!=": return left != right;
                    case ">": return left > right;
                    default: return left < right;
                }
            }
            if (term[0] == '!') return resolve(term.Substring(1).Trim()) == 0;
            return resolve(term) != 0;
        }
    }

    /// <summary>
    /// Máquina de estados de un diálogo, independiente de la interfaz. La capa Unity muestra el nodo
    /// actual, llama a Advance() o Choose(i) y ejecuta las acciones que se le notifican.
    /// </summary>
    public sealed class DialogueRunner
    {
        private readonly GameDatabase db;
        private readonly GameState state;
        private readonly Action<string> onAction;
        private DialogueDef dialogue;

        public DialogueNode Current { get; private set; }
        public bool IsFinished { get { return Current == null; } }

        public DialogueRunner(GameDatabase db, GameState state, Action<string> onAction)
        {
            this.db = db;
            this.state = state;
            this.onAction = onAction;
        }

        public void Start(string dialogueId)
        {
            dialogue = db.Dialogues[dialogueId];
            Enter(dialogue.Start);
        }

        /// <summary>Opciones visibles del nodo actual (las que cumplen su condición).</summary>
        public List<DialogueChoice> VisibleChoices()
        {
            var result = new List<DialogueChoice>();
            if (Current == null) return result;
            foreach (var choice in Current.Choices)
                if (ConditionEvaluator.Evaluate(choice.Condition, state.ResolveVariable)) result.Add(choice);
            return result;
        }

        public void Advance()
        {
            if (Current == null) return;
            if (VisibleChoices().Count > 0) throw new InvalidOperationException("El nodo actual requiere elegir una opción");
            Enter(Current.Next);
        }

        public void Choose(int visibleIndex)
        {
            var choice = VisibleChoices()[visibleIndex];
            state.Flags.Apply(choice.Effects, db);
            Enter(choice.Next);
        }

        private void Enter(string nodeId)
        {
            // Salta nodos cuya condición no se cumple (con límite para evitar ciclos).
            for (int guard = 0; guard < 256; guard++)
            {
                Current = dialogue.Find(nodeId);
                if (Current == null) return;
                if (ConditionEvaluator.Evaluate(Current.Condition, state.ResolveVariable)) break;
                nodeId = Current.Else ?? Current.Next;
            }
            state.Flags.Apply(Current.Effects, db);
            foreach (string action in Current.Actions)
                if (onAction != null) onAction(action);
        }
    }
}
