using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Rules.Application;
using PirateGame.Core;

namespace PirateGame.Tests.T03
{
    public sealed class FakeSaveStore : ISaveStore
    {
        public SessionSnapshot Committed;
        public bool Fail;
        public bool Throw;
        public int Attempts;
        public Action<SaveCandidate> DuringCommit;
        public readonly List<SaveCandidate> Candidates = new List<SaveCandidate>();
        public RuleResult Commit(SaveCandidate candidate)
        {
            Attempts++; Candidates.Add(candidate); DuringCommit?.Invoke(candidate);
            if (Throw) throw new InvalidOperationException("Injected storage exception");
            if (Fail) return new RuleResult(RuleError.SaveFailed);
            if (Committed.Revision == candidate.Snapshot.Revision && Committed.CommittedRequests.Contains(candidate.RequestId)) return new RuleResult();
            if (Committed.Revision != candidate.ExpectedRevision) return new RuleResult(RuleError.SaveFailed, detail: "Stale candidate");
            Committed = candidate.Snapshot;
            return new RuleResult();
        }
    }

    public sealed class FakeArrival : IWorldArrival
    {
        public bool Fail;
        public int Calls;
        public ArrivalRequest Last;
        public RuleResult EnsureReady(ArrivalRequest request)
        {
            Calls++; Last = request;
            return new RuleResult(Fail ? RuleError.ArrivalFailed : RuleError.None);
        }
    }

    public static class RuleFixtures
    {
        public static readonly SeaPosition Home = new SeaPosition("region-a", 0, 0);
        public static Dictionary<string, int> Bundle(int wood = 0) => new Dictionary<string, int> { ["wood"] = wood };
        public static DefinitionCatalog Catalog(double cargo = 10, IEnumerable<AuthoredIdentity> identities = null) => new DefinitionCatalog(
            new Dictionary<string, int> { ["wood"] = 1, ["iron"] = 2 },
            new[] { new HullDefinition("starter", new Dictionary<string, SlotKind> { ["gun"] = SlotKind.Weapon, ["ability"] = SlotKind.Ability },
                new[] { new StatDefinition("health", 100, 1, 1000), new StatDefinition("cargo", cargo, 0, int.MaxValue), new StatDefinition("speed", 8, 1, 50) }) },
            new[] { new EquipmentDefinition("cannon", SlotKind.Weapon, Array.Empty<StatModifier>()),
                new EquipmentDefinition("heavy-cannon", SlotKind.Weapon, new[] { new StatModifier("health", ModifierOperation.Flat, 20) }),
                new EquipmentDefinition("dash", SlotKind.Ability, Array.Empty<StatModifier>()) },
            new[] { new HubDefinition("home", Home, 5, 1), new HubDefinition("other", new SeaPosition("region-b", 50, 0), 3, 0.5) },
            new[] { new UpgradeDefinition("hull-1", "hull", 1, Bundle(6), Array.Empty<string>(), new[] { "fast-travel" },
                new[] { new StatModifier("health", ModifierOperation.Percent, 0.5) }),
                new UpgradeDefinition("hull-2", "hull", 2, Bundle(8), new[] { "fast-travel" }, Array.Empty<string>(), Array.Empty<StatModifier>()) },
            new[] { "fast-travel" }, identities ?? Array.Empty<AuthoredIdentity>(), new[] { "barrel", "enemy" }, new[] { "region-a", "region-b" });

        public static SessionSnapshot Initial(DefinitionCatalog definitions, int bank = 0, bool otherActive = false, bool travel = false)
        {
            var initial = CampaignSession.NewCampaign(definitions, "home", "starter",
                new Dictionary<string, string> { ["weapon-1"] = "cannon", ["weapon-2"] = "heavy-cannon", ["ability-1"] = "dash" },
                new Dictionary<string, string> { ["gun"] = "weapon-1", ["ability"] = "ability-1" });
            var c = initial.Campaign;
            var hubs = Values.Copy(c.Hubs); hubs["other"] = new HubState(otherActive, otherActive, new[] { "local-story" });
            return new SessionSnapshot(0, new CampaignState(c.HullId, c.CurrentHub, c.LastSafeHub, Bundle(bank), c.Tiers,
                c.OwnedEquipment, c.Loadout, hubs, travel ? new[] { "fast-travel" } : Array.Empty<string>(), c.ResolvedExpeditions), null, Array.Empty<Guid>());
        }

        public static EntityState Loot(string id, int wood) => new EntityState(EntityId.Authored(id), "barrel", Home, 1, false,
            Bundle(wood), new Dictionary<string, double>(), "idle");
        public static EmbarkPlan Plan(params EntityState[] entities) => new EmbarkPlan(Guid.NewGuid(), 42, "rng:42:0", entities,
            new Dictionary<string, string> { ["spawn-a"] = "enemy" });
    }
}
