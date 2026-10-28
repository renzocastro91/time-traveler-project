using System.IO;
using System.Linq;
using TimeTraveler.Core;
using Xunit;

namespace TimeTraveler.Core.Tests
{
    public class ProgressionTests
    {
        [Fact]
        public void NewGameStartsWithVioletaAndAngelLocked()
        {
            var state = GameState.NewGame(TestData.Db);
            Assert.Equal(new[] { "violeta", "angel" }, state.Party.Active);
            Assert.Contains("violeta", state.Party.Locked);
            Assert.Contains("v_latigazo", state.Characters["violeta"].LearnedTechniques);
            Assert.Equal("cable_microfono", state.Characters["violeta"].Equipment[EquipSlot.Arma]);
        }

        [Fact]
        public void GainingXpLevelsUpAndLearnsTechniques()
        {
            var db = TestData.Db;
            var state = GameState.NewGame(db);
            var angel = state.Characters["angel"];
            var ups = ProgressionService.GainXp(db, angel, ExperienceCurve.TotalXpForLevel(9));
            Assert.Equal(9, angel.Level);
            Assert.Equal(8, ups.Count);
            Assert.Contains("a_corte_cruzado", angel.LearnedTechniques);
            Assert.Contains("a_chispazo", angel.LearnedTechniques);
            Assert.DoesNotContain("a_sobrecarga", angel.LearnedTechniques);
            var stats = StatCalculator.ForCharacter(db, angel);
            Assert.True(stats.Get(StatType.MaxHp) > 150);
        }

        [Fact]
        public void EquipmentRespectsWeaponTypeAndReturnsOldItem()
        {
            var db = TestData.Db;
            var state = GameState.NewGame(db);
            state.Inventory.Add("espada_de_bronce");
            state.Inventory.Add("gorra_de_lana");

            Assert.False(EquipmentRules.EquipCharacter(db, state, "violeta", EquipSlot.Arma, "espada_de_bronce"));
            Assert.True(EquipmentRules.EquipCharacter(db, state, "angel", EquipSlot.Arma, "espada_de_bronce"));
            Assert.Equal(1, state.Inventory.Count("espada_de_utileria"));
            Assert.Equal(0, state.Inventory.Count("espada_de_bronce"));

            Assert.True(EquipmentRules.EquipCharacter(db, state, "violeta", EquipSlot.Casco, "gorra_de_lana"));
            Assert.True(EquipmentRules.EquipCharacter(db, state, "violeta", EquipSlot.Casco, null));
            Assert.Equal(1, state.Inventory.Count("gorra_de_lana"));
            Assert.False(EquipmentRules.EquipCharacter(db, state, "violeta", EquipSlot.Arma, null));
        }

        [Fact]
        public void PetEquipmentOnlyFitsCreatures()
        {
            var db = TestData.Db;
            var state = GameState.NewGame(db);
            state.Inventory.Add("collar_de_cuero");
            Assert.False(EquipmentRules.EquipCharacter(db, state, "angel", EquipSlot.Accesorio, "collar_de_cuero"));
            var pet = CreatureFactory.Create(db, "tropel", 3, state.NewCreatureUid());
            int before = StatCalculator.ForCreature(db, pet).Get(StatType.Fuerza);
            Assert.True(EquipmentRules.EquipCreature(db, state, pet, EquipSlot.Collar, "collar_de_cuero"));
            Assert.Equal(before + 3, StatCalculator.ForCreature(db, pet).Get(StatType.Fuerza));
        }

        [Fact]
        public void FieldItemsHealAndAreNotWastedOnFullHealth()
        {
            var db = TestData.Db;
            var state = GameState.NewGame(db);
            int vendas = state.Inventory.Count("venda");
            Assert.False(FieldItemService.Use(db, state, "venda", "violeta", null));
            Assert.Equal(vendas, state.Inventory.Count("venda"));

            state.Characters["violeta"].Hp = 5;
            Assert.True(FieldItemService.Use(db, state, "venda", "violeta", null));
            Assert.Equal(65, state.Characters["violeta"].Hp);
            Assert.Equal(vendas - 1, state.Inventory.Count("venda"));

            state.Characters["angel"].Hp = 0;
            Assert.False(FieldItemService.Use(db, state, "venda", "angel", null));
            state.Inventory.Add("sales_de_olor");
            Assert.True(FieldItemService.Use(db, state, "sales_de_olor", "angel", null));
            Assert.True(state.Characters["angel"].Hp > 0);
        }

        [Fact]
        public void LockedMembersCannotBeSwappedOut()
        {
            var db = TestData.Db;
            var state = GameState.NewGame(db);
            state.Recruit(db, "lia", 5);
            state.Recruit(db, "sura", 5);
            Assert.Equal(3, state.Party.Active.Count);
            Assert.False(state.Party.Swap("violeta", "sura", state.Characters));
            Assert.True(state.Party.Swap("lia", "sura", state.Characters));
            Assert.Contains("sura", state.Party.Active);
        }

        [Fact]
        public void BondingNeedsTuneAndRespectsSpecies()
        {
            var db = TestData.Db;
            var tropel = db.Species("tropel");
            Assert.Equal(0f, BondingSystem.AcceptanceChance(tropel, 0.3f, null));
            float plain = BondingSystem.AcceptanceChance(tropel, 0.8f, null);
            float withFavorite = BondingSystem.AcceptanceChance(tropel, 0.8f, db.Item("bizcocho"));
            Assert.True(withFavorite > plain);
            Assert.True(BondingSystem.AcceptanceChance(tropel, 1f, db.Item("bizcocho")) <= BondingSystem.MaxChance);
            Assert.Equal(0f, BondingSystem.AcceptanceChance(db.Species("cria_emplumada"), 1f, null));
        }

