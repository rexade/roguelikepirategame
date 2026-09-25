using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PirateGame.Rules.Application;
using PirateGame.Content.Definitions;
using PirateGame.Core;
using UnityEngine;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Tests.T03
{
    public sealed class DefinitionTests
    {
        [Test]
        public void AC15_DuplicateAuthoredIdentityReportsBothRegions()
        {
            var error = Assert.Throws<ArgumentException>(() => RuleFixtures.Catalog(identities: new[] {
                new AuthoredIdentity("wreck", "region-a/Island/Wreck"), new AuthoredIdentity("wreck", "region-b/Island/Wreck") }));
            Assert.That(error.Message, Does.Contain("region-a/Island/Wreck").And.Contain("region-b/Island/Wreck"));
        }

        [Test]
        public void StatsUseFlatThenAdditivePercentThenClamp()
        {
            var stat = new StatDefinition("damage", 100, 0, 200);
            var modifiers = new[] { new StatModifier("damage", ModifierOperation.Percent, 0.1),
                new StatModifier("damage", ModifierOperation.Flat, 20), new StatModifier("damage", ModifierOperation.Percent, 0.2),
                new StatModifier("unrelated", ModifierOperation.Flat, 900) };
            Assert.That(ShipRules.Calculate(stat, modifiers), Is.EqualTo(156).Within(0.00001));
            Assert.That(ShipRules.Calculate(stat, modifiers.Reverse()), Is.EqualTo(156).Within(0.00001));
            Assert.That(ShipRules.Calculate(stat, new[] { new StatModifier("damage", ModifierOperation.Flat, 500) }), Is.EqualTo(200));
            Assert.That(ShipRules.Calculate(stat, new[] { new StatModifier("damage", ModifierOperation.Percent, -2) }), Is.Zero);
        }

        [Test]
        public void InvalidNumbersOperationsAndWeightsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new StatModifier("health", (ModifierOperation)99, 1));
            Assert.Throws<ArgumentException>(() => new StatModifier("health", ModifierOperation.Flat, double.NaN));
            Assert.Throws<ArgumentException>(() => new StatDefinition("health", double.PositiveInfinity, 0, 100));
            Assert.Throws<ArgumentException>(() => Values.Bundle(new Dictionary<string, int> { ["wood"] = -1 }));
            Assert.Throws<ArgumentException>(() => new DefinitionCatalog(new Dictionary<string, int> { ["wood"] = 0 },
                Array.Empty<HullDefinition>(), Array.Empty<EquipmentDefinition>(), Array.Empty<HubDefinition>(), Array.Empty<UpgradeDefinition>(),
                Array.Empty<string>(), Array.Empty<AuthoredIdentity>()));
            Assert.Throws<ArgumentException>(() => ShipRules.Calculate(new StatDefinition("damage", 1, 0, 10),
                new[] { new StatModifier("damage", ModifierOperation.Flat, double.MaxValue), new StatModifier("damage", ModifierOperation.Flat, double.MaxValue) }));
            Assert.Throws<OverflowException>(() => ShipRules.Weight(RuleFixtures.Catalog(), new Dictionary<string, int> { ["iron"] = int.MaxValue }));
        }

        [Test]
        public void MissingReferencesAndDuplicateDefinitionsFailBeforeSessionCreation()
        {
            var d = RuleFixtures.Catalog();
            Assert.Throws<ArgumentException>(() => new DefinitionCatalog(d.ResourceWeights, d.Hulls.Values,
                d.Equipment.Values.Concat(d.Equipment.Values), d.Hubs.Values, d.Upgrades.Values, d.UnlockIds, Array.Empty<AuthoredIdentity>()));
            Assert.Throws<ArgumentException>(() => new DefinitionCatalog(d.ResourceWeights, d.Hulls.Values,
                new[] { new EquipmentDefinition("bad", SlotKind.Weapon, new[] { new StatModifier("missing", ModifierOperation.Flat, 1) }) },
                d.Hubs.Values, d.Upgrades.Values, d.UnlockIds, Array.Empty<AuthoredIdentity>()));
            Assert.Throws<ArgumentException>(() => new DefinitionCatalog(d.ResourceWeights, d.Hulls.Values, d.Equipment.Values,
                d.Hubs.Values, new[] { new UpgradeDefinition("bad", "test", 1, RuleFixtures.Bundle(), new[] { "missing" }, Array.Empty<string>(), Array.Empty<StatModifier>()) },
                d.UnlockIds, Array.Empty<AuthoredIdentity>()));
            var initial = RuleFixtures.Initial(d); var c = initial.Campaign;
            var unknown = new SessionSnapshot(0, new CampaignState("removed-hull", c.CurrentHub, c.LastSafeHub, c.Bank, c.Tiers,
                c.OwnedEquipment, c.Loadout, c.Hubs, c.Unlocks, c.ResolvedExpeditions), null, Array.Empty<Guid>());
            Assert.That(CampaignSession.ValidateSnapshot(d, unknown).Error, Is.EqualTo(RuleError.UnknownId));
        }

        [Test]
        public void AuthoredAdapterAndSnapshotsAreDetachedAndImmutable()
        {
            var asset = ScriptableObject.CreateInstance<DefinitionCatalogAsset>();
            try
            {
                asset.resources = new[] { new ResourceRow { id = "wood", weight = 1 } };
                asset.hulls = new[] { new HullRow { id = "starter", slots = new[] { new SlotRow { id = "gun", kind = SlotKind.Weapon } },
                    stats = new[] { new StatRow { id = "health", value = 100, minimum = 1, maximum = 1000 },
                        new StatRow { id = "cargo", value = 10, minimum = 0, maximum = 100 } } } };
                asset.equipment = new[] { new EquipmentRow { id = "cannon", kind = SlotKind.Weapon } };
                asset.hubs = new[] { new HubRow { id = "home", regionId = "sea" } }; asset.regionIds = new[] { "sea" };
                var definitions = asset.Freeze();
                var owned = new Dictionary<string, string> { ["item"] = "cannon" };
                var loadout = new Dictionary<string, string> { ["gun"] = "item" };
                var initial = CampaignSession.NewCampaign(definitions, "home", "starter", owned, loadout);
                owned.Clear(); loadout.Clear();
                var session = new CampaignSession(definitions, initial, new FakeSaveStore { Committed = initial }, new FakeArrival());
                session.RetryArrival();
                var embarked = session.Embark(Guid.NewGuid(), new EmbarkPlan(Guid.NewGuid(), 42, "rng:42:0", Array.Empty<EntityState>(), new Dictionary<string, string>()));
                Assert.That(embarked.IsSuccess, Is.True, embarked.Detail);
                session.CompleteTick(session.Snapshot.Expedition.Id, 1, 30, new SeaPosition("sea", 0, 0), 0);
                Assert.That(asset.hulls[0].stats[0].value, Is.EqualTo(100));
                Assert.That(definitions.Hulls["starter"].Stats["health"].Base, Is.EqualTo(100));
                asset.hulls[0].stats[0].value = 999; asset.resources[0].weight = 20;
                Assert.That(definitions.Hulls["starter"].Stats["health"].Base, Is.EqualTo(100));
                Assert.That(definitions.ResourceWeights["wood"], Is.EqualTo(1));
                Assert.That(initial.Campaign.Loadout["gun"], Is.EqualTo("item"));
                Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)initial.Campaign.Bank).Add("wood", 100));
                Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)definitions.ResourceWeights)["wood"] = 2);
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        [Test]
        public void EntityIdentityIsStableAndExpeditionScoped()
        {
            var expedition = Guid.NewGuid(); var a = EntityId.Generated(expedition, "spawn-a");
            Assert.That(a, Is.EqualTo(EntityId.Generated(expedition, "spawn-a")));
            Assert.That(a, Is.Not.EqualTo(EntityId.Generated(Guid.NewGuid(), "spawn-a")));
            Assert.That(EntityId.Authored("wreck"), Is.EqualTo(EntityId.Authored("wreck")));
            Assert.Throws<ArgumentException>(() => EntityId.Generated(Guid.Empty, "spawn-a"));
        }

        [Test]
        public void InputIntentRejectsNonfiniteAndOutOfRangeAxes()
        {
            Assert.Throws<ArgumentException>(() => new InputIntent(2, 0, 0, 1, false, false, false));
            Assert.Throws<ArgumentException>(() => new InputIntent(0, 0, double.NaN, 1, false, false, false));
            var intent = new InputIntent(-1, 1, 0, 1, true, false, true);
            Assert.That(intent.Throttle, Is.EqualTo(-1)); Assert.That(intent.Fire, Is.True);
        }

        [Test]
        public void CoreAssemblyHasNoEngineOrAdapterReferences()
        {
            var references = typeof(CampaignSession).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
            Assert.That(references.Any(n => n.StartsWith("Unity") || n.StartsWith("PirateGame.Definitions")), Is.False);
            Assert.That(typeof(CampaignSession).Assembly, Is.SameAs(typeof(CampaignState).Assembly));
        }
    }
}
