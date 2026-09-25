using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PirateGame.Rules.Application;
using PirateGame.Core;

namespace PirateGame.Tests.T03
{
    public sealed class CampaignRulesTests
    {
        private DefinitionCatalog definitions;
        private CampaignSession session;
        private FakeSaveStore store;
        private FakeArrival arrival;

        private void Start(int bank = 0, double cargo = 10, bool otherActive = false, bool travel = false)
        {
            definitions = RuleFixtures.Catalog(cargo);
            Load(RuleFixtures.Initial(definitions, bank, otherActive, travel));
        }
        private void Load(SessionSnapshot initial)
        {
            store = new FakeSaveStore { Committed = initial }; arrival = new FakeArrival();
            session = new CampaignSession(definitions, initial, store, arrival);
            Assert.That(session.InputLocked, Is.True);
            Ok(session.RetryArrival());
        }
        private Guid Sail(params EntityState[] entities)
        {
            var plan = RuleFixtures.Plan(entities); Ok(session.Embark(Guid.NewGuid(), plan));
            return plan.ExpeditionId;
        }
        private RuleResult Tick(double damage = 0, SeaPosition? position = null, double speed = 0) =>
            session.CompleteTick(session.Snapshot.Expedition.Id, session.Tick + 1, damage, position ?? RuleFixtures.Home, speed);
        private static void Ok(RuleResult result) => Assert.That(result.Error, Is.EqualTo(RuleError.None), result.Detail);
        private static void Error(RuleResult result, RuleError error) => Assert.That(result.Error, Is.EqualTo(error), result.Detail);

        [Test]
        public void AC01_DoubleDockBanksSevenOnce()
        {
            Start(); Guid expedition = Sail(RuleFixtures.Loot("wreck", 7));
            Ok(session.CollectLoot(Guid.NewGuid(), expedition, EntityId.Authored("wreck")));
            var request = Guid.NewGuid(); var queued = session.RequestDock(request, expedition, "home");
            Ok(queued); Assert.That(queued.IsPending, Is.True);
            Assert.That(session.Snapshot.Campaign.Bank["wood"], Is.Zero);
            Ok(Tick());
            Error(session.RequestDock(request, expedition, "home"), RuleError.DuplicateRequest);
            Error(session.RequestDock(Guid.NewGuid(), expedition, "home"), RuleError.WrongLifecycle);
            Assert.That(session.Snapshot.Campaign.Bank["wood"], Is.EqualTo(7));
            Assert.That(session.Snapshot.Expedition, Is.Null);
            Assert.That(session.Snapshot.Campaign.ResolvedExpeditions[expedition], Is.EqualTo(Outcome.Docked));
            Assert.That(session.DrainEvents().Count(e => e.Command == "Dock"), Is.EqualTo(1));
        }

        [Test]
        public void AC02_LethalDamageWinsOverQueuedDockAndFailureFreezesSink()
        {
            Start(); Guid expedition = Sail(RuleFixtures.Loot("wreck", 7));
            Ok(session.CollectLoot(Guid.NewGuid(), expedition, EntityId.Authored("wreck")));
            session.DrainEvents(); var before = session.LastCommitted;
            Ok(session.RequestDock(Guid.NewGuid(), expedition, "home")); store.Fail = true;
            Error(Tick(100), RuleError.SaveFailed);
            Assert.That(session.Lifecycle, Is.EqualTo(Lifecycle.Resolving));
            Assert.That(session.LastCommitted, Is.SameAs(before));
            Assert.That(session.PendingSave.Command, Is.EqualTo("Sink"));
            Assert.That(session.DrainEvents(), Is.Empty);
            Error(Tick(), RuleError.Busy);
            store.Fail = false; Ok(session.RetrySave());
            Assert.That(session.Snapshot.Campaign.Bank["wood"], Is.Zero);
            Assert.That(session.Snapshot.Campaign.ResolvedExpeditions[expedition], Is.EqualTo(Outcome.Sunk));
            Assert.That(session.DrainEvents().Single().Command, Is.EqualTo("Sink"));
            Error(session.ResolveSink(Guid.NewGuid(), expedition), RuleError.WrongLifecycle);
        }

