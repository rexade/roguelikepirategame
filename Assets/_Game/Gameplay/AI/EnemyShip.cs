using System;
using System.Collections.Generic;
using PirateGame.Core;
using PirateGame.Content.Combat;
using PirateGame.Gameplay.Combat;
using PirateGame.Gameplay.Ships;
using PirateGame.Rules.Application;
using UnityEngine;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Gameplay.AI
{
    [Serializable] public sealed class EnemyMotion
    {
        public int version = 1;
        public string mode = "idle";
        public float yaw;
        public Vector3 velocity, angularVelocity;
    }
    [RequireComponent(typeof(ShipMotor), typeof(CombatTarget))]
    public sealed class EnemyShip : MonoBehaviour, ICombatEnemy
    {
        public EntityId Id { get; private set; }
        public CombatTarget Target { get; private set; }
        public EnemySpec Definition { get; private set; }
        public string Mode { get; private set; } = "idle";
        public double Cooldown { get; private set; }
        private string region;
        private ShipMotor motor;
        private Vector3 velocity, angularVelocity;
        private IReadOnlyDictionary<string, int> loot = new Dictionary<string, int>();
        public void Initialize(EntityId id, string regionId, EnemySpec definition)
        {
            if (!id.IsValid) throw new ArgumentException("Stable enemy identity required.");
            Id = id; region = Values.Id(regionId); Definition = definition;
            motor = GetComponent<ShipMotor>(); Target = GetComponent<CombatTarget>(); Target.motor = motor;
            Target.Initialize(id.ToString(), 1, definition.Health);
            motor.Configure(new Dictionary<string, double> { ["speed"] = definition.Speed });
            Cooldown = definition.ReloadSeconds;
            Target.Died += OnDeath;
        }
        private void OnDeath(CombatTarget target) { Mode = "defeated"; }
        private void OnDestroy() { if (Target != null) Target.Died -= OnDeath; }
        private void OnDisable() { if (motor != null) Suspend(); }
        public void Step(Vector3 player, float delta, Action<CombatTarget, WeaponSpec, Vector3> fire)
        {
            if (Target.Defeated || !isActiveAndEnabled) { Suspend(); return; }
            motor.Suspend(false);
            Cooldown = Math.Max(0, Cooldown - delta);
            var offset = player - motor.Body.position; offset.y = 0;
            float distance = offset.magnitude;
            var aim = distance > 0.001f ? offset / distance : transform.forward;
            bool engaged = distance <= Definition.EngagementRange;
            float throttle = 0;
            if (!engaged) Mode = "idle";
            else if (Definition.Tactic == EnemyTactic.Pursue)
            {
                Mode = distance > Definition.PreferredRange ? "pursue" : "attack";
                throttle = distance > Definition.PreferredRange ? 1 : 0;
            }
            else
            {
                Mode = distance < Definition.PreferredRange - 2 ? "retreat" : distance > Definition.PreferredRange + 2 ? "approach" : "attack";
                throttle = Mode == "retreat" ? -0.65f : Mode == "approach" ? 0.65f : 0;
            }
            float angle = Vector3.SignedAngle(motor.Body.rotation * Vector3.forward, aim, Vector3.up);
            float turn = engaged ? Mathf.Clamp(angle / 35, -1, 1) : 0;
            var travel = motor.Body.rotation * Vector3.forward * Mathf.Sign(throttle);
            // A short engine cast turns away from coastline and other hulls.
            if (throttle != 0 && Physics.SphereCast(motor.Body.position + Vector3.up * 0.5f, 1,
                travel, out var obstacle, 5, ~0, QueryTriggerInteraction.Ignore) && obstacle.collider.transform.root != transform.root)
            { throttle *= 0.2f; turn = 1; Mode = "avoid"; }
            motor.Step(new InputIntent(throttle, turn, aim.x, aim.z, false, false, false), throttle == 0, delta);
            if (engaged && distance <= Definition.Weapon.Range && Cooldown <= 0)
            {
                var firingAim = player - motor.weaponOrigin.position; firingAim.y = 0;
                fire(Target, Definition.Weapon, firingAim.sqrMagnitude > 0.001f ? firingAim.normalized : aim);
                Cooldown = Definition.ReloadSeconds;
            }
        }
        public void Suspend()
        {
            if (motor == null || motor.Body == null) return;
            if (!motor.Body.isKinematic) { velocity = motor.Body.linearVelocity; angularVelocity = motor.Body.angularVelocity; }
            motor.Suspend(true);
        }
        public EntityState Capture()
        {
            var p = motor.Body.position;
            var motion = new EnemyMotion { mode = Mode, yaw = motor.Body.rotation.eulerAngles.y,
                velocity = motor.Body.isKinematic ? velocity : motor.Body.linearVelocity,
                angularVelocity = motor.Body.isKinematic ? angularVelocity : motor.Body.angularVelocity };
            return new EntityState(Id, Definition.Id, new SeaPosition(region, p.x, p.z), Target.Health, Target.Defeated,
                loot, new Dictionary<string, double> { ["weapon"] = Cooldown }, JsonUtility.ToJson(motion));
        }
        public RuleResult Validate(EntityState state)
        {
            try
            {
                if (state == null || !state.Id.Equals(Id) || state.DefinitionId != Definition.Id || state.Position.RegionId != region)
                    return new RuleResult(RuleError.UnknownId);
                var motion = JsonUtility.FromJson<EnemyMotion>(state.BehaviorState);
                if (state.Health > Definition.Health || state.Defeated != (state.Health == 0) ||
                    !float.IsFinite((float)state.Position.X) || !float.IsFinite((float)state.Position.Z) ||
                    !state.Cooldowns.TryGetValue("weapon", out var timer) || timer > Definition.ReloadSeconds ||
                    motion == null || motion.version != 1 || !float.IsFinite(motion.yaw) ||
                    !SweptProjectile.Finite(motion.velocity) || !SweptProjectile.Finite(motion.angularVelocity) ||
                    Array.IndexOf(new[] { "idle", "pursue", "attack", "approach", "retreat", "avoid", "defeated" }, motion.mode) < 0 ||
                    (motion.mode == "defeated") != state.Defeated) return new RuleResult(RuleError.InvalidRequest);
                return new RuleResult();
            }
            catch (ArgumentException e) { return new RuleResult(RuleError.InvalidRequest, detail: e.Message); }
        }
        public RuleResult Restore(EntityState state)
        {
            var valid = Validate(state); if (!valid.IsSuccess) return valid;
            var motion = JsonUtility.FromJson<EnemyMotion>(state.BehaviorState);
            Target.RestoreHealth(state.Health); Mode = motion.mode; Cooldown = state.Cooldowns["weapon"]; loot = state.Loot;
            motor.Suspend(false);
            motor.Body.position = new Vector3((float)state.Position.X, 0, (float)state.Position.Z);
            motor.Body.rotation = Quaternion.Euler(0, motion.yaw, 0);
            motor.Body.linearVelocity = velocity = motion.velocity; motor.Body.angularVelocity = angularVelocity = motion.angularVelocity;
            motor.Suspend(true);
            return new RuleResult();
        }
    }
}
