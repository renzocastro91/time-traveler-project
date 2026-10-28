using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeTraveler.Core
{
    /// <summary>Orden especial que una regla dramática añade al menú de batalla.</summary>
    public sealed class RuleCommand
    {
        public string Id;
        public string NameKey;
        public string DescKey;
        /// <summary>Si la orden necesita elegir un enemigo como objetivo.</summary>
        public bool TargetsEnemy;
    }

    /// <summary>
    /// Regla dramática de un jefe (sección 7.4 del guion). El combate sigue siendo convencional,
    /// pero la regla altera el daño, añade órdenes especiales y puede impedir la derrota del jefe
    /// mientras no se cumpla su condición narrativa.
    /// </summary>
    public interface IDramaticRule
    {
        string Id { get; }
        void OnBattleStart(BattleEngine engine);
        /// <summary>Se llama antes de aplicar cualquier daño (en ambos sentidos).</summary>
        int ModifyDamage(BattleEngine engine, BattleUnit attacker, BattleUnit target, int damage, EffectSpec spec);
        void OnAfterAction(BattleEngine engine, BattleAction action);
        IEnumerable<RuleCommand> GetCommands(BattleEngine engine, BattleUnit actor);
        void ExecuteCommand(BattleEngine engine, BattleUnit actor, string commandId, BattleUnit target);
        bool CanDie(BattleEngine engine, BattleUnit unit);
    }

    public abstract class DramaticRuleBase : IDramaticRule
    {
        public abstract string Id { get; }
        protected BattleUnit Boss;

        public virtual void OnBattleStart(BattleEngine engine)
        {
            Boss = engine.Units.FirstOrDefault(u => u.IsBoss);
        }

        public virtual int ModifyDamage(BattleEngine engine, BattleUnit attacker, BattleUnit target, int damage, EffectSpec spec) { return damage; }
        public virtual void OnAfterAction(BattleEngine engine, BattleAction action) { }
        public virtual IEnumerable<RuleCommand> GetCommands(BattleEngine engine, BattleUnit actor) { yield break; }
        public virtual void ExecuteCommand(BattleEngine engine, BattleUnit actor, string commandId, BattleUnit target) { }
        public virtual bool CanDie(BattleEngine engine, BattleUnit unit) { return true; }
    }

    public static class DramaticRuleRegistry
    {
        private static readonly Dictionary<string, Func<IDramaticRule>> Factories = new Dictionary<string, Func<IDramaticRule>>
        {
            { HuecoRule.RuleId, () => new HuecoRule() },
            { FeDeErratasRule.RuleId, () => new FeDeErratasRule() },
        };

        public static bool Exists(string id) { return id != null && Factories.ContainsKey(id); }

        public static IDramaticRule Create(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            Func<IDramaticRule> factory;
            if (!Factories.TryGetValue(id, out factory)) throw new KeyNotFoundException("Regla dramática desconocida: " + id);
            return factory();
        }
    }

    /// <summary>
    /// E03 · El Hueco — "Atacarlo lo vuelve más violento"; "no se lo derrota golpeando: el jugador une
    /// tres objetos con sus testimonios para estabilizar el lugar".
    /// Cada golpe que recibe aumenta su Furia (+15 % de daño por punto, máx. 5). La orden «Testimonio»
    /// (con Furia 2 o más y un objeto de la galería aún sin unir) calma la Furia y le inflige un 25 %
    /// de su vida máxima ignorando defensas. No cae hasta haber unido los tres objetos.
    /// </summary>
    public sealed class HuecoRule : DramaticRuleBase
    {
        public const string RuleId = "hueco";
        public const string TestimonyCommand = "testimonio";
        public const int MaxFury = 5;
        public const int FuryForTestimony = 2;
        public const int RequiredTestimonies = 3;
        /// <summary>Objetos de la galería de E03 que pueden unirse a un testimonio.</summary>
        public static readonly string[] TestimonyItems = { "casco_de_operario", "foto_de_turno", "informe_de_julian" };

        public int Fury { get; private set; }
        public int Testimonies { get; private set; }

        public override string Id { get { return RuleId; } }

        public override void OnBattleStart(BattleEngine engine)
        {
            base.OnBattleStart(engine);
            engine.EmitMessage("rule.hueco.intro");
        }

        public override int ModifyDamage(BattleEngine engine, BattleUnit attacker, BattleUnit target, int damage, EffectSpec spec)
        {
            if (target == Boss && attacker != null && attacker.Team == Team.Jugador && spec != null && Fury < MaxFury)
            {
                Fury++;
                Boss.OutgoingDamageMultiplier = 1f + 0.15f * Fury;
                engine.EmitMessage("rule.hueco.fury", Fury);
            }
            return damage;
        }

        public override IEnumerable<RuleCommand> GetCommands(BattleEngine engine, BattleUnit actor)
        {
            int objects = TestimonyItems.Count(id => engine.Inventory.Count(id) > 0);
            if (actor.Kind == UnitKind.Humano && Boss != null && Boss.IsAlive && Fury >= FuryForTestimony && Testimonies < objects)
                yield return new RuleCommand { Id = TestimonyCommand, NameKey = "rule.hueco.cmd.name", DescKey = "rule.hueco.cmd.desc", TargetsEnemy = false };
        }

        public override void ExecuteCommand(BattleEngine engine, BattleUnit actor, string commandId, BattleUnit target)
        {
            if (commandId != TestimonyCommand || Boss == null) return;
            Testimonies++;
            Fury = 0;
            Boss.OutgoingDamageMultiplier = 1f;
            engine.EmitMessage("rule.hueco.testimony", Testimonies);
            engine.ApplyDamage(actor, Boss, Math.Max(1, Boss.MaxHp / 4), false, 1f, null);
        }

        public override bool CanDie(BattleEngine engine, BattleUnit unit)
        {
            if (unit != Boss || Testimonies >= RequiredTestimonies) return true;
            engine.EmitMessage("rule.hueco.unstable", RequiredTestimonies - Testimonies);
            return false;
        }
    }

    /// <summary>
    /// E05 · La Fe de Erratas — "La identidad legal concede invulnerabilidad".
    /// Mientras su Expediente está activo no recibe daño. «Testimonio no registrado» (Lía, o quien lleve
    /// la ficha de Gabriel Mora) lo obliga a leer a alguien que no reconoce y anula el Expediente
    /// durante 3 de sus turnos; después el sistema lo restaura.
    /// </summary>
    public sealed class FeDeErratasRule : DramaticRuleBase
    {
        public const string RuleId = "fe_de_erratas";
        public const string Command = "testimonio_no_registrado";
        public const string KeyItem = "ficha_gabriel";
        public const int ExposedTurns = 3;

        public bool ShieldActive { get; private set; }
        public int ExposedRemaining { get; private set; }

        public override string Id { get { return RuleId; } }

        public override void OnBattleStart(BattleEngine engine)
        {
            base.OnBattleStart(engine);
            ShieldActive = true;
            engine.EmitMessage("rule.erratas.intro");
        }

        public override int ModifyDamage(BattleEngine engine, BattleUnit attacker, BattleUnit target, int damage, EffectSpec spec)
        {
            if (target == Boss && ShieldActive)
            {
                engine.EmitMessage("rule.erratas.immune");
                return 0;
            }
            return damage;
        }

        public override IEnumerable<RuleCommand> GetCommands(BattleEngine engine, BattleUnit actor)
        {
            bool canTestify = actor.SourceId == "lia" || engine.Inventory.Count(KeyItem) > 0;
            if (actor.Kind == UnitKind.Humano && ShieldActive && canTestify)
                yield return new RuleCommand { Id = Command, NameKey = "rule.erratas.cmd.name", DescKey = "rule.erratas.cmd.desc", TargetsEnemy = false };
        }

        public override void ExecuteCommand(BattleEngine engine, BattleUnit actor, string commandId, BattleUnit target)
        {
            if (commandId != Command) return;
            ShieldActive = false;
            ExposedRemaining = ExposedTurns;
            engine.EmitMessage("rule.erratas.exposed", ExposedTurns);
        }

        public override void OnAfterAction(BattleEngine engine, BattleAction action)
        {
            if (action.Actor != Boss || ShieldActive) return;
            ExposedRemaining--;
            if (ExposedRemaining <= 0)
            {
                ShieldActive = true;
                engine.EmitMessage("rule.erratas.restored");
            }
        }
    }
}
