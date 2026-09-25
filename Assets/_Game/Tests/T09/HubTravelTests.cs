using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PirateGame.Core;
using PirateGame.Persistence;
using PirateGame.Rules.Application;

namespace PirateGame.Tests.T09
{
    public sealed class HubTravelTests
    {
        private sealed class Arrival : IWorldArrival
        {
            public bool Fail;
            public readonly List<ArrivalRequest> Requests = new List<ArrivalRequest>();
            public RuleResult EnsureReady(ArrivalRequest request)
            {
                Requests.Add(request);
                return new RuleResult(Fail ? RuleError.ArrivalFailed : RuleError.None);
            }
        }

        private static readonly SeaPosition Home = new SeaPosition("sea", 0, 0);
        private static readonly SeaPosition Outpost = new SeaPosition("sea", 90, 10);
        private DefinitionCatalog definitions;
        private CampaignSession session;
        private Arrival arrival;
        private JsonSaveStore store;
        private string directory;

        [SetUp]
        public void Setup()
        {
            definitions = new DefinitionCatalog(
                new Dictionary<string, int> { ["wood"] = 1, ["iron"] = 2 },
                new[] { new HullDefinition("cutter", new Dictionary<string, SlotKind> { ["weapon"] = SlotKind.Weapon },
                    new[] { new StatDefinition("health", 100, 1, 1000), new StatDefinition("cargo", 10, 0, 100), new StatDefinition("speed", 8, 1, 30) }) },
                new[] { new EquipmentDefinition("cannon", SlotKind.Weapon, Array.Empty<StatModifier>()) },
                new[] { new HubDefinition("home", Home, 5, 1), new HubDefinition("outpost", Outpost, 5, 1) },
                new[] { new UpgradeDefinition("charts", "charts", 1, new Dictionary<string, int> { ["wood"] = 4 }, Array.Empty<string>(),
                    new[] { "fast-travel" }, Array.Empty<StatModifier>()),
                    new UpgradeDefinition("hull", "hull", 1, new Dictionary<string, int> { ["wood"] = 2 }, Array.Empty<string>(), Array.Empty<string>(),
                    new[] { new StatModifier("health", ModifierOperation.Flat, 50) }) },
                new[] { "fast-travel" }, Array.Empty<AuthoredIdentity>(), new[] { "barrel" }, new[] { "sea" });
            directory = Path.Combine(Path.GetTempPath(), "pirate-t09-" + Guid.NewGuid().ToString("N"));
            store = new JsonSaveStore(directory, definitions);
            var initial = CampaignSession.NewCampaign(definitions, "home", "cutter",
                new Dictionary<string, string> { ["cannon-1"] = "cannon" }, new Dictionary<string, string> { ["weapon"] = "cannon-1" });
            var c = initial.Campaign;
            initial = new SessionSnapshot(0, new CampaignState(c.HullId, c.CurrentHub, c.LastSafeHub, new Dictionary<string, int> { ["wood"] = 10 },
                c.Tiers, c.OwnedEquipment, c.Loadout, c.Hubs, c.Unlocks, c.ResolvedExpeditions), null, Array.Empty<Guid>());
            Ok(store.Initialize(initial, out _));
            arrival = new Arrival();
            session = new CampaignSession(definitions, initial, store, arrival);
            Ok(session.RetryArrival());
        }

        [TearDown]
        public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        private static void Ok(RuleResult result) => Assert.That(result.Error, Is.EqualTo(RuleError.None), result.Detail);
        private static void Error(RuleResult result, RuleError error) => Assert.That(result.Error, Is.EqualTo(error), result.Detail);

        private Guid Sail(params EntityState[] entities)
        {
            var id = Guid.NewGuid();
            Ok(session.Embark(Guid.NewGuid(), new EmbarkPlan(id, 5, "rng", entities, new Dictionary<string, string>())));
            return id;
        }

        private RuleResult MoveTo(SeaPosition position, double damage = 0) =>
            session.CompleteTick(session.Snapshot.Expedition.Id, session.Tick + 1, damage, position, 0);

        private RuleResult DockAt(Guid voyage, string hub)
        {
            var queued = session.RequestDock(Guid.NewGuid(), voyage, hub);
            if (!queued.IsPending) return queued;
            return MoveTo(session.Snapshot.Expedition.Position);
        }

