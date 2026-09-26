using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PirateGame.Core
{
    public sealed class HubState
    {
        public bool Discovered { get; }
        public bool Activated { get; }
        public IReadOnlyList<string> StoryFlags { get; }
        public HubState(bool discovered, bool activated, IEnumerable<string> flags)
        {
            if (activated && !discovered) throw new ArgumentException("An activated hub must be discovered.");
            Discovered = discovered; Activated = activated; StoryFlags = Values.List(flags.Select(Values.Id));
        }
    }

    public sealed class CampaignState
    {
        public string HullId { get; }
        public string CurrentHub { get; }
        public string LastSafeHub { get; }
        public IReadOnlyDictionary<string, int> Bank { get; }
        public IReadOnlyDictionary<string, int> Tiers { get; }
        public IReadOnlyDictionary<string, string> OwnedEquipment { get; }
        public IReadOnlyDictionary<string, string> Loadout { get; }
        public IReadOnlyDictionary<string, HubState> Hubs { get; }
        public IReadOnlyList<string> Unlocks { get; }
        public IReadOnlyDictionary<Guid, Outcome> ResolvedExpeditions { get; }
        public CampaignState(string hullId, string currentHub, string lastSafeHub,
            IEnumerable<KeyValuePair<string, int>> bank, IEnumerable<KeyValuePair<string, int>> tiers,
            IEnumerable<KeyValuePair<string, string>> ownedEquipment, IEnumerable<KeyValuePair<string, string>> loadout,
            IEnumerable<KeyValuePair<string, HubState>> hubs, IEnumerable<string> unlocks,
            IEnumerable<KeyValuePair<Guid, Outcome>> resolved)
        {
            HullId = Values.Id(hullId); CurrentHub = Values.Id(currentHub); LastSafeHub = Values.Id(lastSafeHub);
            Bank = Values.Bundle(bank); Tiers = Values.Bundle(tiers); OwnedEquipment = Values.Map(ownedEquipment);
            Loadout = Values.Map(loadout); Hubs = Values.Map(hubs); Unlocks = Values.List(unlocks.Select(Values.Id));
            ResolvedExpeditions = new ReadOnlyDictionary<Guid, Outcome>(resolved.ToDictionary(p => p.Key, p => p.Value));
        }
    }

    public sealed class EntityState
    {
        public EntityId Id { get; }
        public string DefinitionId { get; }
        public SeaPosition Position { get; }
        public double Health { get; }
        public bool Defeated { get; }
        public IReadOnlyDictionary<string, int> Loot { get; }
        public IReadOnlyDictionary<string, double> Cooldowns { get; }
        public string BehaviorState { get; }
        public EntityState(EntityId id, string definitionId, SeaPosition position, double health, bool defeated,
            IEnumerable<KeyValuePair<string, int>> loot, IEnumerable<KeyValuePair<string, double>> cooldowns, string behaviorState)
        {
            if (!id.IsValid || Values.Finite(health) < 0) throw new ArgumentException("Invalid entity state.");
            Values.Id(position.RegionId); Id = id; DefinitionId = Values.Id(definitionId); Position = position;
            Health = health; Defeated = defeated; Loot = Values.Bundle(loot); Cooldowns = Values.Map(cooldowns);
            if (Cooldowns.Values.Any(v => Values.Finite(v) < 0)) throw new ArgumentException("Invalid cooldown.");
            BehaviorState = behaviorState ?? "";
        }
        public EntityState WithoutLoot() => new EntityState(Id, DefinitionId, Position, Health, Defeated,
            new Dictionary<string, int>(), Cooldowns, BehaviorState);
    }

    public sealed class ExpeditionState
    {
        public Guid Id { get; }
        public int Seed { get; }
        public string RngState { get; }
        public long Tick { get; }
        public double Health { get; }
        public double Speed { get; }
        public SeaPosition Position { get; }
        public IReadOnlyDictionary<string, int> Cargo { get; }
        public IReadOnlyList<StatModifier> Modifiers { get; }
        public IReadOnlyDictionary<string, double> Cooldowns { get; }
        public IReadOnlyDictionary<string, string> Encounters { get; }
        public IReadOnlyDictionary<EntityId, EntityState> Entities { get; }
        public ExpeditionState(Guid id, int seed, string rngState, long tick, double health, double speed, SeaPosition position,
            IEnumerable<KeyValuePair<string, int>> cargo, IEnumerable<StatModifier> modifiers,
            IEnumerable<KeyValuePair<string, double>> cooldowns, IEnumerable<KeyValuePair<string, string>> encounters,
            IEnumerable<EntityState> entities)
        {
            if (id == Guid.Empty || tick < 0 || Values.Finite(health) < 0 || Values.Finite(speed) < 0) throw new ArgumentException("Invalid expedition.");
            Values.Id(position.RegionId); Id = id; Seed = seed; RngState = Values.Id(rngState); Tick = tick;
            Health = health; Speed = speed; Position = position; Cargo = Values.Bundle(cargo); Modifiers = Values.List(modifiers);
            Cooldowns = Values.Map(cooldowns); Encounters = Values.Map(encounters);
            if (Cooldowns.Values.Any(v => Values.Finite(v) < 0)) throw new ArgumentException("Invalid cooldown.");
            Entities = new ReadOnlyDictionary<EntityId, EntityState>(entities.ToDictionary(e => e.Id));
            if (Entities.Keys.Any(k => k.AuthoredId == null && k.ExpeditionId != id)) throw new ArgumentException("Foreign generated entity.");
        }
    }

    public sealed class SessionSnapshot
    {
        public long Revision { get; }
        public Lifecycle Lifecycle { get; }
        public CampaignState Campaign { get; }
        public ExpeditionState Expedition { get; }
        public IReadOnlyList<Guid> CommittedRequests { get; }
        public SessionSnapshot(long revision, CampaignState campaign, ExpeditionState expedition, IEnumerable<Guid> committedRequests)
        {
            if (revision < 0) throw new ArgumentException("Invalid revision.");
            Revision = revision; Campaign = campaign ?? throw new ArgumentNullException(nameof(campaign)); Expedition = expedition;
            Lifecycle = expedition == null ? Lifecycle.Docked : Lifecycle.AtSea;
            CommittedRequests = Values.List(committedRequests);
        }

        // Same revision, campaign and request history with another expedition. The
        // shared parts are immutable, so this avoids copying them every simulation tick.
        public SessionSnapshot WithExpedition(ExpeditionState expedition) => new SessionSnapshot(this, expedition);

        private SessionSnapshot(SessionSnapshot source, ExpeditionState expedition)
        {
            Revision = source.Revision; Campaign = source.Campaign; Expedition = expedition;
            Lifecycle = expedition == null ? Lifecycle.Docked : Lifecycle.AtSea;
            CommittedRequests = source.CommittedRequests;
        }
    }
}
