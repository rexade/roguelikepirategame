using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PirateGame.Composition;
using PirateGame.Content.Combat;
using PirateGame.Content.World;
using PirateGame.Core;
using UnityEditor;
using UnityEngine;

namespace PirateGame.Tests.T10
{
    public sealed class VoyageVariationTests
    {
        private FirstRegionAsset homeward, galewater;
        private CombatCatalog combat;
        private ExpeditionPlanner planner;

        [SetUp]
        public void Setup()
        {
            homeward = AssetDatabase.LoadAssetAtPath<FirstRegionAsset>("Assets/_Game/Content/World/FirstRegion/FirstRegion.asset");
            galewater = AssetDatabase.LoadAssetAtPath<FirstRegionAsset>("Assets/_Game/Content/World/Galewater/GalewaterReach.asset");
            combat = AssetDatabase.LoadAssetAtPath<CombatCatalogAsset>("Assets/_Game/Content/Production/GameCombat.asset").Freeze();
            planner = new ExpeditionPlanner(new[] { homeward, galewater }, combat);
        }

        private static Dictionary<string, string> Spawns(PirateGame.Rules.Application.EmbarkPlan plan) =>
            plan.Encounters.ToDictionary(p => p.Key, p => p.Value);

        [Test]
        public void SameSeedSamePlanDifferentSeedsVaryEncounters()
        {
            var a = planner.Plan(Guid.NewGuid(), 42);
            var b = planner.Plan(Guid.NewGuid(), 42);
            Assert.That(Spawns(a), Is.EquivalentTo(Spawns(b)), "Seeded selection is reproducible");
            Assert.That(a.RngState, Is.EqualTo(b.RngState));
            var variants = Enumerable.Range(1, 12).Select(seed => string.Join(";", Spawns(planner.Plan(Guid.NewGuid(), seed))
                .OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + "=" + p.Value))).Distinct().Count();
            Assert.That(variants, Is.GreaterThan(3), "New expeditions vary their encounters");
        }

        [Test]
        public void GeographyAndSalvageNeverMoveBetweenVoyages()
        {
            string Layout(PirateGame.Rules.Application.EmbarkPlan plan) => string.Join(";", plan.Entities
                .Where(e => e.Id.AuthoredId != null).OrderBy(e => e.Id.AuthoredId, StringComparer.Ordinal)
                .Select(e => e.Id.AuthoredId + "@" + e.Position.X + "," + e.Position.Z + ":" + string.Join(",", e.Loot.Select(l => l.Key + l.Value))));
            var first = Layout(planner.Plan(Guid.NewGuid(), 1));
            foreach (int seed in new[] { 2, 99, 12345, -7 }) Assert.That(Layout(planner.Plan(Guid.NewGuid(), seed)), Is.EqualTo(first));
            Assert.That(first.Split(';').Length, Is.EqualTo(homeward.salvage.Length + galewater.salvage.Length));
        }

        [Test]
        public void EncounterTablesAndCountsAreRespectedPerRegion()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                var plan = planner.Plan(Guid.NewGuid(), seed);
                var ships = plan.Entities.Where(e => combat.Enemies.ContainsKey(e.DefinitionId)).ToList();
                Assert.That(ships.Where(e => e.Position.RegionId == homeward.regionId).All(e => e.DefinitionId != "corsair"),
                    "Corsairs only sail Galewater");
                foreach (var site in galewater.encounters)
                {
                    int count = ships.Count(e => e.Id.SpawnId.StartsWith(site.id + "#", StringComparison.Ordinal));
                    int minimum = site.minShips > 0 ? site.minShips : site.ships;
                    Assert.That(count, Is.InRange(minimum, site.ships), site.id);
                }
                Assert.That(ships.Count(e => e.Position.RegionId == homeward.regionId), Is.EqualTo(3), "Homeward sizes are fixed");
                Assert.That(ships.All(e => plan.Encounters[e.Id.SpawnId] == e.DefinitionId), "Chosen encounters are recorded");
            }
        }

        [Test]
        public void AC15_DuplicateIdAcrossRegionsNamesBothOrigins()
        {
            var clash = ScriptableObject.CreateInstance<FirstRegionAsset>();
            try
            {
                clash.regionId = "clash"; clash.homeId = "clash-harbor";
                clash.islands = new[] { new IslandSite { id = homeward.islands[0].id, position = Vector2.zero, size = Vector2.one } };
                var error = Assert.Throws<ArgumentException>(() => FirstRegionAsset.ValidateIdentities(
                    homeward.Identities("homeward.asset").Concat(galewater.Identities("galewater.asset")).Concat(clash.Identities("clash.asset"))));
                StringAssert.Contains(homeward.islands[0].id, error.Message);
                StringAssert.Contains("homeward.asset", error.Message);
                StringAssert.Contains("clash.asset", error.Message);
                Assert.DoesNotThrow(() => FirstRegionAsset.ValidateIdentities(homeward.Identities("a").Concat(galewater.Identities("b"))));
            }
            finally { UnityEngine.Object.DestroyImmediate(clash); }
        }

        [Test]
        public void RegionsTileWithoutOverlapAndOwnTheirContent()
        {
            Assert.That(homeward.bounds.Overlaps(galewater.bounds), Is.False);
            Assert.That(homeward.Contains(new Vector2(0, 164.9f)) && galewater.Contains(new Vector2(0, 165f)), Is.True, "Shared edge belongs to exactly one region");
            Assert.That(homeward.Contains(new Vector2(0, 165f)), Is.False);
            Assert.That(galewater.salvage.All(s => galewater.Contains(s.position)) && homeward.salvage.All(s => homeward.Contains(s.position)));
        }
    }
}
