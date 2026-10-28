using System.Collections.Generic;
using System.Linq;
using TimeTraveler.Core;
using Xunit;

namespace TimeTraveler.Core.Tests
{
    /// <summary>
    /// Recorre el Acto I completo usando sólo datos y lógica del Core, como lo haría la capa Unity:
    /// reproduce diálogos, elige opciones, ejecuta acciones y resuelve lo que en el juego es visual
    /// (puzzles, combates, viajes) marcándolo directamente.
    /// </summary>
    public class Act1Tests
    {
        private readonly GameDatabase db = TestData.Db;

        /// <summary>Reproduce un diálogo eligiendo las opciones indicadas. Devuelve las acciones diferidas.</summary>
        private List<string> Play(GameState state, string dialogueId, params int[] choices)
        {
            var deferred = new List<string>();
            var queue = new Queue<int>(choices);
            var runner = new DialogueRunner(db, state, action =>
            {
                if (StoryActions.IsDeferred(action)) deferred.Add(action);
                else Assert.True(StoryActions.Execute(db, state, action), "Acción desconocida: " + action);
            });
            runner.Start(dialogueId);
            for (int guard = 0; guard < 200 && !runner.IsFinished; guard++)
            {
                if (runner.VisibleChoices().Count > 0)
                {
                    Assert.True(queue.Count > 0, "Faltan opciones para " + dialogueId + "/" + runner.Current.Id);
                    runner.Choose(queue.Dequeue());
                }
                else runner.Advance();
            }
            Assert.True(runner.IsFinished);
            return deferred;
        }

        private string Objective(GameState state)
        {
            var o = StoryDirector.CurrentObjective(db, state);
            return o == null ? null : o.Id;
        }

        private void Resolve(GameState state, string id) { StoryActions.Execute(db, state, "resolve:" + id); }

