using System.Linq;
using TimeTraveler.Core;
using Xunit;

namespace TimeTraveler.Core.Tests
{
    public class BattleTests
    {
        private static GameState NewState(int level = 5)
        {
            var db = TestData.Db;
            var state = GameState.NewGame(db);
            foreach (var c in state.Characters.Values)
            {
                ProgressionService.GainXp(db, c, ExperienceCurve.TotalXpForLevel(level));
                var stats = StatCalculator.ForCharacter(db, c);
                c.Hp = stats.Get(StatType.MaxHp);
                c.Mp = stats.Get(StatType.MaxMp);
            }
            return state;
        }

        /// <summary>Avanza el tiempo hasta que algún humano esté listo (o termine el combate).</summary>
        private static void TickUntilReady(BattleEngine engine, int maxSteps = 5000)
        {
            for (int i = 0; i < maxSteps && engine.ReadyPlayers.Count == 0 && engine.Outcome == BattleOutcome.EnCurso; i++)
                engine.Tick(0.05f);
        }

        private static void TickUntilAllHumansReady(BattleEngine engine)
        {
            var humans = engine.Units.Where(u => u.Kind == UnitKind.Humano).ToList();
            for (int i = 0; i < 5000 && humans.Any(h => !h.IsReady) && engine.Outcome == BattleOutcome.EnCurso; i++)
                engine.Tick(0.05f);
        }

        [Fact]
        public void PartyDefeatsFirstEncounterByAttacking()
        {
            var db = TestData.Db;
            var state = NewState();
            var engine = BattleFactory.Create(db, state, "enc_estacion_huecos", new SystemRandom(7));
            engine.Mode = BattleMode.Espera;

            for (int turn = 0; turn < 200 && engine.Outcome == BattleOutcome.EnCurso; turn++)
            {
                TickUntilReady(engine);
                if (engine.Outcome != BattleOutcome.EnCurso) break;
                var actor = engine.ReadyPlayers[0];
                Assert.True(engine.Submit(BattleAction.Attack(actor, engine.Opponents(actor).First())));
            }

            Assert.Equal(BattleOutcome.Victoria, engine.Outcome);
            Assert.Equal(28, engine.Rewards.Xp);

            int moneyBefore = state.Inventory.Money;
            var summary = BattleResultService.Apply(db, state, engine);
            Assert.Equal(moneyBefore + 16, state.Inventory.Money);
            Assert.All(state.Characters.Values, c => Assert.True(c.Hp >= 1));
            Assert.NotNull(summary);
        }

        [Fact]
        public void WaitModeFreezesTimeWhileMenuIsOpen()
        {
            var engine = BattleFactory.Create(TestData.Db, NewState(), "enc_estacion_huecos", new SystemRandom(1));
            engine.Mode = BattleMode.Espera;
            engine.MenuOpen = true;
            var before = engine.Units.Select(u => u.Atb).ToList();
            engine.Tick(1f);
            Assert.Equal(before, engine.Units.Select(u => u.Atb).ToList());

            engine.Mode = BattleMode.Activa;
            engine.Tick(0.1f);
            Assert.NotEqual(before, engine.Units.Select(u => u.Atb).ToList());
        }

        [Fact]
        public void DualComboNeedsBothReadyAndSpendsBothTurns()
        {
            var db = TestData.Db;
            var engine = BattleFactory.Create(db, NewState(), "enc_estacion_huecos", new SystemRandom(3));
            var violeta = engine.Units.First(u => u.SourceId == "violeta");
            var angel = engine.Units.First(u => u.SourceId == "angel");

            TickUntilAllHumansReady(engine);
            Assert.True(violeta.IsReady && angel.IsReady);

            var combos = engine.AvailableCombos(violeta).Select(c => c.Id).ToList();
            Assert.Contains("estatica_del_aula", combos);
            Assert.Contains("tajo_vendaval", combos);
            Assert.DoesNotContain("cancion_de_la_tierra", combos); // Sura no está

            int mpV = violeta.Mp, mpA = angel.Mp;
            Assert.True(engine.Submit(BattleAction.Combo(violeta, "estatica_del_aula", null)));
            Assert.Equal(mpV - db.Technique("v_eco").MpCost, violeta.Mp);
            Assert.Equal(mpA - db.Technique("a_pulso").MpCost, angel.Mp);
            Assert.False(violeta.IsReady);
            Assert.False(angel.IsReady);
            Assert.Empty(engine.ReadyPlayers);

            var events = engine.DrainEvents();
            var start = events.First(e => e.Type == BattleEventType.ActionStart && e.ActionType == ActionType.Combo);
            Assert.Equal(2, start.Participants.Count);
        }

