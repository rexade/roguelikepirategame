using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Content.Definitions;
using PirateGame.Core;
using PirateGame.Rules.Application;
using PirateGame.UI.Harbor;
using UnityEngine;

namespace PirateGame.Tests.T07
{
    public sealed class HarborFixture : MonoBehaviour
    {
        public DefinitionCatalogAsset catalog;
        public HarborView view;
        public CampaignSession Session { get; private set; }
        public void Start()
        {
            var definitions = catalog.Freeze();
            var initial = Initial(definitions);
            Session = new CampaignSession(definitions, initial, new MemoryStore(initial), new ReadyArrival());
            Session.RetryArrival(); view.Bind(Session, definitions, Plan);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-t07-capture") >= 0)
                gameObject.AddComponent<HarborCapture>().session = Session;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-t07-keyboard") >= 0)
                gameObject.AddComponent<HarborKeyboardCheck>().fixture = this;
        }

        // Explicit fixture grant, before ownership passes to the session. No UI credit command.
        public static SessionSnapshot Initial(DefinitionCatalog definitions, int wood = 11, int iron = 2)
        {
            var c = CampaignSession.NewCampaign(definitions, "home", "starter",
                new Dictionary<string, string> { ["cannon-01"] = "cannon", ["heavy-01"] = "heavy-cannon", ["dash-01"] = "dash" },
                new Dictionary<string, string> { ["weapon"] = "cannon-01", ["ability"] = "dash-01" }).Campaign;
            return new SessionSnapshot(0, new CampaignState(c.HullId, c.CurrentHub, c.LastSafeHub,
                new Dictionary<string, int> { ["wood"] = wood, ["iron"] = iron }, c.Tiers, c.OwnedEquipment,
                c.Loadout, c.Hubs, c.Unlocks, c.ResolvedExpeditions), null, Array.Empty<Guid>());
        }
        public static EmbarkPlan Plan() => new EmbarkPlan(Guid.NewGuid(), 7, "t07:7:0", Array.Empty<EntityState>(), new Dictionary<string, string>());
    }

    public sealed class ReadyArrival : IWorldArrival
    {
        public bool Fail;
        public RuleResult EnsureReady(ArrivalRequest request) => new RuleResult(Fail ? RuleError.ArrivalFailed : RuleError.None);
    }

    public sealed class MemoryStore : ISaveStore
    {
        public SessionSnapshot Committed { get; private set; }
        public bool Fail;
        public MemoryStore(SessionSnapshot initial) { Committed = initial; }
        public RuleResult Commit(SaveCandidate candidate)
        {
            if (Fail) return new RuleResult(RuleError.SaveFailed);
            if (Committed.Revision == candidate.Snapshot.Revision && Committed.CommittedRequests.Contains(candidate.RequestId)) return new RuleResult();
            if (Committed.Revision != candidate.ExpectedRevision) return new RuleResult(RuleError.SaveFailed);
            Committed = candidate.Snapshot; return new RuleResult();
        }
    }
}
