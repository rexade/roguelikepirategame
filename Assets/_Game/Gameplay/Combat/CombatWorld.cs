using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Core;
using PirateGame.Content.Combat;
using PirateGame.Gameplay.Ships;
using PirateGame.Rules.Application;
using UnityEngine;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Gameplay.Combat
{
    public interface ICombatEnemy : IEntityStatePort
    {
        CombatTarget Target { get; }
        void Step(Vector3 player, float delta, Action<CombatTarget, WeaponSpec, Vector3> fire);
        void Suspend();
        RuleResult Validate(EntityState state);
    }
    [Serializable] public sealed class CombatContext
    {
        public int version = 1;
        public long generation;
        public string weapon;
        public float playerYaw;
        public ShotState[] shots = Array.Empty<ShotState>();
    }
    public sealed class CombatWorld : MonoBehaviour, IExpeditionCapture
    {
        public const string PlayerKey = "player";
        public const string WeaponTimer = "combat.weapon";
        public const string AbilityTimer = "combat.ability";
        public const string BraceTimer = "combat.brace";
        public ShipSimulation Simulation { get; private set; }
        public CombatTarget Player { get; private set; }
        public CombatCatalog Catalog { get; private set; }
        public WeaponSpec Weapon { get; private set; }
        public double WeaponCooldown { get; private set; }
        public double AbilityCooldown { get; private set; }
        public double BraceRemaining { get; private set; }
        public IReadOnlyList<SweptProjectile> Projectiles => shots;
        public IReadOnlyList<ICombatEnemy> Enemies => enemies;
        public event Action<SweptProjectile> ShotFired;
        private readonly List<SweptProjectile> shots = new List<SweptProjectile>();
        private readonly List<ICombatEnemy> enemies = new List<ICombatEnemy>();
        private EntityId contextId;
        private long generation;
        private bool subscribed;

        public void Bind(ShipSimulation simulation, CombatCatalog catalog, CombatTarget player, IEnumerable<ICombatEnemy> targets, EntityId captureId)
        {
            if (Simulation != null) throw new InvalidOperationException("Create a new combat world for a new binding.");
            if (simulation.HasPendingStep || !captureId.IsValid) throw new ArgumentException("Bind at an idle boundary with a stable context identity.");
            var session = simulation.Session;
            var expedition = session.Snapshot.Expedition;
            if (expedition == null) throw new InvalidOperationException("Bind combat to an active expedition.");
            Catalog = catalog; Weapon = catalog.EquippedWeapon(simulation.Session.Snapshot.Campaign);
            var list = targets.ToList();
            if (list.Any(e => !e.Id.IsValid || e.Id.Equals(captureId)) || list.Select(e => e.Id).Distinct().Count() != list.Count)
                throw new ArgumentException("Duplicate combat entity identity.");
            Simulation = simulation; Player = player; contextId = captureId; enemies.AddRange(list);
            player.Initialize(PlayerKey, 0, simulation.Session.ShipStats()["health"], amount =>
                Simulation.AddDamage(amount * (BraceRemaining > 0 ? Catalog.DamageMultiplier : 1)), () =>
                {
                    // Query publication directly: no coroutine/subscriber ordering or event draining.
                    var snapshot = session.Snapshot;
                    if (snapshot.Expedition != null && snapshot.Expedition.Id == expedition.Id)
                        return snapshot.Expedition.Health;
                    if (snapshot.Campaign.ResolvedExpeditions.TryGetValue(expedition.Id, out var outcome))
                        return outcome == Outcome.Sunk ? 0 : player.MaximumHealth;
                    throw new InvalidOperationException("Bound expedition is absent from the session.");
                });
            Subscribe();
        }
        private void Subscribe()
        {
            if (!subscribed && Simulation != null && isActiveAndEnabled)
            { Simulation.TickStarted += Tick; Simulation.TickSkipped += FreezeEnemies; subscribed = true; }
        }
        private void OnEnable() => Subscribe();
        private void OnDisable()
        {
            if (subscribed) { Simulation.TickStarted -= Tick; Simulation.TickSkipped -= FreezeEnemies; }
            subscribed = false;
            FreezeEnemies();
        }
        // Enemies move only inside accepted ticks. Between ticks they stay dynamic,
        // so Rigidbody interpolation keeps them smooth on screen; they freeze before
        // any physics step that belongs to no tick (TickSkipped).
        private void FixedUpdate()
        {
            // A disabled simulation raises no events: freeze before its physics step.
            if (Simulation != null && !Simulation.isActiveAndEnabled) FreezeEnemies();
        }
        private void FreezeEnemies()
        {
            foreach (var enemy in enemies) enemy.Suspend();
        }
        private void Tick(InputIntent intent)
        {
            float delta = (float)Simulation.Session.FixedDeltaSeconds;
            WeaponCooldown = Math.Max(0, WeaponCooldown - delta);
            AbilityCooldown = Math.Max(0, AbilityCooldown - delta);
            BraceRemaining = Math.Max(0, BraceRemaining - delta);
            if (intent.Ability && AbilityCooldown <= 0) { AbilityCooldown = Catalog.AbilityCooldown; BraceRemaining = Catalog.BraceDuration; }
            if (intent.Fire && WeaponCooldown <= 0)
            {
                float damage = Weapon.Damage;
                if (Simulation.Session.ShipStats().TryGetValue("damage-scale", out var scale)) damage *= (float)scale;
                Fire(Player, Weapon, Simulation.motor.AimDirection, damage);
                WeaponCooldown = Weapon.Cooldown;
            }
            foreach (var enemy in enemies) enemy.Step(Simulation.motor.Body.position, delta, (target, weapon, aim) => Fire(target, weapon, aim, weapon.Damage));
            Physics.SyncTransforms();
            foreach (var shot in shots) shot.Step(delta);
        }
        private void Fire(CombatTarget target, WeaponSpec weapon, Vector3 direction, float damage)
        {
            var shot = shots.FirstOrDefault(s => !s.Active);
            if (shot == null) { shot = new SweptProjectile(); shots.Add(shot); }
            shot.Launch(checked(++generation), target.Key, target.Team, target.motor.weaponOrigin.position, direction, weapon, damage);
            ShotFired?.Invoke(shot);
        }
        // T10 streaming: add or remove an enemy at an idle boundary without rebinding.
        // The session ledger keeps a detached enemy's last captured state; take a
        // checkpoint before detaching so that state is current.
        public void Attach(ICombatEnemy enemy)
        {
            Boundary();
            if (enemy == null || !enemy.Id.IsValid || enemy.Id.Equals(contextId) || enemies.Any(e => e.Id.Equals(enemy.Id)))
                throw new ArgumentException("Duplicate or invalid combat entity identity.");
            enemies.Add(enemy);
            enemy.Suspend();
        }

        public bool Detach(EntityId id)
        {
            Boundary();
            var enemy = enemies.FirstOrDefault(e => e.Id.Equals(id));
            if (enemy == null) return false;
            enemy.Suspend();
            enemies.Remove(enemy);
            // Shots of a ship leaving the loaded world vanish rather than outlive their owner.
            foreach (var shot in shots) if (shot.Active && shot.Owner == enemy.Target.Key) shot.Retire();
            return true;
        }

        private void Boundary()
        {
            if (Simulation == null || Simulation.HasPendingStep || Simulation.Session.Snapshot.Expedition == null)
                throw new InvalidOperationException("Combat capture/restore requires an idle at-sea boundary.");
        }
        public ExpeditionState Capture(Guid expeditionId, long tick)
        {
            Boundary();
            var state = Simulation.Session.Snapshot.Expedition;
            if (state.Id != expeditionId || state.Tick != tick) throw new ArgumentException("Wrong capture boundary.");
            var ledger = state.Entities.ToDictionary(p => p.Key, p => p.Value);
            foreach (var enemy in enemies)
            {
                var captured = enemy.Capture();
                // The application owns depletion; never refill an already collected wreck.
                if (ledger.TryGetValue(enemy.Id, out var prior))
                    captured = new EntityState(captured.Id, captured.DefinitionId, captured.Position, captured.Health,
                        captured.Defeated, prior.Loot, captured.Cooldowns, captured.BehaviorState);
                ledger[enemy.Id] = captured;
            }
            var context = new CombatContext { generation = generation, weapon = Weapon.Id, playerYaw = Simulation.motor.Body.rotation.eulerAngles.y,
                shots = shots.Where(s => s.Active).Select(s => s.Capture()).ToArray() };
            ledger[contextId] = new EntityState(contextId, CombatCatalog.ContextDefinition, state.Position, 1, false,
                new Dictionary<string, int>(), new Dictionary<string, double>(), JsonUtility.ToJson(context));
            var timers = Values.Copy(state.Cooldowns);
            timers[WeaponTimer] = WeaponCooldown; timers[AbilityTimer] = AbilityCooldown; timers[BraceTimer] = BraceRemaining;
            return new ExpeditionState(state.Id, state.Seed, state.RngState, state.Tick, state.Health, state.Speed, state.Position,
                state.Cargo, state.Modifiers, timers, state.Encounters, ledger.Values);
        }
        public RuleResult Restore(ExpeditionState state)
        {
            try
            {
                Boundary();
                var current = Simulation.Session.Snapshot.Expedition;
                if (!Simulation.Session.IsPaused) return new RuleResult(RuleError.Busy, detail: "Suspend the simulation before restoring.");
                if (state == null || state.Id != current.Id) return new RuleResult(RuleError.WrongExpedition);
                if (state.Tick != current.Tick || state.Health != current.Health || state.Position.X != current.Position.X ||
                    state.Position.Z != current.Position.Z || state.Position.RegionId != current.Position.RegionId)
                    return new RuleResult(RuleError.InvalidRequest, detail: "Restore the authoritative session before combat.");
                if (!state.Entities.TryGetValue(contextId, out var entity) || entity.DefinitionId != CombatCatalog.ContextDefinition)
                    return new RuleResult(RuleError.UnknownId);
                var context = JsonUtility.FromJson<CombatContext>(entity.BehaviorState);
                if (context == null || context.version != 1 || context.weapon != Weapon.Id || context.generation < 0 || context.shots == null ||
                    !float.IsFinite(context.playerYaw) || !float.IsFinite((float)state.Position.X) || !float.IsFinite((float)state.Position.Z))
                    return new RuleResult(RuleError.InvalidRequest);
                foreach (var enemy in enemies)
                    if (!state.Entities.TryGetValue(enemy.Id, out var e) || !enemy.Validate(e).IsSuccess) return new RuleResult(RuleError.InvalidRequest);
                var restored = new List<SweptProjectile>();
                var owners = enemies.ToDictionary(e => e.Target.Key, e => e.Target.Team); owners.Add(PlayerKey, 0);
                var identities = new HashSet<long>();
                foreach (var shot in context.shots)
                {
                    if (shot == null || !Catalog.Weapons.TryGetValue(shot.weapon ?? "", out var spec) || !owners.TryGetValue(shot.owner ?? "", out var team) ||
                        team != shot.team || shot.generation > context.generation || !identities.Add(shot.generation) ||
                        !float.IsFinite(shot.remaining) || shot.remaining <= 0 || shot.remaining > spec.Range ||
                        Math.Abs(shot.direction.sqrMagnitude - 1) > 0.001) return new RuleResult(RuleError.InvalidRequest);
                    var projectile = new SweptProjectile(); projectile.Restore(shot, spec); restored.Add(projectile);
                }
                double weaponTimer = Timer(state, WeaponTimer, Weapon.Cooldown);
                double abilityTimer = Timer(state, AbilityTimer, Catalog.AbilityCooldown);
                double braceTimer = Timer(state, BraceTimer, Catalog.BraceDuration);
                if (state.Health > Player.MaximumHealth) return new RuleResult(RuleError.InvalidRequest);
                // All validation precedes mutation, including every enemy and projectile.
                foreach (var enemy in enemies) enemy.Restore(state.Entities[enemy.Id]);
                generation = context.generation; shots.Clear(); shots.AddRange(restored);
                WeaponCooldown = weaponTimer; AbilityCooldown = abilityTimer; BraceRemaining = braceTimer;
                var motor = Simulation.motor;
                motor.Suspend(false);
                motor.Body.position = new Vector3((float)state.Position.X, 0, (float)state.Position.Z);
                motor.Body.rotation = Quaternion.Euler(0, context.playerYaw, 0);
                // T03 persists scalar player speed, not solver velocity or angular momentum.
                motor.Body.linearVelocity = motor.Body.rotation * Vector3.forward * (float)state.Speed;
                motor.Body.angularVelocity = Vector3.zero;
                motor.Suspend(true);
                return new RuleResult();
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException || e is OverflowException)
            { return new RuleResult(RuleError.InvalidRequest, detail: e.Message); }
        }
        private static double Timer(ExpeditionState state, string key, double maximum)
        {
            if (!state.Cooldowns.TryGetValue(key, out var timer) || timer < 0 || timer > maximum) throw new ArgumentException("Invalid combat timer: " + key);
            return timer;
        }
    }
}
