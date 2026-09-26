using System;
using System.Linq;
using PirateGame.Core;

namespace PirateGame.Persistence
{
    // A voyage saved in geography that no longer exists (for example before the
    // Drowned Sun map replaced the old archipelago) cannot be restored. Loading
    // resolves it as lost at sea instead: the campaign resumes docked at its last
    // safe hub and the voyage's cargo is gone. Bank, upgrades, equipment, unlocks
    // and hub states are kept exactly, and the revision and request history are
    // unchanged, so the next normal commit continues the same save.
    public static class SaveMigration
    {
        public static SessionSnapshot AbandonVoyage(SessionSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var voyage = snapshot.Expedition;
            if (voyage == null) return snapshot;
            var c = snapshot.Campaign;
            var resolved = c.ResolvedExpeditions.ToDictionary(p => p.Key, p => p.Value);
            if (!resolved.ContainsKey(voyage.Id)) resolved[voyage.Id] = Outcome.Sunk;
            var campaign = new CampaignState(c.HullId, c.LastSafeHub, c.LastSafeHub, c.Bank, c.Tiers, c.OwnedEquipment, c.Loadout,
                c.Hubs, c.Unlocks, resolved);
            return new SessionSnapshot(snapshot.Revision, campaign, null, snapshot.CommittedRequests);
        }
    }
}
