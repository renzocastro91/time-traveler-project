using System;
using System.Collections.Generic;
using System.Linq;

namespace TimeTraveler.Core
{
    /// <summary>
    /// Motor de combate por turnos con barra de tiempo (ATB), sin dependencias de Unity.
    ///
    /// Flujo: la capa visual llama a Tick(dt) cada frame. Cuando un humano llena su barra entra en
    /// ReadyPlayers y la interfaz le pide una orden (Submit). Enemigos y mascota actúan solos.
    /// Cada acción produce BattleEvents que la vista anima; mientras lo hace pone Paused = true.
    /// En modo Espera el tiempo se detiene mientras hay un menú abierto (MenuOpen).
    /// </summary>
    public sealed class BattleEngine
    {
        public const float AtbMax = 100f;

        private readonly GameDatabase db;
        private readonly IRandom rng;
        private readonly List<BattleUnit> readyPlayers = new List<BattleUnit>();
        private readonly List<BattleEvent> events = new List<BattleEvent>();

        public readonly List<BattleUnit> Units = new List<BattleUnit>();
        public readonly EncounterDef Encounter;
        public readonly Inventory Inventory;
        public IDramaticRule Rule { get; private set; }

        public BattleMode Mode = BattleMode.Espera;
        public float Speed = 1f;
        public float PetHoldSeconds = 2f;
        public bool MenuOpen;
        public bool Paused;

        public BattleOutcome Outcome { get; private set; }
        public BattleRewards Rewards { get; private set; }
        /// <summary>Combos en los que participó la mascota (suben su afinidad).</summary>
        public int PetCombos { get; private set; }

        public IReadOnlyList<BattleUnit> ReadyPlayers { get { return readyPlayers; } }
        public GameDatabase Database { get { return db; } }
        public IRandom Random { get { return rng; } }

        public BattleEngine(GameDatabase db, IRandom rng, IEnumerable<BattleUnit> units, EncounterDef encounter, Inventory inventory)
        {
            this.db = db;
            this.rng = rng;
            Encounter = encounter;
            Inventory = inventory ?? new Inventory();
            foreach (var unit in units)
            {
                unit.Index = Units.Count;
                // Barra inicial parcialmente llena según velocidad, para que no actúen todos a la vez.
                unit.Atb = Math.Min(AtbMax * 0.8f, rng.Value() * 40f + unit.Stats.Get(StatType.Velocidad));
                Units.Add(unit);
            }
            Rule = DramaticRuleRegistry.Create(encounter != null ? encounter.DramaticRule : null);
            if (Rule != null) Rule.OnBattleStart(this);
        }

        // ---------------------------------------------------------------- consultas

        public IEnumerable<BattleUnit> Alive(Team team) { return Units.Where(u => u.Team == team && u.IsAlive); }
        public IEnumerable<BattleUnit> Opponents(BattleUnit unit) { return Alive(unit.Team == Team.Jugador ? Team.Enemigo : Team.Jugador); }
        public IEnumerable<BattleUnit> Allies(BattleUnit unit) { return Units.Where(u => u.Team == unit.Team); }
        public BattleUnit Pet { get { return Units.FirstOrDefault(u => u.Kind == UnitKind.Mascota); } }

        public static float AtbRate(BattleUnit unit)
        {
            float rate = (10f + unit.Stats.Get(StatType.Velocidad)) * 1.6f;
            if (unit.HasStatus(StatusType.Prisa)) rate *= 1.5f;
            if (unit.HasStatus(StatusType.Lentitud)) rate *= 0.6f;
            return rate;
        }

        public List<BattleEvent> DrainEvents()
        {
            var copy = new List<BattleEvent>(events);
            events.Clear();
            return copy;
        }

        public void Emit(BattleEvent e) { events.Add(e); }

        public void EmitMessage(string textKey, params object[] args)
        {
            events.Add(new BattleEvent { Type = BattleEventType.RuleMessage, TextKey = textKey, Args = args });
        }

        public bool CanUseTechnique(BattleUnit unit, string techniqueId)
        {
            if (!unit.Techniques.Contains(techniqueId)) return false;
            var tech = db.Technique(techniqueId);
            if (unit.Mp < tech.MpCost) return false;
            if (unit.HasStatus(StatusType.Olvido)) return false;
            if (tech.Kind == TechniqueKind.Magica && unit.HasStatus(StatusType.Silencio)) return false;
            return true;
        }

