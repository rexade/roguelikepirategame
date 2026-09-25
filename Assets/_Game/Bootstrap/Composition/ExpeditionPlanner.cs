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
    // Builds a NEW voyage's ledger for every region: each authored salvage site at
    // full contents, plus seeded enemy spawns at the authored encounter markers
    // drawn from the region's encounter table. Geography never varies; the seed only
    // chooses which ships appear where and how many. Existing voyages are always
    // restored from their saved ledger, never re-planned.
    public sealed class ExpeditionPlanner
    {
        private readonly FirstRegionAsset[] regions;
        private readonly CombatCatalog combat;
        // Diagnostic workload knobs (T11 stress run only): more ships per site and
        // extra generated barrels scattered over the home region.
        public int ShipMultiplier { get; set; } = 1;
        public int ExtraBarrels { get; set; }

        public ExpeditionPlanner(IEnumerable<FirstRegionAsset> regions, CombatCatalog combat)
        {
            this.regions = (regions ?? throw new ArgumentNullException(nameof(regions))).ToArray();
            this.combat = combat ?? throw new ArgumentNullException(nameof(combat));
            if (this.regions.Length == 0) throw new ArgumentException("At least one region is required.");
            if (combat.Enemies.Count == 0) throw new ArgumentException("No enemy definitions to place.");
            foreach (var region in this.regions)
                foreach (var entry in region.encounterTable)
                    if (!combat.Enemies.ContainsKey(entry.enemyId)) throw new ArgumentException("Unknown enemy in encounter table: " + entry.enemyId);
        }

        public ExpeditionPlanner(FirstRegionAsset region, CombatCatalog combat) : this(new[] { region }, combat) { }

        public static string SpawnId(string site, int index) => site + "#" + index.ToString(CultureInfo.InvariantCulture);

        public EmbarkPlan Plan(Guid expeditionId, int seed)
        {
            var rng = new SplitMix64(unchecked((ulong)(uint)seed * 0x9E3779B97F4A7C15UL + 0x632BE59BD9B4E019UL));
            var entities = new List<EntityState>();
            var encounters = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var region in regions)
            {
                entities.AddRange(region.salvage.Select(s => s.Initial(region.regionId)));
                var table = Table(region);
                int total = table.Sum(t => t.weight);
                foreach (var site in region.encounters)
                {
                    int minimum = site.minShips > 0 ? site.minShips : site.ships;
                    int count = (minimum + rng.NextInt(site.ships - minimum + 1)) * (region == regions[0] ? Math.Max(1, ShipMultiplier) : 1);
                    float spread = rng.NextFloat() * Mathf.PI * 2;
                    for (int i = 0; i < count; i++)
                    {
                        var spec = combat.Enemies[Pick(table, total, rng)];
                        // Escorts fan out around the marker so hulls never start overlapping.
                        float angle = spread + i * Mathf.PI * 2 / Math.Max(1, count);
                        var offset = count == 1 ? Vector2.zero : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 7f;
                        string spawn = SpawnId(site.id, i);
                        encounters[spawn] = spec.Id;
                        entities.Add(Enemy(EntityId.Generated(expeditionId, spawn), spec, region.regionId, site.position + offset, rng.NextFloat() * 360f));
                    }
                }
            }
            var home = regions[0];
            for (int i = 0; i < ExtraBarrels; i++)
            {
                var at = new Vector2(Mathf.Lerp(home.bounds.xMin + 10, home.bounds.xMax - 10, rng.NextFloat()), Mathf.Lerp(home.dock.y + 8, home.bounds.yMax - 10, rng.NextFloat()));
                entities.Add(new EntityState(EntityId.Generated(expeditionId, "stress/barrel#" + i.ToString(CultureInfo.InvariantCulture)), "barrel",
                    new SeaPosition(home.regionId, at.x, at.y), 1, false, new Dictionary<string, int> { ["wood"] = 1 }, new Dictionary<string, double>(), "salvage"));
            }
            return new EmbarkPlan(expeditionId, seed, "splitmix64:" + rng.State.ToString("x16", CultureInfo.InvariantCulture), entities, encounters);
        }

        private WeightedEnemy[] Table(FirstRegionAsset region) =>
            region.encounterTable.Length > 0 ? region.encounterTable
                : combat.Enemies.Keys.OrderBy(k => k, StringComparer.Ordinal).Select(k => new WeightedEnemy { enemyId = k, weight = 1 }).ToArray();

        private static string Pick(WeightedEnemy[] table, int total, SplitMix64 rng)
        {
            int roll = rng.NextInt(total);
            foreach (var entry in table)
            {
                if (roll < entry.weight) return entry.enemyId;
                roll -= entry.weight;
            }
            return table[table.Length - 1].enemyId;
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
