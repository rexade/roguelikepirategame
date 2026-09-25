#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PirateGame.Composition;
using PirateGame.Core;
using PirateGame.Gameplay.World.Streaming;
using PirateGame.Rules.Application;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Tests.T10
{
    public sealed class RegionStreamingTests
    {
        private const string Scene = "Assets/_Game/Scenes/OceanWorld.unity";
        private const string Gale = "galewater-reach";
        private const string Home = "first-region";
        private string saves;
        private GameDirector director;

        [SetUp]
        public void Setup()
        {
            saves = Path.Combine(Path.GetTempPath(), "pirate-t10-play-" + Guid.NewGuid().ToString("N"));
            LaunchOptions.SaveDirectoryOverride = saves;
            LaunchOptions.SeedOverride = 4242;
        }

        [TearDown]
        public void Cleanup()
        {
            LaunchOptions.SaveDirectoryOverride = null; LaunchOptions.SeedOverride = null; LaunchOptions.Mode = LaunchMode.Auto;
            if (Directory.Exists(saves)) Directory.Delete(saves, true);
        }

        private IEnumerator Load(LaunchMode mode)
        {
            LaunchOptions.Mode = mode;
            director = null;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 400 && (director == null || !director.Ready); i++)
            {
                director = Object.FindFirstObjectByType<GameDirector>();
                yield return null;
            }
            Assert.That(director != null && director.Ready, "Director did not arrive");
        }

        private static void Ok(RuleResult result) => Assert.That(result.Error, Is.EqualTo(RuleError.None), result.Detail);

        private IEnumerator Embark()
        {
            if (director.CardOpen) director.menus.Activate("continue");
            yield return null;
            Ok(director.Session.Embark(Guid.NewGuid(), director.PlanEmbark()));
            for (int i = 0; i < 200 && !director.Ready; i++) yield return null;
            Assert.That(director.Session.Lifecycle, Is.EqualTo(Lifecycle.AtSea));
        }

        // Test-only relocation; streaming and the region-hold logic then react as in play.
        private IEnumerator Sail(Vector3 to, int settleTicks = 3)
        {
            var sim = director.player;
            while (sim.HasPendingStep) yield return new WaitForFixedUpdate();
            sim.SetPaused(true);
            sim.motor.Teleport(to, 0, 0);
            Physics.SyncTransforms();
            sim.SetPaused(false);
            for (int i = 0; i < 600; i++)
            {
                yield return null;
                var at = director.Session.Snapshot.Expedition?.Position;
                if (!director.Holding && at.HasValue && Math.Abs(at.Value.Z - to.z) < 1 && Math.Abs(at.Value.X - to.x) < 1) break;
            }
            for (int i = 0; i < settleTicks; i++) yield return new WaitForFixedUpdate();
            yield return null;
        }

        private IEnumerator Until(Func<bool> condition, int frames = 600)
        {
            for (int i = 0; i < frames && !condition(); i++) yield return null;
        }

        private int GaleShips() => director.Enemies.Count(e => e.Id.SpawnId != null && e.Id.SpawnId.StartsWith("gale:", StringComparison.Ordinal));

        [UnityTest]
        public IEnumerator ApproachingGalewaterStreamsItAndCrossingChangesTheVoyageRegion()
        {
            yield return Load(LaunchMode.NewCampaign);
            yield return Embark();
            var streamer = director.streamer;
            Assert.That(streamer.IsReady(Home), Is.True);
            Assert.That(streamer.StateOf(Gale), Is.EqualTo(RegionState.Unloaded), "Distant region is not loaded at home");
            Assert.That(GaleShips(), Is.Zero);

            yield return Sail(new Vector3(0, 0, 140));
            yield return Until(() => streamer.IsReady(Gale) && GaleShips() > 0);
            Assert.That(streamer.IsReady(Gale), Is.True, "Loaded ahead of the border");
            Assert.That(GaleShips(), Is.GreaterThan(0), "Galewater ships are placed from the ledger");
            Assert.That(director.Session.Snapshot.Expedition.Position.RegionId, Is.EqualTo(Home));
            Assert.That(director.interaction.sources.Any(s => s != null && s.Id.AuthoredId == "gale:barrel-01"), "Galewater salvage is reachable");

            yield return Sail(new Vector3(0, 0, 182));
            Assert.That(director.Session.Snapshot.Expedition.Position.RegionId, Is.EqualTo(Gale), "Published position carries the new region");
            var ledger = director.Session.Snapshot.Expedition.Entities.Values.Count(e => e.Id.SpawnId != null && e.Id.SpawnId.StartsWith("gale:", StringComparison.Ordinal) && e.DefinitionId != "wreck");
            Assert.That(GaleShips(), Is.EqualTo(ledger), "Exactly one scene ship per ledger entry");
        }

        [UnityTest]
        public IEnumerator LeavingAndReturningKeepsGalewaterStateWithoutDuplicates()
        {
            yield return Load(LaunchMode.NewCampaign);
            yield return Embark();
            var session = director.Session;
            yield return Sail(new Vector3(0, 0, 182));
            yield return Until(() => GaleShips() > 0);
            // The escort near the border would engage at once; sink it to keep the test about streaming.
            foreach (var escort in director.Enemies.Where(e => e.Id.SpawnId.StartsWith("gale:encounter-01", StringComparison.Ordinal)).ToList())
                escort.Target.ApplyDamage(10000);
            for (int i = 0; i < 5; i++) yield return null;
            // Salvage a Galewater barrel and wound a Galewater ship.
            director.RequestInteract();
            yield return Until(() => Values.Amount(session.Snapshot.Expedition.Cargo, "iron") > 0);
            Assert.That(Values.Amount(session.Snapshot.Expedition.Cargo, "iron"), Is.EqualTo(1), "gale:barrel-01 salvaged");
            var victim = director.Enemies.First(e => e.Id.SpawnId.StartsWith("gale:encounter-03", StringComparison.Ordinal));
            var id = victim.Id;
            victim.Target.ApplyDamage(25);
            double wounded = victim.Target.Health;

            yield return Sail(new Vector3(0, 0, -5));
            yield return Until(() => director.streamer.StateOf(Gale) == RegionState.Unloaded);
            Assert.That(director.streamer.StateOf(Gale), Is.EqualTo(RegionState.Unloaded), "Far region retired");
            Assert.That(GaleShips(), Is.Zero, "Its ships left the scene");
            var kept = session.Snapshot.Expedition.Entities[id];
            Assert.That(kept.Health, Is.EqualTo(wounded), "Captured into the ledger before unloading");
            Assert.That(session.Snapshot.Expedition.Entities[EntityId.Authored("gale:barrel-01")].Loot.Values.Sum(), Is.Zero);

            yield return Sail(new Vector3(0, 0, 182));
            yield return Until(() => GaleShips() > 0);
            var again = director.Enemies.Where(e => e.Id.Equals(id)).ToList();
            Assert.That(again.Count, Is.EqualTo(1), "No duplicated ship after returning");
            Assert.That(again[0].Target.Health, Is.EqualTo(wounded), "Damage survives the round trip");
            var barrel = director.interaction.sources.Single(s => s != null && s.Id.AuthoredId == "gale:barrel-01");
            Assert.That(barrel.Depleted, Is.True, "Loot is not restocked by reloading the region");
            Assert.That(Values.Amount(session.Snapshot.Expedition.Cargo, "iron"), Is.EqualTo(1), "Cargo credited once");
        }

        [UnityTest]
        public IEnumerator ResumeInsideGalewaterWaitsForTheRegionAndRestoresIt()
        {
            yield return Load(LaunchMode.NewCampaign);
            yield return Embark();
            yield return Sail(new Vector3(-6, 0, 200));
            yield return Until(() => GaleShips() > 0);
            while (director.player.HasPendingStep) yield return new WaitForFixedUpdate();
            Ok(director.Checkpoint());
            director.player.SetPaused(true);
            var saved = director.Session.Snapshot.Expedition;
            int ships = GaleShips();

            yield return Load(LaunchMode.Continue);
            var restored = director.Session.LastCommitted.Expedition;
            Assert.That(restored.Id, Is.EqualTo(saved.Id));
            Assert.That(restored.Position.RegionId, Is.EqualTo(Gale));
            Assert.That(director.streamer.IsReady(Gale), Is.True, "Arrival waited for the region scene");
            Assert.That(GaleShips(), Is.EqualTo(ships), "Same Galewater ships after resume");
            var at = director.player.motor.Body.position;
            Assert.That(at.z, Is.EqualTo((float)saved.Position.Z).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator UnreadyRegionHoldsTheShipAtItsEdge()
        {
            yield return Load(LaunchMode.NewCampaign);
            yield return Embark();
            var streamer = director.streamer;
            streamer.BlockedRegions.Add(Gale);
            director.playerInput.enabled = false;
            yield return Sail(new Vector3(0, 0, 158));
            Assert.That(streamer.StateOf(Gale), Is.EqualTo(RegionState.Failed), "Blocked region cannot become ready");
            // Sail north across the border at full speed (bounded by simulated time, not frames).
            for (int i = 0; i < 600 && !director.Holding; i++)
            {
                director.player.Submit(new InputIntent(1, 0, 0, 0, false, false, false), false);
                yield return new WaitForFixedUpdate();
                yield return null;
            }
            Assert.That(director.Holding, Is.True, "Ship is held at the edge instead of sailing into missing collision");
            long tick = director.Session.Tick;
            for (int i = 0; i < 15; i++) { director.player.Submit(new InputIntent(1, 0, 0, 0, false, false, false), false); yield return new WaitForFixedUpdate(); }
            Assert.That(director.Session.Tick, Is.EqualTo(tick), "Simulation stays suspended while held");
            Assert.That(director.player.motor.Body.position.z, Is.LessThan(166.5f), "At most one tick past the border");
            Assert.That(director.menus.IsOpen, Is.False, "Holding is not an error");

            streamer.BlockedRegions.Clear();
            streamer.Retry(Gale);
            yield return Until(() => !director.Holding);
            Assert.That(director.Holding, Is.False, "Released once the region is ready");
            for (int i = 0; i < 300 && director.Session.Snapshot.Expedition.Position.RegionId != Gale; i++)
            {
                director.player.Submit(new InputIntent(1, 0, 0, 0, false, false, false), false);
                yield return new WaitForFixedUpdate();
            }
            Assert.That(director.Session.Snapshot.Expedition.Position.RegionId, Is.EqualTo(Gale));
        }
    }
}
#endif
