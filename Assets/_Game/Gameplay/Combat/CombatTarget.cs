using System;
using PirateGame.Core;
using PirateGame.Gameplay.Ships;
using UnityEngine;

namespace PirateGame.Gameplay.Combat
{
    public sealed class CombatTarget : MonoBehaviour
    {
        public ShipMotor motor;
        public string Key { get; private set; }
        public int Team { get; private set; }
        public double Health => readHealth != null ? readHealth() : health;
        public double MaximumHealth { get; private set; }
        public bool Defeated => Health <= 0;
        // Enemy-only. Player lifecycle success is the session's committed Sink event.
        public event Action<CombatTarget> Died;
        private double health;
        private Func<double> readHealth;
        private Action<double> playerDamage;
        private Collider[] colliders;
        private bool[] colliderEnabled;
        public void Initialize(string key, int team, double health, Action<double> damage = null, Func<double> healthQuery = null)
        {
            Key = Values.Id(key); Team = team;
            if (Values.Finite(health) <= 0) throw new ArgumentException("Positive maximum health required.");
            MaximumHealth = this.health = health; playerDamage = damage; readHealth = healthQuery;
            colliders = GetComponentsInChildren<Collider>(); colliderEnabled = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++) colliderEnabled[i] = colliders[i].enabled;
        }
        public void ApplyDamage(double amount)
        {
            if (Values.Finite(amount) < 0) throw new ArgumentException("Negative damage.");
            if (Defeated) return;
            if (playerDamage != null) { playerDamage(amount); return; }
            health = Math.Max(0, health - amount);
            if (Defeated) { SetCollision(); motor?.Suspend(true); Died?.Invoke(this); }
        }
        // Restore replaces state without publishing another death.
        public void RestoreHealth(double health)
        {
            if (readHealth != null) throw new InvalidOperationException("Restore authoritative player state through the session.");
            if (Values.Finite(health) < 0 || health > MaximumHealth) throw new ArgumentException("Invalid health.");
            this.health = health;
            SetCollision();
        }
        private void SetCollision()
        {
            if (colliders == null) return;
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = colliderEnabled[i] && !Defeated;
        }
    }
}
