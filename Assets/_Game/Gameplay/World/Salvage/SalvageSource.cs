using System;
using PirateGame.Core;
using PirateGame.Rules.Application;
using UnityEngine;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Gameplay.World
{
    public sealed class SalvageSource : MonoBehaviour, IEntityStatePort
    {
        [SerializeField] private string authoredId;
        private CampaignSession session;
        private string definition, region;
        private double x, z;
        private EntityId identity;
        // Authored sites and generated salvage (e.g. wrecks of sunk ships) alike.
        public EntityId Id => identity.IsValid ? identity : EntityId.Authored(authoredId);
        public bool Depleted => Capture().Loot.Count == 0;

        public void Bind(CampaignSession owner, EntityState state)
        {
            if (owner.Snapshot.Expedition == null || !owner.Snapshot.Expedition.Entities.TryGetValue(state.Id, out var current) || !ReferenceEquals(current, state))
                throw new ArgumentException("Bind from the authoritative expedition ledger.");
            session = owner; identity = state.Id; authoredId = state.Id.AuthoredId; definition = state.DefinitionId;
            region = state.Position.RegionId; x = state.Position.X; z = state.Position.Z;
            Apply(state);
        }
        public EntityState Capture()
        {
            if (session?.Snapshot.Expedition == null || !session.Snapshot.Expedition.Entities.TryGetValue(Id, out var state))
                throw new InvalidOperationException("Source is not bound to an active expedition.");
            return state;
        }
        public RuleResult Restore(EntityState state)
        {
            if (state == null || !state.Id.Equals(Id) || state.DefinitionId != definition || state.Position.RegionId != region ||
                state.Position.X != x || state.Position.Z != z) return new RuleResult(RuleError.InvalidRequest);
            // Presentation cannot install a second writable copy or refill the ledger.
            if (!ReferenceEquals(state, Capture())) return new RuleResult(RuleError.InvalidRequest, detail: "Restore from the bound session snapshot.");
            Apply(state); return new RuleResult();
        }
        private void LateUpdate()
        {
            if (session?.Snapshot.Expedition != null) Apply(Capture());
            else foreach (var view in GetComponentsInChildren<Renderer>(true)) view.enabled = false;
        }
        private void Apply(EntityState state)
        {
            transform.position = new Vector3((float)state.Position.X, 0, (float)state.Position.Z);
            foreach (var view in GetComponentsInChildren<Renderer>(true)) view.enabled = state.Loot.Count > 0;
        }
    }
}