        [Fact]
        public void FullActOnePlaythrough()
        {
            var state = GameState.NewGame(db);
            Assert.Equal("instituto_2000", state.Location.MapId);

            // E01 · inspeccionar cinco objetos, luego el cuaderno
            Assert.Equal("E01", state.Story.CurrentScene);
            Play(state, "e01_intro");
            Assert.Null(StoryDirector.TakePendingDialogue(db, state));
            foreach (var d in new[] { "insp_album", "insp_lista", "insp_casete", "insp_maqueta", "insp_mural" }) Play(state, d);
            Play(state, "insp_album"); // repetir no vuelve a contar
            Assert.Equal(5, state.Flags.Get("E01_INSPECCIONES"));
            Play(state, "insp_maqueta"); // segunda vez: el plano oculto
            Assert.Equal(1, state.Flags.Get("PLANO_OCULTO"));
            Assert.Null(Objective(state));
            Assert.Equal("e01_cuaderno", StoryDirector.TakePendingDialogue(db, state));
            Assert.Null(StoryDirector.TakePendingDialogue(db, state)); // sólo una vez
            Play(state, "e01_cuaderno", 0);
            Assert.Equal("E02", state.Story.CurrentScene);
            Assert.Equal(1, state.Inventory.Count("cinta_2026"));

            // E02 · la cinta
            Assert.Equal("cinta", Objective(state));
            Assert.Contains("puzzle:cinta", Play(state, "e02_cinta"));
            Puzzles.MarkSolved(db, state, "cinta");
            Play(state, "e02_resuelto", 0);
            Assert.Contains("gabriel", state.Story.Absents);
            Assert.Equal(1, state.Inventory.Count("ficha_gabriel"));
            Assert.Equal("E03", state.Story.CurrentScene);

            // E03 · Marta, objetos, decisión (con plano: ambas), el Hueco
            Assert.Equal("ir", Objective(state));
            Play(state, "e03_marta");
            Assert.Equal("objetos", Objective(state));
            foreach (var d in new[] { "insp_casco", "insp_foto", "insp_informe" }) Play(state, d);
            Assert.True(HuecoRule.TestimonyItems.All(i => state.Inventory.Count(i) == 1));
            Assert.Equal("decidir", Objective(state));
            Play(state, "e03_eleccion", 2);
            Assert.Equal(3, state.Flags.Get("E03_ELECCION"));
            Assert.Equal(1, state.Inventory.Count("nomina_mojada"));
            Assert.Equal("hueco", Objective(state));
            Resolve(state, "g_hueco"); // victoria en combate
            Assert.Equal("e03_circulos", StoryDirector.TakePendingDialogue(db, state));
            Assert.Contains("travel:calle_2026:default", Play(state, "e03_circulos"));
            Assert.Equal("E04", state.Story.CurrentScene);

            // E04 · tres puntos persistentes
            Play(state, "e04_llegada");
            Play(state, "e04_transeunte");
            foreach (var d in new[] { "insp_arbol", "insp_toma", "insp_mural_museo" }) Play(state, d);
            Assert.Equal("e04_anochecer", StoryDirector.TakePendingDialogue(db, state));
            Play(state, "e04_anochecer");
            Assert.Equal("E05", state.Story.CurrentScene);

            // E05 · Lía y la Fe de Erratas
            Play(state, "e05_lia", 1);
            Assert.True(state.Characters["lia"].Recruited);
            Assert.Equal(2, state.Flags.Get("E05_ESTRATEGIA"));
            Assert.Equal("erratas", Objective(state));
            Resolve(state, "a_erratas");
            Assert.Equal("e05_despues", StoryDirector.TakePendingDialogue(db, state));
            Play(state, "e05_despues");
            Assert.Equal("E06", state.Story.CurrentScene);

            // E06 · paso subterráneo, Lira y el primer Cierre (con reconsideración)
            Assert.Contains("travel:lira_puerto:default", Play(state, "e06_paso"));
            Assert.NotNull(state.Story.PendingNudo);
            state.Story.VisitedMaps.Add("lira_puerto");
            Assert.Equal("entrar", Objective(state));
            Assert.Contains("cierre:cierre_e06", Play(state, "e06_guardias", 0));
            Assert.Equal(0, state.Inventory.Count("telefono_de_lia"));
            Assert.Equal(1, CierreService.Preview(db, state, "cierre_e06").Value);

            Assert.True(CierreService.Reconsider(state));
            Assert.Equal(0, state.Flags.Get("E06_ENTRADA"));
            Assert.Equal(1, state.Inventory.Count("telefono_de_lia"));
            Play(state, "e06_guardias", 2); // historia falsa
            var record = CierreService.Confirm(db, state, "cierre_e06");
            Assert.Equal(3, record.Value);
            Assert.Null(state.Story.PendingNudo);
            Assert.False(CierreService.Reconsider(state));
            Assert.Equal("E07", state.Story.CurrentScene);
            Play(state, "e06_confirmado");

            // E07 · Sura, alfabeto acústico, censo y Diapasón
            Assert.Contains("puzzle:signos", Play(state, "e07_sura"));
            Puzzles.MarkSolved(db, state, "signos");
            Assert.Equal("decidir", Objective(state));
            Assert.Contains("cierre:cierre_e07", Play(state, "e07_bifurcacion", 1));
            Assert.NotNull(state.Story.PendingNudo);
            CierreService.Reconsider(state); // reconsiderar no deshace el alfabeto aprendido
            Assert.Equal(1, state.Flags.Get("E07_SIGNOS"));
            Assert.Equal(0, state.Inventory.Count("diapason_de_arcilla"));
            Play(state, "e07_bifurcacion", 1);
            Assert.Equal(1, state.Inventory.Count("diapason_de_arcilla"));
            CierreService.Confirm(db, state, "cierre_e07");
            Assert.Equal("E08", state.Story.CurrentScene);
            Assert.Contains("endact:1", Play(state, "e07_confirmado"));
            Assert.True(state.Characters["sura"].Recruited);
            Assert.Equal(2, state.Story.Cierres.Count);
            Assert.Equal(new[] { "E01", "E02", "E03", "E04", "E05", "E06", "E07" }, state.Story.CompletedScenes);
        }

