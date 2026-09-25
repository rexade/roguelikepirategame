using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Core;

namespace PirateGame.Rules.Application
{
    // Bootstrap owns one instance per session and calls it on the simulation thread.
    // Every persistent command builds a detached candidate before invoking storage.
    public sealed class CampaignSession : IGameplayClock
    {
        private readonly DefinitionCatalog definitions;
        private readonly ISaveStore store;
        private readonly IWorldArrival arrival;
        private readonly Queue<TransitionCommitted> events = new Queue<TransitionCommitted>();
        private SessionSnapshot state;
        private SaveCandidate pending;
        private ArrivalRequest pendingArrival;
        private ArrivalRequest candidateArrival;
        private Guid dockRequest;
        private string dockHub;
        private bool writing;
        private bool arriving;
        private bool paused;
        private Lifecycle pendingLifecycle;

        public SessionSnapshot Snapshot => state;
        public SessionSnapshot LastCommitted { get; private set; }
        public SaveCandidate PendingSave => pending;
        public Lifecycle Lifecycle => pending != null ? pendingLifecycle : state.Lifecycle;
        public bool InputLocked => paused || pending != null || pendingArrival != null || writing || arriving;
        public bool IsPaused => InputLocked || state.Expedition == null;
        public long Tick => state.Expedition?.Tick ?? 0;
        public double FixedDeltaSeconds { get; }

        public CampaignSession(DefinitionCatalog definitions, SessionSnapshot initial, ISaveStore store, IWorldArrival arrival, double fixedDeltaSeconds = 0.02)
        {
            this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.arrival = arrival ?? throw new ArgumentNullException(nameof(arrival));
            if (Values.Finite(fixedDeltaSeconds) <= 0) throw new ArgumentException("Fixed step must be positive.");
            FixedDeltaSeconds = fixedDeltaSeconds;
            var valid = ValidateSnapshot(definitions, initial);
            if (!valid.IsSuccess) throw new ArgumentException(valid.Detail, nameof(initial));
            state = LastCommitted = initial;
            // Even a loaded Docked save cannot accept input before world restoration.
            pendingArrival = ArrivalFor(Guid.NewGuid(), initial);
        }

        public static SessionSnapshot NewCampaign(DefinitionCatalog definitions, string homeHub, string hull,
            IReadOnlyDictionary<string, string> owned, IReadOnlyDictionary<string, string> loadout)
        {
            if (!definitions.Hubs.ContainsKey(homeHub)) throw new ArgumentException("Unknown home hub.");
            var hubs = definitions.Hubs.Keys.ToDictionary(id => id, id => new HubState(id == homeHub, id == homeHub, Array.Empty<string>()));
            var campaign = new CampaignState(hull, homeHub, homeHub, new Dictionary<string, int>(), new Dictionary<string, int>(),
                owned, loadout, hubs, Array.Empty<string>(), new Dictionary<Guid, Outcome>());
            var snapshot = new SessionSnapshot(0, campaign, null, Array.Empty<Guid>());
            var valid = ValidateSnapshot(definitions, snapshot);
            if (!valid.IsSuccess) throw new ArgumentException(valid.Detail);
            return snapshot;
        }

        public IReadOnlyDictionary<string, double> ShipStats() => ComputeStats(definitions, state.Campaign, state.Expedition?.Modifiers);
        public RuleResult ValidateLoadout(IReadOnlyDictionary<string, string> loadout) =>
            loadout == null ? new RuleResult(RuleError.InvalidLoadout) : ShipRules.ValidateLoadout(definitions, state.Campaign.HullId, state.Campaign.OwnedEquipment, loadout);
        public void SetPaused(bool value) { paused = value; }
        public IReadOnlyList<TransitionCommitted> DrainEvents()
        {
            var result = Values.List(events); events.Clear(); return result;
        }