        /// <summary>
        /// Combos disponibles para este actor ahora: todos los participantes vivos, con la barra llena,
        /// conociendo la técnica requerida y con PM suficientes.
        /// </summary>
        public List<ComboDef> AvailableCombos(BattleUnit actor)
        {
            var result = new List<ComboDef>();
            foreach (var combo in db.Combos.Values)
            {
                List<BattleUnit> participants;
                if (TryGetComboParticipants(combo, out participants) && participants.Contains(actor)) result.Add(combo);
            }
            return result;
        }

        public bool TryGetComboParticipants(ComboDef combo, out List<BattleUnit> participants)
        {
            participants = new List<BattleUnit>();
            foreach (var p in combo.Participants)
            {
                BattleUnit unit = p.IsPet ? Pet : Units.FirstOrDefault(u => u.Kind == UnitKind.Humano && u.SourceId == p.Actor);
                if (unit == null || !unit.IsAlive || !unit.IsReady) return false;
                if (unit.HasStatus(StatusType.Olvido)) return false;
                if (p.IsPet)
                {
                    if (p.PetElement.HasValue && unit.Element != p.PetElement.Value) return false;
                    if (unit.Mp < combo.PetMpCost) return false;
                }
                if (p.TechniqueId != null && !CanUseTechnique(unit, p.TechniqueId)) return false;
                participants.Add(unit);
            }
            return true;
        }

        /// <summary>Objetivos válidos para un tipo de objetivo, desde el punto de vista del actor.</summary>
        public List<BattleUnit> ValidTargets(BattleUnit actor, TargetType target, bool allowDead)
        {
            switch (target)
            {
                case TargetType.EnemigoUno:
                case TargetType.EnemigoArea:
                case TargetType.EnemigosTodos:
                    return Opponents(actor).ToList();
                case TargetType.AliadoUno:
                case TargetType.AliadosTodos:
                    return Allies(actor).Where(u => allowDead || u.IsAlive).ToList();
                default:
                    return new List<BattleUnit> { actor };
            }
        }

        // ---------------------------------------------------------------- tiempo

        public void Tick(float dt)
        {
            if (Outcome != BattleOutcome.EnCurso || Paused) return;
            if (Mode == BattleMode.Espera && MenuOpen) return;
            float scaled = dt * Speed;

            foreach (var unit in Units)
            {
                if (!unit.IsAlive) continue;

                if (unit.IsReady)
                {
                    if (unit.Kind != UnitKind.Mascota) continue;
                    // La mascota espera un instante por si el jugador quiere usarla en un combo.
                    unit.PetHoldTimer -= scaled;
                    if (unit.PetHoldTimer <= 0f)
                    {
                        Execute(PetAI.Choose(this, unit));
                        return;
                    }
                    continue;
                }

                unit.Atb += AtbRate(unit) * scaled;
                if (unit.Atb < AtbMax) continue;
                unit.Atb = AtbMax;
                if (BeginTurn(unit)) return;
            }
        }

        /// <summary>Comienza el turno de una unidad. Devuelve true si se produjo una acción automática.</summary>
        private bool BeginTurn(BattleUnit unit)
        {
            unit.IsDefending = false;
            bool skip = unit.HasStatus(StatusType.Sueno) || unit.HasStatus(StatusType.Aturdido);
            ProcessTurnStatuses(unit);
            if (!unit.IsAlive) { CheckOutcome(); return true; }

            if (skip)
            {
                Emit(new BattleEvent { Type = BattleEventType.TurnSkipped, Actor = unit });
                unit.Atb = 0f;
                return true;
            }

            switch (unit.Kind)
            {
                case UnitKind.Enemigo:
                    Execute(EnemyAI.Choose(this, unit));
                    return true;
                case UnitKind.Mascota:
                    unit.IsReady = true;
                    unit.PetHoldTimer = PetHoldSeconds;
                    return false;
                default:
                    if (unit.HasStatus(StatusType.Confusion))
                    {
                        var candidates = Units.Where(u => u.IsAlive && u != unit).ToList();
                        Execute(BattleAction.Attack(unit, candidates[rng.Range(0, candidates.Count)]));
                        return true;
                    }
                    unit.IsReady = true;
                    readyPlayers.Add(unit);
                    Emit(new BattleEvent { Type = BattleEventType.TurnReady, Actor = unit });
                    return false;
            }
        }

        private void ProcessTurnStatuses(BattleUnit unit)
        {
            if (unit.HasStatus(StatusType.Veneno))
            {
                int dmg = Math.Max(1, unit.MaxHp / 16);
                ApplyDamage(null, unit, dmg, false, 1f, null);
            }
            if (unit.HasStatus(StatusType.Regeneracion) && unit.IsAlive)
            {
                int heal = Math.Max(1, unit.MaxHp / 16);
                unit.Hp = Math.Min(unit.MaxHp, unit.Hp + heal);
                Emit(new BattleEvent { Type = BattleEventType.Heal, Target = unit, Amount = heal });
            }
            foreach (var status in unit.Statuses.ToList())
            {
                status.Remaining--;
                if (status.Remaining <= 0)
                {
                    unit.Statuses.Remove(status);
                    Emit(new BattleEvent { Type = BattleEventType.StatusRemoved, Target = unit, Status = status.Type });
                }
            }
        }

