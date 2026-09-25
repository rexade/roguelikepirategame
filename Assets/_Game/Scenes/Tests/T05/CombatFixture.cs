using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Core;
using PirateGame.Content.Combat;
using PirateGame.Content.Definitions;
using PirateGame.Gameplay.Combat;
using PirateGame.Gameplay.AI;
using PirateGame.Gameplay.Ships;
using PirateGame.Rules.Application;
using UnityEngine;
using UnityEngine.InputSystem;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Tests.T05
{
    public sealed class CombatFixture : MonoBehaviour
    {
        public CombatCatalogAsset catalog;
        public ShipSimulation simulation;
        public EnemyShip pursuer, gunner;
        public CombatWorld World { get; private set; }
        public static DefinitionCatalogAsset CreateRules()
        {
            var asset = ScriptableObject.CreateInstance<DefinitionCatalogAsset>();
            asset.resources = new[] { new ResourceRow { id = "wood", weight = 1 } };
            asset.hulls = new[] { new HullRow { id = "cutter", slots = new[] {
                new SlotRow { id = "weapon", kind = SlotKind.Weapon }, new SlotRow { id = "ability", kind = SlotKind.Ability } },
                stats = new[] { new StatRow { id = "health", value = 100, minimum = 1, maximum = 200 },
                    new StatRow { id = "cargo", value = 10, minimum = 0, maximum = 100 },
                    new StatRow { id = "speed", value = 8, minimum = 1, maximum = 30 },
                    new StatRow { id = "damage-scale", value = 1, minimum = 0.1, maximum = 4 } } } };
            asset.equipment = new[] { new EquipmentRow { id = "cannon", kind = SlotKind.Weapon },
                new EquipmentRow { id = "repeater", kind = SlotKind.Weapon }, new EquipmentRow { id = "brace", kind = SlotKind.Ability } };
            asset.hubs = new[] { new HubRow { id = "home", regionId = "test-region", x = 7, z = -9, radius = 5, maximumSpeed = 1 } };
            asset.regionIds = new[] { "test-region" };
            asset.entityDefinitionIds = new[] { "raider", "gunner", CombatCatalog.ContextDefinition };
            return asset;
        }
        public static CombatCatalogAsset CreateCatalog(DefinitionCatalogAsset rules)
        {
            var asset = ScriptableObject.CreateInstance<CombatCatalogAsset>(); asset.rules = rules;
            asset.weapons = new[] { new WeaponRow { id = "cannon", damage = 30, cooldown = 1.1f, speed = 65, range = 45, radius = 0.24f },
                new WeaponRow { id = "repeater", damage = 8, cooldown = 0.22f, speed = 90, range = 30, radius = 0.13f } };
            asset.enemies = new[] { new EnemyRow { id = "raider", weaponId = "repeater", tactic = EnemyTactic.Pursue,
                health = 70, speed = 5, engagementRange = 38, preferredRange = 9 },
                new EnemyRow { id = "gunner", weaponId = "cannon", tactic = EnemyTactic.KeepRange,
                health = 90, speed = 3.5f, engagementRange = 42, preferredRange = 23 } };
            return asset;
        }
        public sealed class MemoryStore : ISaveStore
        {
            public bool Fail;
            public SaveCandidate Last;
            public RuleResult Commit(SaveCandidate candidate) { Last = candidate; return new RuleResult(Fail ? RuleError.SaveFailed : RuleError.None); }
        }
        public sealed class ReadyArrival : IWorldArrival { public RuleResult EnsureReady(ArrivalRequest request) => new RuleResult(); }
        public static CampaignSession CreateSession(CombatCatalog definitions, string weapon = "cannon", MemoryStore store = null, bool embark = true)
        {
            var initial = CampaignSession.NewCampaign(definitions.Rules, "home", "cutter",
                new Dictionary<string, string> { ["owned-cannon"] = "cannon", ["owned-repeater"] = "repeater", ["owned-brace"] = "brace" },
                new Dictionary<string, string> { ["weapon"] = "owned-" + weapon, ["ability"] = "owned-brace" });
            var session = new CampaignSession(definitions.Rules, initial, store ?? new MemoryStore(), new ReadyArrival());
            session.RetryArrival();
            if (embark) session.Embark(Guid.NewGuid(), new EmbarkPlan(Guid.NewGuid(), 5, "t05", Array.Empty<EntityState>(), new Dictionary<string, string>()));
            return session;
        }
        private void Awake()
        {
            var definitions = catalog.Freeze();
            string weapon = Array.IndexOf(Environment.GetCommandLineArgs(), "-t05-repeater") >= 0 ? "repeater" : "cannon";
            simulation.Bind(CreateSession(definitions, weapon));
            pursuer.Initialize(EntityId.Authored("t05-raider"), "test-region", definitions.Enemies["raider"]);
            gunner.Initialize(EntityId.Authored("t05-gunner"), "test-region", definitions.Enemies["gunner"]);
            var player = simulation.gameObject.AddComponent<CombatTarget>(); player.motor = simulation.motor;
            World = gameObject.AddComponent<CombatWorld>();
            World.Bind(simulation, definitions, player, new ICombatEnemy[] { pursuer, gunner }, EntityId.Authored("t05-context"));
            gameObject.AddComponent<CombatView>().world = World;
        }
        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) simulation.SetPaused(!simulation.Session.IsPaused);
        }
    }
}
