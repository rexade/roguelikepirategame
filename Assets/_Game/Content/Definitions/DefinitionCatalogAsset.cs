using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Core;
using UnityEngine;

namespace PirateGame.Content.Definitions
{
    [Serializable] public sealed class ResourceRow { public string id; public int weight = 1; }
    [Serializable] public sealed class QuantityRow { public string id; public int quantity; }
    [Serializable] public sealed class SlotRow { public string id; public SlotKind kind; }
    [Serializable] public sealed class StatRow { public string id; public double value; public double minimum; public double maximum; }
    [Serializable] public sealed class ModifierRow
    {
        public string statId; public ModifierOperation operation; public double value;
        public StatModifier Freeze() => new StatModifier(statId, operation, value);
    }
    [Serializable] public sealed class HullRow { public string id; public SlotRow[] slots = Array.Empty<SlotRow>(); public StatRow[] stats = Array.Empty<StatRow>(); }
    [Serializable] public sealed class EquipmentRow { public string id; public SlotKind kind; public ModifierRow[] modifiers = Array.Empty<ModifierRow>(); }
    [Serializable] public sealed class HubRow { public string id; public string regionId; public double x, z; public double radius = 5; public double maximumSpeed = 1; }
    [Serializable] public sealed class UpgradeRow
    {
        public string id, trackId; public int tier = 1;
        public QuantityRow[] cost = Array.Empty<QuantityRow>();
        public string[] requiredUnlocks = Array.Empty<string>();
        public string[] grants = Array.Empty<string>();
        public ModifierRow[] modifiers = Array.Empty<ModifierRow>();
    }
    [Serializable] public sealed class IdentityRow { public string id; public string origin; }

    [CreateAssetMenu(menuName = "Pirate Game/Definition Catalog")]
    public sealed class DefinitionCatalogAsset : ScriptableObject
    {
        public ResourceRow[] resources = Array.Empty<ResourceRow>();
        public HullRow[] hulls = Array.Empty<HullRow>();
        public EquipmentRow[] equipment = Array.Empty<EquipmentRow>();
        public HubRow[] hubs = Array.Empty<HubRow>();
        public UpgradeRow[] upgrades = Array.Empty<UpgradeRow>();
        public string[] unlockIds = Array.Empty<string>();
        public string[] entityDefinitionIds = Array.Empty<string>();
        public string[] regionIds = Array.Empty<string>();
        public IdentityRow[] worldIdentities = Array.Empty<IdentityRow>();

        // Authored rows stay in Unity. The returned graph holds only detached,
        // immutable C# values and is the sole input consumed by runtime rules.
        public DefinitionCatalog Freeze() => new DefinitionCatalog(
            resources.Select(r => new KeyValuePair<string, int>(r.id, r.weight)),
            hulls.Select(h => new HullDefinition(h.id, h.slots.Select(s => new KeyValuePair<string, SlotKind>(s.id, s.kind)),
                h.stats.Select(s => new StatDefinition(s.id, s.value, s.minimum, s.maximum)))),
            equipment.Select(e => new EquipmentDefinition(e.id, e.kind, e.modifiers.Select(m => m.Freeze()))),
            hubs.Select(h => new HubDefinition(h.id, new SeaPosition(h.regionId, h.x, h.z), h.radius, h.maximumSpeed)),
            upgrades.Select(u => new UpgradeDefinition(u.id, u.trackId, u.tier,
                u.cost.Select(c => new KeyValuePair<string, int>(c.id, c.quantity)), u.requiredUnlocks, u.grants, u.modifiers.Select(m => m.Freeze()))),
            unlockIds, worldIdentities.Select(i => new AuthoredIdentity(i.id, i.origin)), entityDefinitionIds, regionIds);
    }
}
