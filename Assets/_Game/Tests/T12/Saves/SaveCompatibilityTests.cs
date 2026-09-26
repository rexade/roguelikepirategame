using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PirateGame.Core;
using PirateGame.Persistence;
using PirateGame.Rules.Application;

namespace PirateGame.Tests.T12.Saves
{
    // A voyage saved in the old archipelago cannot be restored into the Drowned Sun
    // map. Loading resolves it as lost at sea; the campaign's progress is kept.
    public sealed class SaveCompatibilityTests
    {
        private sealed class ReadyArrival : IWorldArrival
        {
            public RuleResult EnsureReady(ArrivalRequest request) => new RuleResult();
        }

        private string directory;
        private readonly Guid voyageId = Guid.NewGuid();

        [SetUp] public void Setup() => directory = Path.Combine(Path.GetTempPath(), "pirate-t12-saves-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        // Same hubs, resources, upgrades and equipment in both worlds; only the
        // regions and salvage definitions changed (as in the Drowned Sun rebuild).
        private static DefinitionCatalog World(string region, string salvage) => new DefinitionCatalog(
            new Dictionary<string, int> { ["wood"] = 1, ["iron"] = 2 },
            new[] { new HullDefinition("cutter", new Dictionary<string, SlotKind> { ["weapon"] = SlotKind.Weapon, ["ability"] = SlotKind.Ability },
                new[] { new StatDefinition("health", 100, 1, 1000), new StatDefinition("cargo", 10, 0, 1000), new StatDefinition("speed", 8, 1, 30) }) },
            new[] { new EquipmentDefinition("cannon", SlotKind.Weapon, Array.Empty<StatModifier>()),
                new EquipmentDefinition("brace", SlotKind.Ability, Array.Empty<StatModifier>()) },
            new[] { new HubDefinition("home-harbor", new SeaPosition(region, 0, -12), 5, 1),
                new HubDefinition("saltmarsh-harbor", new SeaPosition(region, 58, 124), 5, 1) },
            new[] { new UpgradeDefinition("harbor-storehouse", "harbor", 1, new Dictionary<string, int> { ["wood"] = 5 }, Array.Empty<string>(),
                new[] { "storehouse" }, new[] { new StatModifier("cargo", ModifierOperation.Flat, 5) }) },
            new[] { "storehouse" }, Array.Empty<AuthoredIdentity>(), new[] { salvage, "wreck", "raider" }, new[] { region });

        private static DefinitionCatalog OldWorld() => World("first-region", "barrel");
        private static DefinitionCatalog NewWorld() => World("gilded-shallows", "relic");

        // What the Drowned Sun director checks: the voyage must lie in a region it knows.
        private static string Check(ExpeditionState voyage) =>
            voyage.Position.RegionId == "gilded-shallows" ? null : "Saved in waters that no longer exist: " + voyage.Position.RegionId + ".";

        // An at-sea save: docked last at home, departed from Saltmarsh, carrying cargo.
        private SessionSnapshot AtSea(string region, string salvage)
        {
            var campaign = new CampaignState("cutter", "saltmarsh-harbor", "home-harbor",
                new Dictionary<string, int> { ["wood"] = 12, ["iron"] = 3 }, new Dictionary<string, int> { ["harbor"] = 1 },
                new Dictionary<string, string> { ["cannon-1"] = "cannon", ["brace-1"] = "brace" },
                new Dictionary<string, string> { ["weapon"] = "cannon-1", ["ability"] = "brace-1" },
                new Dictionary<string, HubState> { ["home-harbor"] = new HubState(true, true, new[] { "met-keeper" }),
                    ["saltmarsh-harbor"] = new HubState(true, true, new string[0]) },
                new[] { "storehouse" }, new Dictionary<Guid, Outcome> { [Guid.NewGuid()] = Outcome.Docked });
            var entities = new[] { new EntityState(EntityId.Authored("site-01"), salvage, new SeaPosition(region, 4, 8), 1, false,
                new Dictionary<string, int> { ["wood"] = 2 }, new Dictionary<string, double>(), "salvage") };
            var voyage = new ExpeditionState(voyageId, 7, "splitmix64:00ff", 420, 80, 2.5, new SeaPosition(region, 10, 20),
                new Dictionary<string, int> { ["wood"] = 3, ["iron"] = 1 }, Array.Empty<StatModifier>(), new Dictionary<string, double>(),
                new Dictionary<string, string>(), entities);
            return new SessionSnapshot(9, campaign, voyage, new[] { Guid.NewGuid(), Guid.NewGuid() });
        }

        private void Save(DefinitionCatalog world, SessionSnapshot snapshot)
        {
            var store = new JsonSaveStore(directory, world);
            var created = store.Initialize(snapshot, out _);
            Assert.That(created.IsSuccess, created.Detail);
        }

        private static void Ok(RuleResult result) => Assert.That(result.Error, Is.EqualTo(RuleError.None), result.Detail);

        [Test]
        public void AbandonVoyageKeepsCampaignProgress()
        {
            var original = AtSea("first-region", "barrel");
            var migrated = SaveMigration.AbandonVoyage(original);
            Assert.That(migrated.Expedition, Is.Null);
            Assert.That(migrated.Lifecycle, Is.EqualTo(Lifecycle.Docked));
            Assert.That(migrated.Revision, Is.EqualTo(original.Revision));
            Assert.That(migrated.CommittedRequests, Is.EqualTo(original.CommittedRequests));
            var before = original.Campaign; var after = migrated.Campaign;
            Assert.That(after.CurrentHub, Is.EqualTo("home-harbor"), "back at the last safe hub");
            Assert.That(after.LastSafeHub, Is.EqualTo("home-harbor"));
            Assert.That(after.ResolvedExpeditions[voyageId], Is.EqualTo(Outcome.Sunk));
            Assert.That(after.ResolvedExpeditions.Count, Is.EqualTo(before.ResolvedExpeditions.Count + 1));
            Assert.That(after.Bank, Is.EquivalentTo(before.Bank), "voyage cargo is not banked");
            Assert.That(after.Tiers, Is.EquivalentTo(before.Tiers));
            Assert.That(after.OwnedEquipment, Is.EquivalentTo(before.OwnedEquipment));
            Assert.That(after.Loadout, Is.EquivalentTo(before.Loadout));
            Assert.That(after.Unlocks, Is.EqualTo(before.Unlocks));
            Assert.That(after.Hubs.Keys, Is.EquivalentTo(before.Hubs.Keys));
            Assert.That(after.Hubs.All(h => h.Value.Activated == before.Hubs[h.Key].Activated));
            Assert.That(after.Hubs["home-harbor"].StoryFlags, Is.EqualTo(new[] { "met-keeper" }));

            var docked = SaveMigration.AbandonVoyage(migrated);
            Assert.That(docked, Is.SameAs(migrated), "a docked snapshot is returned as is");
        }

        [Test]
        public void OldWorldVoyageLoadsAsLostAtSeaWithoutRewritingTheFile()
        {
            Save(OldWorld(), AtSea("first-region", "barrel"));
            var store = new JsonSaveStore(directory, NewWorld(), voyageProblem: Check);
            var bytes = File.ReadAllBytes(store.MainPath);

            var loaded = store.Load();

            Assert.That(loaded.Status, Is.EqualTo(SaveLoadStatus.Loaded), loaded.Detail);
            Assert.That(loaded.Notice, Does.Contain("first-region"));
            Assert.That(loaded.AbandonedVoyage.Id, Is.EqualTo(voyageId));
            Assert.That(loaded.AbandonedVoyage.Cargo["wood"], Is.EqualTo(3), "the lost cargo can be reported");
            Assert.That(loaded.Snapshot.Expedition, Is.Null);
            Assert.That(loaded.Snapshot.Campaign.CurrentHub, Is.EqualTo("home-harbor"));
            Assert.That(loaded.Snapshot.Campaign.ResolvedExpeditions[voyageId], Is.EqualTo(Outcome.Sunk));
            Assert.That(loaded.Snapshot.Campaign.Bank["wood"], Is.EqualTo(12));
            Assert.That(loaded.Snapshot.Revision, Is.EqualTo(9));
            Assert.That(File.ReadAllBytes(store.MainPath), Is.EqualTo(bytes), "loading never writes");
        }

        [Test]
        public void TheNextCommitContinuesTheMigratedCampaign()
        {
            Save(OldWorld(), AtSea("first-region", "barrel"));
            var world = NewWorld();
            var store = new JsonSaveStore(directory, world, voyageProblem: Check);
            var loaded = store.Load();
            var session = new CampaignSession(world, loaded.Snapshot, store, new ReadyArrival());
            Ok(session.RetryArrival());
            Assert.That(session.Lifecycle, Is.EqualTo(Lifecycle.Docked));

            Ok(session.Checkpoint(Guid.NewGuid()));

            var reloaded = new JsonSaveStore(directory, world, voyageProblem: Check).Load();
            Assert.That(reloaded.Status, Is.EqualTo(SaveLoadStatus.Loaded), reloaded.Detail);
            Assert.That(reloaded.Notice, Is.Empty, "the migrated campaign is now saved without the voyage");
            Assert.That(reloaded.AbandonedVoyage, Is.Null);
            Assert.That(reloaded.Snapshot.Revision, Is.EqualTo(10));
            Assert.That(reloaded.Snapshot.Expedition, Is.Null);
            Assert.That(reloaded.Snapshot.Campaign.ResolvedExpeditions[voyageId], Is.EqualTo(Outcome.Sunk));
        }

        [Test]
        public void CompatibleVoyageLoadsUnchanged()
        {
            var world = NewWorld();
            Save(world, AtSea("gilded-shallows", "relic"));
            var loaded = new JsonSaveStore(directory, world, voyageProblem: Check).Load();
            Assert.That(loaded.Status, Is.EqualTo(SaveLoadStatus.Loaded), loaded.Detail);
            Assert.That(loaded.Notice, Is.Empty);
            Assert.That(loaded.AbandonedVoyage, Is.Null);
            Assert.That(loaded.Snapshot.Expedition.Id, Is.EqualTo(voyageId));
            Assert.That(loaded.Snapshot.Campaign.CurrentHub, Is.EqualTo("saltmarsh-harbor"));
        }

        [Test]
        public void WithoutACheckAnOldWorldVoyageIsUnreadable()
        {
            Save(OldWorld(), AtSea("first-region", "barrel"));
            var loaded = new JsonSaveStore(directory, NewWorld()).Load();
            Assert.That(loaded.Status, Is.EqualTo(SaveLoadStatus.Unreadable));
            Assert.That(loaded.HasCampaign, Is.False);
        }

        [Test]
        public void AFailingCheckStillLoadsTheCampaign()
        {
            Save(OldWorld(), AtSea("first-region", "barrel"));
            var loaded = new JsonSaveStore(directory, NewWorld(), voyageProblem: _ => throw new InvalidOperationException("boom")).Load();
            Assert.That(loaded.Status, Is.EqualTo(SaveLoadStatus.Loaded), loaded.Detail);
            Assert.That(loaded.Notice, Does.Contain("boom"));
            Assert.That(loaded.Snapshot.Expedition, Is.Null);
        }

        [Test]
        public void RecoveredBackupIsCheckedToo()
        {
            Save(OldWorld(), AtSea("first-region", "barrel"));
            var store = new JsonSaveStore(directory, NewWorld(), voyageProblem: Check);
            // Only a backup survives (for example the main file was lost mid-replace).
            File.Move(store.MainPath, store.BackupPath);
            var loaded = store.Load();
            Assert.That(loaded.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromBackup), loaded.Detail);
            Assert.That(loaded.Notice, Is.Not.Empty);
            Assert.That(loaded.Snapshot.Expedition, Is.Null);
        }
    }
}