        public RuleResult Embark(Guid requestId, EmbarkPlan plan)
        {
            var gate = Gate(requestId, Lifecycle.Docked); if (!gate.IsSuccess) return gate;
            if (plan == null) return new RuleResult(RuleError.InvalidRequest);
            if (state.Campaign.ResolvedExpeditions.ContainsKey(plan.ExpeditionId)) return new RuleResult(RuleError.AlreadyResolved);
            var valid = ValidateLoadout(state.Campaign.Loadout); if (!valid.IsSuccess) return valid;
            try
            {
                var expedition = new ExpeditionState(plan.ExpeditionId, plan.Seed, plan.RngState, 0, ShipStats()["health"], 0,
                    definitions.Hubs[state.Campaign.CurrentHub].Dock, new Dictionary<string, int>(), Array.Empty<StatModifier>(),
                    new Dictionary<string, double>(), plan.Encounters, plan.Entities);
                return Prepare(requestId, "Embark", state.Campaign, expedition, Lifecycle.Departing, true);
            }
            catch (ArgumentException e) { return new RuleResult(RuleError.InvalidRequest, detail: e.Message); }
        }

        public RuleResult CollectLoot(Guid requestId, Guid expeditionId, EntityId sourceId)
        {
            var gate = SeaGate(requestId, expeditionId); if (!gate.IsSuccess) return gate;
            if (!state.Expedition.Entities.TryGetValue(sourceId, out var source)) return new RuleResult(RuleError.UnknownId);
            if (source.Loot.Values.All(v => v == 0)) return new RuleResult(RuleError.Depleted);
            try
            {
                var draft = new ExpeditionDraft(state.Expedition);
                foreach (var item in source.Loot) draft.Cargo[item.Key] = checked(Values.Amount(draft.Cargo, item.Key) + item.Value);
                if (ShipRules.Weight(definitions, draft.Cargo) > ShipStats()["cargo"]) return new RuleResult(RuleError.CargoFull);
                draft.Entities[sourceId] = source.WithoutLoot();
                return Prepare(requestId, "CollectLoot", state.Campaign, draft.Freeze(), Lifecycle.Resolving, false);
            }
            catch (OverflowException) { return new RuleResult(RuleError.Overflow); }
        }

        public RuleResult RequestDock(Guid requestId, Guid expeditionId, string hubId)
        {
            var gate = SeaGate(requestId, expeditionId); if (!gate.IsSuccess) return gate;
            if (dockRequest != Guid.Empty) return new RuleResult(RuleError.Busy);
            if (hubId == null || !definitions.Hubs.ContainsKey(hubId)) return new RuleResult(RuleError.UnknownId);
            if (!state.Campaign.Hubs[hubId].Activated) return new RuleResult(RuleError.HubInactive);
            dockRequest = requestId; dockHub = hubId;
            return new RuleResult(pending: true);
        }

        // Invoke once after all damage producers finish the fixed tick. A docking
        // request only queues intent; it can never commit before this damage total.
        public RuleResult CompleteTick(Guid expeditionId, long tick, double damage, SeaPosition position, double speed)
        {
            if (InputLocked) return new RuleResult(paused ? RuleError.Paused : RuleError.Busy);
            if (state.Expedition == null) return new RuleResult(RuleError.WrongLifecycle);
            if (state.Expedition.Id != expeditionId) return new RuleResult(RuleError.WrongExpedition);
            if (tick != state.Expedition.Tick + 1) return new RuleResult(RuleError.StaleTick);
            if (!IsNonnegative(damage) || !IsNonnegative(speed) || position.RegionId == null || !definitions.RegionIds.Contains(position.RegionId))
                return new RuleResult(RuleError.InvalidRequest);
            var draft = new ExpeditionDraft(state.Expedition);
            draft.Tick = tick; draft.Health = Math.Max(0, draft.Health - damage); draft.Position = position; draft.Speed = speed;
            foreach (var id in draft.Cooldowns.Keys.ToArray()) draft.Cooldowns[id] = Math.Max(0, draft.Cooldowns[id] - FixedDeltaSeconds);
            state = new SessionSnapshot(state.Revision, state.Campaign, draft.Freeze(), state.CommittedRequests);
            Guid request = dockRequest; string hub = dockHub; dockRequest = Guid.Empty; dockHub = null;
            if (draft.Health == 0) return ResolveSink(request == Guid.Empty ? Guid.NewGuid() : request, expeditionId);
            return request == Guid.Empty ? new RuleResult() : Dock(request, hub);
        }