        [Fact]
        public void PetJoinsBattleAndCanComboWithAngel()
        {
            var db = TestData.Db;
            var state = NewState();
            var pet = CreatureFactory.Create(db, "tropel", 5, state.NewCreatureUid());
            state.Sanctuary.Creatures.Add(pet);
            state.Party.ActivePetUid = pet.Uid;

            var engine = BattleFactory.Create(db, state, "enc_estacion_huecos", new SystemRandom(5));
            engine.PetHoldSeconds = 1000f; // que la mascota espere para el combo
            var petUnit = engine.Pet;
            Assert.NotNull(petUnit);
            Assert.Equal(UnitKind.Mascota, petUnit.Kind);

            var angel = engine.Units.First(u => u.SourceId == "angel");
            for (int i = 0; i < 5000 && !(angel.IsReady && petUnit.IsReady); i++)
            {
                engine.Tick(0.05f);
                // Mientras esperamos, los demás humanos se defienden para no bloquear.
                foreach (var r in engine.ReadyPlayers.Where(u => u != angel).ToList()) engine.Submit(BattleAction.Defend(r));
            }

            Assert.Contains("carga_conjunta", engine.AvailableCombos(angel).Select(c => c.Id));
            Assert.True(engine.Submit(BattleAction.Combo(angel, "carga_conjunta", engine.Opponents(angel).First())));
            Assert.Equal(1, engine.PetCombos);
            Assert.False(petUnit.IsReady);
        }

        [Fact]
        public void PetActsOnItsOwnAfterHoldTime()
        {
            var db = TestData.Db;
            var state = NewState();
            var pet = CreatureFactory.Create(db, "tropel", 5, state.NewCreatureUid());
            state.Sanctuary.Creatures.Add(pet);
            state.Party.ActivePetUid = pet.Uid;
            var engine = BattleFactory.Create(db, state, "enc_estacion_huecos", new SystemRandom(11));
            engine.Mode = BattleMode.Activa;

            bool petActed = false;
            for (int i = 0; i < 2000 && !petActed && engine.Outcome == BattleOutcome.EnCurso; i++)
            {
                engine.Tick(0.05f);
                petActed = engine.DrainEvents().Any(e => e.Type == BattleEventType.ActionStart && e.Actor == engine.Pet);
                foreach (var r in engine.ReadyPlayers.ToList()) engine.Submit(BattleAction.Defend(r));
            }
            Assert.True(petActed);
        }

        [Fact]
        public void ElementalWeaknessIncreasesDamage()
        {
            var spec = new EffectSpec { Kind = TechniqueKind.Magica, Element = Element.Agua, Power = 20, Target = TargetType.EnemigoUno };
            var fireTarget = new BattleUnit { Element = Element.Fuego, Stats = new StatBlock { { StatType.DefensaMagica, 5 } }, Hp = 100 };
            var neutralTarget = new BattleUnit { Element = Element.Neutro, Stats = new StatBlock { { StatType.DefensaMagica, 5 } }, Hp = 100 };
            var rng = new TestData.FixedRandom(0.5f);
            var weak = DamageCalculator.Compute(spec, 20, 10, 0, false, 1f, fireTarget, rng);
            var normal = DamageCalculator.Compute(spec, 20, 10, 0, false, 1f, neutralTarget, rng);
            Assert.Equal(1.5f, weak.ElementMultiplier);
            Assert.True(weak.Amount > normal.Amount);
        }