        // ---------------------------------------------------------------- órdenes

        /// <summary>Envía la orden de un humano listo. Devuelve false si no es válida.</summary>
        public bool Submit(BattleAction action)
        {
            if (Outcome != BattleOutcome.EnCurso || !readyPlayers.Contains(action.Actor)) return false;
            if (!IsValid(action)) return false;
            Execute(action);
            return true;
        }

        private bool IsValid(BattleAction action)
        {
            switch (action.Type)
            {
                case ActionType.Tecnica:
                    return CanUseTechnique(action.Actor, action.TechniqueId);
                case ActionType.Combo:
                    ComboDef combo;
                    List<BattleUnit> participants;
                    return db.Combos.TryGetValue(action.ComboId, out combo) && TryGetComboParticipants(combo, out participants) && participants.Contains(action.Actor);
                case ActionType.Objeto:
                    ItemDef item;
                    return Inventory.Count(action.ItemId) > 0 && db.Items.TryGetValue(action.ItemId, out item) && item.UsableInBattle && item.Effect != null;
                case ActionType.ReglaDramatica:
                    return Rule != null && Rule.GetCommands(this, action.Actor).Any(c => c.Id == action.RuleCommandId);
                default:
                    return true;
            }
        }

        private void Execute(BattleAction action)
        {
            var participants = new List<BattleUnit> { action.Actor };
            var start = new BattleEvent { Type = BattleEventType.ActionStart, Actor = action.Actor, Target = action.Target, ActionType = action.Type, TechniqueId = action.TechniqueId, ComboId = action.ComboId, ItemId = action.ItemId };

            switch (action.Type)
            {
                case ActionType.Atacar:
                    Emit(start);
                    Resolve(participants, EffectSpec.BasicAttack(action.Actor), action.Target);
                    break;

                case ActionType.Tecnica:
                {
                    var tech = db.Technique(action.TechniqueId);
                    action.Actor.Mp -= tech.MpCost;
                    Emit(start);
                    Resolve(participants, EffectSpec.From(tech), action.Target);
                    break;
                }

                case ActionType.Combo:
                {
                    var combo = db.Combos[action.ComboId];
                    TryGetComboParticipants(combo, out participants);
                    for (int i = 0; i < combo.Participants.Count; i++)
                    {
                        var p = combo.Participants[i];
                        if (p.TechniqueId != null) participants[i].Mp -= db.Technique(p.TechniqueId).MpCost;
                        else if (p.IsPet) participants[i].Mp -= combo.PetMpCost;
                        if (p.IsPet) PetCombos++;
                    }
                    start.Participants = participants;
                    Emit(start);
                    Resolve(participants, EffectSpec.From(combo), action.Target);
                    break;
                }

                case ActionType.Objeto:
                    Emit(start);
                    UseItem(action.Actor, db.Item(action.ItemId), action.Target);
                    break;

                case ActionType.Defender:
                    action.Actor.IsDefending = true;
                    Emit(new BattleEvent { Type = BattleEventType.Defend, Actor = action.Actor });
                    break;

                case ActionType.Huir:
                    Emit(start);
                    TryFlee(action.Actor);
                    break;

                case ActionType.ReglaDramatica:
                    Emit(start);
                    Rule.ExecuteCommand(this, action.Actor, action.RuleCommandId, action.Target);
                    break;
            }

            foreach (var p in participants) EndTurn(p);
            if (Rule != null && Outcome == BattleOutcome.EnCurso) Rule.OnAfterAction(this, action);
            CheckOutcome();
        }

        private void EndTurn(BattleUnit unit)
        {
            unit.Atb = 0f;
            unit.IsReady = false;
            unit.PetHoldTimer = 0f;
            readyPlayers.Remove(unit);
        }

        // ---------------------------------------------------------------- resolución