        private RuleResult Dock(Guid requestId, string hubId)
        {
            var hub = definitions.Hubs[hubId]; var expedition = state.Expedition;
            double dx = expedition.Position.X - hub.Dock.X, dz = expedition.Position.Z - hub.Dock.Z;
            if (expedition.Position.RegionId != hub.Dock.RegionId || (dx / hub.Radius) * (dx / hub.Radius) + (dz / hub.Radius) * (dz / hub.Radius) > 1)
                return new RuleResult(RuleError.NotInDockZone);
            if (expedition.Speed > hub.MaximumSpeed) return new RuleResult(RuleError.TooFast);
            try
            {
                var draft = new CampaignDraft(state.Campaign);
                foreach (var item in expedition.Cargo) draft.Bank[item.Key] = checked(Values.Amount(draft.Bank, item.Key) + item.Value);
                draft.Resolved.Add(expedition.Id, Outcome.Docked); draft.CurrentHub = draft.LastSafeHub = hubId;
                return Prepare(requestId, "Dock", draft.Freeze(), null, Lifecycle.Resolving, true);
            }
            catch (OverflowException) { return new RuleResult(RuleError.Overflow); }
        }

        public RuleResult ResolveSink(Guid requestId, Guid expeditionId)
        {
            var gate = SeaGate(requestId, expeditionId); if (!gate.IsSuccess) return gate;
            if (state.Expedition.Health > 0) return new RuleResult(RuleError.NotSunk);
            var draft = new CampaignDraft(state.Campaign);
            draft.Resolved.Add(expeditionId, Outcome.Sunk); draft.CurrentHub = draft.LastSafeHub;
            dockRequest = Guid.Empty; dockHub = null;
            return Prepare(requestId, "Sink", draft.Freeze(), null, Lifecycle.Resolving, true);
        }

        public RuleResult PurchaseUpgrade(Guid requestId, string upgradeId)
        {
            var gate = Gate(requestId, Lifecycle.Docked); if (!gate.IsSuccess) return gate;
            if (upgradeId == null || !definitions.Upgrades.TryGetValue(upgradeId, out var upgrade)) return new RuleResult(RuleError.UnknownId);
            int current = Values.Amount(state.Campaign.Tiers, upgrade.TrackId);
            if (current >= upgrade.Tier) return new RuleResult(RuleError.TierAlreadyOwned);
            if (current != upgrade.Tier - 1 || upgrade.RequiredUnlocks.Any(u => !state.Campaign.Unlocks.Contains(u)))
                return new RuleResult(RuleError.PrerequisiteMissing);
            if (upgrade.Cost.Any(c => Values.Amount(state.Campaign.Bank, c.Key) < c.Value)) return new RuleResult(RuleError.InsufficientBank);
            var draft = new CampaignDraft(state.Campaign);
            foreach (var cost in upgrade.Cost) draft.Bank[cost.Key] = checked(Values.Amount(draft.Bank, cost.Key) - cost.Value);
            draft.Tiers[upgrade.TrackId] = upgrade.Tier;
            foreach (var unlock in upgrade.Grants) if (!draft.Unlocks.Contains(unlock)) draft.Unlocks.Add(unlock);
            return Prepare(requestId, "PurchaseUpgrade", draft.Freeze(), null, Lifecycle.Resolving, false);
        }

        public RuleResult SetLoadout(Guid requestId, IReadOnlyDictionary<string, string> loadout)
        {
            var gate = Gate(requestId, Lifecycle.Docked); if (!gate.IsSuccess) return gate;
            var valid = ValidateLoadout(loadout); if (!valid.IsSuccess) return valid;
            var draft = new CampaignDraft(state.Campaign); draft.Loadout = Values.Copy(loadout);
            return Prepare(requestId, "SetLoadout", draft.Freeze(), null, Lifecycle.Resolving, false);
        }

        public RuleResult FastTravel(Guid requestId, string destinationHub)
        {
            var gate = Gate(requestId, Lifecycle.Docked); if (!gate.IsSuccess) return gate;
            if (destinationHub == null || !definitions.Hubs.ContainsKey(destinationHub)) return new RuleResult(RuleError.UnknownId);
            if (!state.Campaign.Unlocks.Contains("fast-travel")) return new RuleResult(RuleError.TravelLocked);
            if (!state.Campaign.Hubs[destinationHub].Activated) return new RuleResult(RuleError.HubInactive);
            var draft = new CampaignDraft(state.Campaign); draft.CurrentHub = draft.LastSafeHub = destinationHub;
            return Prepare(requestId, "FastTravel", draft.Freeze(), null, Lifecycle.Resolving, true);
        }