        [Fact]
        public void HuecoGrowsFuriousAndTestimonyCalmsIt()
        {
            var db = TestData.Db;
            var engine = BattleFactory.Create(db, NewState(), "enc_boss_hueco", new TestData.FixedRandom(0.5f));
            var rule = Assert.IsType<HuecoRule>(engine.Rule);
            var boss = engine.Units.First(u => u.IsBoss);

            TickUntilReady(engine);
            var actor = engine.ReadyPlayers[0];
            Assert.Empty(rule.GetCommands(engine, actor));
            engine.Submit(BattleAction.Attack(actor, boss));
            TickUntilReady(engine);
            engine.Submit(BattleAction.Attack(engine.ReadyPlayers[0], boss));
            Assert.Equal(2, rule.Fury);
            Assert.True(boss.OutgoingDamageMultiplier > 1f);

            TickUntilReady(engine);
            actor = engine.ReadyPlayers[0];
            var command = rule.GetCommands(engine, actor).Single();
            int hpBefore = boss.Hp;
            Assert.True(engine.Submit(BattleAction.Rule(actor, command.Id, boss)));
            Assert.Equal(0, rule.Fury);
            Assert.Equal(1, rule.Testimonies);
            Assert.Equal(hpBefore - boss.MaxHp / 4, boss.Hp);
        }

        [Fact]
        public void FeDeErratasIsImmuneUntilUnregisteredTestimony()
        {
            var db = TestData.Db;
            var state = NewState(9);
            state.Recruit(db, "lia", 9);
            var engine = BattleFactory.Create(db, state, "enc_boss_erratas", new TestData.FixedRandom(0.5f));
            var rule = Assert.IsType<FeDeErratasRule>(engine.Rule);
            var boss = engine.Units.First(u => u.IsBoss);
            var lia = engine.Units.First(u => u.SourceId == "lia");

            for (int i = 0; i < 5000 && !lia.IsReady; i++)
            {
                engine.Tick(0.05f);
                foreach (var r in engine.ReadyPlayers.Where(u => u != lia).ToList()) engine.Submit(BattleAction.Attack(r, boss));
            }
            Assert.Equal(boss.MaxHp, boss.Hp); // inmune mientras el Expediente está activo
            Assert.True(engine.Submit(BattleAction.Rule(lia, FeDeErratasRule.Command, null)));
            Assert.False(rule.ShieldActive);

            TickUntilReady(engine);
            engine.Submit(BattleAction.Attack(engine.ReadyPlayers[0], boss));
            Assert.True(boss.Hp < boss.MaxHp);
        }

        [Fact]
        public void BossEncountersCannotBeFled()
        {
            var engine = BattleFactory.Create(TestData.Db, NewState(), "enc_boss_hueco", new SystemRandom(2));
            TickUntilReady(engine);
            engine.Submit(BattleAction.Flee(engine.ReadyPlayers[0]));
            Assert.Equal(BattleOutcome.EnCurso, engine.Outcome);
            Assert.Contains(engine.DrainEvents(), e => e.Type == BattleEventType.FleeFailed);
        }

        [Fact]
        public void ItemsHealAndAreConsumed()
        {
            var db = TestData.Db;
            var state = NewState();
            var engine = BattleFactory.Create(db, state, "enc_estacion_huecos", new SystemRandom(4));
            TickUntilReady(engine);
            var actor = engine.ReadyPlayers[0];
            actor.Hp = 10;
            int vendas = state.Inventory.Count("venda");
            Assert.True(engine.Submit(BattleAction.Item(actor, "venda", actor)));
            Assert.Equal(70, actor.Hp);
            Assert.Equal(vendas - 1, state.Inventory.Count("venda"));
        }

        [Fact]
        public void PoisonDamagesAtTurnStartAndExpires()
        {
            var db = TestData.Db;
            var engine = BattleFactory.Create(db, NewState(), "enc_estacion_huecos", new SystemRandom(9));
            var enemy = engine.Units.First(u => u.Team == Team.Enemigo);
            engine.ApplyStatus(enemy, StatusType.Veneno, 2);
            int hp = enemy.Hp;

            // Fuerza el turno del enemigo: sólo él está a punto de llenar su barra.
            foreach (var u in engine.Units) u.Atb = 0f;
            enemy.Atb = BattleEngine.AtbMax - 0.01f;
            engine.Tick(0.01f);
            Assert.Equal(hp - enemy.MaxHp / 16, enemy.Hp);
            Assert.True(enemy.HasStatus(StatusType.Veneno));

            foreach (var u in engine.Units) u.Atb = 0f;
            enemy.Atb = BattleEngine.AtbMax - 0.01f;
            engine.Tick(0.01f);
            Assert.False(enemy.HasStatus(StatusType.Veneno));
        }
    }
}
