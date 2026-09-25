#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PirateGame.Composition;
using PirateGame.Core;
using PirateGame.Gameplay.World;
using PirateGame.Persistence;
using PirateGame.Rules.Application;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Tests.T08
{
    // Integrated OceanWorld scene: real session, real disk store, real arrival.
    public sealed class ExpeditionLoopTests
    {
        private const string Scene = "Assets/_Game/Scenes/OceanWorld.unity";
        private string saves;
        private GameDirector director;

        [SetUp]
        public void Setup()
        {
            saves = Path.Combine(Path.GetTempPath(), "pirate-t08-play-" + Guid.NewGuid().ToString("N"));
            LaunchOptions.SaveDirectoryOverride = saves;
            LaunchOptions.SeedOverride = 1234;
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
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 200 && (director == null || !director.Ready); i++)
            {
                director = Object.FindFirstObjectByType<GameDirector>();
                yield return null;
            }
            Assert.That(director != null && director.Ready, "OceanWorld director did not start a session");
        }

        private static void Ok(RuleResult result) => Assert.That(result.Error, Is.EqualTo(RuleError.None), result.Detail);

        private IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }

        private IEnumerator Ticks(int count) { for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate(); yield return null; }

        // Test-only relocation at an idle boundary; the next tick publishes the new position.
        private IEnumerator Sail(Vector3 to)
        {
            var sim = director.player;
            while (sim.HasPendingStep) yield return new WaitForFixedUpdate();
            sim.SetPaused(true);
            sim.motor.Teleport(to, 0, 0);
            Physics.SyncTransforms();
            sim.SetPaused(false);
            long tick = director.Session.Tick;
            for (int i = 0; i < 20 && director.Session.Tick < tick + 2; i++) yield return new WaitForFixedUpdate();
            yield return null;
        }

        private IEnumerator Interact(Func<bool> until, int frames = 120)
        {
            director.RequestInteract();
            for (int i = 0; i < frames && !until(); i++) yield return null;
        }

        private IEnumerator Embark()
        {
            if (director.CardOpen) director.menus.Activate("continue");
            yield return null;
            Ok(director.Session.Embark(Guid.NewGuid(), director.PlanEmbark()));
            yield return Frames(3);
            Assert.That(director.Session.Lifecycle, Is.EqualTo(Lifecycle.AtSea));
        }

        [UnityTest]
        public IEnumerator FreshCampaignDocksAtHomeWithIntroCard()
        {
            yield return Load(LaunchMode.NewCampaign);
            Assert.That(director.Session.Lifecycle, Is.EqualTo(Lifecycle.Docked));
            Assert.That(director.CardOpen, Is.True);
            Assert.That(director.CardTitle, Is.EqualTo("Homeward Harbor"));
            Assert.That(File.Exists(Path.Combine(saves, JsonSaveStore.MainName)), Is.True, "New campaign is saved immediately");
            var dock = director.Definitions.Hubs["home-harbor"].Dock;
            Assert.That(Vector3.Distance(director.player.motor.Body.position, new Vector3((float)dock.X, 0, (float)dock.Z)), Is.LessThan(0.01f));
            yield return null;
            Assert.That(director.harbor.Root.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(director.hud.Visible, Is.False);
        }

        [UnityTest]
        public IEnumerator FullLoopSalvagesSinksAWreckDocksUpgradesAndSailsStronger()
        {
            yield return Load(LaunchMode.NewCampaign);
            yield return Embark();
            var session = director.Session;
            Assert.That(director.Enemies.Count, Is.EqualTo(3), "Two encounter markers: one ship and an escorted pair");
            Assert.That(director.salvage.Sources.Length, Is.EqualTo(5));
            Assert.That(director.hud.Visible, Is.True);

            yield return Sail(new Vector3(0, 0, 8));
            yield return Interact(() => Values.Amount(session.Snapshot.Expedition.Cargo, "wood") == 3);
            Assert.That(session.Snapshot.Expedition.Cargo["wood"], Is.EqualTo(3), "Barrel salvaged");
            Assert.That(director.salvage.Sources.Single(s => s.Id.Equals(EntityId.Authored("first:barrel-01"))).Depleted, Is.True);

            var enemy = director.Enemies[0];
            var spec = director.Combat.Enemies[enemy.Definition.Id];
            enemy.Target.ApplyDamage(10000);
            for (int i = 0; i < 60 && director.salvage.Sources.Length < 6; i++) yield return null;
            Assert.That(director.salvage.Sources.Length, Is.EqualTo(6), "Sunk ship leaves a salvageable wreck");
            var wreck = director.salvage.Sources.Last();
            Assert.That(wreck.Id.AuthoredId, Is.Null);
            Assert.That(session.Snapshot.Expedition.Entities[enemy.Id].Defeated, Is.True, "Defeat is in the saved ledger");

            yield return Sail(wreck.transform.position + new Vector3(0, 0, -3));
            int woodBefore = session.Snapshot.Expedition.Cargo["wood"];
            yield return Interact(() => Values.Amount(session.Snapshot.Expedition.Cargo, "wood") > woodBefore);
            Assert.That(session.Snapshot.Expedition.Cargo["wood"], Is.EqualTo(woodBefore + spec.WreckLoot["wood"]));
            var cargo = session.Snapshot.Expedition.Cargo.ToDictionary(p => p.Key, p => p.Value);

            var dock = director.Definitions.Hubs["home-harbor"].Dock;
            yield return Sail(new Vector3((float)dock.X, 0, (float)dock.Z + 1));
            yield return Interact(() => session.Lifecycle == Lifecycle.Docked, 400);
            Assert.That(session.Lifecycle, Is.EqualTo(Lifecycle.Docked), "Docked through the assisted docking flow");
            yield return Frames(2);
            foreach (var pair in cargo) Assert.That(Values.Amount(session.Snapshot.Campaign.Bank, pair.Key), Is.EqualTo(pair.Value), "Banked " + pair.Key);
            Assert.That(director.CardTitle, Is.EqualTo("Safe harbor"));
            director.menus.Activate("continue");

            double cargoBefore = session.ShipStats()["cargo"];
            Ok(session.PurchaseUpgrade(Guid.NewGuid(), "harbor-storehouse"));
            Assert.That(session.ShipStats()["cargo"], Is.EqualTo(cargoBefore + 5));
            var disk = new JsonSaveStore(saves, director.Definitions).Load().Snapshot;
            Assert.That(disk.Campaign.Tiers["harbor"], Is.EqualTo(1), "Purchase is durable");

            yield return Embark();
            Assert.That(director.Enemies.Count, Is.EqualTo(3), "Fresh voyage, fresh encounters");
            Assert.That(director.salvage.Sources.All(s => !s.Depleted), Is.True, "New voyages restock authored salvage");
            yield return Frames(2);
            Assert.That(director.hud.Root.Q<Label>("cargo-value").text, Is.EqualTo("0 / 15"), "Upgrade visible on the next voyage");
        }

        [UnityTest]
        public IEnumerator SinkingLosesCargoKeepsProgressAndReturnsHome()
        {
            yield return Load(LaunchMode.NewCampaign);
            yield return Embark();
            var session = director.Session;
            var voyage = session.Snapshot.Expedition.Id;
            yield return Sail(new Vector3(0, 0, 8));
            yield return Interact(() => Values.Amount(session.Snapshot.Expedition.Cargo, "wood") == 3);
            bool hit = false;
            Action<InputIntent> lethal = _ => { if (!hit) { hit = true; director.player.AddDamage(100000); } };
            director.player.TickStarted += lethal;
            for (int i = 0; i < 120 && session.Lifecycle != Lifecycle.Docked; i++) yield return null;
            director.player.TickStarted -= lethal;
            Assert.That(session.Snapshot.Campaign.ResolvedExpeditions[voyage], Is.EqualTo(Outcome.Sunk));
            Assert.That(Values.Amount(session.Snapshot.Campaign.Bank, "wood"), Is.Zero, "At-risk cargo is lost");
            Assert.That(director.CardTitle, Is.EqualTo("Your ship went down"));
            var dock = director.Definitions.Hubs["home-harbor"].Dock;
            Assert.That(Vector3.Distance(director.player.motor.Body.position, new Vector3((float)dock.X, 0, (float)dock.Z)), Is.LessThan(0.01f));
            director.menus.Activate("continue");
            yield return Embark();
            Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(session.ShipStats()["health"]), "Free departure at full health");
        }

        [UnityTest]
        public IEnumerator ContinueRestoresVoyageExactlyAtTheLastCheckpoint()
        {
            yield return Load(LaunchMode.NewCampaign);
            yield return Embark();
            var session = director.Session;
            yield return Sail(new Vector3(0, 0, 8));
            yield return Interact(() => Values.Amount(session.Snapshot.Expedition.Cargo, "wood") == 3);
            var enemy = director.Enemies[1];
            enemy.Target.ApplyDamage(20);
            bool hit = false;
            Action<InputIntent> scratch = _ => { if (!hit) { hit = true; director.player.AddDamage(12.5); } };
            director.player.TickStarted += scratch;
            director.player.Submit(new InputIntent(0, 0, 0, 1, true, false, false), false);
            yield return Ticks(3);
            director.player.TickStarted -= scratch;
            director.player.Submit(default, false);
            while (director.player.HasPendingStep) yield return new WaitForFixedUpdate();
            Ok(director.Checkpoint());
            director.player.SetPaused(true);
            var saved = session.Snapshot.Expedition;
            double weapon = director.CombatWorld.WeaponCooldown;
            Assert.That(weapon, Is.GreaterThan(0), "A shot was fired before the checkpoint");
            Assert.That(saved.Health, Is.EqualTo(session.ShipStats()["health"] - 12.5));

            director = null;
            yield return Load(LaunchMode.Continue);
            // The resumed world keeps simulating; freeze it, then account for any ticks it ran.
            director.player.SetPaused(true);
            var restored = director.Session.LastCommitted.Expedition;
            long elapsed = director.Session.Tick - saved.Tick;
            Assert.That(restored.Id, Is.EqualTo(saved.Id));
            Assert.That(restored.Tick, Is.EqualTo(saved.Tick), "Loaded exactly the checkpoint");
            Assert.That(elapsed, Is.InRange(0, 10));
            Assert.That(restored.Health, Is.EqualTo(saved.Health), "Health is not refilled");
            Assert.That(director.Session.Snapshot.Expedition.Health, Is.EqualTo(saved.Health));
            Assert.That(restored.Cargo["wood"], Is.EqualTo(3));
            Assert.That(director.salvage.Sources.Single(s => s.Id.Equals(EntityId.Authored("first:barrel-01"))).Depleted, Is.True, "Loot is not restocked");
            var again = director.Enemies.Single(e => e.Id.Equals(enemy.Id));
            Assert.That(again.Target.Health, Is.EqualTo(enemy.Definition.Health - 20), "Enemy damage persists");
            Assert.That(director.CombatWorld.WeaponCooldown, Is.EqualTo(Math.Max(0, weapon - elapsed * director.Session.FixedDeltaSeconds)).Within(1e-6),
                "Cooldown resumes where it was saved, not reset");
            var position = director.player.motor.Body.position;
            Assert.That(position.x, Is.EqualTo((float)saved.Position.X).Within(0.01f));
            Assert.That(position.z, Is.EqualTo((float)saved.Position.Z).Within(0.01f));
        }
    }
}
#endif