        private static EntityState Barrel(int wood) => new EntityState(EntityId.Authored("barrel"), "barrel", Outpost, 1, false,
            new Dictionary<string, int> { ["wood"] = wood }, new Dictionary<string, double>(), "salvage");

        [Test]
        public void NewCampaignsKnowOnlyTheirHomeHub()
        {
            Assert.That(session.Snapshot.Campaign.Hubs["home"].Activated, Is.True);
            Assert.That(session.Snapshot.Campaign.Hubs["outpost"].Discovered, Is.False);
            Assert.That(session.Snapshot.Campaign.Hubs["outpost"].Activated, Is.False);
        }

        [Test]
        public void ActivatingInsideTheBerthCommitsDiscoveryWithoutDocking()
        {
            var voyage = Sail();
            Ok(MoveTo(new SeaPosition("sea", 88, 12)));
            long revision = store.CommittedRevision.Value;
            Ok(session.ActivateHub(Guid.NewGuid(), voyage, "outpost"));
            var campaign = session.Snapshot.Campaign;
            Assert.That(campaign.Hubs["outpost"].Discovered && campaign.Hubs["outpost"].Activated, Is.True);
            Assert.That(session.Lifecycle, Is.EqualTo(Lifecycle.AtSea), "Activation does not end the voyage");
            Assert.That(campaign.LastSafeHub, Is.EqualTo("home"), "Only docking changes the safe hub");
            Assert.That(store.CommittedRevision, Is.EqualTo(revision + 1));
            Assert.That(session.DrainEvents().Count(e => e.Command == "ActivateHub"), Is.EqualTo(1));
            Assert.That(store.Load().Snapshot.Campaign.Hubs["outpost"].Activated, Is.True, "Durable");
        }

        [Test]
        public void ActivationRejectsOutsideTheBerthRepeatsAndDockedShips()
        {
            var voyage = Sail();
            Ok(MoveTo(new SeaPosition("sea", 70, 10)));
            var before = session.Snapshot;
            Error(session.ActivateHub(Guid.NewGuid(), voyage, "outpost"), RuleError.NotInDockZone);
            Error(session.ActivateHub(Guid.NewGuid(), voyage, "nowhere"), RuleError.UnknownId);
            Error(session.ActivateHub(Guid.NewGuid(), Guid.NewGuid(), "outpost"), RuleError.WrongExpedition);
            Assert.That(session.Snapshot, Is.SameAs(before));
            Ok(MoveTo(Outpost));
            var request = Guid.NewGuid();
            Ok(session.ActivateHub(request, voyage, "outpost"));
            Error(session.ActivateHub(request, voyage, "outpost"), RuleError.DuplicateRequest);
            Error(session.ActivateHub(Guid.NewGuid(), voyage, "outpost"), RuleError.AlreadyActivated);
            Error(session.ActivateHub(Guid.NewGuid(), voyage, "home"), RuleError.AlreadyActivated);
            Ok(MoveTo(Home));
            Ok(DockAt(voyage, "home"));
            Error(session.ActivateHub(Guid.NewGuid(), voyage, "outpost"), RuleError.WrongLifecycle);
        }

        [Test]
        public void DockingNeedsActivationAndThenMakesTheOutpostSafe()
        {
            var voyage = Sail(Barrel(4));
            Ok(MoveTo(Outpost));
            Ok(session.CollectLoot(Guid.NewGuid(), voyage, EntityId.Authored("barrel")));
            Error(session.RequestDock(Guid.NewGuid(), voyage, "outpost"), RuleError.HubInactive);
            Ok(session.ActivateHub(Guid.NewGuid(), voyage, "outpost"));
            Ok(DockAt(voyage, "outpost"));
            var campaign = session.Snapshot.Campaign;
            Assert.That(campaign.CurrentHub, Is.EqualTo("outpost"));
            Assert.That(campaign.LastSafeHub, Is.EqualTo("outpost"));
            Assert.That(campaign.Bank["wood"], Is.EqualTo(14), "Shared bank credited at the outpost");
            Assert.That(arrival.Requests.Last().HubId, Is.EqualTo("outpost"));
        }

        [Test]
        public void SinkingReturnsToTheLastDockedHubEvenAfterActivatingAnother()
        {
            var voyage = Sail();
            Ok(MoveTo(Outpost));
            Ok(session.ActivateHub(Guid.NewGuid(), voyage, "outpost"));
            Ok(MoveTo(Outpost, 1000));
            var campaign = session.Snapshot.Campaign;
            Assert.That(campaign.ResolvedExpeditions[voyage], Is.EqualTo(Outcome.Sunk));
            Assert.That(campaign.CurrentHub, Is.EqualTo("home"));
            Assert.That(campaign.Hubs["outpost"].Activated, Is.True, "Committed activation survives the sinking");
        }

