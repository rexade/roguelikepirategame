using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Content.World;
using PirateGame.Core;
using PirateGame.Gameplay.Ships;
using PirateGame.Gameplay.World;
using PirateGame.Rules.Application;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PirateGame.Tests.T06
{
    // Test composition only; T08 injects its existing campaign and durable store.
    public sealed class SalvageFixture : MonoBehaviour
    {
        public ShipSimulation simulation;
        public SalvageRegion region;
        public SalvageInteraction interaction;
        public static DefinitionCatalog Catalog(FirstRegionAsset region, double capacity = 10) => new DefinitionCatalog(
            new Dictionary<string, int> { ["wood"] = 1, ["iron"] = 2 },
            new[] { new HullDefinition("starter", new Dictionary<string, SlotKind>(), new[] {
                new StatDefinition("health", 100, 1, 100), new StatDefinition("cargo", capacity, 0, 100), new StatDefinition("speed", 8, 1, 50) }) },
            Array.Empty<EquipmentDefinition>(), region.Hubs, Array.Empty<UpgradeDefinition>(), Array.Empty<string>(),
            region.Identities(region.name), new[] { "barrel", "wreck" }.Concat(region.encounterTable.Select(e => e.enemyId)).Distinct(),
            new[] { region.regionId });
        public static CampaignSession CreateSession(FirstRegionAsset region, ISaveStore store = null, double capacity = 10)
        {
            var catalog = Catalog(region, capacity); region.Validate(catalog);
            var initial = CampaignSession.NewCampaign(catalog, region.homeId, "starter", new Dictionary<string, string>(), new Dictionary<string, string>());
            var session = new CampaignSession(catalog, initial, store ?? new MemoryStore(), new ReadyArrival());
            session.RetryArrival();
            var result = session.Embark(Guid.NewGuid(), new EmbarkPlan(Guid.NewGuid(), 6, "t06:0",
                region.salvage.Select(s => s.Initial(region.regionId)), new Dictionary<string, string>()));
            if (!result.IsSuccess) throw new InvalidOperationException(result.Error.ToString());
            return session;
        }
        private void Start()
        {
            simulation.motor.Body.position = new Vector3(region.content.dock.x, 0, region.content.dock.y);
            simulation.Bind(CreateSession(region.content));
            Recreate();
        }
        public void Recreate()
        {
            if (simulation.HasPendingStep) return;
            var result = region.Recreate(simulation.Session);
            if (!result.IsSuccess) throw new InvalidOperationException(result.Detail);
            interaction.sources = region.Sources;
        }
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || simulation.Session == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) simulation.SetPaused(!simulation.Session.IsPaused);
            if (keyboard.rKey.wasPressedThisFrame && !simulation.HasPendingStep) Recreate();
            if (keyboard.fKey.wasPressedThisFrame && simulation.Session.Snapshot.Expedition != null)
                simulation.Session.RequestDock(Guid.NewGuid(), simulation.Session.Snapshot.Expedition.Id, region.content.homeId);
            if (keyboard.enterKey.wasPressedThisFrame && simulation.Session.PendingSave != null) simulation.Session.RetrySave();
        }
        private void OnGUI()
        {
            if (simulation.Session == null) return;
            var style = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleLeft };
            GUI.Box(new Rect(16, 16, Math.Min(Screen.width - 32, 490), 82), HudText(), style);
        }
        public string HudText()
        {
            var expedition = simulation.Session.Snapshot.Expedition;
            var cargo = expedition == null ? "Docked" : "Wood " + Values.Amount(expedition.Cargo, "wood") + "   Iron " + Values.Amount(expedition.Cargo, "iron");
            var result = interaction.LastResult;
            var status = result == null ? "" : result.IsPending ? "Collecting" : result.Error == RuleError.CargoFull ? "Cargo full" : result.Error == RuleError.None ? "" : result.Detail ?? result.Error.ToString();
            return "  Homeward Reach\n  " + cargo + "   " + status;
        }
        public sealed class MemoryStore : ISaveStore
        {
            public bool Fail;
            public RuleResult Commit(SaveCandidate candidate) => new RuleResult(Fail ? RuleError.SaveFailed : RuleError.None);
        }
        public sealed class ReadyArrival : IWorldArrival { public RuleResult EnsureReady(ArrivalRequest request) => new RuleResult(); }
    }
}