        public RuleResult Checkpoint(Guid requestId)
        {
            var gate = Gate(requestId, state.Lifecycle); if (!gate.IsSuccess) return gate;
            if (dockRequest != Guid.Empty) return new RuleResult(RuleError.Busy);
            return Prepare(requestId, "Checkpoint", state.Campaign, state.Expedition, Lifecycle.Resolving, false);
        }

        public RuleResult Checkpoint(Guid requestId, ExpeditionState captured)
        {
            var gate = Gate(requestId, Lifecycle.AtSea); if (!gate.IsSuccess) return gate;
            if (dockRequest != Guid.Empty) return new RuleResult(RuleError.Busy);
            var current = state.Expedition;
            if (captured == null || captured.Id != current.Id) return new RuleResult(RuleError.WrongExpedition);
            if (captured.Tick != current.Tick) return new RuleResult(RuleError.StaleTick);
            // World capture owns enemy/ability state, never cargo accounting or
            // source refill. Retain unloaded entities and previously chosen spawns.
            if (captured.Seed != current.Seed || captured.Health != current.Health || captured.Speed != current.Speed ||
                captured.Position.RegionId != current.Position.RegionId || captured.Position.X != current.Position.X || captured.Position.Z != current.Position.Z ||
                !SameBundle(captured.Cargo, current.Cargo) ||
                current.Entities.Any(p => !captured.Entities.TryGetValue(p.Key, out var next) || next.DefinitionId != p.Value.DefinitionId ||
                    !SameBundle(next.Loot, p.Value.Loot) || (p.Value.Defeated && !next.Defeated)) ||
                current.Encounters.Any(p => !captured.Encounters.TryGetValue(p.Key, out var next) || next != p.Value))
                return new RuleResult(RuleError.InvalidRequest, detail: "Capture changed authoritative accounting or lost world history.");
            return Prepare(requestId, "Checkpoint", state.Campaign, captured, Lifecycle.Resolving, false);
        }

        private static bool SameBundle(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b) =>
            a.Count == b.Count && a.All(p => b.TryGetValue(p.Key, out var value) && value == p.Value);

        private RuleResult Gate(Guid request, Lifecycle expected)
        {
            if (request == Guid.Empty) return new RuleResult(RuleError.InvalidRequest);
            if (InputLocked) return new RuleResult(paused ? RuleError.Paused : RuleError.Busy);
            if (state.CommittedRequests.Contains(request)) return new RuleResult(RuleError.DuplicateRequest);
            if (request == dockRequest) return new RuleResult(RuleError.DuplicateRequest);
            if (state.Lifecycle != expected) return new RuleResult(RuleError.WrongLifecycle);
            return new RuleResult();
        }

        private RuleResult SeaGate(Guid request, Guid expedition)
        {
            var gate = Gate(request, Lifecycle.AtSea); if (!gate.IsSuccess) return gate;
            if (state.Campaign.ResolvedExpeditions.ContainsKey(expedition)) return new RuleResult(RuleError.AlreadyResolved);
            return state.Expedition.Id != expedition ? new RuleResult(RuleError.WrongExpedition) : new RuleResult();
        }

        private RuleResult Prepare(Guid request, string command, CampaignState campaign, ExpeditionState expedition, Lifecycle transition, bool needsArrival)
        {
            try
            {
                var snapshot = new SessionSnapshot(checked(state.Revision + 1), campaign, expedition, state.CommittedRequests.Concat(new[] { request }));
                var valid = ValidateSnapshot(definitions, snapshot); if (!valid.IsSuccess) return valid;
                pending = new SaveCandidate(request, state.Revision, snapshot, command);
                candidateArrival = needsArrival ? ArrivalFor(request, snapshot) : null;
                pendingLifecycle = transition;
                return RetrySave();
            }
            catch (OverflowException) { return new RuleResult(RuleError.Overflow); }
        }