        [Test]
        public void AC07_ProgressIsSharedAcrossHubsAndTravelMovesTheSafeHub()
        {
            var voyage = Sail();
            Ok(MoveTo(Outpost));
            Ok(session.ActivateHub(Guid.NewGuid(), voyage, "outpost"));
            Ok(MoveTo(Home));
            Ok(DockAt(voyage, "home"));
            Ok(session.PurchaseUpgrade(Guid.NewGuid(), "hull"));
            Ok(session.PurchaseUpgrade(Guid.NewGuid(), "charts"));
            var before = session.Snapshot.Campaign;
            Ok(session.FastTravel(Guid.NewGuid(), "outpost"));
            var after = session.Snapshot.Campaign;
            Assert.That(after.CurrentHub, Is.EqualTo("outpost"));
            Assert.That(after.LastSafeHub, Is.EqualTo("outpost"));
            Assert.That(after.Bank, Is.EquivalentTo(before.Bank));
            Assert.That(after.Tiers, Is.EquivalentTo(before.Tiers));
            Assert.That(after.Unlocks, Is.EqualTo(before.Unlocks));
            Assert.That(session.ShipStats()["health"], Is.EqualTo(150), "Upgrade bought at home applies at the outpost");
            Assert.That(arrival.Requests.Last().HubId, Is.EqualTo("outpost"));
            Assert.That(arrival.Requests.Last().ExpeditionId, Is.Null);
            var disk = store.Load().Snapshot.Campaign;
            Assert.That(disk.CurrentHub, Is.EqualTo("outpost"));
            Sail();
            Assert.That(session.Snapshot.Expedition.Position.X, Is.EqualTo(Outpost.X), "Next voyage departs from the outpost");
        }

        [Test]
        public void AC08_TravelIsRejectedAtSeaLockedOrToInactiveHubs()
        {
            var before = session.Snapshot;
            Error(session.FastTravel(Guid.NewGuid(), "outpost"), RuleError.TravelLocked);
            Ok(session.PurchaseUpgrade(Guid.NewGuid(), "charts"));
            before = session.Snapshot;
            Error(session.FastTravel(Guid.NewGuid(), "outpost"), RuleError.HubInactive);
            Assert.That(session.Snapshot, Is.SameAs(before));
            var voyage = Sail();
            before = session.Snapshot;
            Error(session.FastTravel(Guid.NewGuid(), "home"), RuleError.WrongLifecycle);
            Assert.That(session.Snapshot, Is.SameAs(before), "No cargo escape through travel");
            Assert.That(voyage, Is.Not.EqualTo(Guid.Empty));
        }

        [Test]
        public void AC18_FailedDestinationLoadKeepsInputLockedUntilRetry()
        {
            var voyage = Sail();
            Ok(MoveTo(Outpost));
            Ok(session.ActivateHub(Guid.NewGuid(), voyage, "outpost"));
            Ok(MoveTo(Home));
            Ok(DockAt(voyage, "home"));
            Ok(session.PurchaseUpgrade(Guid.NewGuid(), "charts"));
            arrival.Fail = true;
            Error(session.FastTravel(Guid.NewGuid(), "outpost"), RuleError.ArrivalFailed);
            Assert.That(session.InputLocked, Is.True);
            Assert.That(session.ArrivalPending, Is.True);
            Assert.That(store.Load().Snapshot.Campaign.CurrentHub, Is.EqualTo("outpost"), "Travel is committed before arrival");
            Error(session.Embark(Guid.NewGuid(), new EmbarkPlan(Guid.NewGuid(), 1, "rng", new EntityState[0], new Dictionary<string, string>())), RuleError.Busy);
            var request = arrival.Requests.Last();
            arrival.Fail = false;
            Ok(session.RetryArrival());
            Assert.That(arrival.Requests.Last(), Is.SameAs(request), "Retry uses the same saved destination request");
            Assert.That(session.InputLocked, Is.False);
            Assert.That(session.DrainEvents().Count(e => e.Command == "FastTravel"), Is.EqualTo(1), "Retry does not publish again");
        }
    }
}
