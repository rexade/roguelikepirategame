#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PirateGame.Composition;
using PirateGame.Content.Definitions;
using PirateGame.Core;
using PirateGame.Persistence;
using PirateGame.Rules.Application;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace PirateGame.Tests.T09
{
    public sealed class SharedHubsTests
    {
        private const string Scene = "Assets/_Game/Scenes/OceanWorld.unity";
        private const string Outpost = "saltmarsh-harbor";
        private const string Home = "home-harbor";
        private string saves;
        private GameDirector director;

        [SetUp]
        public void Setup()
        {
            saves = Path.Combine(Path.GetTempPath(), "pirate-t09-play-" + Guid.NewGuid().ToString("N"));
            LaunchOptions.SaveDirectoryOverride = saves;
            LaunchOptions.SeedOverride = 99;
        }

        [TearDown]
        public void Cleanup()
        {
            LaunchOptions.SaveDirectoryOverride = null; LaunchOptions.SeedOverride = null; LaunchOptions.Mode = LaunchMode.Auto;
            JsonSaveStore.Flush(saves);
            if (Directory.Exists(saves)) Directory.Delete(saves, true);
        }

        // Test-only starting state: a docked campaign with a stocked bank.
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
            var result = new JsonSaveStore(saves, definitions).Initialize(stocked, out _);
            Assert.That(result.IsSuccess, result.Detail);
        }

        private IEnumerator Load()
        {
            LaunchOptions.Mode = LaunchMode.Continue;
            director = null;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 200 && (director == null || !director.Ready); i++)
            {
                director = Object.FindFirstObjectByType<GameDirector>();
                yield return null;
            }
            Assert.That(director != null && director.Ready);
            yield return null;
        }

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

        private static void Ok(RuleResult result) => Assert.That(result.Error, Is.EqualTo(RuleError.None), result.Detail);

        [UnityTest]
        public IEnumerator ClaimSaltmarshDockThereAndFastTravelHome()
        {
            PrepareSave(20, 10);
            yield return Load();
            var session = director.Session;
            Assert.That(director.harbor.Root.Q<Label>("travel-locked"), Is.Not.Null, "Travel starts locked");
            Assert.That(director.harbor.Root.Q<Label>("travel-unknown-" + Outpost), Is.Not.Null, "Saltmarsh starts uncharted");
            Ok(session.PurchaseUpgrade(Guid.NewGuid(), "navigators-charts"));
            Assert.That(session.Snapshot.Campaign.Unlocks, Contains.Item("fast-travel"));

            Ok(session.Embark(Guid.NewGuid(), director.PlanEmbark()));
            yield return null;
            var dock = director.Definitions.Hubs[Outpost].Dock;
            var sim = director.player;
            while (sim.HasPendingStep) yield return new WaitForFixedUpdate();
            sim.SetPaused(true);
            sim.motor.Teleport(new Vector3((float)dock.X, 0, (float)dock.Z - 1), 0, 0);
            Physics.SyncTransforms();
            sim.SetPaused(false);
            long tick = session.Tick;
            while (session.Tick < tick + 2) yield return new WaitForFixedUpdate();
            yield return null;
            director.RequestInteract();
            for (int i = 0; i < 400 && session.Lifecycle != Lifecycle.Docked; i++) yield return null;
            yield return null;
            var campaign = session.Snapshot.Campaign;
            Assert.That(campaign.Hubs[Outpost].Activated, Is.True, "Flag raised");
            Assert.That(campaign.CurrentHub, Is.EqualTo(Outpost), "Moored at Saltmarsh");
            Assert.That(campaign.LastSafeHub, Is.EqualTo(Outpost));
            Assert.That(director.harbor.Root.Q<Label>("title").text, Is.EqualTo("SALTMARSH HARBOR"));
            director.menus.Activate("continue");
            yield return null;

            var bank = new Dictionary<string, int>(campaign.Bank);
            yield return Click("travel-" + Home);
            campaign = session.Snapshot.Campaign;
            Assert.That(campaign.CurrentHub, Is.EqualTo(Home), "Fast travel through the harbor UI");
            Assert.That(campaign.Bank, Is.EquivalentTo(bank), "Shared bank unchanged by travel");
            var home = director.Definitions.Hubs[Home].Dock;
            Assert.That(Vector3.Distance(director.player.motor.Body.position, new Vector3((float)home.X, 0, (float)home.Z)), Is.LessThan(0.01f));
            Assert.That(session.InputLocked, Is.False);

            yield return Load();
            campaign = director.Session.Snapshot.Campaign;
            Assert.That(campaign.CurrentHub, Is.EqualTo(Home), "Reload keeps the travel destination");
            Assert.That(campaign.Hubs[Outpost].Activated, Is.True, "Reload keeps the claimed harbor");
            Assert.That(director.harbor.Root.Q<Button>("travel-" + Outpost), Is.Not.Null);
        }
    }
}
#endif