        public RuleResult RetrySave()
        {
            if (writing || arriving) return new RuleResult(RuleError.Busy);
            if (pending == null) return new RuleResult(RuleError.InvalidRequest);
            writing = true;
            RuleResult result;
            try { result = store.Commit(pending); }
            catch (Exception e) { result = new RuleResult(RuleError.SaveFailed, detail: e.Message); }
            finally { writing = false; }
            if (result == null || !result.IsSuccess || result.IsPending) return new RuleResult(RuleError.SaveFailed, detail: result?.Detail ?? "No commit acknowledgement.");
            var committed = pending;
            state = LastCommitted = committed.Snapshot;
            pending = null; pendingArrival = candidateArrival; candidateArrival = null;
            events.Enqueue(new TransitionCommitted(committed));
            return pendingArrival == null ? new RuleResult() : RetryArrival();
        }

        public RuleResult RetryArrival()
        {
            if (writing || arriving || pending != null) return new RuleResult(RuleError.Busy);
            if (pendingArrival == null) return new RuleResult(RuleError.InvalidRequest);
            arriving = true;
            RuleResult result;
            try { result = arrival.EnsureReady(pendingArrival); }
            catch (Exception e) { result = new RuleResult(RuleError.ArrivalFailed, detail: e.Message); }
            finally { arriving = false; }
            if (result == null || !result.IsSuccess || result.IsPending) return new RuleResult(RuleError.ArrivalFailed, detail: result?.Detail ?? "Destination not ready.");
            pendingArrival = null;
            return new RuleResult();
        }

        private ArrivalRequest ArrivalFor(Guid request, SessionSnapshot snapshot) =>
            new ArrivalRequest(request, snapshot.Revision, snapshot.Expedition?.Position ?? definitions.Hubs[snapshot.Campaign.CurrentHub].Dock,
                snapshot.Campaign.CurrentHub, snapshot.Expedition?.Id);

        private static bool IsNonnegative(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;

        public static IReadOnlyDictionary<string, double> ComputeStats(DefinitionCatalog definitions, CampaignState campaign, IEnumerable<StatModifier> temporary = null)
        {
            var modifiers = campaign.Loadout.Values.SelectMany(id => definitions.Equipment[campaign.OwnedEquipment[id]].Modifiers)
                .Concat(definitions.Upgrades.Values.Where(u => Values.Amount(campaign.Tiers, u.TrackId) >= u.Tier).SelectMany(u => u.Modifiers))
                .Concat(temporary ?? Array.Empty<StatModifier>()).ToArray();
            return Values.Map(definitions.Hulls[campaign.HullId].Stats.Values.Select(stat =>
                new KeyValuePair<string, double>(stat.Id, ShipRules.Calculate(stat, modifiers))));
        }

        public static RuleResult ValidateSnapshot(DefinitionCatalog definitions, SessionSnapshot snapshot)
        {
            try
            {
                if (snapshot == null) return new RuleResult(RuleError.InvalidRequest, detail: "Missing snapshot.");
                var c = snapshot.Campaign;
                if (!definitions.Hulls.ContainsKey(c.HullId) || c.Bank.Keys.Any(k => !definitions.ResourceWeights.ContainsKey(k)) ||
                    c.OwnedEquipment.Values.Any(id => id == null || !definitions.Equipment.ContainsKey(id)) || c.Unlocks.Any(id => !definitions.UnlockIds.Contains(id)) ||
                    c.Hubs.Count != definitions.Hubs.Count || c.Hubs.Any(h => !definitions.Hubs.ContainsKey(h.Key) || h.Value == null) ||
                    !c.Hubs.ContainsKey(c.CurrentHub) || !c.Hubs.ContainsKey(c.LastSafeHub))
                    return new RuleResult(RuleError.UnknownId, detail: "Unknown or missing required campaign definition.");
                if (!c.Hubs[c.CurrentHub].Activated || !c.Hubs[c.LastSafeHub].Activated ||
                    c.ResolvedExpeditions.Any(p => p.Key == Guid.Empty || !Enum.IsDefined(typeof(Outcome), p.Value)) ||
                    snapshot.CommittedRequests.Any(id => id == Guid.Empty) || snapshot.CommittedRequests.Distinct().Count() != snapshot.CommittedRequests.Count)
                    return new RuleResult(RuleError.InvalidRequest, detail: "Invalid hub, outcome or request history.");
                foreach (var tier in c.Tiers)
                    if (!definitions.Upgrades.Values.Any(u => u.TrackId == tier.Key && u.Tier == tier.Value))
                        return new RuleResult(RuleError.UnknownId, detail: "Unknown upgrade tier.");
                var loadout = ShipRules.ValidateLoadout(definitions, c.HullId, c.OwnedEquipment, c.Loadout);
                if (!loadout.IsSuccess) return new RuleResult(loadout.Error, detail: "Invalid owned loadout.");
                var e = snapshot.Expedition;
                var stats = ComputeStats(definitions, c, e?.Modifiers);
                if (e != null)
                {
                    if (c.ResolvedExpeditions.ContainsKey(e.Id) || e.Health <= 0 || e.Health > stats["health"])
                        return new RuleResult(RuleError.InvalidRequest, detail: "Invalid active expedition health/outcome.");
                    if (!definitions.RegionIds.Contains(e.Position.RegionId) || e.Cargo.Keys.Any(k => !definitions.ResourceWeights.ContainsKey(k)) ||
                        e.Encounters.Values.Any(id => !definitions.EntityDefinitionIds.Contains(id)) ||
                        e.Modifiers.Any(m => !definitions.Hulls[c.HullId].Stats.ContainsKey(m.StatId)) ||
                        e.Entities.Values.Any(entity => !definitions.EntityDefinitionIds.Contains(entity.DefinitionId) ||
                            !definitions.RegionIds.Contains(entity.Position.RegionId) || entity.Loot.Keys.Any(k => !definitions.ResourceWeights.ContainsKey(k))))
                        return new RuleResult(RuleError.UnknownId, detail: "Unknown expedition definition.");
                    if (ShipRules.Weight(definitions, e.Cargo) > stats["cargo"]) return new RuleResult(RuleError.CargoFull);
                }
                return new RuleResult();
            }
            catch (OverflowException) { return new RuleResult(RuleError.Overflow); }
            catch (ArgumentException e) { return new RuleResult(RuleError.InvalidRequest, detail: e.Message); }
        }
    }

