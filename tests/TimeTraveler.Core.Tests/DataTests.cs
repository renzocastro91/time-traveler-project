using System.Collections.Generic;
using System.Linq;
using TimeTraveler.Core;
using Xunit;

namespace TimeTraveler.Core.Tests
{
    public class DataTests
    {
        [Fact]
        public void ContentHasNoBrokenReferences()
        {
            var errors = TestData.Db.Validate();
            Assert.True(errors.Count == 0, string.Join("\n", errors));
        }

        [Fact]
        public void EveryContentKeyHasSpanishText()
        {
            var missing = TestData.Db.RequiredLocalizationKeys().Where(k => !TestData.Loc.Has(k)).Distinct().ToList();
            Assert.True(missing.Count == 0, "Textos faltantes:\n" + string.Join("\n", missing));
        }

        [Fact]
        public void AllEightPlayableCharactersHaveDistinctElements()
        {
            var elements = TestData.Db.Characters.Values.Select(c => c.Element).ToList();
            Assert.Equal(8, elements.Count);
            Assert.Equal(8, elements.Distinct().Count());
            Assert.DoesNotContain(Element.Neutro, elements);
        }

        [Fact]
        public void EveryHumanLearnsPhysicalAndMagicalTechniques()
        {
            var db = TestData.Db;
            foreach (var c in db.Characters.Values)
            {
                var kinds = c.Learnset.Select(e => db.Technique(e.TechniqueId).Kind).ToList();
                Assert.Contains(TechniqueKind.Fisica, kinds);
                Assert.Contains(TechniqueKind.Magica, kinds);
            }
        }

        [Fact]
        public void CretaceousFaunaIsNeverBondable()
        {
            // Regla del guion: en el Cretácico la cría es señal de campo, no mascota de combate.
            foreach (var s in TestData.Db.Creatures.Values.Where(s => s.Era == "-72400000"))
                Assert.False(s.Bondable, s.Id);
        }

        [Fact]
        public void BondableFamiliesCanEvolve()
        {
            var db = TestData.Db;
            var stageOne = db.Creatures.Values.Where(s => s.Bondable && s.Stage == 1).ToList();
            Assert.True(stageOne.Count >= 10);
            foreach (var s in stageOne) Assert.NotEmpty(s.Evolutions);
        }

        [Fact]
        public void ConditionEvaluatorHandlesOperators()
        {
            var vars = new Dictionary<string, int> { { "A", 2 }, { "B", 0 }, { "C", -1 } };
            System.Func<string, int> r = n => vars.TryGetValue(n, out var v) ? v : 0;
            Assert.True(ConditionEvaluator.Evaluate("A>=2", r));
            Assert.False(ConditionEvaluator.Evaluate("A>2", r));
            Assert.True(ConditionEvaluator.Evaluate("!B && A==2", r));
            Assert.True(ConditionEvaluator.Evaluate("B || C<0", r));
            Assert.False(ConditionEvaluator.Evaluate("B && A", r));
            Assert.True(ConditionEvaluator.Evaluate("A!=1", r));
            Assert.True(ConditionEvaluator.Evaluate("", r));
        }

        [Fact]
        public void ElementChartFollowsCycle()
        {
            Assert.Equal(ElementChart.Strong, ElementChart.Multiplier(Element.Agua, Element.Fuego));
            Assert.Equal(ElementChart.Weak, ElementChart.Multiplier(Element.Fuego, Element.Agua));
            Assert.Equal(ElementChart.Strong, ElementChart.Multiplier(Element.Luz, Element.Vacio));
            Assert.Equal(ElementChart.Strong, ElementChart.Multiplier(Element.Vacio, Element.Luz));
            Assert.Equal(ElementChart.Same, ElementChart.Multiplier(Element.Rayo, Element.Rayo));
            Assert.Equal(ElementChart.Normal, ElementChart.Multiplier(Element.Neutro, Element.Fuego));
            Assert.Equal(ElementChart.Normal, ElementChart.Multiplier(Element.Fuego, Element.Tierra));
        }
    }
}
