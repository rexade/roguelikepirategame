using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Core;

namespace PirateGame.Persistence
{
    // Load-time upgrades for saves written by earlier content versions. They run
    // before content validation and never rewrite the file themselves: the next
    // normal commit writes the upgraded snapshot.
    public static class SaveMigration
    {
        // Harbors added to the content after the save was written start
        // undiscovered. Harbors the save names but the content no longer has are
        // left in place, so validation still reports them (INV-12).
        public static SessionSnapshot AddNewHubs(DefinitionCatalog definitions, SessionSnapshot snapshot)
        {
            var c = snapshot.Campaign;
            var added = definitions.Hubs.Keys.Where(id => !c.Hubs.ContainsKey(id))
                .Select(id => new KeyValuePair<string, HubState>(id, new HubState(false, false, Array.Empty<string>()))).ToArray();
            if (added.Length == 0) return snapshot;
            var campaign = new CampaignState(c.HullId, c.CurrentHub, c.LastSafeHub, c.Bank, c.Tiers, c.OwnedEquipment, c.Loadout,
                c.Hubs.Concat(added), c.Unlocks, c.ResolvedExpeditions);
            return new SessionSnapshot(snapshot.Revision, campaign, snapshot.Expedition, snapshot.CommittedRequests);
        }
    }
}