        [Fact]
        public void SuccessfulBondAddsCreatureAndMakesItTheActivePet()
        {
            var db = TestData.Db;
            var state = GameState.NewGame(db);
            state.Inventory.Add("bizcocho");
            var result = BondingSystem.Attempt(db, state, "tropel", 4, 1f, "bizcocho", new TestData.FixedRandom(0f));
            Assert.True(result.Accepted);
            Assert.Single(state.Sanctuary.Creatures);
            Assert.Equal(result.Creature.Uid, state.Party.ActivePetUid);
            Assert.Equal(0, state.Inventory.Count("bizcocho"));

            var refused = BondingSystem.Attempt(db, state, "promesin", 4, 1f, null, new TestData.FixedRandom(0f));
            Assert.False(refused.Accepted);
            Assert.Equal("bond.reason.condition", refused.ReasonKey);
        }

        [Fact]
        public void EvolutionRequiresLevelAffinityItemAndEra()
        {
            var db = TestData.Db;
            var state = GameState.NewGame(db);
            var pet = CreatureFactory.Create(db, "tropel", 14, state.NewCreatureUid());
            Assert.Empty(EvolutionSystem.Available(db, state, pet, "2000"));
            pet.Affinity = 30;
            var evo = EvolutionSystem.Available(db, state, pet, "2000").Single();
            EvolutionSystem.Evolve(db, state, pet, evo, "2000");
            Assert.Equal("ventolero", pet.SpeciesId);
            Assert.Equal(14, pet.Level);

            var nutria = CreatureFactory.Create(db, "nutria_de_mareas", 33, state.NewCreatureUid());
            nutria.Affinity = 80;
            state.Inventory.Add("escama_de_marea");
            Assert.Empty(EvolutionSystem.Available(db, state, nutria, "2026"));
            var marejada = EvolutionSystem.Available(db, state, nutria, "1542").Single();
            EvolutionSystem.Evolve(db, state, nutria, marejada, "1542");
            Assert.Equal("marejada", nutria.SpeciesId);
            Assert.Equal(0, state.Inventory.Count("escama_de_marea"));
        }

        [Fact]
        public void DialogueChoicesChangeNarrativeFlags()
        {
            var db = TestData.Db;
            var state = GameState.NewGame(db);
            var actions = new System.Collections.Generic.List<string>();
            var runner = new DialogueRunner(db, state, actions.Add);
            runner.Start("e01_aula");
            while (runner.VisibleChoices().Count == 0) runner.Advance();
            Assert.Equal("n9", runner.Current.Id);
            runner.Choose(0);
            Assert.Equal(1, state.Flags.Get("CONFIANZA_VA"));
            Assert.Equal(1, state.Flags.Get("CONSENTIMIENTO"));
            runner.Advance();
            Assert.Equal("n11", runner.Current.Id);
            Assert.Contains("scene:E02", actions);
            runner.Advance();
            Assert.True(runner.IsFinished);
        }

        [Fact]
        public void DialogueHidesChoicesWhoseConditionFails()
        {
            var db = TestData.Db;
            var state = GameState.NewGame(db);
            var runner = new DialogueRunner(db, state, _ => { });
            runner.Start("proto_vendedor");
            runner.Advance();
            Assert.Single(runner.VisibleChoices());
            state.Inventory.Money = 1000;
            Assert.Equal(2, runner.VisibleChoices().Count);
        }

        [Fact]
        public void FlagsAreClampedToTheirRange()
        {
            var db = TestData.Db;
            var flags = new FlagStore();
            flags.Set("CONFIANZA_VA", 50, db);
            Assert.Equal(6, flags.Get("CONFIANZA_VA"));
            flags.Apply(new FlagEffect { Flag = "CONFIANZA_VA", Op = FlagOp.Add, Value = -20 }, db);
            Assert.Equal(-4, flags.Get("CONFIANZA_VA"));
        }

        [Fact]
        public void SaveAndLoadRoundTrip()
        {
            var db = TestData.Db;
            var state = GameState.NewGame(db);
            state.Flags.Set("PACTO_SURA", 2, db);
            var pet = CreatureFactory.Create(db, "ascuela", 5, state.NewCreatureUid());
            pet.Equipment[EquipSlot.Collar] = "collar_de_cuero";
            state.Sanctuary.Creatures.Add(pet);
            state.Party.ActivePetUid = pet.Uid;
            state.Location.X = 3.5f;

            string dir = Path.Combine(Path.GetTempPath(), "tt_save_test_" + System.Guid.NewGuid().ToString("N"));
            try
            {
                SaveService.Save(state, dir, 1);
                var loaded = SaveService.Load(dir, 1);
                Assert.Equal(2, loaded.Flags.Get("PACTO_SURA"));
                Assert.Equal("collar_de_cuero", loaded.ActivePet.Equipment[EquipSlot.Collar]);
                Assert.Equal(3.5f, loaded.Location.X);
                Assert.Equal(state.NextCreatureNumber, loaded.NextCreatureNumber);
                Assert.Equal("cable_microfono", loaded.Characters["violeta"].Equipment[EquipSlot.Arma]);
                Assert.Equal(1, SaveService.Peek(dir, 1).LeaderLevel);
                Assert.Null(SaveService.Load(dir, 2));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
