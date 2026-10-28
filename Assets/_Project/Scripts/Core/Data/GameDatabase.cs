using System;
using System.Collections.Generic;
using System.IO;

namespace TimeTraveler.Core
{
    /// <summary>
    /// Todas las definiciones estáticas del juego, indexadas por Id.
    /// Se carga desde una carpeta con characters.json, techniques.json, combos.json, items.json,
    /// creatures.json, enemies.json, encounters.json, flags.json y dialogues/*.json.
    /// </summary>
    public sealed class GameDatabase
    {
        public readonly Dictionary<string, CharacterDef> Characters = new Dictionary<string, CharacterDef>();
        public readonly Dictionary<string, TechniqueDef> Techniques = new Dictionary<string, TechniqueDef>();
        public readonly Dictionary<string, ComboDef> Combos = new Dictionary<string, ComboDef>();
        public readonly Dictionary<string, ItemDef> Items = new Dictionary<string, ItemDef>();
        public readonly Dictionary<string, CreatureSpeciesDef> Creatures = new Dictionary<string, CreatureSpeciesDef>();
        public readonly Dictionary<string, EnemyDef> Enemies = new Dictionary<string, EnemyDef>();
        public readonly Dictionary<string, EncounterDef> Encounters = new Dictionary<string, EncounterDef>();
        public readonly Dictionary<string, FlagDef> Flags = new Dictionary<string, FlagDef>();
        public readonly Dictionary<string, DialogueDef> Dialogues = new Dictionary<string, DialogueDef>();

        public static GameDatabase LoadFromDirectory(string directory)
        {
            var db = new GameDatabase();
            db.AddAll(LoadList<CharacterDef>(directory, "characters.json"), db.Characters, d => d.Id);
            db.AddAll(LoadList<TechniqueDef>(directory, "techniques.json"), db.Techniques, d => d.Id);
            db.AddAll(LoadList<ComboDef>(directory, "combos.json"), db.Combos, d => d.Id);
            db.AddAll(LoadList<ItemDef>(directory, "items.json"), db.Items, d => d.Id);
            db.AddAll(LoadList<CreatureSpeciesDef>(directory, "creatures.json"), db.Creatures, d => d.Id);
            db.AddAll(LoadList<EnemyDef>(directory, "enemies.json"), db.Enemies, d => d.Id);
            db.AddAll(LoadList<EncounterDef>(directory, "encounters.json"), db.Encounters, d => d.Id);
            db.AddAll(LoadList<FlagDef>(directory, "flags.json"), db.Flags, d => d.Id);

            string dialogueDir = Path.Combine(directory, "dialogues");
            if (Directory.Exists(dialogueDir))
            {
                foreach (string file in Directory.GetFiles(dialogueDir, "*.json"))
                {
                    var dialogue = Json.Deserialize<DialogueDef>(File.ReadAllText(file));
                    db.Dialogues[dialogue.Id] = dialogue;
                }
            }
            return db;
        }

        private static List<T> LoadList<T>(string directory, string fileName)
        {
            string path = Path.Combine(directory, fileName);
            if (!File.Exists(path)) return new List<T>();
            return Json.Deserialize<List<T>>(File.ReadAllText(path)) ?? new List<T>();
        }

        private void AddAll<T>(List<T> items, Dictionary<string, T> target, Func<T, string> getId)
        {
            foreach (var item in items)
            {
                string id = getId(item);
                if (target.ContainsKey(id))
                    throw new InvalidDataException("Id duplicado en datos: " + typeof(T).Name + " '" + id + "'");
                target[id] = item;
            }
        }

        public TechniqueDef Technique(string id) { return Get(Techniques, id, "técnica"); }
        public ItemDef Item(string id) { return Get(Items, id, "objeto"); }
        public CharacterDef Character(string id) { return Get(Characters, id, "personaje"); }
        public CreatureSpeciesDef Species(string id) { return Get(Creatures, id, "criatura"); }
        public EnemyDef Enemy(string id) { return Get(Enemies, id, "enemigo"); }
        public EncounterDef Encounter(string id) { return Get(Encounters, id, "encuentro"); }

        private static T Get<T>(Dictionary<string, T> dict, string id, string kind)
        {
            T value;
            if (id != null && dict.TryGetValue(id, out value)) return value;
            throw new KeyNotFoundException("No existe " + kind + " con id '" + id + "'");
        }

        /// <summary>Comprueba que todas las referencias entre definiciones existen. Devuelve los errores.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            Action<bool, string> check = (ok, msg) => { if (!ok) errors.Add(msg); };

            foreach (var c in Characters.Values)
            {
                foreach (var entry in c.Learnset)
                    check(Techniques.ContainsKey(entry.TechniqueId), "Personaje " + c.Id + ": técnica inexistente " + entry.TechniqueId);
                foreach (var eq in c.StartingEquipment)
                {
                    ItemDef item;
                    check(Items.TryGetValue(eq.Value, out item), "Personaje " + c.Id + ": equipo inicial inexistente " + eq.Value);
                    if (item != null)
                        check(EquipmentRules.CanEquip(item, eq.Key, c), "Personaje " + c.Id + ": no puede equipar " + eq.Value + " en " + eq.Key);
                }
            }

