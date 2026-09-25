using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PirateGame.Core;
using PirateGame.Persistence;
using PirateGame.Rules.Application;

namespace PirateGame.Tests.T08
{
    // Injects failures at each filesystem step of a commit.
    public sealed class FaultyFileSystem : ISaveFileSystem
    {
        public enum Step { None, Write, Replace, AfterReplace }
        public Step FailAt;
        public int Failures;
        private readonly DiskFileSystem disk = new DiskFileSystem();
        public bool Exists(string path) => disk.Exists(path);
        public byte[] ReadAllBytes(string path) => disk.ReadAllBytes(path);
        public void WriteAllBytes(string path, byte[] bytes) { Maybe(Step.Write); disk.WriteAllBytes(path, bytes); }
        public void Replace(string source, string destination, string backup)
        {
            Maybe(Step.Replace);
            disk.Replace(source, destination, backup);
            Maybe(Step.AfterReplace);
        }
        public void Move(string source, string destination) { Maybe(Step.Replace); disk.Move(source, destination); Maybe(Step.AfterReplace); }
        public void CreateDirectory(string path) => disk.CreateDirectory(path);
        private void Maybe(Step step)
        {
            if (FailAt != step) return;
            Failures++;
            throw new IOException("Injected " + step + " failure");
        }
    }

    public sealed class ReadyArrival : IWorldArrival
    {
        public RuleResult EnsureReady(ArrivalRequest request) => new RuleResult();
    }

    public sealed class PersistenceTests
    {
        private string directory;
        private DefinitionCatalog definitions;

        [SetUp]
        public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "pirate-t08-" + Guid.NewGuid().ToString("N"));
            definitions = Catalog();
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        public static DefinitionCatalog Catalog() => new DefinitionCatalog(
            new Dictionary<string, int> { ["wood"] = 1, ["iron"] = 2 },
            new[] { new HullDefinition("cutter", new Dictionary<string, SlotKind> { ["weapon"] = SlotKind.Weapon, ["ability"] = SlotKind.Ability },
                new[] { new StatDefinition("health", 100, 1, 1000), new StatDefinition("cargo", 10, 0, 1000), new StatDefinition("speed", 8, 1, 30) }) },
            new[] { new EquipmentDefinition("cannon", SlotKind.Weapon, Array.Empty<StatModifier>()),
                new EquipmentDefinition("repeater", SlotKind.Weapon, Array.Empty<StatModifier>()),
                new EquipmentDefinition("brace", SlotKind.Ability, Array.Empty<StatModifier>()) },
            new[] { new HubDefinition("home", new SeaPosition("sea", 0, 0), 5, 1), new HubDefinition("far", new SeaPosition("sea", 90, 0), 5, 1) },
            new[] { new UpgradeDefinition("hull-1", "hull", 1, new Dictionary<string, int> { ["wood"] = 6 }, Array.Empty<string>(), new[] { "charts" },
                new[] { new StatModifier("health", ModifierOperation.Percent, 0.5) }) },
            new[] { "charts" }, Array.Empty<AuthoredIdentity>(), new[] { "barrel", "raider" }, new[] { "sea" });

        private static SessionSnapshot Fresh(DefinitionCatalog definitions) => CampaignSession.NewCampaign(definitions, "home", "cutter",
            new Dictionary<string, string> { ["cannon-1"] = "cannon", ["repeater-1"] = "repeater", ["brace-1"] = "brace" },
            new Dictionary<string, string> { ["weapon"] = "cannon-1", ["ability"] = "brace-1" });

        private static EntityState Barrel(string id, int wood) => new EntityState(EntityId.Authored(id), "barrel", new SeaPosition("sea", 0, 0), 1, false,
            new Dictionary<string, int> { ["wood"] = wood }, new Dictionary<string, double>(), "salvage");

        private JsonSaveStore Store(ISaveFileSystem files = null) => new JsonSaveStore(directory, definitions, files);

        private CampaignSession Begin(JsonSaveStore store, SessionSnapshot initial = null)
        {
            initial = initial ?? Fresh(definitions);
            Ok(store.Initialize(initial, out _));
            var session = new CampaignSession(definitions, initial, store, new ReadyArrival());
            Ok(session.RetryArrival());
            return session;
        }