        [Fact]
        public void StealingTheStoneMeansSuraDoesNotJoin()
        {
            var state = GameState.NewGame(db);
            state.Story.CurrentScene = "E07";
            Play(state, "e07_sura");
            Puzzles.MarkSolved(db, state, "signos");
            Play(state, "e07_bifurcacion", 2);
            CierreService.Confirm(db, state, "cierre_e07");
            Play(state, "e07_confirmado");
            Assert.False(state.Characters.ContainsKey("sura") && state.Characters["sura"].Recruited);
            Assert.Equal(1, state.Flags.Get("DEUDA"));
            Assert.Equal(1, state.Inventory.Count("diapason_de_arcilla"));
        }

        [Fact]
        public void BothOptionInE03NeedsHiddenPlan()
        {
            var state = GameState.NewGame(db);
            state.Story.CurrentScene = "E03";
            var runner = new DialogueRunner(db, state, _ => { });
            runner.Start("e03_eleccion");
            Assert.Equal(2, runner.VisibleChoices().Count);
            state.Flags.Set("PLANO_OCULTO", 1, db);
            Assert.Equal(3, runner.VisibleChoices().Count);
        }

        [Fact]
        public void TapePuzzleTruthIsWhatMemoriesShare()
        {
            Assert.Equal(new[] { 2, 4, 5 }, TapePuzzle.Solution());
            foreach (var memory in TapePuzzle.Memories)
            {
                Assert.Equal(2, TapePuzzle.Signal(memory)); // cada recuerdo acierta dos de tres
                Assert.False(TapePuzzle.IsSolved(memory));
            }
            Assert.True(TapePuzzle.IsSolved(new[] { 2, 4, 5 }));
        }

        [Fact]
        public void SignPuzzleRepeatsRoundOnMistake()
        {
            var puzzle = new SignSequencePuzzle(42);
            Assert.Equal(3, puzzle.Sequence.Count);
            Assert.False(puzzle.Submit(new[] { 9, 9, 9 }));
            Assert.Equal(0, puzzle.Round);
            Assert.Equal(1, puzzle.Mistakes);
            for (int round = 0; round < SignSequencePuzzle.RoundLengths.Length; round++)
            {
                Assert.Equal(SignSequencePuzzle.RoundLengths[round], puzzle.Sequence.Count);
                Assert.True(puzzle.Submit(puzzle.Sequence.ToList()));
            }
            Assert.True(puzzle.IsComplete);
        }

        [Fact]
        public void EveryMapIsReachableFromTheStart()
        {
            // Mapas alcanzables por salidas, por viajes de diálogo o (el Nudo de pruebas) desde el título.
            var reachable = new HashSet<string> { "instituto_2000", "nudo_pruebas" };
            var travelTargets = db.Dialogues.Values.SelectMany(d => d.Nodes).SelectMany(n => n.Actions)
                                  .Where(a => a.StartsWith("travel:")).Select(a => a.Split(':')[1]);
            foreach (var t in travelTargets) reachable.Add(t);
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var map in db.Maps.Values.Where(m => reachable.Contains(m.Id)).ToList())
                    foreach (var exit in map.Exits)
                        changed |= reachable.Add(exit.ToMap);
            }
            Assert.Equal(db.Maps.Keys.OrderBy(k => k), reachable.OrderBy(k => k));
        }

        [Fact]
        public void TestModeStartsWithEverythingToTry()
        {
            var state = GameState.NewTestGame(db);
            Assert.Equal("nudo_pruebas", state.Location.MapId);
            Assert.Equal(3, state.Party.Active.Count);
            Assert.True(state.Characters.Values.All(c => c.Level == 8));
            Assert.Equal(1, state.Inventory.Count("diapason_de_arcilla"));
        }
    }
}