        [Test]
        public void AC03_UnaffordableUpgradeLeavesBankAndTierUnchanged()
        {
            Start(bank: 5); var before = session.Snapshot;
            Error(session.PurchaseUpgrade(Guid.NewGuid(), "hull-1"), RuleError.InsufficientBank);
            Assert.That(session.Snapshot, Is.SameAs(before)); Assert.That(store.Attempts, Is.Zero);
        }

        [Test]
        public void AC04_WholeBundleRejectsWithoutDepletionAndDuplicateCannotCredit()
        {
            Start(); Guid expedition = Sail(RuleFixtures.Loot("first", 8), RuleFixtures.Loot("second", 3));
            Ok(session.CollectLoot(Guid.NewGuid(), expedition, EntityId.Authored("first")));
            var before = session.Snapshot;
            Error(session.CollectLoot(Guid.NewGuid(), expedition, EntityId.Authored("second")), RuleError.CargoFull);
            Assert.That(session.Snapshot, Is.SameAs(before));
            Assert.That(session.Snapshot.Expedition.Entities[EntityId.Authored("second")].Loot["wood"], Is.EqualTo(3));
            Error(session.CollectLoot(Guid.NewGuid(), expedition, EntityId.Authored("first")), RuleError.Depleted);
            Assert.That(session.Snapshot.Expedition.Cargo["wood"], Is.EqualTo(8));
        }

        [Test]
        public void AC06_SinkWithEmptyBankPreservesUpgradedLoadoutAndFreeEmbark()
        {
            Start(bank: 6); Ok(session.PurchaseUpgrade(Guid.NewGuid(), "hull-1"));
            Ok(session.SetLoadout(Guid.NewGuid(), new Dictionary<string, string> { ["gun"] = "weapon-2", ["ability"] = "ability-1" }));
            Guid old = Sail(RuleFixtures.Loot("wreck", 3));
            Ok(session.CollectLoot(Guid.NewGuid(), old, EntityId.Authored("wreck")));
            Ok(Tick(1000));
            Assert.That(session.Snapshot.Campaign.Bank["wood"], Is.Zero);
            Assert.That(session.Snapshot.Campaign.Loadout["gun"], Is.EqualTo("weapon-2"));
            Assert.That(session.Snapshot.Campaign.OwnedEquipment.Count, Is.EqualTo(3));
            Assert.That(session.Snapshot.Campaign.Tiers["hull"], Is.EqualTo(1));
            Assert.That(session.Snapshot.Campaign.Unlocks, Does.Contain("fast-travel"));
            var next = Sail(); Assert.That(next, Is.Not.EqualTo(old));
            Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(180));
            Assert.That(session.Snapshot.Expedition.Cargo, Is.Empty);
            Assert.That(session.Snapshot.Expedition.Modifiers, Is.Empty);
            Error(session.ResolveSink(Guid.NewGuid(), old), RuleError.AlreadyResolved);
        }

        [Test]
        public void AC08_TravelAtSeaOrInactiveDestinationChangesNothing()
        {
            Start(travel: true); var before = session.Snapshot;
            Error(session.FastTravel(Guid.NewGuid(), "other"), RuleError.HubInactive); Assert.That(session.Snapshot, Is.SameAs(before));
            Sail(); before = session.Snapshot;
            Error(session.FastTravel(Guid.NewGuid(), "home"), RuleError.WrongLifecycle); Assert.That(session.Snapshot, Is.SameAs(before));
        }

