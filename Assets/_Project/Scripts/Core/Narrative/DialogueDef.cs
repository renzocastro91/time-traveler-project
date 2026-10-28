using System.Collections.Generic;

namespace TimeTraveler.Core
{
    public enum FlagOp
    {
        Add,
        Set
    }

    public sealed class FlagEffect
    {
        public string Flag;
        public FlagOp Op = FlagOp.Add;
        public int Value = 1;
    }

    public sealed class DialogueChoice
    {
        public string TextKey;
        public string Next;
        public string Condition;
        public List<FlagEffect> Effects = new List<FlagEffect>();
    }

    public sealed class DialogueNode
    {
        public string Id;
        /// <summary>Clave del nombre del hablante (p. ej. "char.violeta.name"); vacío = narrador.</summary>
        public string SpeakerKey;
        public string TextKey;
        /// <summary>Si la condición no se cumple, el nodo se salta y se pasa a Else (o a Next si no hay Else).</summary>
        public string Condition;
        public string Next;
        public string Else;
        public List<DialogueChoice> Choices = new List<DialogueChoice>();
        public List<FlagEffect> Effects = new List<FlagEffect>();
        /// <summary>Acciones al entrar en el nodo (ver StoryActions para la lista completa).</summary>
        public List<string> Actions = new List<string>();
    }

    public sealed class DialogueDef
    {
        public string Id;
        public string Start;
        public List<DialogueNode> Nodes = new List<DialogueNode>();

        public DialogueNode Find(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            foreach (var node in Nodes)
                if (node.Id == nodeId) return node;
            return null;
        }
    }
}