        private static Guid Sail(CampaignSession session, params EntityState[] entities)
        {
            var id = Guid.NewGuid();
            Ok(session.Embark(Guid.NewGuid(), new EmbarkPlan(id, 11, "rng:11", entities, new Dictionary<string, string> { ["spawn-1"] = "raider" })));
            return id;
        }

        private static RuleResult Tick(CampaignSession session, double damage = 0) =>
            session.CompleteTick(session.Snapshot.Expedition.Id, session.Tick + 1, damage, new SeaPosition("sea", 0, 0), 0);

        private static void Ok(RuleResult result) => Assert.That(result.Error, Is.EqualTo(RuleError.None), result.Detail);

        [Test]
        public void RoundTripPreservesEveryField()
        {
            var expedition = Guid.NewGuid();
            var campaign = new CampaignState("cutter", "far", "home",
                new Dictionary<string, int> { ["wood"] = 12, ["iron"] = 3 }, new Dictionary<string, int> { ["hull"] = 1 },
                new Dictionary<string, string> { ["cannon-1"] = "cannon", ["repeater-1"] = "repeater", ["brace-1"] = "brace" },
                new Dictionary<string, string> { ["weapon"] = "repeater-1", ["ability"] = "brace-1" },
                new Dictionary<string, HubState> { ["home"] = new HubState(true, true, new[] { "met-harbormaster" }), ["far"] = new HubState(true, true, new string[0]) },
                new[] { "charts" }, new Dictionary<Guid, Outcome> { [Guid.NewGuid()] = Outcome.Docked, [Guid.NewGuid()] = Outcome.Sunk });
            var entities = new[]
            {
                new EntityState(EntityId.Authored("barrel-a"), "barrel", new SeaPosition("sea", 1.25, -3.5), 1, false,
                    new Dictionary<string, int> { ["wood"] = 3 }, new Dictionary<string, double>(), "salvage"),
                new EntityState(EntityId.Generated(expedition, "spawn#0"), "raider", new SeaPosition("sea", 0.1 + 0.2, 1e-9), 33.333333333333336, false,
                    new Dictionary<string, int>(), new Dictionary<string, double> { ["weapon"] = 0.66000002622604370 }, "{\"mode\":\"pursue\",\"yaw\":12.5}"),
                new EntityState(EntityId.Generated(expedition, "wreck/spawn#1"), "barrel", new SeaPosition("sea", -7, 8), 0, true,
                    new Dictionary<string, int>(), new Dictionary<string, double>(), "")
            };
            var voyage = new ExpeditionState(expedition, -42, "splitmix64:00ff", 1234, 71.5, 3.2500000000000004, new SeaPosition("sea", 12.345678901234567, -0.1),
                new Dictionary<string, int> { ["wood"] = 2, ["iron"] = 1 }, new[] { new StatModifier("speed", ModifierOperation.Percent, 0.1) },
                new Dictionary<string, double> { ["combat.weapon"] = 0.42, ["combat.brace"] = 0 }, new Dictionary<string, string> { ["spawn#0"] = "raider" }, entities);
            var original = new SessionSnapshot(7, campaign, voyage, new[] { Guid.NewGuid(), Guid.NewGuid() });
            Ok(CampaignSession.ValidateSnapshot(definitions, original));

            var bytes = SaveCodec.Encode(SaveMapper.ToDto(original, Guid.NewGuid(), "Checkpoint", new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc)));
            var copy = SaveMapper.FromDto(SaveCodec.Decode(bytes));

