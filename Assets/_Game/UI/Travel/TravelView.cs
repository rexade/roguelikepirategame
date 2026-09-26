using System;
using System.Linq;
using PirateGame.Core;
using PirateGame.Rules.Application;
using UnityEngine.UIElements;

namespace PirateGame.UI.Travel
{
    // Beacon paths (fast travel) in the beacon's ledger. It only issues FastTravel
    // commands; eligibility (docked, unlock, lit destination) stays in the rules.
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
            var heading = new Label("Beacon Paths"); heading.AddToClassList("heading"); Add(heading);
            var campaign = snapshot.Campaign;
            bool unlocked = campaign.Unlocks.Contains(Unlock);
            if (!unlocked)
            {
                var hint = new Label("Learn the Beacon Paths to sail straight to any beacon you have relit.") { name = "travel-locked" };
                hint.AddToClassList("muted"); Add(hint);
            }
            foreach (var hub in definitions.Hubs.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                if (hub == campaign.CurrentHub) continue;
                if (!campaign.Hubs[hub].Activated)
                {
                    var unknown = new Label("A dark beacon: relight it to open its path.") { name = "travel-unknown-" + hub };
                    unknown.AddToClassList("muted"); Add(unknown);
                    continue;
                }
                var target = hub;
                var button = new Button(() => completed(session.FastTravel(Guid.NewGuid(), target)))
                    { text = "Sail to " + hubName(hub), name = "travel-" + hub };
                button.SetEnabled(unlocked);
                Add(button);
            }
        }
    }
}
