using System;
using System.Collections.Generic;
using System.Linq;

namespace PirateGame.Core
{
    public sealed class StatDefinition
    {
        public string Id { get; }
        public double Base { get; }
        public double Minimum { get; }
        public double Maximum { get; }
        public StatDefinition(string id, double value, double minimum, double maximum)
        {
            Id = Values.Id(id); Base = Values.Finite(value); Minimum = Values.Finite(minimum); Maximum = Values.Finite(maximum);
            if (minimum > maximum || value < minimum || value > maximum) throw new ArgumentException("Invalid stat limits.");
        }
    }

    public sealed class StatModifier
    {
        public string StatId { get; }
        public ModifierOperation Operation { get; }
        public double Value { get; }
        public StatModifier(string statId, ModifierOperation operation, double value)
        {
            StatId = Values.Id(statId); Value = Values.Finite(value); Operation = operation;
            if (!Enum.IsDefined(typeof(ModifierOperation), operation)) throw new ArgumentException("Unsupported modifier operation.");
        }
    }

    public sealed class HullDefinition
    {
        public string Id { get; }
        public IReadOnlyDictionary<string, SlotKind> Slots { get; }
        public IReadOnlyDictionary<string, StatDefinition> Stats { get; }
        public HullDefinition(string id, IEnumerable<KeyValuePair<string, SlotKind>> slots, IEnumerable<StatDefinition> stats)
        {
            Id = Values.Id(id); Slots = Values.Map(slots); Stats = Values.Map(stats.Select(s => new KeyValuePair<string, StatDefinition>(s.Id, s)));
            if (!Slots.Values.All(s => Enum.IsDefined(typeof(SlotKind), s))) throw new ArgumentException("Unknown slot kind.");
            if (!Stats.ContainsKey("health") || Stats["health"].Minimum <= 0 || !Stats.ContainsKey("cargo") || Stats["cargo"].Minimum < 0)
                throw new ArgumentException("Hull requires positive health and nonnegative cargo limits.");
        }
    }

    public sealed class EquipmentDefinition
    {
        public string Id { get; }
        public SlotKind Kind { get; }
        public IReadOnlyList<StatModifier> Modifiers { get; }
        public EquipmentDefinition(string id, SlotKind kind, IEnumerable<StatModifier> modifiers)
        {
            Id = Values.Id(id); Kind = kind; Modifiers = Values.List(modifiers);
            if (!Enum.IsDefined(typeof(SlotKind), kind)) throw new ArgumentException("Unknown equipment kind.");
        }
    }

    public sealed class HubDefinition
    {
        public string Id { get; }
        public SeaPosition Dock { get; }
        public double Radius { get; }
        public double MaximumSpeed { get; }
        public HubDefinition(string id, SeaPosition dock, double radius, double maximumSpeed)
        {
            Id = Values.Id(id); Dock = dock; Values.Id(dock.RegionId);
            Radius = Values.Finite(radius); MaximumSpeed = Values.Finite(maximumSpeed);
            if (radius <= 0 || maximumSpeed < 0) throw new ArgumentException("Invalid docking thresholds.");
        }
    }

    public sealed class UpgradeDefinition
    {
        public string Id { get; }
        public string TrackId { get; }
        public int Tier { get; }
        public IReadOnlyDictionary<string, int> Cost { get; }
        public IReadOnlyList<string> RequiredUnlocks { get; }
        public IReadOnlyList<string> Grants { get; }
        public IReadOnlyList<StatModifier> Modifiers { get; }
        public UpgradeDefinition(string id, string track, int tier, IEnumerable<KeyValuePair<string, int>> cost,
            IEnumerable<string> requiredUnlocks, IEnumerable<string> grants, IEnumerable<StatModifier> modifiers)
        {
            Id = Values.Id(id); TrackId = Values.Id(track); Tier = tier;
            if (tier < 1) throw new ArgumentException("Tier must be positive.");
            Cost = Values.Bundle(cost); RequiredUnlocks = Values.List(requiredUnlocks.Select(Values.Id));
            Grants = Values.List(grants.Select(Values.Id)); Modifiers = Values.List(modifiers);
        }
    }

    public sealed class AuthoredIdentity
    {
        public string Id { get; }
        public string Origin { get; }
        public AuthoredIdentity(string id, string origin) { Id = Values.Id(id); Origin = Values.Id(origin); }
    }