            Assert.That(copy.Revision, Is.EqualTo(7));
            Assert.That(copy.CommittedRequests, Is.EqualTo(original.CommittedRequests));
            var c = copy.Campaign;
            Assert.That(c.HullId, Is.EqualTo("cutter")); Assert.That(c.CurrentHub, Is.EqualTo("far")); Assert.That(c.LastSafeHub, Is.EqualTo("home"));
            Assert.That(c.Bank, Is.EquivalentTo(campaign.Bank)); Assert.That(c.Tiers, Is.EquivalentTo(campaign.Tiers));
            Assert.That(c.OwnedEquipment, Is.EquivalentTo(campaign.OwnedEquipment)); Assert.That(c.Loadout, Is.EquivalentTo(campaign.Loadout));
            Assert.That(c.Unlocks, Is.EqualTo(campaign.Unlocks));
            Assert.That(c.ResolvedExpeditions, Is.EquivalentTo(campaign.ResolvedExpeditions));
            Assert.That(c.Hubs["home"].StoryFlags, Is.EqualTo(new[] { "met-harbormaster" }));
            Assert.That(c.Hubs["far"].Activated, Is.True);
            var e = copy.Expedition;
            Assert.That(e.Id, Is.EqualTo(expedition)); Assert.That(e.Seed, Is.EqualTo(-42)); Assert.That(e.RngState, Is.EqualTo("splitmix64:00ff"));
            Assert.That(e.Tick, Is.EqualTo(1234)); Assert.That(e.Health, Is.EqualTo(71.5)); Assert.That(e.Speed, Is.EqualTo(3.2500000000000004));
            Assert.That(e.Position.X, Is.EqualTo(12.345678901234567)); Assert.That(e.Position.Z, Is.EqualTo(-0.1));
            Assert.That(e.Cargo, Is.EquivalentTo(voyage.Cargo)); Assert.That(e.Cooldowns, Is.EquivalentTo(voyage.Cooldowns));
            Assert.That(e.Encounters, Is.EquivalentTo(voyage.Encounters));
            Assert.That(e.Modifiers.Single().Operation, Is.EqualTo(ModifierOperation.Percent));
            Assert.That(e.Modifiers.Single().Value, Is.EqualTo(0.1));
            foreach (var entity in entities)
            {
                var restored = e.Entities[entity.Id];
                Assert.That(restored.DefinitionId, Is.EqualTo(entity.DefinitionId));
                Assert.That(restored.Position.X, Is.EqualTo(entity.Position.X)); Assert.That(restored.Position.Z, Is.EqualTo(entity.Position.Z));
                Assert.That(restored.Health, Is.EqualTo(entity.Health)); Assert.That(restored.Defeated, Is.EqualTo(entity.Defeated));
                Assert.That(restored.Loot, Is.EquivalentTo(entity.Loot)); Assert.That(restored.Cooldowns, Is.EquivalentTo(entity.Cooldowns));
                Assert.That(restored.BehaviorState, Is.EqualTo(entity.BehaviorState));
            }
        }

        [Test]
        public void MalformedDocumentsAreRejectedStructurally()
        {
            Assert.Throws<SaveFormatException>(() => SaveCodec.Decode(new byte[0]));
            Assert.Throws<SaveFormatException>(() => SaveCodec.Decode(Encoding.UTF8.GetBytes("{\"format\":\"pirate-prototype-save\",\"schema\":1,")));
            Assert.Throws<SaveFormatException>(() => SaveCodec.Decode(Encoding.UTF8.GetBytes("[1,2,3]")));
            Assert.Throws<SaveFormatException>(() => SaveCodec.Decode(Encoding.UTF8.GetBytes("{\"format\":\"other\",\"schema\":1}")));
            var future = Assert.Throws<SaveFormatException>(() => SaveCodec.Decode(Encoding.UTF8.GetBytes("{\"format\":\"pirate-prototype-save\",\"schema\":2}")));
            Assert.That(future.Unsupported, Is.True);
            var valid = SaveMapper.ToDto(Fresh(definitions), Guid.Empty, "NewCampaign", DateTime.UtcNow);
            valid.campaign.bank = null;
            Assert.Throws<SaveFormatException>(() => SaveMapper.FromDto(valid));
            valid = SaveMapper.ToDto(Fresh(definitions), Guid.Empty, "NewCampaign", DateTime.UtcNow);
            valid.campaign.bank["wood"] = -1;
            Assert.Throws<SaveFormatException>(() => SaveMapper.FromDto(valid));
            valid = SaveMapper.ToDto(Fresh(definitions), Guid.Empty, "NewCampaign", DateTime.UtcNow);
            valid.committedRequests.Add("not-a-guid");
            Assert.Throws<SaveFormatException>(() => SaveMapper.FromDto(valid));
        }

        [Test]
        public void CommitsAreOrderedAndReloadable()
        {
            var store = Store();
            var session = Begin(store);
            var voyage = Sail(session, Barrel("barrel-a", 3));
            Ok(session.CollectLoot(Guid.NewGuid(), voyage, EntityId.Authored("barrel-a")));
            Assert.That(File.Exists(store.MainPath) && File.Exists(store.BackupPath), Is.True);
            var reloaded = Store().Load();
            Assert.That(reloaded.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(reloaded.Snapshot.Revision, Is.EqualTo(session.Snapshot.Revision));
            Assert.That(reloaded.Snapshot.Expedition.Cargo["wood"], Is.EqualTo(3));
            Assert.That(reloaded.Snapshot.Expedition.Entities[EntityId.Authored("barrel-a")].Loot.Values.Sum(), Is.Zero, "Depleted loot stays depleted");
        }

        [Test]
        public void AC17_OlderCandidateCannotOverwriteNewerCommit()
        {
            var store = Store();
            var session = Begin(store, WithBank(Fresh(definitions), 20));
            var stale = new SaveCandidate(Guid.NewGuid(), 0, new SessionSnapshot(1, session.Snapshot.Campaign, null, new[] { Guid.NewGuid() }), "Checkpoint");
            Ok(session.PurchaseUpgrade(Guid.NewGuid(), "hull-1"));
            var result = store.Commit(stale);
            Assert.That(result.Error, Is.EqualTo(RuleError.SaveFailed));
            var disk = Store().Load().Snapshot;
            Assert.That(disk.Campaign.Tiers["hull"], Is.EqualTo(1), "Purchase survives the stale write");
            Assert.That(disk.Campaign.Bank["wood"], Is.EqualTo(14));
        }

        [Test]
        public void AcknowledgedRetryIsIdempotent()
        {
            var store = Store();
            var session = Begin(store);
            var candidate = new SaveCandidate(Guid.NewGuid(), 0, new SessionSnapshot(1, session.Snapshot.Campaign, null, new[] { Guid.NewGuid() }), "Checkpoint");
            Ok(store.Commit(candidate));
            Ok(store.Commit(candidate));
            Assert.That(store.CommittedRevision, Is.EqualTo(1));
        }

        [Test]
        public void AC09_FailureBeforeReplacementKeepsPriorSaveAndRetryBanksOnce()
        {
            var files = new FaultyFileSystem();
            var store = Store(files);
            var session = Begin(store);
            var voyage = Sail(session, Barrel("wreck", 7));
            Ok(session.CollectLoot(Guid.NewGuid(), voyage, EntityId.Authored("wreck")));
            Assert.That(session.RequestDock(Guid.NewGuid(), voyage, "home").IsPending, Is.True);
            files.FailAt = FaultyFileSystem.Step.Replace;
            var failed = Tick(session);
            Assert.That(failed.Error, Is.EqualTo(RuleError.SaveFailed));
            Assert.That(session.PendingSave, Is.Not.Null);
            Assert.That(session.InputLocked, Is.True, "Outcome is frozen, not resumed");
            var disk = Store().Load().Snapshot;
            Assert.That(disk.Expedition, Is.Not.Null, "Prior at-sea save is still the committed file");
            Assert.That(Values.Amount(disk.Campaign.Bank, "wood"), Is.Zero);
            files.FailAt = FaultyFileSystem.Step.None;
            Ok(session.RetrySave());
            Ok(session.RetrySave().Error == RuleError.InvalidRequest ? new RuleResult() : new RuleResult(RuleError.Busy));
            disk = Store().Load().Snapshot;
            Assert.That(disk.Expedition, Is.Null);
            Assert.That(disk.Campaign.Bank["wood"], Is.EqualTo(7), "Banked exactly once");
            Assert.That(session.DrainEvents().Count(e => e.Command == "Dock"), Is.EqualTo(1));
        }

        [Test]
        public void AC10_CrashAfterReplacementLoadsCommittedResultWithoutRepeatingIt()
        {
            var files = new FaultyFileSystem();
            var store = Store(files);
            var session = Begin(store);
            var voyage = Sail(session, Barrel("wreck", 7));
            Ok(session.CollectLoot(Guid.NewGuid(), voyage, EntityId.Authored("wreck")));
            session.RequestDock(Guid.NewGuid(), voyage, "home");
            files.FailAt = FaultyFileSystem.Step.AfterReplace;
            Assert.That(Tick(session).Error, Is.EqualTo(RuleError.SaveFailed), "Process 'dies' before acknowledgement");
            var pending = session.PendingSave;

            // Restart: a new store and session read what reached the disk.
            var restartedStore = Store();
            var loaded = restartedStore.Load();
            Assert.That(loaded.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(loaded.Snapshot.Expedition, Is.Null);
            Assert.That(loaded.Snapshot.Campaign.Bank["wood"], Is.EqualTo(7));
            Assert.That(loaded.Snapshot.Campaign.ResolvedExpeditions[voyage], Is.EqualTo(Outcome.Docked));
            var restarted = new CampaignSession(definitions, loaded.Snapshot, restartedStore, new ReadyArrival());
            Ok(restarted.RetryArrival());
            Assert.That(restarted.RequestDock(Guid.NewGuid(), voyage, "home").Error, Is.EqualTo(RuleError.WrongLifecycle));
            // Re-delivering the old candidate is acknowledged without a second effect.
            Ok(restartedStore.Commit(pending));
            Assert.That(Store().Load().Snapshot.Campaign.Bank["wood"], Is.EqualTo(7));
        }

        [Test]
        public void AC12_CorruptMainRecoversBackupAndPreservesOriginal()
        {
            var store = Store();
            var session = Begin(store, WithBank(Fresh(definitions), 9));
            Ok(session.PurchaseUpgrade(Guid.NewGuid(), "hull-1"));
            File.WriteAllText(store.MainPath, "{ \"format\": \"pirate-prototype-save\", \"schema\": 1, \"revision\": ");
            var result = Store().Load();
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromBackup));
            Assert.That(result.Snapshot.Revision, Is.EqualTo(0), "Backup holds the previous committed snapshot");
            Assert.That(result.PreservedFiles.Count, Is.EqualTo(1));
            Assert.That(File.ReadAllText(result.PreservedFiles[0]), Does.StartWith("{ \"format\""), "Corrupt original kept verbatim");
            Assert.That(Store().Load().Status, Is.EqualTo(SaveLoadStatus.Loaded), "Recovered backup became the main save");
        }

        [Test]
        public void AC12_UnsupportedSchemaWithoutValidBackupIsReportedAndUntouched()
        {
            var store = Store();
            Begin(store);
            string future = "{\"format\":\"pirate-prototype-save\",\"schema\":9}";
            File.WriteAllText(store.MainPath, future);
            var result = Store().Load();
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Unreadable));
            Assert.That(result.Detail, Does.Contain("Unsupported save schema 9"));
            Assert.That(File.ReadAllText(store.MainPath), Is.EqualTo(future), "Unreadable save is never overwritten by loading");
        }

        [Test]
        public void UnknownContentFailsValidationAndFallsBackToBackup()
        {
            var store = Store();
            var session = Begin(store);
            Sail(session);
            string text = File.ReadAllText(store.MainPath).Replace("\"hull\": \"cutter\"", "\"hull\": \"galleon\"");
            File.WriteAllText(store.MainPath, text);
            var result = Store().Load();
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.RecoveredFromBackup));
            Assert.That(result.Detail, Does.Contain("UnknownId"));
        }

        [Test]
        public void NewCampaignKeepsPreviousFilesAside()
        {
            var store = Store();
            var session = Begin(store);
            Sail(session);
            Ok(Store().Initialize(Fresh(definitions), out var preserved));
            Assert.That(preserved.Count, Is.EqualTo(2));
            Assert.That(preserved.All(File.Exists), Is.True);
            Assert.That(Store().Load().Snapshot.Expedition, Is.Null);
        }

        [Test]
        public void AC11_ResumePreservesHealthCooldownsCargoAndWorld()
        {
            var store = Store();
            var session = Begin(store);
            var voyage = Sail(session, Barrel("barrel-a", 3), Barrel("barrel-b", 2));
            Ok(Tick(session, 23.5));
            Ok(session.CollectLoot(Guid.NewGuid(), voyage, EntityId.Authored("barrel-a")));
            var current = session.Snapshot.Expedition;
            var captured = new ExpeditionState(current.Id, current.Seed, current.RngState, current.Tick, current.Health, current.Speed, current.Position,
                current.Cargo, current.Modifiers, new Dictionary<string, double> { ["combat.weapon"] = 0.37 }, current.Encounters, current.Entities.Values);
            Ok(session.Checkpoint(Guid.NewGuid(), captured));
            var loaded = Store().Load().Snapshot.Expedition;
            Assert.That(loaded.Health, Is.EqualTo(76.5));
            Assert.That(loaded.Cooldowns["combat.weapon"], Is.EqualTo(0.37));
            Assert.That(loaded.Cargo["wood"], Is.EqualTo(3));
            Assert.That(loaded.Entities[EntityId.Authored("barrel-a")].Loot.Values.Sum(), Is.Zero);
            Assert.That(loaded.Entities[EntityId.Authored("barrel-b")].Loot["wood"], Is.EqualTo(2));
            Assert.That(loaded.Tick, Is.EqualTo(current.Tick));
        }

        [Test]
        public void AC06_SinkingKeepsBankUpgradesAndAFreeDeparture()
        {
            var store = Store();
            var session = Begin(store, WithBank(Fresh(definitions), 6));
            Ok(session.PurchaseUpgrade(Guid.NewGuid(), "hull-1"));
            var voyage = Sail(session, Barrel("barrel-a", 3));
            Ok(session.CollectLoot(Guid.NewGuid(), voyage, EntityId.Authored("barrel-a")));
            Ok(Tick(session, 1000));
            var disk = Store().Load().Snapshot;
            Assert.That(disk.Expedition, Is.Null);
            Assert.That(disk.Campaign.ResolvedExpeditions[voyage], Is.EqualTo(Outcome.Sunk));
            Assert.That(disk.Campaign.Tiers["hull"], Is.EqualTo(1));
            Assert.That(Values.Amount(disk.Campaign.Bank, "wood"), Is.Zero);
            Assert.That(disk.Campaign.CurrentHub, Is.EqualTo("home"));
            Sail(session);
            Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(150), "Full computed health, no repair cost");
        }

        [Test]
        public void AC01_AC02_OutcomesCommitOnceThroughTheRealStore()
        {
            var store = Store();
            var session = Begin(store);
            var voyage = Sail(session, Barrel("wreck", 7));
            Ok(session.CollectLoot(Guid.NewGuid(), voyage, EntityId.Authored("wreck")));
            var request = Guid.NewGuid();
            session.RequestDock(request, voyage, "home");
            Ok(Tick(session, 1000)); // lethal damage and docking in the same tick: sinking wins
            var disk = Store().Load().Snapshot;
            Assert.That(disk.Campaign.ResolvedExpeditions[voyage], Is.EqualTo(Outcome.Sunk));
            Assert.That(Values.Amount(disk.Campaign.Bank, "wood"), Is.Zero);
            Assert.That(session.RequestDock(request, voyage, "home").Error, Is.EqualTo(RuleError.DuplicateRequest));
        }

        private static SessionSnapshot WithBank(SessionSnapshot snapshot, int wood)
        {
            var c = snapshot.Campaign;
            return new SessionSnapshot(snapshot.Revision, new CampaignState(c.HullId, c.CurrentHub, c.LastSafeHub,
                new Dictionary<string, int> { ["wood"] = wood }, c.Tiers, c.OwnedEquipment, c.Loadout, c.Hubs, c.Unlocks, c.ResolvedExpeditions),
                null, snapshot.CommittedRequests);
        }
    }
}
