#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PirateGame.Composition;
using PirateGame.Content.Definitions;
using PirateGame.Core;
using PirateGame.Gameplay.World.Streaming;
using PirateGame.Persistence;
using PirateGame.Rules.Application;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace PirateGame.Tests.T11
{
    // T11: travel between hubs in different regions, with region streaming.
    public sealed class CrossRegionTravelTests
    {
        private const string Scene = "Assets/_Game/Scenes/OceanWorld.unity";
        private const string Home = "home-harbor", Stormwatch = "stormwatch-harbor";
        private const string HomeRegion = "first-region", Gale = "galewater-reach";
        private string saves;
        private GameDirector director;

        [SetUp]
        public void Setup()
        {
            saves = Path.Combine(Path.GetTempPath(), "pirate-t11-play-" + Guid.NewGuid().ToString("N"));
            LaunchOptions.SaveDirectoryOverride = saves;
            LaunchOptions.SeedOverride = 11;
        }

        [TearDown]
        public void Cleanup()
        {
            LaunchOptions.SaveDirectoryOverride = null; LaunchOptions.SeedOverride = null; LaunchOptions.Mode = LaunchMode.Auto;
            JsonSaveStore.Flush(saves);
            if (Directory.Exists(saves)) Directory.Delete(saves, true);
        }

        private void PrepareSave(int wood, int iron)
        {
            var definitions = AssetDatabase.LoadAssetAtPath<DefinitionCatalogAsset>("Assets/_Game/Content/Production/GameCatalog.asset").Freeze();
            var fresh = CampaignSession.NewCampaign(definitions, Home, "cutter",
                new Dictionary<string, string> { ["cannon-1"] = "cannon", ["repeater-1"] = "repeater", ["brace-1"] = "brace" },
                new Dictionary<string, string> { ["weapon"] = "cannon-1", ["ability"] = "brace-1" });
            var c = fresh.Campaign;
            var stocked = new SessionSnapshot(0, new CampaignState(c.HullId, c.CurrentHub, c.LastSafeHub,
                new Dictionary<string, int> { ["wood"] = wood, ["iron"] = iron }, c.Tiers, c.OwnedEquipment, c.Loadout, c.Hubs, c.Unlocks,
                c.ResolvedExpeditions), null, Array.Empty<Guid>());
            Assert.That(new JsonSaveStore(saves, definitions).Initialize(stocked, out _).IsSuccess);
        }

        private IEnumerator Load()
        {
            LaunchOptions.Mode = LaunchMode.Continue;
            director = null;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Until(() => (director = director ?? Object.FindFirstObjectByType<GameDirector>()) != null && director.Ready);
            Assert.That(director != null && director.Ready, "Arrived");
        }

        private static IEnumerator Until(Func<bool> condition, int frames = 900)
        {
            for (int i = 0; i < frames && !condition(); i++) yield return null;
        }

        private static void Ok(RuleResult result) => Assert.That(result.Error, Is.EqualTo(RuleError.None), result.Detail);

        private IEnumerator Click(string name)
        {
            var button = director.harbor.Root.Q<Button>(name);
            Assert.That(button, Is.Not.Null, name);
            Assert.That(button.enabledInHierarchy, Is.True, name + " enabled");
            button.Focus();
            yield return null;
            using (var e = NavigationSubmitEvent.GetPooled()) { e.target = button; button.SendEvent(e); }
            yield return null;
        }

        private Vector3 DockOf(string hub)
        {
            var dock = director.Definitions.Hubs[hub].Dock;
            return new Vector3((float)dock.X, 0, (float)dock.Z);
        }

        [UnityTest]
        public IEnumerator TravelBetweenHomewardAndStormwatchStreamsEachDestination()
        {
            PrepareSave(30, 12);
            yield return Load();
            var session = director.Session;
            Ok(session.PurchaseUpgrade(Guid.NewGuid(), "navigators-charts"));

            // Sail to Stormwatch in Galewater, claim it and moor.
            Ok(session.Embark(Guid.NewGuid(), director.PlanEmbark()));
            yield return Until(() => director.Ready);
            var sim = director.player;
            while (sim.HasPendingStep) yield return new WaitForFixedUpdate();
            sim.SetPaused(true);
            sim.motor.Teleport(DockOf(Stormwatch) + new Vector3(0, 0, -1), 0, 0);
            Physics.SyncTransforms();
            sim.SetPaused(false);
            yield return Until(() => director.streamer.IsReady(Gale) && !director.Holding && session.Snapshot.Expedition.Position.RegionId == Gale);
            Assert.That(session.Snapshot.Expedition.Position.RegionId, Is.EqualTo(Gale), "Voyage entered Galewater");
            director.RequestInteract();
            yield return Until(() => session.Lifecycle == Lifecycle.Docked);
            Assert.That(session.Snapshot.Campaign.CurrentHub, Is.EqualTo(Stormwatch), "Claimed and moored at Stormwatch");
            Assert.That(session.Snapshot.Campaign.Hubs[Stormwatch].Activated, Is.True);
            if (director.CardOpen) director.menus.Activate("continue");
            yield return Until(() => director.streamer.StateOf(HomeRegion) == RegionState.Unloaded, 300);
            Assert.That(director.streamer.StateOf(HomeRegion), Is.EqualTo(RegionState.Unloaded), "Homeward is far away and retired");

            // Fast travel home: arrival waits for Homeward to stream back in.
            var bank = new Dictionary<string, int>(session.Snapshot.Campaign.Bank);
            yield return Click("travel-" + Home);
            Assert.That(session.Snapshot.Campaign.CurrentHub, Is.EqualTo(Home), "Travel committed");
            yield return Until(() => director.Ready);
            Assert.That(director.Ready, Is.True, "Arrival completed after the destination region loaded");
            Assert.That(director.streamer.IsReady(HomeRegion), Is.True);
            Assert.That(Vector3.Distance(director.player.motor.Body.position, DockOf(Home)), Is.LessThan(0.01f));
            Assert.That(session.Snapshot.Campaign.Bank, Is.EquivalentTo(bank), "One shared bank across regions");
            Assert.That(session.Snapshot.Campaign.LastSafeHub, Is.EqualTo(Home));

            // And back to Stormwatch across the regions.
            yield return Until(() => director.harbor.Root.Q<Button>("travel-" + Stormwatch) != null, 60);
            yield return Click("travel-" + Stormwatch);
            yield return Until(() => director.Ready);
            Assert.That(session.Snapshot.Campaign.CurrentHub, Is.EqualTo(Stormwatch));
            Assert.That(director.streamer.IsReady(Gale), Is.True);
            Assert.That(Vector3.Distance(director.player.motor.Body.position, DockOf(Stormwatch)), Is.LessThan(0.01f));

            // A reload resumes docked at Stormwatch with its region loaded.
            yield return Load();
            Assert.That(director.Session.Snapshot.Campaign.CurrentHub, Is.EqualTo(Stormwatch));
            Assert.That(director.streamer.IsReady(Gale), Is.True);
            Assert.That(Vector3.Distance(director.player.motor.Body.position, DockOf(Stormwatch)), Is.LessThan(0.01f));
        }
    }
}
#endif
