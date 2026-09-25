using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PirateGame.Content.Combat;
using PirateGame.Content.World;
using PirateGame.Core;
using PirateGame.Gameplay.AI;
using PirateGame.Rules.Application;
using UnityEngine;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Composition
{
    // Builds a NEW voyage's ledger: every authored salvage site at full contents
    // plus seeded enemy spawns at the authored encounter markers. Geography never
    // varies; the seed only chooses which ships appear where. Existing voyages are
    // always restored from their saved ledger, never re-planned.
    public sealed class ExpeditionPlanner
    {
        private readonly FirstRegionAsset region;
        private readonly CombatCatalog combat;
        private readonly string[] enemyTypes;

        public ExpeditionPlanner(FirstRegionAsset region, CombatCatalog combat)
        {
            this.region = region ?? throw new ArgumentNullException(nameof(region));
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
            enemyTypes = combat.Enemies.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
            if (enemyTypes.Length == 0) throw new ArgumentException("No enemy definitions to place.");
        }

        public static string SpawnId(string site, int index) => site + "#" + index.ToString(CultureInfo.InvariantCulture);

        public EmbarkPlan Plan(Guid expeditionId, int seed)
        {
            var rng = new SplitMix64(unchecked((ulong)(uint)seed * 0x9E3779B97F4A7C15UL + 0x632BE59BD9B4E019UL));
            var entities = region.salvage.Select(s => s.Initial(region.regionId)).ToList();
            var encounters = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var site in region.encounters)
            {
                float spread = rng.NextFloat() * Mathf.PI * 2;
                for (int i = 0; i < site.ships; i++)
                {
                    var spec = combat.Enemies[enemyTypes[rng.NextInt(enemyTypes.Length)]];
                    // Escorts fan out around the marker so hulls never start overlapping.
                    float angle = spread + i * Mathf.PI * 2 / Math.Max(1, site.ships);
                    var offset = i == 0 && site.ships == 1 ? Vector2.zero : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 7f;
                    var position = site.position + offset;
                    string spawn = SpawnId(site.id, i);
                    encounters[spawn] = spec.Id;
                    entities.Add(Enemy(EntityId.Generated(expeditionId, spawn), spec, region.regionId, position, rng.NextFloat() * 360f));
                }
            }
            return new EmbarkPlan(expeditionId, seed, "splitmix64:" + rng.State.ToString("x16", CultureInfo.InvariantCulture), entities, encounters);
        }

        public static EntityState Enemy(EntityId id, EnemySpec spec, string regionId, Vector2 position, float yaw) =>
            new EntityState(id, spec.Id, new SeaPosition(regionId, position.x, position.y), spec.Health, false,
                new Dictionary<string, int>(), new Dictionary<string, double> { ["weapon"] = spec.ReloadSeconds },
                JsonUtility.ToJson(new EnemyMotion { mode = "idle", yaw = Mathf.Repeat(yaw, 360f) }));
    }

    // Small deterministic generator whose full state fits in the saved RNG string.
    public sealed class SplitMix64
    {
        public ulong State { get; private set; }
        public SplitMix64(ulong state) { State = state; }
        public ulong Next()
        {
            unchecked
            {
                State += 0x9E3779B97F4A7C15UL;
                ulong z = State;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
        public int NextInt(int count) => count <= 1 ? 0 : (int)(Next() % (ulong)count);
        public float NextFloat() => (Next() >> 40) / (float)(1UL << 24);
    }
}