            foreach (var combo in Combos.Values)
            {
                check(combo.Participants.Count >= 2, "Combo " + combo.Id + ": necesita al menos 2 participantes");
                foreach (var p in combo.Participants)
                {
                    if (!p.IsPet) check(Characters.ContainsKey(p.Actor), "Combo " + combo.Id + ": personaje inexistente " + p.Actor);
                    if (p.TechniqueId != null) check(Techniques.ContainsKey(p.TechniqueId), "Combo " + combo.Id + ": técnica inexistente " + p.TechniqueId);
                }
            }

            foreach (var item in Items.Values)
            {
                if (item.Category == ItemCategory.Equipo) check(item.Slot.HasValue, "Objeto " + item.Id + ": equipo sin espacio");
                if (item.Category == ItemCategory.Consumible) check(item.Effect != null, "Objeto " + item.Id + ": consumible sin efecto");
                foreach (var actor in item.AllowedActors)
                    check(actor == "mascota" || Characters.ContainsKey(actor), "Objeto " + item.Id + ": actor inexistente " + actor);
            }

            foreach (var s in Creatures.Values)
            {
                foreach (var entry in s.Learnset)
                    check(Techniques.ContainsKey(entry.TechniqueId), "Criatura " + s.Id + ": técnica inexistente " + entry.TechniqueId);
                foreach (var evo in s.Evolutions)
                {
                    check(Creatures.ContainsKey(evo.ToSpeciesId), "Criatura " + s.Id + ": evoluciona a especie inexistente " + evo.ToSpeciesId);
                    if (evo.RequiredItemId != null) check(Items.ContainsKey(evo.RequiredItemId), "Criatura " + s.Id + ": objeto de evolución inexistente " + evo.RequiredItemId);
                }
                if (s.FavoriteOfferingId != null) check(Items.ContainsKey(s.FavoriteOfferingId), "Criatura " + s.Id + ": ofrenda inexistente " + s.FavoriteOfferingId);
            }

            foreach (var e in Enemies.Values)
            {
                check(e.Actions.Count > 0, "Enemigo " + e.Id + ": sin acciones");
                foreach (var a in e.Actions)
                    if (a.TechniqueId != null) check(Techniques.ContainsKey(a.TechniqueId), "Enemigo " + e.Id + ": técnica inexistente " + a.TechniqueId);
                foreach (var d in e.Drops)
                    check(Items.ContainsKey(d.ItemId), "Enemigo " + e.Id + ": botín inexistente " + d.ItemId);
            }

            foreach (var enc in Encounters.Values)
            {
                check(enc.Enemies.Count > 0, "Encuentro " + enc.Id + ": vacío");
                foreach (var slot in enc.Enemies)
                    check(Enemies.ContainsKey(slot.EnemyId), "Encuentro " + enc.Id + ": enemigo inexistente " + slot.EnemyId);
                if (enc.DramaticRule != null)
                    check(DramaticRuleRegistry.Exists(enc.DramaticRule), "Encuentro " + enc.Id + ": regla dramática desconocida " + enc.DramaticRule);
            }

            foreach (var dialogue in Dialogues.Values)
            {
                check(dialogue.Find(dialogue.Start) != null, "Diálogo " + dialogue.Id + ": nodo inicial inexistente");
                foreach (var node in dialogue.Nodes)
                {
                    if (node.Next != null) check(dialogue.Find(node.Next) != null, "Diálogo " + dialogue.Id + "/" + node.Id + ": next inexistente " + node.Next);
                    foreach (var choice in node.Choices)
                        if (choice.Next != null) check(dialogue.Find(choice.Next) != null, "Diálogo " + dialogue.Id + "/" + node.Id + ": opción hacia nodo inexistente " + choice.Next);
                    foreach (var effect in AllEffects(node))
                        check(Flags.ContainsKey(effect.Flag), "Diálogo " + dialogue.Id + "/" + node.Id + ": bandera inexistente " + effect.Flag);
                }
            }
            return errors;
        }

        private static IEnumerable<FlagEffect> AllEffects(DialogueNode node)
        {
            foreach (var e in node.Effects) yield return e;
            foreach (var c in node.Choices)
                foreach (var e in c.Effects) yield return e;
        }

        /// <summary>Todas las claves de localización que el contenido necesita (para detectar textos faltantes).</summary>
        public IEnumerable<string> RequiredLocalizationKeys()
        {
            foreach (var c in Characters.Values) { yield return c.NameKey; yield return c.BioKey; }
            foreach (var t in Techniques.Values) { yield return t.NameKey; yield return t.DescKey; }
            foreach (var c in Combos.Values) { yield return c.NameKey; yield return c.DescKey; }
            foreach (var i in Items.Values) { yield return i.NameKey; yield return i.DescKey; }
            foreach (var s in Creatures.Values) { yield return s.NameKey; yield return s.DescKey; }
            foreach (var e in Enemies.Values) yield return e.NameKey;
            foreach (var f in Flags.Values) yield return f.DescKey;
            foreach (var d in Dialogues.Values)
                foreach (var n in d.Nodes)
                {
                    if (!string.IsNullOrEmpty(n.SpeakerKey)) yield return n.SpeakerKey;
                    if (!string.IsNullOrEmpty(n.TextKey)) yield return n.TextKey;
                    foreach (var ch in n.Choices) yield return ch.TextKey;
                }
        }
    }
}