        [Test]
        public void UpgradeAndTravelShareCampaignButPreserveLocalFlags()
        {
            Start(bank: 10, otherActive: true);
            Error(session.FastTravel(Guid.NewGuid(), "other"), RuleError.TravelLocked);
            Error(session.PurchaseUpgrade(Guid.NewGuid(), "hull-2"), RuleError.PrerequisiteMissing);
            var request = Guid.NewGuid(); Ok(session.PurchaseUpgrade(request, "hull-1"));
            Error(session.PurchaseUpgrade(request, "hull-1"), RuleError.DuplicateRequest);
            Error(session.PurchaseUpgrade(Guid.NewGuid(), "hull-1"), RuleError.TierAlreadyOwned);
            Ok(session.FastTravel(Guid.NewGuid(), "other"));
            Assert.That(session.Snapshot.Campaign.Bank["wood"], Is.EqualTo(4));
            Assert.That(session.Snapshot.Campaign.Tiers["hull"], Is.EqualTo(1));
            Assert.That(session.Snapshot.Campaign.CurrentHub, Is.EqualTo("other"));
            Assert.That(session.Snapshot.Campaign.LastSafeHub, Is.EqualTo("other"));
            Assert.That(session.Snapshot.Campaign.Hubs["other"].StoryFlags, Does.Contain("local-story"));
            Assert.That(session.ShipStats()["health"], Is.EqualTo(150));
        }

        [Test]
        public void DockFailureRetriesIdenticalCandidateAndPublishesOnlyAfterCommit()
        {
            Start(); Guid expedition = Sail(RuleFixtures.Loot("wreck", 7));
            Ok(session.CollectLoot(Guid.NewGuid(), expedition, EntityId.Authored("wreck"))); session.DrainEvents();
            var committed = store.Committed; store.Fail = true;
            Ok(session.RequestDock(Guid.NewGuid(), expedition, "home")); Error(Tick(), RuleError.SaveFailed);
            var pending = session.PendingSave;
            Assert.That(store.Committed, Is.SameAs(committed)); Assert.That(session.DrainEvents(), Is.Empty);
            Error(session.CollectLoot(Guid.NewGuid(), expedition, EntityId.Authored("wreck")), RuleError.Busy);
            Error(session.PurchaseUpgrade(Guid.NewGuid(), "hull-1"), RuleError.Busy);
            Error(session.Checkpoint(Guid.NewGuid()), RuleError.Busy);
            Error(session.RetrySave(), RuleError.SaveFailed); Assert.That(session.PendingSave, Is.SameAs(pending));
            store.Fail = false; Ok(session.RetrySave());
            Assert.That(store.Candidates.Last(), Is.SameAs(pending));
            Assert.That(session.Snapshot.Campaign.Bank["wood"], Is.EqualTo(7));
            Assert.That(session.DrainEvents().Count, Is.EqualTo(1));
            Error(session.RetrySave(), RuleError.InvalidRequest);
        }

