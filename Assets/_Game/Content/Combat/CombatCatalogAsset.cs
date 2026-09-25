using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Core;
using PirateGame.Content.Definitions;
using UnityEngine;

namespace PirateGame.Content.Combat
{
    public enum EnemyTactic { Pursue, KeepRange }
    [Serializable] public sealed class WeaponRow
    {
        public string id;
        public float damage = 20, cooldown = 1, speed = 45, range = 40, radius = 0.2f;
    }
    [Serializable] public sealed class EnemyRow
    {
        public string id, weaponId;
        public EnemyTactic tactic;
        public float health = 60, speed = 4, engagementRange = 28, preferredRange = 12;
        public float reloadScale = 3;
        // Salvage left by a defeated ship: a new generated wreck entity, never the ship itself.
        public QuantityRow[] wreckLoot = Array.Empty<QuantityRow>();
    }
    public sealed class WeaponSpec
    {
        public string Id { get; }
        public float Damage { get; }
        public float Cooldown { get; }
        public float Speed { get; }
        public float Range { get; }
        public float Radius { get; }
        public WeaponSpec(WeaponRow row)
        {
            Id = Values.Id(row.id); Damage = Positive(row.damage); Cooldown = Positive(row.cooldown);
            Speed = Positive(row.speed); Range = Positive(row.range); Radius = Positive(row.radius);
        }
        internal static float Positive(float value)
        {
            if (Values.Finite(value) <= 0) throw new ArgumentException("Combat values must be finite and positive.");
            return value;
        }
    }
    public sealed class EnemySpec
    {
        public string Id { get; }
        public WeaponSpec Weapon { get; }
        public EnemyTactic Tactic { get; }
        public float Health { get; }
        public float Speed { get; }
        public float EngagementRange { get; }
        public float PreferredRange { get; }
        public float ReloadSeconds { get; }
        public IReadOnlyDictionary<string, int> WreckLoot { get; }
        public EnemySpec(EnemyRow row, IReadOnlyDictionary<string, WeaponSpec> weapons)
        {
            Id = Values.Id(row.id); Weapon = weapons[row.weaponId]; Tactic = row.tactic;
            WreckLoot = Values.Bundle((row.wreckLoot ?? Array.Empty<QuantityRow>()).Select(q => new KeyValuePair<string, int>(q.id, q.quantity)));
            if (!Enum.IsDefined(typeof(EnemyTactic), Tactic)) throw new ArgumentException("Unknown enemy tactic.");
            Health = WeaponSpec.Positive(row.health); Speed = WeaponSpec.Positive(row.speed);
            EngagementRange = WeaponSpec.Positive(row.engagementRange); PreferredRange = WeaponSpec.Positive(row.preferredRange);
            ReloadSeconds = WeaponSpec.Positive(Weapon.Cooldown * WeaponSpec.Positive(row.reloadScale));
            if (PreferredRange >= EngagementRange) throw new ArgumentException("Invalid preferred range.");
        }
    }
    public sealed class CombatCatalog
    {
        public const string ContextDefinition = "combat-context-v1";
        public const string WreckDefinition = "wreck";
        public DefinitionCatalog Rules { get; }
        public IReadOnlyDictionary<string, WeaponSpec> Weapons { get; }
        public IReadOnlyDictionary<string, EnemySpec> Enemies { get; }
        public string AbilityId { get; }
        public float AbilityCooldown { get; }
        public float BraceDuration { get; }
        public float DamageMultiplier { get; }
        public CombatCatalog(DefinitionCatalog rules, IEnumerable<WeaponRow> weapons, IEnumerable<EnemyRow> enemies,
            string abilityId, float cooldown, float duration, float multiplier)
        {
            Rules = rules;
            Weapons = Values.Map(weapons.Select(w => new WeaponSpec(w)).Select(w => new KeyValuePair<string, WeaponSpec>(w.Id, w)));
            Enemies = Values.Map(enemies.Select(e => new EnemySpec(e, Weapons)).Select(e => new KeyValuePair<string, EnemySpec>(e.Id, e)));
            AbilityId = Values.Id(abilityId); AbilityCooldown = WeaponSpec.Positive(cooldown); BraceDuration = WeaponSpec.Positive(duration);
            DamageMultiplier = (float)Values.Finite(multiplier);
            if (multiplier < 0 || multiplier > 1 || duration > cooldown) throw new ArgumentException("Invalid defensive ability.");
            foreach (var id in Weapons.Keys)
                if (!rules.Equipment.TryGetValue(id, out var e) || e.Kind != SlotKind.Weapon) throw new ArgumentException("Unknown weapon: " + id);
            if (!rules.Equipment.TryGetValue(abilityId, out var ability) || ability.Kind != SlotKind.Ability)
                throw new ArgumentException("Unknown ability equipment.");
            if (!rules.EntityDefinitionIds.Contains(ContextDefinition) || Enemies.Keys.Any(id => !rules.EntityDefinitionIds.Contains(id)))
                throw new ArgumentException("Combat entity definitions missing from rule catalog.");
            foreach (var enemy in Enemies.Values)
                if (enemy.WreckLoot.Keys.Any(k => !rules.ResourceWeights.ContainsKey(k)) ||
                    (enemy.WreckLoot.Values.Any(v => v > 0) && !rules.EntityDefinitionIds.Contains(WreckDefinition)))
                    throw new ArgumentException("Invalid wreck loot for enemy: " + enemy.Id);
        }
        public WeaponSpec EquippedWeapon(CampaignState campaign)
        {
            if (!ShipRules.ValidateLoadout(Rules, campaign.HullId, campaign.OwnedEquipment, campaign.Loadout).IsSuccess)
                throw new ArgumentException("Invalid combat loadout.");
            var ids = campaign.Loadout.Values.Select(i => campaign.OwnedEquipment[i]).ToArray();
            var weapon = ids.Single(id => Rules.Equipment[id].Kind == SlotKind.Weapon);
            if (ids.Single(id => Rules.Equipment[id].Kind == SlotKind.Ability) != AbilityId) throw new ArgumentException("Unsupported ability.");
            return Weapons[weapon];
        }
    }
    [CreateAssetMenu(menuName = "Pirate Game/Combat Catalog")]
    public sealed class CombatCatalogAsset : ScriptableObject
    {
        public DefinitionCatalogAsset rules;
        public WeaponRow[] weapons = Array.Empty<WeaponRow>();
        public EnemyRow[] enemies = Array.Empty<EnemyRow>();
        public string abilityId = "brace";
        public float abilityCooldown = 6, braceDuration = 2, damageMultiplier = 0.25f;
        public CombatCatalog Freeze() => new CombatCatalog(rules.Freeze(), weapons, enemies, abilityId, abilityCooldown, braceDuration, damageMultiplier);
    }
}
