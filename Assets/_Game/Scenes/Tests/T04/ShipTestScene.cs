using System;
using System.Collections.Generic;
using PirateGame.Core;
using PirateGame.Rules.Application;
using PirateGame.Gameplay.Ships;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PirateGame.Tests.T04
{
    // Isolated fixture only. Production injects the campaign owned by Bootstrap.
    public sealed class ShipTestScene : MonoBehaviour
    {
        public ShipSimulation simulation;
        public static CampaignSession CreateSession()
        {
            var catalog = new DefinitionCatalog(new Dictionary<string, int> { ["wood"] = 1 },
                new[] { new HullDefinition("test-cutter", new Dictionary<string, SlotKind>(), new[] {
                    new StatDefinition("health", 100, 1, 100), new StatDefinition("cargo", 10, 0, 100), new StatDefinition("speed", 8, 1, 50) }) },
                Array.Empty<EquipmentDefinition>(), new[] { new HubDefinition("test-harbor", new SeaPosition("test-region", 7, -9), 5, 1) },
                Array.Empty<UpgradeDefinition>(), Array.Empty<string>(), Array.Empty<AuthoredIdentity>(), Array.Empty<string>(), new[] { "test-region" });
            var initial = CampaignSession.NewCampaign(catalog, "test-harbor", "test-cutter", new Dictionary<string, string>(), new Dictionary<string, string>());
            var session = new CampaignSession(catalog, initial, new MemoryStore(), new ReadyArrival());
            session.RetryArrival();
            session.Embark(Guid.NewGuid(), new EmbarkPlan(Guid.NewGuid(), 4, "t04", Array.Empty<EntityState>(), new Dictionary<string, string>()));
            return session;
        }
        private void Awake() { simulation.Bind(CreateSession()); }
        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
                simulation.SetPaused(!simulation.Session.IsPaused);
        }
        private sealed class MemoryStore : ISaveStore { public RuleResult Commit(SaveCandidate candidate) => new RuleResult(); }
        private sealed class ReadyArrival : IWorldArrival { public RuleResult EnsureReady(ArrivalRequest request) => new RuleResult(); }
    }
}
