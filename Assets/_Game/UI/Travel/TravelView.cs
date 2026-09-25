using System;
using System.Linq;
using PirateGame.Core;
using PirateGame.Rules.Application;
using UnityEngine.UIElements;

namespace PirateGame.UI.Travel
{
    // Fast-travel list for the harbor screen. It only issues FastTravel commands;
    // eligibility (docked, unlock, activated destination) stays in the rules.
    public sealed class TravelView : VisualElement
    {
        public const string Unlock = "fast-travel";
        private readonly CampaignSession session;
        private readonly DefinitionCatalog definitions;
        private readonly Func<string, string> hubName;
        private readonly Action<RuleResult> completed;
        private long shownRevision = -1;

        public TravelView(CampaignSession session, DefinitionCatalog definitions, Func<string, string> hubName, Action<RuleResult> completed)
        {
            this.session = session; this.definitions = definitions; this.hubName = hubName; this.completed = completed;
            name = "travel"; AddToClassList("travel");
            Refresh();
        }

        public void Refresh()
        {
            var snapshot = session.Snapshot;
            if (snapshot.Revision == shownRevision && childCount > 0) return;
            shownRevision = snapshot.Revision;
            Clear();
            var heading = new Label("Fast travel"); heading.AddToClassList("heading"); Add(heading);
            var campaign = snapshot.Campaign;
            bool unlocked = campaign.Unlocks.Contains(Unlock);
            if (!unlocked)
            {
                var hint = new Label("Buy Navigator's Charts to sail straight to any harbor that flies your flag.") { name = "travel-locked" };
                hint.AddToClassList("muted"); Add(hint);
            }
            foreach (var hub in definitions.Hubs.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                if (hub == campaign.CurrentHub) continue;
                if (!campaign.Hubs[hub].Activated)
                {
                    var unknown = new Label("Uncharted harbor: raise your flag in its berth to add it.") { name = "travel-unknown-" + hub };
                    unknown.AddToClassList("muted"); Add(unknown);
                    continue;
                }
                var target = hub;
                var button = new Button(() => completed(session.FastTravel(Guid.NewGuid(), target)))
                    { text = "Travel to " + hubName(hub), name = "travel-" + hub };
                button.SetEnabled(unlocked);
                Add(button);
            }
        }
    }
}