        public List<BattleUnit> ResolveTargets(BattleUnit actor, EffectSpec spec, BattleUnit primary)
        {
            // Un actor confundido puede golpear a cualquiera, incluidos sus aliados.
            if (actor.HasStatus(StatusType.Confusion) && spec.Target == TargetType.EnemigoUno && primary != null && primary.IsAlive)
                return new List<BattleUnit> { primary };

            bool allowDead = spec.Revive > 0f;
            var valid = ValidTargets(actor, spec.Target, allowDead);
            if (valid.Count == 0) return valid;

            if (primary == null || !valid.Contains(primary)) primary = valid[rng.Range(0, valid.Count)];

            switch (spec.Target)
            {
                case TargetType.EnemigoArea:
                    return valid.Where(u => u == primary || u.Position.DistanceTo(primary.Position) <= spec.AreaRadius).ToList();
                case TargetType.EnemigosTodos:
                case TargetType.AliadosTodos:
                    return valid;
                default:
                    return new List<BattleUnit> { primary };
            }
        }

        public void Resolve(List<BattleUnit> attackers, EffectSpec spec, BattleUnit primary)
        {
            var actor = attackers[0];
            var targets = ResolveTargets(actor, spec, primary);
            bool physical = spec.Kind == TechniqueKind.Fisica;
            int attackStat = DamageCalculator.CombinedStat(attackers, physical ? StatType.Fuerza : StatType.Magia);
            int magic = DamageCalculator.CombinedStat(attackers, StatType.Magia);
            float outgoing = attackers.Max(a => a.OutgoingDamageMultiplier);

            foreach (var target in targets)
            {
                if (spec.Healing || spec.Revive > 0f)
                {
                    if (!target.IsAlive)
                    {
                        if (spec.Revive <= 0f) continue;
                        target.Hp = Math.Max(1, (int)(target.MaxHp * spec.Revive));
                        target.Atb = 0f;
                        Emit(new BattleEvent { Type = BattleEventType.Revive, Actor = actor, Target = target, Amount = target.Hp });
                    }
                    else if (spec.Healing)
                    {
                        int heal = Math.Min(target.MaxHp - target.Hp, DamageCalculator.Healing(spec.Power, magic, rng));
                        target.Hp += heal;
                        Emit(new BattleEvent { Type = BattleEventType.Heal, Actor = actor, Target = target, Amount = heal });
                    }
                }
                else if (spec.Power > 0)
                {
                    var result = DamageCalculator.Compute(spec, attackStat, actor.Stats.Get(StatType.Precision), actor.Stats.Get(StatType.Suerte),
                        actor.HasStatus(StatusType.Ceguera), outgoing, target, rng);
                    if (!result.Hit)
                    {
                        Emit(new BattleEvent { Type = BattleEventType.Miss, Actor = actor, Target = target });
                        continue;
                    }
                    if (result.Amount < 0)
                    {
                        int heal = Math.Min(target.MaxHp - target.Hp, -result.Amount);
                        target.Hp += heal;
                        Emit(new BattleEvent { Type = BattleEventType.Heal, Actor = actor, Target = target, Amount = heal, ElementMultiplier = result.ElementMultiplier });
                        continue;
                    }
                    if (result.Amount == 0)
                    {
                        Emit(new BattleEvent { Type = BattleEventType.Immune, Actor = actor, Target = target, ElementMultiplier = 0f });
                        continue;
                    }
                    ApplyDamage(actor, target, result.Amount, result.Critical, result.ElementMultiplier, spec);
                }

                if (!target.IsAlive) continue;
                foreach (var cure in spec.CureStatuses) CureStatus(target, cure);
                if (spec.ApplyStatus.HasValue && rng.Value() < spec.StatusChance)
                    ApplyStatus(target, spec.ApplyStatus.Value, spec.StatusDuration);
            }
        }

        public void ApplyDamage(BattleUnit attacker, BattleUnit target, int amount, bool critical, float elementMultiplier, EffectSpec spec)
        {
            if (Rule != null) amount = Rule.ModifyDamage(this, attacker, target, amount, spec);
            if (amount <= 0)
            {
                Emit(new BattleEvent { Type = BattleEventType.Immune, Actor = attacker, Target = target });
                return;
            }

            target.Hp -= amount;
            Emit(new BattleEvent { Type = BattleEventType.Damage, Actor = attacker, Target = target, Amount = amount, Critical = critical, ElementMultiplier = elementMultiplier });

            if (target.HasStatus(StatusType.Sueno)) CureStatus(target, StatusType.Sueno);

            if (target.Hp <= 0)
            {
                if (Rule != null && !Rule.CanDie(this, target))
                {
                    target.Hp = 1;
                    return;
                }
                target.Hp = 0;
                target.Statuses.Clear();
                EndTurn(target);
                Emit(new BattleEvent { Type = BattleEventType.Death, Actor = attacker, Target = target });
            }
        }

