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
        public readonly Dictionary<string, MapDef> Maps = new Dictionary<string, MapDef>();
        public readonly Dictionary<string, SceneDef> Scenes = new Dictionary<string, SceneDef>();
        public readonly Dictionary<string, CierreDef> Cierres = new Dictionary<string, CierreDef>();
        /// <summary>Ids de fichas de ausentes conocidas (textos absent.{id}.name / .desc).</summary>
        public readonly List<string> AbsentIds = new List<string>();

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
            db.AddAll(LoadList<SceneDef>(directory, "scenes.json"), db.Scenes, d => d.Id);
            db.AddAll(LoadList<CierreDef>(directory, "cierres.json"), db.Cierres, d => d.Id);
            db.AbsentIds.AddRange(LoadList<string>(directory, "absents.json"));

            string mapDir = Path.Combine(directory, "maps");
            if (Directory.Exists(mapDir))
            {
                foreach (string file in Directory.GetFiles(mapDir, "*.json"))
                {
                    var map = Json.Deserialize<MapDef>(File.ReadAllText(file));
                    if (db.Maps.ContainsKey(map.Id)) throw new InvalidDataException("Mapa duplicado: " + map.Id);
                    db.Maps[map.Id] = map;
                }
            }

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
        public MapDef Map(string id) { return Get(Maps, id, "mapa"); }

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

            foreach (var map in Maps.Values) ValidateMap(map, check);

            foreach (var p in Puzzles.OnSolved)
            {
                check(Flags.ContainsKey(p.Value.Flag), "Puzzle " + p.Key + ": bandera inexistente " + p.Value.Flag);
                check(Dialogues.ContainsKey(p.Value.Dialogue), "Puzzle " + p.Key + ": diálogo inexistente " + p.Value.Dialogue);
            }

            foreach (var scene in Scenes.Values)
            {
                if (scene.OnObjectivesComplete != null)
                    check(Dialogues.ContainsKey(scene.OnObjectivesComplete), "Escena " + scene.Id + ": diálogo inexistente " + scene.OnObjectivesComplete);
                foreach (var o in scene.Objectives) CheckCondition(o.Condition, "Escena " + scene.Id + "/" + o.Id, check);
            }

            foreach (var c in Cierres.Values)
            {
                check(Flags.ContainsKey(c.Flag), "Cierre " + c.Id + ": bandera inexistente " + c.Flag);
                check(c.Values.Count > 0, "Cierre " + c.Id + ": sin variantes");
                check(c.DecisionDialogue != null && Dialogues.ContainsKey(c.DecisionDialogue), "Cierre " + c.Id + ": diálogo de decisión inexistente " + c.DecisionDialogue);
                if (c.OnConfirmDialogue != null) check(Dialogues.ContainsKey(c.OnConfirmDialogue), "Cierre " + c.Id + ": diálogo de confirmación inexistente " + c.OnConfirmDialogue);
                if (c.NextScene != null) check(Scenes.ContainsKey(c.NextScene), "Cierre " + c.Id + ": escena siguiente inexistente " + c.NextScene);
            }

            foreach (var dialogue in Dialogues.Values)
            {
                foreach (var node in dialogue.Nodes)
                {
                    CheckCondition(node.Condition, "Diálogo " + dialogue.Id + "/" + node.Id, check);
                    foreach (var choice in node.Choices) CheckCondition(choice.Condition, "Diálogo " + dialogue.Id + "/" + node.Id, check);
                    foreach (var action in node.Actions) CheckAction(action, "Diálogo " + dialogue.Id + "/" + node.Id, check);
                }
                check(dialogue.Find(dialogue.Start) != null, "Diálogo " + dialogue.Id + ": nodo inicial inexistente");
                foreach (var node in dialogue.Nodes)
                {
                    if (node.Next != null) check(dialogue.Find(node.Next) != null, "Diálogo " + dialogue.Id + "/" + node.Id + ": next inexistente " + node.Next);
                    if (node.Else != null) check(dialogue.Find(node.Else) != null, "Diálogo " + dialogue.Id + "/" + node.Id + ": else inexistente " + node.Else);
                    foreach (var choice in node.Choices)
                        if (choice.Next != null) check(dialogue.Find(choice.Next) != null, "Diálogo " + dialogue.Id + "/" + node.Id + ": opción hacia nodo inexistente " + choice.Next);
                    foreach (var effect in AllEffects(node))
                        check(Flags.ContainsKey(effect.Flag), "Diálogo " + dialogue.Id + "/" + node.Id + ": bandera inexistente " + effect.Flag);
                }
            }
            return errors;
        }

        private void ValidateMap(MapDef map, Action<bool, string> check)
        {
            string where = "Mapa " + map.Id;
            check(map.Spawns.ContainsKey("default"), where + ": falta el punto de aparición 'default'");
            foreach (var n in map.Npcs)
            {
                check(Dialogues.ContainsKey(n.Dialogue ?? ""), where + "/" + n.Id + ": diálogo inexistente " + n.Dialogue);
                CheckCondition(n.Condition, where + "/" + n.Id, check);
            }
            foreach (var c in map.Chests) check(Items.ContainsKey(c.ItemId ?? ""), where + "/" + c.Id + ": objeto inexistente " + c.ItemId);
            foreach (var e in map.Enemies)
            {
                check(Encounters.ContainsKey(e.Encounter ?? ""), where + "/" + e.Id + ": encuentro inexistente " + e.Encounter);
                CheckCondition(e.Condition, where + "/" + e.Id, check);
            }
            foreach (var w in map.Wild) check(Creatures.ContainsKey(w.Species ?? ""), where + "/" + w.Id + ": especie inexistente " + w.Species);
            foreach (var x in map.Exits)
            {
                MapDef target;
                check(Maps.TryGetValue(x.ToMap ?? "", out target), where + ": salida a mapa inexistente " + x.ToMap);
                if (target != null) check(target.Spawns.ContainsKey(x.ToSpawn), where + ": salida a punto inexistente " + x.ToMap + "/" + x.ToSpawn);
                CheckCondition(x.Condition, where + "/salida", check);
            }
            foreach (var t in map.Triggers)
            {
                check(Dialogues.ContainsKey(t.Dialogue ?? ""), where + "/" + t.Id + ": diálogo inexistente " + t.Dialogue);
                CheckCondition(t.Condition, where + "/" + t.Id, check);
            }
            foreach (var p in map.NudoPoints) CheckCondition(p.Condition, where + "/nudo", check);
            CheckCondition(map.NudoCondition, where + "/nudoCondition", check);
        }

        /// <summary>Comprueba que cada identificador de una condición existe (bandera, objeto, escena…).</summary>
        private void CheckCondition(string condition, string where, Action<bool, string> check)
        {
            if (string.IsNullOrWhiteSpace(condition)) return;
            try
            {
                foreach (string name in ConditionEvaluator.Identifiers(condition))
                    check(IsKnownVariable(name), where + ": variable desconocida en condición '" + name + "'");
            }
            catch (Exception e)
            {
                check(false, where + ": condición mal escrita '" + condition + "' (" + e.Message + ")");
            }
        }

        private bool IsKnownVariable(string name)
        {
            int dot = name.IndexOf('.');
            if (dot < 0) return name == "money" || Flags.ContainsKey(name);
            string prefix = name.Substring(0, dot), rest = name.Substring(dot + 1);
            switch (prefix)
            {
                case "item": return Items.ContainsKey(rest);
                case "recruited": return Characters.ContainsKey(rest);
                case "scene":
                case "current": return Scenes.ContainsKey(rest) || rest == "PRUEBAS";
                case "cierre": return Cierres.ContainsKey(rest);
                case "visited": return Maps.ContainsKey(rest);
                case "absent": return AbsentIds.Contains(rest);
                case "resolved": return true;
                default: return false;
            }
        }

        private void CheckAction(string action, string where, Action<bool, string> check)
        {
            string[] p = action.Split(':');
            string arg = p.Length > 1 ? p[1] : "";
            switch (p[0])
            {
                case "give": check(Items.ContainsKey(arg), where + ": give de objeto inexistente " + arg); break;
                case "take": check(Items.ContainsKey(arg), where + ": take de objeto inexistente " + arg); break;
                case "recruit": check(Characters.ContainsKey(arg), where + ": recruit de personaje inexistente " + arg); break;
                case "battle": check(Encounters.ContainsKey(arg), where + ": battle de encuentro inexistente " + arg); break;
                case "dialogue": check(Dialogues.ContainsKey(arg), where + ": dialogue inexistente " + arg); break;
                case "scene": check(Scenes.ContainsKey(arg), where + ": escena inexistente " + arg); break;
                case "cierre": check(Cierres.ContainsKey(arg), where + ": cierre inexistente " + arg); break;
                case "absent": check(AbsentIds.Contains(arg), where + ": ausente desconocido " + arg); break;
                case "travel":
                    MapDef map;
                    check(Maps.TryGetValue(arg, out map), where + ": travel a mapa inexistente " + arg);
                    if (map != null && p.Length > 2) check(map.Spawns.ContainsKey(p[2]), where + ": travel a punto inexistente " + arg + "/" + p[2]);
                    break;
                case "puzzle": check(Puzzles.OnSolved.ContainsKey(arg), where + ": puzzle desconocido " + arg); break;
                case "resolve":
                case "money":
                case "nudo":
                case "heal":
                case "endact": break;
                default: check(false, where + ": acción desconocida " + action); break;
            }
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
            foreach (var m in Maps.Values) yield return m.NameKey;
            foreach (var s in Scenes.Values)
            {
                yield return s.TitleKey;
                foreach (var o in s.Objectives) yield return s.ObjectiveKey(o);
            }
            foreach (var c in Cierres.Values)
            {
                yield return c.CauseKey;
                yield return c.BeforeKey;
                foreach (int v in c.Values) { yield return c.NowKey(v); yield return c.PersonKey(v); }
            }
            foreach (var a in AbsentIds) { yield return "absent." + a + ".name"; yield return "absent." + a + ".desc"; }
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