    public sealed class DefinitionCatalog
    {
        public IReadOnlyDictionary<string, int> ResourceWeights { get; }
        public IReadOnlyDictionary<string, HullDefinition> Hulls { get; }
        public IReadOnlyDictionary<string, EquipmentDefinition> Equipment { get; }
        public IReadOnlyDictionary<string, HubDefinition> Hubs { get; }
        public IReadOnlyDictionary<string, UpgradeDefinition> Upgrades { get; }
        public IReadOnlyList<string> UnlockIds { get; }
        public IReadOnlyList<string> EntityDefinitionIds { get; }
        public IReadOnlyList<string> RegionIds { get; }
        public DefinitionCatalog(IEnumerable<KeyValuePair<string, int>> weights, IEnumerable<HullDefinition> hulls,
            IEnumerable<EquipmentDefinition> equipment, IEnumerable<HubDefinition> hubs, IEnumerable<UpgradeDefinition> upgrades,
            IEnumerable<string> unlockIds, IEnumerable<AuthoredIdentity> worldIds,
            IEnumerable<string> entityDefinitionIds = null, IEnumerable<string> regionIds = null)
        {
            ResourceWeights = Values.Bundle(weights);
            if (ResourceWeights.Values.Any(v => v <= 0)) throw new ArgumentException("Resource weights must be positive.");
            Hulls = Index(hulls, x => x.Id); Equipment = Index(equipment, x => x.Id);
            Hubs = Index(hubs, x => x.Id); Upgrades = Index(upgrades, x => x.Id);
            UnlockIds = Values.List(unlockIds.Select(Values.Id));
            EntityDefinitionIds = Values.List((entityDefinitionIds ?? Array.Empty<string>()).Select(Values.Id));
            RegionIds = Values.List((regionIds ?? Hubs.Values.Select(h => h.Dock.RegionId).Distinct()).Select(Values.Id));
            if (EntityDefinitionIds.Distinct().Count() != EntityDefinitionIds.Count || RegionIds.Distinct().Count() != RegionIds.Count ||
                Hubs.Values.Any(h => !RegionIds.Contains(h.Dock.RegionId))) throw new ArgumentException("Invalid entity/region definitions.");
            var ids = worldIds.ToArray();
            var duplicates = ids.GroupBy(x => x.Id).Where(g => g.Count() > 1).ToArray();
            if (duplicates.Length > 0) throw new ArgumentException("Duplicate authored IDs: " + string.Join("; ", duplicates.Select(g => g.Key + " at " + string.Join(", ", g.Select(x => x.Origin)))));
            if (UnlockIds.Distinct().Count() != UnlockIds.Count) throw new ArgumentException("Duplicate unlock ID.");
            var stats = new HashSet<string>(Hulls.Values.SelectMany(h => h.Stats.Keys));
            foreach (var modifier in Equipment.Values.SelectMany(e => e.Modifiers).Concat(Upgrades.Values.SelectMany(u => u.Modifiers)))
                if (!stats.Contains(modifier.StatId)) throw new ArgumentException("Missing stat: " + modifier.StatId);
            foreach (var upgrade in Upgrades.Values)
            {
                if (upgrade.Cost.Keys.Any(k => !ResourceWeights.ContainsKey(k))) throw new ArgumentException("Unknown cost resource: " + upgrade.Id);
                if (upgrade.RequiredUnlocks.Concat(upgrade.Grants).Any(k => !UnlockIds.Contains(k))) throw new ArgumentException("Unknown unlock: " + upgrade.Id);
            }
            foreach (var track in Upgrades.Values.GroupBy(u => u.TrackId))
                if (!track.Select(u => u.Tier).OrderBy(t => t).SequenceEqual(Enumerable.Range(1, track.Count())))
                    throw new ArgumentException("Upgrade tiers must be unique and consecutive: " + track.Key);
        }

        private static IReadOnlyDictionary<string, T> Index<T>(IEnumerable<T> items, Func<T, string> id) =>
            Values.Map(items.Select(x => new KeyValuePair<string, T>(id(x), x)));
    }

    public static class ShipRules
    {
        public static RuleResult ValidateLoadout(DefinitionCatalog definitions, string hullId,
            IReadOnlyDictionary<string, string> owned, IReadOnlyDictionary<string, string> loadout)
        {
            if (loadout == null || owned == null) return new RuleResult(RuleError.InvalidLoadout);
            if (hullId == null) return new RuleResult(RuleError.UnknownId);
            if (!definitions.Hulls.TryGetValue(hullId, out var hull)) return new RuleResult(RuleError.UnknownId);
            if (loadout.Count != hull.Slots.Count || loadout.Values.Distinct().Count() != loadout.Count)
                return new RuleResult(RuleError.InvalidLoadout);
            foreach (var slot in loadout)
                if (string.IsNullOrWhiteSpace(slot.Value) || !hull.Slots.TryGetValue(slot.Key, out var kind) || !owned.TryGetValue(slot.Value, out var definitionId) ||
                    definitionId == null || !definitions.Equipment.TryGetValue(definitionId, out var equipment) || equipment.Kind != kind)
                    return new RuleResult(RuleError.InvalidLoadout);
            return new RuleResult();
        }

        public static double Calculate(StatDefinition stat, IEnumerable<StatModifier> modifiers)
        {
            double flat = 0, percent = 0;
            foreach (var modifier in modifiers.Where(m => m.StatId == stat.Id))
                if (modifier.Operation == ModifierOperation.Flat) flat = Values.Finite(flat + modifier.Value);
                else percent = Values.Finite(percent + modifier.Value);
            double result = Values.Finite(Values.Finite(stat.Base + flat) * Values.Finite(1 + percent));
            return Math.Max(stat.Minimum, Math.Min(stat.Maximum, result));
        }

        public static int Weight(DefinitionCatalog definitions, IReadOnlyDictionary<string, int> bundle)
        {
            int weight = 0;
            foreach (var pair in bundle) weight = checked(weight + checked(pair.Value * definitions.ResourceWeights[pair.Key]));
            return weight;
        }
    }
}