        public void ApplyStatus(BattleUnit target, StatusType status, int duration)
        {
            if (target.Immunities.Contains(status))
            {
                Emit(new BattleEvent { Type = BattleEventType.Immune, Target = target, Status = status });
                return;
            }
            var existing = target.Statuses.FirstOrDefault(s => s.Type == status);
            if (existing != null) existing.Remaining = Math.Max(existing.Remaining, duration);
            else target.Statuses.Add(new StatusInstance { Type = status, Remaining = duration });
            Emit(new BattleEvent { Type = BattleEventType.StatusApplied, Target = target, Status = status });
        }

        public void CureStatus(BattleUnit target, StatusType status)
        {
            if (!target.HasStatus(status)) return;
            target.RemoveStatus(status);
            Emit(new BattleEvent { Type = BattleEventType.StatusRemoved, Target = target, Status = status });
        }

        private void UseItem(BattleUnit actor, ItemDef item, BattleUnit primary)
        {
            Inventory.Remove(item.Id);
            var effect = item.Effect;
            var targets = effect.AllAllies
                ? Allies(actor).ToList()
                : new List<BattleUnit> { primary ?? actor };

            foreach (var target in targets)
            {
                if (target.Team != actor.Team) continue;
                if (!target.IsAlive)
                {
                    if (effect.Revive <= 0f) continue;
                    target.Hp = Math.Max(1, (int)(target.MaxHp * effect.Revive));
                    target.Atb = 0f;
                    Emit(new BattleEvent { Type = BattleEventType.Revive, Actor = actor, Target = target, Amount = target.Hp, ItemId = item.Id });
                    continue;
                }
                int heal = effect.HealHp + (int)(target.MaxHp * effect.HealHpPercent);
                if (heal > 0)
                {
                    heal = Math.Min(target.MaxHp - target.Hp, heal);
                    target.Hp += heal;
                    Emit(new BattleEvent { Type = BattleEventType.Heal, Actor = actor, Target = target, Amount = heal, ItemId = item.Id });
                }
                if (effect.HealMp > 0)
                {
                    int mp = Math.Min(target.MaxMp - target.Mp, effect.HealMp);
                    target.Mp += mp;
                    Emit(new BattleEvent { Type = BattleEventType.MpRestore, Actor = actor, Target = target, Amount = mp, ItemId = item.Id });
                }
                foreach (var cure in effect.CureStatuses) CureStatus(target, cure);
            }
        }

        private void TryFlee(BattleUnit actor)
        {
            if (Encounter != null && !Encounter.CanFlee)
            {
                Emit(new BattleEvent { Type = BattleEventType.FleeFailed, Actor = actor, TextKey = "battle.flee.impossible" });
                return;
            }
            double partySpeed = Alive(Team.Jugador).Average(u => u.Stats.Get(StatType.Velocidad));
            double enemySpeed = Alive(Team.Enemigo).Average(u => u.Stats.Get(StatType.Velocidad));
            double chance = Math.Max(0.1, Math.Min(0.95, 0.5 + (partySpeed - enemySpeed) * 0.02));
            if (rng.Value() < chance)
            {
                Outcome = BattleOutcome.Huida;
                Emit(new BattleEvent { Type = BattleEventType.Fled, Actor = actor });
            }
            else
            {
                Emit(new BattleEvent { Type = BattleEventType.FleeFailed, Actor = actor, TextKey = "battle.flee.failed" });
            }
        }

        // ---------------------------------------------------------------- final

        private void CheckOutcome()
        {
            if (Outcome != BattleOutcome.EnCurso) return;

            if (!Alive(Team.Enemigo).Any())
            {
                Outcome = BattleOutcome.Victoria;
                Rewards = ComputeRewards();
                readyPlayers.Clear();
                Emit(new BattleEvent { Type = BattleEventType.Victory });
                return;
            }

            // La derrota depende de los humanos: la mascota sola no sostiene el combate.
            if (!Units.Any(u => u.Team == Team.Jugador && u.Kind == UnitKind.Humano && u.IsAlive))
            {
                Outcome = BattleOutcome.Derrota;
                readyPlayers.Clear();
                Emit(new BattleEvent { Type = BattleEventType.Defeat });
            }
        }

        private BattleRewards ComputeRewards()
        {
            var rewards = new BattleRewards();
            foreach (var enemy in Units.Where(u => u.Kind == UnitKind.Enemigo && u.EnemyDef != null))
            {
                rewards.Xp += enemy.EnemyDef.Xp;
                rewards.Money += enemy.EnemyDef.Money;
                foreach (var drop in enemy.EnemyDef.Drops)
                    if (rng.Value() < drop.Chance) rewards.Items.Add(drop.ItemId);
            }
            return rewards;
        }
    }
}
