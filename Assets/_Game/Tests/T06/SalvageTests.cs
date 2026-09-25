#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PirateGame.Content.World;
using PirateGame.Core;
using PirateGame.Gameplay.World;
using PirateGame.Rules.Application;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Tests.T06
{
    public sealed class SalvageTests
    {
        private FirstRegionAsset content;
        [SetUp] public void Setup() => content = AssetDatabase.LoadAssetAtPath<FirstRegionAsset>("Assets/_Game/Content/World/FirstRegion/FirstRegion.asset");
        [UnityTest] public IEnumerator StartupHudAndFreshReloadAreNeutral()
        {
            for (int reload = 0; reload < 2; reload++)
            {
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/Scenes/Tests/T06/Salvage.unity", new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                var fixture = Object.FindFirstObjectByType<SalvageFixture>();
                fixture.simulation.SetPaused(true);
                while (fixture.simulation.HasPendingStep) yield return new WaitForFixedUpdate();
                Assert.IsNull(fixture.interaction.LastResult);
                var before = fixture.simulation.Session.Snapshot;
                Assert.AreEqual("  Homeward Reach\n  Wood 0   Iron 0   ", fixture.HudText());
                Assert.AreSame(before, fixture.simulation.Session.Snapshot);
                Assert.IsTrue(fixture.region.Sources.All(s => !s.Depleted));
                Assert.AreEqual(1, fixture.simulation.Session.DrainEvents().Count, "HUD must retain the embark event");
                var empty = SceneManager.CreateScene("T06 HUD clean " + reload); SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(fixture.gameObject.scene);
            }
        }
        [Test] public void FullCargoRejectsWholeBundle()
        {
            var session = SalvageFixture.CreateSession(content, capacity: 2);
            var before = session.Snapshot;
            var result = session.CollectLoot(Guid.NewGuid(), before.Expedition.Id, EntityId.Authored(content.salvage[0].id));
            Assert.AreEqual(RuleError.CargoFull, result.Error); Assert.AreSame(before, session.Snapshot);
            Assert.AreEqual(3, Values.Amount(session.Snapshot.Expedition.Entities[EntityId.Authored(content.salvage[0].id)].Loot, "wood"));
        }
        [UnityTest] public IEnumerator HudReportsNoSourceAndFullCargoWithoutMutation()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/Scenes/Tests/T06/Salvage.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var fixture = Object.FindFirstObjectByType<SalvageFixture>();
            var simulation = fixture.simulation;
            simulation.SetPaused(true);
            while (simulation.HasPendingStep) yield return new WaitForFixedUpdate();
            simulation.SetPaused(false);
            Assert.AreEqual(RuleError.UnknownId, fixture.interaction.TryCollect(Guid.NewGuid()).Error);
            StringAssert.Contains("Wood 0   Iron 0   No salvage in reach.", fixture.HudText());
            foreach (float z in new[] { 8f, 38f })
            {
                simulation.motor.Body.position = new Vector3(0, 0, z);
                simulation.motor.transform.position = simulation.motor.Body.position;
                Physics.SyncTransforms();
                var result = fixture.interaction.TryCollect(Guid.NewGuid());
                Assert.IsTrue(result.IsSuccess, result.Error + ": " + result.Detail);
            }
            simulation.motor.Body.position = new Vector3(0, 0, 82);
            simulation.motor.transform.position = simulation.motor.Body.position;
            Physics.SyncTransforms();
            Assert.AreEqual(RuleError.CargoFull, fixture.interaction.TryCollect(Guid.NewGuid()).Error);
            var before = simulation.Session.Snapshot;
            Assert.AreEqual("  Homeward Reach\n  Wood 5   Iron 0   Cargo full", fixture.HudText());
            Assert.AreSame(before, simulation.Session.Snapshot);
            fixture.Recreate();
            Assert.IsTrue(fixture.region.Sources[0].Depleted);
            Assert.IsTrue(fixture.region.Sources[1].Depleted);
            var wreck = fixture.region.Sources.Single(s => s.Id.AuthoredId == "first:wreck-01").Capture();
            Assert.AreEqual(2, Values.Amount(wreck.Loot, "wood"));
            Assert.AreEqual(2, Values.Amount(wreck.Loot, "iron"));
            Assert.AreEqual(3, simulation.Session.DrainEvents().Count, "Embark and two pickups remain available");
            simulation.SetPaused(true);
            var empty = SceneManager.CreateScene("T06 status clean"); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(fixture.gameObject.scene);
        }
        [Test] public void DuplicateCallbacksCreditOnce()
        {
            var session = SalvageFixture.CreateSession(content); var id = EntityId.Authored(content.salvage[0].id); var request = Guid.NewGuid();
            Assert.IsTrue(session.CollectLoot(request, session.Snapshot.Expedition.Id, id).IsSuccess);
            Assert.AreEqual(RuleError.DuplicateRequest, session.CollectLoot(request, session.Snapshot.Expedition.Id, id).Error);
            Assert.AreEqual(RuleError.Depleted, session.CollectLoot(Guid.NewGuid(), session.Snapshot.Expedition.Id, id).Error);
            Assert.AreEqual(3, Values.Amount(session.Snapshot.Expedition.Cargo, "wood"));
        }
        [Test] public void FailedSaveKeepsSourceAndCargoUntilRetry()
        {
            var store = new SalvageFixture.MemoryStore(); var session = SalvageFixture.CreateSession(content, store);
            session.DrainEvents(); store.Fail = true;
            var before = session.Snapshot; var id = EntityId.Authored(content.salvage[0].id);
            Assert.AreEqual(RuleError.SaveFailed, session.CollectLoot(Guid.NewGuid(), before.Expedition.Id, id).Error);
            Assert.AreSame(before, session.Snapshot); Assert.IsEmpty(session.DrainEvents()); Assert.IsTrue(session.InputLocked);
            var candidate = session.PendingSave;
            store.Fail = false; Assert.IsTrue(session.RetrySave().IsSuccess);
            Assert.AreSame(candidate.Snapshot, session.Snapshot); Assert.IsEmpty(session.Snapshot.Expedition.Entities[id].Loot);
            Assert.AreEqual(1, session.DrainEvents().Count);
        }
        [Test] public void DuplicateIdsReportBothRegions()
        {
            var error = Assert.Throws<ArgumentException>(() => FirstRegionAsset.ValidateIdentities(new[] {
                new AuthoredIdentity("duplicate", "region-a/salvage[0]"), new AuthoredIdentity("duplicate", "region-b/salvage[2]") }));
            StringAssert.Contains("region-a/salvage[0]", error.Message); StringAssert.Contains("region-b/salvage[2]", error.Message);
        }
        [Test] public void AssetRenameDoesNotChangeIdentityOrGeography()
        {
            var clone = Object.Instantiate(content); var original = clone.salvage.Select(s => s.Initial(clone.regionId)).ToArray();
            clone.name = "Renamed region";
            Assert.AreEqual(content.Identities("old").Select(x => x.Id), clone.Identities("new").Select(x => x.Id));
            Assert.AreEqual(original.Select(x => x.Position.X), clone.salvage.Select(s => s.position.x).Select(x => (double)x));
            Object.DestroyImmediate(clone);
        }
        [Test] public void InvalidReferencesFailBeforePlay()
        {
            var clone = Object.Instantiate(content); clone.salvage[0].definitionId = "missing";
            Assert.Throws<ArgumentException>(() => clone.Validate(SalvageFixture.Catalog(clone))); Object.DestroyImmediate(clone);
        }
        [UnityTest] public IEnumerator RecreateAndReloadKeepDepletedWreck()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/World/FirstRegion.prefab"));
            var region = root.GetComponent<SalvageRegion>(); var session = SalvageFixture.CreateSession(content);
            Assert.IsTrue(region.Recreate(session).IsSuccess);
            var wreck = region.Sources.Single(s => s.Id.AuthoredId == "first:wreck-01");
            Assert.IsTrue(session.CollectLoot(Guid.NewGuid(), session.Snapshot.Expedition.Id, wreck.Id).IsSuccess);
            Assert.IsEmpty(wreck.Capture().Loot);
            var snapshot = session.Snapshot;
            Object.Destroy(root); yield return null;
            var restored = new CampaignSession(SalvageFixture.Catalog(content), snapshot, new SalvageFixture.MemoryStore(), new SalvageFixture.ReadyArrival());
            restored.RetryArrival();
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/World/FirstRegion.prefab"));
            region = root.GetComponent<SalvageRegion>(); Assert.IsTrue(region.Recreate(restored).IsSuccess);
            wreck = region.Sources.Single(s => s.Id.AuthoredId == "first:wreck-01");
            Assert.IsTrue(wreck.Depleted); Assert.IsTrue(wreck.Restore(wreck.Capture()).IsSuccess);
            Assert.AreEqual(RuleError.InvalidRequest, wreck.Restore(content.salvage[2].Initial(content.regionId)).Error);
            Assert.AreEqual(2, Values.Amount(restored.Snapshot.Expedition.Cargo, "wood"));
            Assert.AreEqual(2, Values.Amount(restored.Snapshot.Expedition.Cargo, "iron"));
            Assert.IsTrue(wreck.GetComponentsInChildren<Renderer>().All(r => !r.enabled));
            Object.Destroy(root); yield return null;
        }
        [UnityTest] public IEnumerator MissingEntityDoesNotSeedFreshLoot()
        {
            var session = SalvageFixture.CreateSession(content);
            var other = Object.Instantiate(content); other.salvage[0].id = "unknown";
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/World/FirstRegion.prefab"));
            var region = root.GetComponent<SalvageRegion>(); region.content = other;
            Assert.AreEqual(RuleError.InvalidRequest, region.Recreate(session).Error); Assert.IsEmpty(region.Sources);
            Object.Destroy(root); Object.Destroy(other); yield return null;
        }
        [UnityTest] public IEnumerator AuthoredRouteHasHullClearanceAndIslandCollision()
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/World/FirstRegion.prefab"));
            Physics.SyncTransforms();
            for (int i = 1; i < content.route.Length; i++)
            {
                var a = new Vector3(content.route[i-1].x, 0, content.route[i-1].y); var b = new Vector3(content.route[i].x, 0, content.route[i].y);
                Assert.IsFalse(Physics.CheckSphere(a, 3, ~0, QueryTriggerInteraction.Ignore), "Waypoint blocked: " + i);
                Assert.IsFalse(Physics.SphereCast(a, 3, (b-a).normalized, out _, Vector3.Distance(a,b), ~0, QueryTriggerInteraction.Ignore), "Route segment blocked: " + i);
            }
            foreach (var island in content.islands)
                Assert.IsTrue(Physics.Raycast(new Vector3(island.position.x, 20, island.position.y), Vector3.down, 25, ~0, QueryTriggerInteraction.Ignore), island.id);
            Object.Destroy(root); yield return null;
        }
        [UnityTest] public IEnumerator InteractionQueuesThenCommitsAfterDamageAndPause()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/Scenes/Tests/T06/Salvage.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return scene; yield return null;
            var fixture = Object.FindFirstObjectByType<SalvageFixture>(); var simulation = fixture.simulation;
            yield return new WaitForFixedUpdate();
            simulation.motor.Body.position = new Vector3(0, 0, 8);
            bool queued = false;
            Action<InputIntent> collect = _ => {
                if (queued) return;
                queued = true; simulation.AddDamage(7);
                Assert.IsTrue(fixture.interaction.TryCollect(Guid.NewGuid()).IsPending);
                StringAssert.Contains("Wood 0   Iron 0   Collecting", fixture.HudText());
                Assert.AreEqual(0, Values.Amount(simulation.Session.Snapshot.Expedition.Cargo, "wood"));
                simulation.SetPaused(true);
            };
            simulation.TickStarted += collect;
            for (int i = 0; i < 5 && !simulation.Session.IsPaused; i++) yield return new WaitForFixedUpdate();
            simulation.TickStarted -= collect;
            Assert.IsTrue(queued); Assert.IsFalse(simulation.HasPendingStep);
            Assert.AreEqual(93, simulation.Session.Snapshot.Expedition.Health);
            Assert.AreEqual(3, Values.Amount(simulation.Session.Snapshot.Expedition.Cargo, "wood"));
            yield return null;
            Assert.AreEqual("  Homeward Reach\n  Wood 3   Iron 0   ", fixture.HudText());
            fixture.Recreate(); Assert.IsTrue(fixture.region.Sources[0].Depleted);
            Assert.IsTrue(simulation.Session.IsPaused);
            var empty = SceneManager.CreateScene("T06 clean"); SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(fixture.gameObject.scene);
        }
    }
}
#endif
