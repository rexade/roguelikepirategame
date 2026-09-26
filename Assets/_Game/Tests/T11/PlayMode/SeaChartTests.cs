#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PirateGame.Composition;
using PirateGame.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PirateGame.Tests.T11
{
    public sealed class SeaChartTests
    {
        private string saves;
        private GameDirector director;

        [SetUp]
        public void Setup()
        {
            saves = Path.Combine(Path.GetTempPath(), "pirate-chart-" + Guid.NewGuid().ToString("N"));
            LaunchOptions.SaveDirectoryOverride = saves;
            LaunchOptions.SeedOverride = 5;
        }

        [TearDown]
        public void Cleanup()
        {
            LaunchOptions.SaveDirectoryOverride = null; LaunchOptions.SeedOverride = null; LaunchOptions.Mode = LaunchMode.Auto;
            PirateGame.Persistence.JsonSaveStore.Flush(saves);
            if (Directory.Exists(saves)) Directory.Delete(saves, true);
        }

        [UnityTest]
        public IEnumerator ChartShowsTheArchipelagoAndPausesOnlyAtSea()
        {
            LaunchOptions.Mode = LaunchMode.NewCampaign;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Game/Scenes/OceanWorld.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 400 && (director == null || !director.Ready); i++) { director = Object.FindFirstObjectByType<GameDirector>(); yield return null; }
            Assert.That(director != null && director.Ready);
            if (director.CardOpen) director.menus.Activate("continue");

            var data = director.ChartData();
            Assert.That(data.Regions.Select(r => r.Name), Is.EquivalentTo(new[] { "Homeward Reach", "Galewater Reach" }));
            Assert.That(data.Harbors.Count, Is.EqualTo(3));
            Assert.That(data.Harbors.Single(h => h.Name == "Homeward Harbor").Claimed, Is.True);
            Assert.That(data.Harbors.Where(h => h.Name != "Homeward Harbor").All(h => !h.Claimed), Is.True, "Other harbors start uncharted");
            Assert.That(data.Harbors.Single(h => h.Claimed).Current, Is.True, "Docked here");
            Assert.That(data.Islands.Count, Is.GreaterThanOrEqualTo(10));

            director.OpenChart();
            yield return null;
            Assert.That(director.ChartOpen, Is.True);
            Assert.That(director.Session.ExplicitlyPaused, Is.False, "Docked chart needs no pause");
            director.CloseChart();

            Assert.That(director.Session.Embark(Guid.NewGuid(), director.PlanEmbark()).IsSuccess);
            for (int i = 0; i < 200 && !director.Ready; i++) yield return null;
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            yield return null;
            director.OpenChart();
            Assert.That(director.ChartOpen, Is.True);
            Assert.That(director.Session.ExplicitlyPaused, Is.True, "The voyage waits while the chart is open");
            long tick = director.Session.Tick;
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            Assert.That(director.Session.Tick, Is.EqualTo(tick));
            Assert.That(director.ChartData().ShipVisible, Is.True);
            director.CloseChart();
            Assert.That(director.Session.ExplicitlyPaused, Is.False, "Closing resumes the voyage");
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            Assert.That(director.Session.Tick, Is.GreaterThan(tick));
        }
    }
}
#endif
