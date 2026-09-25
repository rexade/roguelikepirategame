using System;
using PirateGame.Content.Combat;
using UnityEngine;

namespace PirateGame.Gameplay.Combat
{
    [Serializable] public sealed class ShotState
    {
        public long generation;
        public string owner, weapon;
        public int team;
        public Vector3 position, direction;
        public float remaining, damage;
    }
    public sealed class SweptProjectile
    {
        public bool Active { get; private set; }
        public long Generation { get; private set; }
        public string Owner { get; private set; }
        public int Team { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public float Remaining { get; private set; }
        public float Damage { get; private set; }
        public WeaponSpec Weapon { get; private set; }
        public void Launch(long generation, string owner, int team, Vector3 position, Vector3 direction, WeaponSpec weapon, float damage)
        {
            if (generation <= Generation || direction.sqrMagnitude < 0.0001f || !Finite(position) || !Finite(direction)
                || !float.IsFinite(damage) || damage < 0) throw new ArgumentException("Invalid shot launch.");
            Generation = generation; Owner = PirateGame.Core.Values.Id(owner); Team = team;
            Position = position; Direction = direction.normalized; Weapon = weapon;
            Remaining = weapon.Range; Damage = damage; Active = true;
        }
        public bool Hit(CombatTarget target, long callbackGeneration)
        {
            if (!Active || callbackGeneration != Generation || target == null || target.Key == Owner || target.Team == Team || target.Defeated) return false;
            Active = false;
            target.ApplyDamage(Damage);
            return true;
        }
        public void Step(float delta)
        {
            if (!Active) return;
            float distance = Mathf.Min(Remaining, Weapon.Speed * delta);
            // Initial overlaps and ordered full sweeps prevent tunnelling and shooting through walls.
            foreach (var c in Physics.OverlapSphere(Position, Weapon.Radius, ~0, QueryTriggerInteraction.Ignore))
                if (Resolve(c)) return;
            var hits = Physics.SphereCastAll(Position, Weapon.Radius, Direction, distance, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
                if (Resolve(hit.collider)) { Position += Direction * hit.distance; return; }
            Position += Direction * distance; Remaining -= distance;
            if (Remaining <= 0) Active = false;
        }
        private bool Resolve(Collider collider)
        {
            var target = collider.GetComponentInParent<CombatTarget>();
            if (target != null) return Hit(target, Generation);
            Active = false; return true;
        }
        public ShotState Capture() => new ShotState { generation = Generation, owner = Owner, team = Team, weapon = Weapon.Id,
            position = Position, direction = Direction, remaining = Remaining, damage = Damage };
        public void Restore(ShotState state, WeaponSpec weapon)
        {
            Generation = 0;
            Launch(state.generation, state.owner, state.team, state.position, state.direction, weapon, state.damage);
            Remaining = state.remaining;
        }
        public static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