    internal sealed class CampaignDraft
    {
        private readonly CampaignState source;
        public string CurrentHub, LastSafeHub;
        public Dictionary<string, int> Bank, Tiers;
        public Dictionary<string, string> Loadout;
        public List<string> Unlocks;
        public Dictionary<Guid, Outcome> Resolved;
        public CampaignDraft(CampaignState source)
        {
            this.source = source; CurrentHub = source.CurrentHub; LastSafeHub = source.LastSafeHub;
            Bank = Values.Copy(source.Bank); Tiers = Values.Copy(source.Tiers); Loadout = Values.Copy(source.Loadout);
            Unlocks = source.Unlocks.ToList(); Resolved = source.ResolvedExpeditions.ToDictionary(p => p.Key, p => p.Value);
        }
        public CampaignState Freeze() => new CampaignState(source.HullId, CurrentHub, LastSafeHub, Bank, Tiers, source.OwnedEquipment,
            Loadout, source.Hubs, Unlocks, Resolved);
    }

    internal sealed class ExpeditionDraft
    {
        private readonly ExpeditionState source;
        public long Tick;
        public double Health, Speed;
        public SeaPosition Position;
        public Dictionary<string, int> Cargo;
        public Dictionary<string, double> Cooldowns;
        public Dictionary<EntityId, EntityState> Entities;
        public ExpeditionDraft(ExpeditionState source)
        {
            this.source = source; Tick = source.Tick; Health = source.Health; Speed = source.Speed; Position = source.Position;
            Cargo = Values.Copy(source.Cargo); Cooldowns = Values.Copy(source.Cooldowns); Entities = source.Entities.ToDictionary(p => p.Key, p => p.Value);
        }
        public ExpeditionState Freeze() => new ExpeditionState(source.Id, source.Seed, source.RngState, Tick, Health, Speed,
            Position, Cargo, source.Modifiers, Cooldowns, source.Encounters, Entities.Values);
    }
}
