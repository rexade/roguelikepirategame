using System;
using PirateGame.Core;
using PirateGame.Gameplay.Ships;
using PirateGame.Rules.Application;
using UnityEngine;

namespace PirateGame.Gameplay.World
{
    public sealed class SalvageInteraction : MonoBehaviour
    {
        public ShipSimulation simulation;
        public SalvageSource[] sources = Array.Empty<SalvageSource>();
        public float range = 5;
        // Isolated fixtures collect on the Interact edge. Production composition clears
        // this and calls TryCollect itself after capturing a coherent checkpoint.
        public bool collectOnInteractIntent = true;
        public RuleResult LastResult { get; private set; }
        private bool waiting;
        private void OnEnable() { if (simulation != null) simulation.TickStarted += OnTick; }
        private void OnDisable() { if (simulation != null) simulation.TickStarted -= OnTick; }
        private void Update()
        {
            if (waiting && !simulation.HasPendingStep)
            { LastResult = simulation.LastPickupResult; waiting = LastResult.IsPending; }
        }
        private void OnTick(InputIntent intent) { if (intent.Interact && collectOnInteractIntent) TryCollect(Guid.NewGuid()); }
        public SalvageSource Nearest()
        {
            var origin = simulation.motor.interactionOrigin.position;
            SalvageSource nearest = null; float best = range * range;
            foreach (var source in sources)
            {
                if (source == null || !source.isActiveAndEnabled || source.Depleted) continue;
                var delta = source.transform.position - origin; delta.y = 0;
                if (delta.sqrMagnitude > best) continue;
                nearest = source; best = delta.sqrMagnitude;
            }
            return nearest;
        }
        public RuleResult TryCollect(Guid request)
        {
            if (simulation.Session?.Snapshot.Expedition == null) return LastResult = new RuleResult(RuleError.WrongLifecycle);
            if (waiting) return new RuleResult(RuleError.Busy);
            var nearest = Nearest();
            if (nearest == null) return LastResult = new RuleResult(RuleError.UnknownId, detail: "No salvage in reach.");
            LastResult = simulation.CollectLoot(request, simulation.Session.Snapshot.Expedition.Id, nearest.Id);
            waiting = LastResult.IsPending;
            return LastResult;
        }
    }
}