        [Test]
        public void EmbarkFailureDoesNotPublishExpeditionOrEnableInput()
        {
            Start(); store.Throw = true; var initial = session.Snapshot;
            Error(session.Embark(Guid.NewGuid(), RuleFixtures.Plan()), RuleError.SaveFailed);
            Assert.That(session.Lifecycle, Is.EqualTo(Lifecycle.Departing)); Assert.That(session.Snapshot, Is.SameAs(initial));
            Assert.That(session.InputLocked, Is.True); Assert.That(session.DrainEvents(), Is.Empty);
            store.Throw = false; Ok(session.RetrySave()); Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(100));
        }

        [Test]
        public void ArrivalFailureKeepsCommittedDestinationLockedAndRetriesWithoutAnotherSave()
        {
            Start(otherActive: true, travel: true); arrival.Fail = true;
            Error(session.FastTravel(Guid.NewGuid(), "other"), RuleError.ArrivalFailed);
            Assert.That(store.Committed.Campaign.CurrentHub, Is.EqualTo("other"));
            Assert.That(session.InputLocked, Is.True); var request = arrival.Last;
            Error(session.Embark(Guid.NewGuid(), RuleFixtures.Plan()), RuleError.Busy);
            Error(session.RetryArrival(), RuleError.ArrivalFailed); Assert.That(arrival.Last, Is.SameAs(request));
            arrival.Fail = false; Ok(session.RetryArrival());
            Assert.That(session.InputLocked, Is.False); Assert.That(store.Attempts, Is.EqualTo(1));
            Assert.That(session.DrainEvents().Count, Is.EqualTo(1));
        }

        [Test]
        public void SavePortRejectsOlderCheckpointAfterNewPurchaseAndReentrancyIsLocked()
        {
            Start(bank: 20);
            store.DuringCommit = candidate =>
            {
                Error(session.Checkpoint(Guid.NewGuid()), RuleError.Busy);
                Error(session.RetrySave(), RuleError.Busy);
                Assert.That(session.Snapshot.Revision, Is.EqualTo(candidate.ExpectedRevision));
            };
            Ok(session.Checkpoint(Guid.NewGuid())); var older = store.Candidates.Last();
            Ok(session.PurchaseUpgrade(Guid.NewGuid(), "hull-1")); store.DuringCommit = null;
            Error(store.Commit(older), RuleError.SaveFailed);
            Assert.That(store.Committed.Campaign.Bank["wood"], Is.EqualTo(14));
            Assert.That(store.Committed.Revision, Is.EqualTo(2));
        }

        [TestCase(0, 0, 0, RuleError.None)]
        [TestCase(5, 0, 1, RuleError.None)]
        [TestCase(5.01, 0, 0, RuleError.NotInDockZone)]
        [TestCase(0, 0, 1.01, RuleError.TooFast)]
        public void DockUsesAuthoredRadiusAndSpeed(double x, double z, double speed, RuleError expected)
        {
            Start(); var id = Sail(); Ok(session.RequestDock(Guid.NewGuid(), id, "home"));
            Error(Tick(position: new SeaPosition("region-a", x, z), speed: speed), expected);
            Assert.That(session.Snapshot.Expedition == null, Is.EqualTo(expected == RuleError.None));
        }

        [Test]
        public void DockRejectsOtherRegionEvenAtMatchingCoordinates()
        {
            Start(); var id = Sail(); Ok(session.RequestDock(Guid.NewGuid(), id, "home"));
            Error(Tick(position: new SeaPosition("region-b", 0, 0)), RuleError.NotInDockZone);
        }

        [Test]
        public void BankingOverflowCannotPartiallyTransfer()
        {
            Start(bank: int.MaxValue); var id = Sail(RuleFixtures.Loot("wreck", 1));
            Ok(session.CollectLoot(Guid.NewGuid(), id, EntityId.Authored("wreck")));
            Ok(session.RequestDock(Guid.NewGuid(), id, "home")); Error(Tick(), RuleError.Overflow);
            Assert.That(session.Snapshot.Campaign.Bank["wood"], Is.EqualTo(int.MaxValue));
            Assert.That(session.Snapshot.Expedition.Cargo["wood"], Is.EqualTo(1));
            Assert.That(session.Snapshot.Campaign.ResolvedExpeditions, Is.Empty);
        }

        [Test]
        public void AC16_HarborAndEmbarkUseTheSameOwnedCompatibleLoadoutValidation()
        {
            Start();
            var invalid = new[] {
                new Dictionary<string, string> { ["gun"] = "weapon-1", ["ability"] = "weapon-1" },
                new Dictionary<string, string> { ["gun"] = "ability-1", ["ability"] = "weapon-1" },
                new Dictionary<string, string> { ["gun"] = "unowned", ["ability"] = "ability-1" },
                new Dictionary<string, string> { ["gun"] = "weapon-1" }
            };
            foreach (var loadout in invalid)
            {
                Error(session.ValidateLoadout(loadout), RuleError.InvalidLoadout);
                Error(session.SetLoadout(Guid.NewGuid(), loadout), RuleError.InvalidLoadout);
                var c = session.Snapshot.Campaign;
                var bad = new SessionSnapshot(0, new CampaignState(c.HullId, c.CurrentHub, c.LastSafeHub, c.Bank, c.Tiers,
                    c.OwnedEquipment, loadout, c.Hubs, c.Unlocks, c.ResolvedExpeditions), null, Array.Empty<Guid>());
                Error(CampaignSession.ValidateSnapshot(definitions, bad), RuleError.InvalidLoadout);
                Assert.Throws<ArgumentException>(() => new CampaignSession(definitions, bad, store, arrival));
            }
            var id = Sail(); Error(session.SetLoadout(Guid.NewGuid(), session.Snapshot.Campaign.Loadout), RuleError.WrongLifecycle);
            Error(session.CollectLoot(Guid.NewGuid(), Guid.NewGuid(), EntityId.Authored("missing")), RuleError.WrongExpedition);
            Error(session.ResolveSink(Guid.NewGuid(), id), RuleError.NotSunk);
        }

        [Test]
        public void MalformedLoadoutAndUnknownCommandsReturnErrorsWithoutMutation()
        {
            Start(); var before = session.Snapshot;
            Error(session.SetLoadout(Guid.NewGuid(), null), RuleError.InvalidLoadout);
            Error(session.SetLoadout(Guid.NewGuid(), new Dictionary<string, string> { ["gun"] = null, ["ability"] = "ability-1" }), RuleError.InvalidLoadout);
            Error(session.PurchaseUpgrade(Guid.NewGuid(), null), RuleError.UnknownId);
            Error(session.FastTravel(Guid.NewGuid(), "missing"), RuleError.UnknownId);
            Error(session.Embark(Guid.Empty, RuleFixtures.Plan()), RuleError.InvalidRequest);
            Assert.That(session.Snapshot, Is.SameAs(before)); Assert.That(store.Attempts, Is.Zero);
        }

        [Test]
        public void MixedResourceWeightsRejectWholeBundleAndOverflow()
        {
            Start(cargo: 3);
            var loot = new EntityState(EntityId.Authored("mixed"), "barrel", RuleFixtures.Home, 1, false,
                new Dictionary<string, int> { ["wood"] = 2, ["iron"] = 1 }, new Dictionary<string, double>(), "idle");
            var huge = new EntityState(EntityId.Authored("huge"), "barrel", RuleFixtures.Home, 1, false,
                new Dictionary<string, int> { ["iron"] = int.MaxValue }, new Dictionary<string, double>(), "idle");
            var id = Sail(loot, huge); var before = session.Snapshot;
            Error(session.CollectLoot(Guid.NewGuid(), id, loot.Id), RuleError.CargoFull);
            Error(session.CollectLoot(Guid.NewGuid(), id, huge.Id), RuleError.Overflow);
            Assert.That(session.Snapshot, Is.SameAs(before));
        }

        [Test]
        public void PurchaseFailureCannotDeductOrGrantUntilRetry()
        {
            Start(bank: 6); var before = session.Snapshot; store.Fail = true;
            Error(session.PurchaseUpgrade(Guid.NewGuid(), "hull-1"), RuleError.SaveFailed);
            Assert.That(session.Snapshot, Is.SameAs(before)); Assert.That(session.DrainEvents(), Is.Empty);
            store.Fail = false; Ok(session.RetrySave());
            Assert.That(session.Snapshot.Campaign.Bank["wood"], Is.Zero);
            Assert.That(session.Snapshot.Campaign.Tiers["hull"], Is.EqualTo(1));
            Assert.That(session.DrainEvents().Count, Is.EqualTo(1));
        }

        [Test]
        public void PausedClockAndInvalidTicksCannotChangeSimulation()
        {
            Start(); var id = Sail(); var before = session.Snapshot;
            session.SetPaused(true); Error(Tick(10), RuleError.Paused); Assert.That(session.Snapshot, Is.SameAs(before));
            session.SetPaused(false);
            Error(session.CompleteTick(id, 0, 10, RuleFixtures.Home, 0), RuleError.StaleTick);
            Error(Tick(double.NaN), RuleError.InvalidRequest); Error(Tick(-1), RuleError.InvalidRequest);
            Ok(Tick(10)); Assert.That(session.Tick, Is.EqualTo(1)); Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(90));
            Assert.That(session.LastCommitted.Expedition.Health, Is.EqualTo(100));
            Ok(session.Checkpoint(Guid.NewGuid())); Assert.That(session.LastCommitted.Expedition.Health, Is.EqualTo(90));
        }

        [Test]
        public void RestoredSnapshotRetainsDamageCooldownsModifiersEntitiesAndRng()
        {
            Start(); var id = Sail(RuleFixtures.Loot("wreck", 7));
            Ok(session.CollectLoot(Guid.NewGuid(), id, EntityId.Authored("wreck")));
            var e = session.Snapshot.Expedition;
            var resumed = new ExpeditionState(e.Id, e.Seed, "rng:42:17", 20, 63, 0.7, new SeaPosition("region-b", 9, 3), e.Cargo,
                new[] { new StatModifier("health", ModifierOperation.Flat, 5) }, new Dictionary<string, double> { ["gun"] = 2 }, e.Encounters, e.Entities.Values);
            Load(new SessionSnapshot(session.Snapshot.Revision, session.Snapshot.Campaign, resumed, session.Snapshot.CommittedRequests));
            Assert.That(session.Snapshot.Expedition, Is.SameAs(resumed));
            Assert.That(arrival.Last.Destination.RegionId, Is.EqualTo("region-b"));
            Error(session.CollectLoot(Guid.NewGuid(), id, EntityId.Authored("wreck")), RuleError.Depleted);
            Ok(Tick()); Assert.That(session.Snapshot.Expedition.Cooldowns["gun"], Is.EqualTo(1.98).Within(0.00001));
            Ok(Tick(1000)); Sail(); Assert.That(session.Snapshot.Expedition.Modifiers, Is.Empty);
            Assert.That(session.Snapshot.Expedition.Cooldowns, Is.Empty); Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(100));
        }

        [Test]
        public void LootFailureRetainsCargoAndSourceUntilCommit()
        {
            Start(); var id = Sail(RuleFixtures.Loot("wreck", 7)); var before = session.Snapshot; store.Fail = true;
            Error(session.CollectLoot(Guid.NewGuid(), id, EntityId.Authored("wreck")), RuleError.SaveFailed);
            Assert.That(session.Snapshot, Is.SameAs(before)); Assert.That(before.Expedition.Cargo, Is.Empty);
            Assert.That(before.Expedition.Entities[EntityId.Authored("wreck")].Loot["wood"], Is.EqualTo(7));
            store.Fail = false; Ok(session.RetrySave());
            Assert.That(session.Snapshot.Expedition.Cargo["wood"], Is.EqualTo(7));
        }

        [Test]
        public void WorldCaptureCommitsCoherentStateButCannotRefillLootOrLoseHistory()
        {
            Start(); var id = Sail(RuleFixtures.Loot("wreck", 7));
            Ok(session.CollectLoot(Guid.NewGuid(), id, EntityId.Authored("wreck"))); Ok(Tick(10));
            var e = session.Snapshot.Expedition;
            Func<IEnumerable<EntityState>, ExpeditionState> capture = entities => new ExpeditionState(e.Id, e.Seed, "rng:42:22", e.Tick,
                e.Health, e.Speed, e.Position, e.Cargo, e.Modifiers, new Dictionary<string, double> { ["gun"] = 1.5 }, e.Encounters, entities);
            Error(session.Checkpoint(Guid.NewGuid(), capture(new[] { RuleFixtures.Loot("wreck", 7) })), RuleError.InvalidRequest);
            Error(session.Checkpoint(Guid.NewGuid(), capture(Array.Empty<EntityState>())), RuleError.InvalidRequest);
            var valid = capture(e.Entities.Values); store.Fail = true;
            Error(session.Checkpoint(Guid.NewGuid(), valid), RuleError.SaveFailed);
            Assert.That(session.Snapshot.Expedition.RngState, Is.EqualTo("rng:42:0"));
            store.Fail = false; Ok(session.RetrySave());
            Assert.That(session.LastCommitted.Expedition.RngState, Is.EqualTo("rng:42:22"));
            Assert.That(session.LastCommitted.Expedition.Cooldowns["gun"], Is.EqualTo(1.5));
        }
    }
}
