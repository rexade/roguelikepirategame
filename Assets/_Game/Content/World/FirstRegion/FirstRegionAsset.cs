using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Core;
using UnityEngine;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Content.World
{
    [Serializable] public sealed class IslandSite
    {
        public string id;
        public Vector2 position;
        public Vector2 size;
    }
    [Serializable] public sealed class SalvageSite
    {
        public string id, definitionId;
        public Vector2 position;
        public int wood, iron;
        public EntityState Initial(string region) => new EntityState(EntityId.Authored(id), definitionId,
            new SeaPosition(region, position.x, position.y), 1, false,
            new Dictionary<string, int> { ["wood"] = wood, ["iron"] = iron },
            new Dictionary<string, double>(), "salvage");
    }
    // A further harbor in this region. It starts undiscovered; the player raises
    // a flag in its berth to activate it (T09). Geometry is presentation data.
    [Serializable] public sealed class HarborSite
    {
        public string id, name;
        public Vector2 dock;
        public float dockRadius = 5, dockMaximumSpeed = 1;
        public Vector2 landmass, landmassSize = new Vector2(26, 18);
    }
    [Serializable] public sealed class EncounterSite
    {
        public string id;
        public Vector2 position;
        // Ships spawned here per voyage; their types are drawn from the voyage seed.
        [Min(1)] public int ships = 1;
    }

    [CreateAssetMenu(menuName = "Pirate Game/First Region")]
    public sealed class FirstRegionAsset : ScriptableObject
    {
        public string regionId = "first-region", homeId = "home-harbor", homeName = "Homeward Harbor";
        public Vector2 dock = new Vector2(0, -12);
        public float dockRadius = 5, dockMaximumSpeed = 1;
        public Vector2 homeLandmass = new Vector2(-8, -33), homeLandmassSize = new Vector2(30, 20);
        public HarborSite[] outposts = Array.Empty<HarborSite>();
        public IslandSite[] islands = Array.Empty<IslandSite>();
        public SalvageSite[] salvage = Array.Empty<SalvageSite>();
        public EncounterSite[] encounters = Array.Empty<EncounterSite>();
        public Vector2[] route = Array.Empty<Vector2>();
        public HubDefinition Home => new HubDefinition(homeId, new SeaPosition(regionId, dock.x, dock.y), dockRadius, dockMaximumSpeed);
        public IEnumerable<HubDefinition> Hubs => new[] { Home }.Concat(outposts.Select(o =>
            new HubDefinition(o.id, new SeaPosition(regionId, o.dock.x, o.dock.y), o.dockRadius, o.dockMaximumSpeed)));
        public string HubName(string id) =>
            id == homeId ? homeName : outposts.FirstOrDefault(o => o.id == id)?.name ?? id;
        // Moored heading: bow pointing away from the harbor's landmass, out to sea.
        public float DockYaw(string id)
        {
            var site = outposts.FirstOrDefault(o => o.id == id);
            var from = site != null ? site.landmass : homeLandmass;
            var to = site != null ? site.dock : dock;
            var d = to - from;
            return d.sqrMagnitude < 0.0001f ? 0 : Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
        }

        public IEnumerable<AuthoredIdentity> Identities(string origin)
        {
            yield return new AuthoredIdentity(homeId, origin + "/harbor");
            for (int i = 0; i < outposts.Length; i++) yield return new AuthoredIdentity(outposts[i].id, origin + "/outposts[" + i + "]");
            for (int i = 0; i < islands.Length; i++) yield return new AuthoredIdentity(islands[i].id, origin + "/islands[" + i + "]");
            for (int i = 0; i < salvage.Length; i++) yield return new AuthoredIdentity(salvage[i].id, origin + "/salvage[" + i + "]");
            for (int i = 0; i < encounters.Length; i++) yield return new AuthoredIdentity(encounters[i].id, origin + "/encounters[" + i + "]");
        }
        public void Validate(DefinitionCatalog catalog)
        {
            if (!catalog.RegionIds.Contains(regionId)) throw new ArgumentException("Unknown region: " + regionId);
            var home = Home;
            if (!catalog.Hubs.TryGetValue(homeId, out var hub) || hub.Dock.RegionId != regionId ||
                hub.Dock.X != home.Dock.X || hub.Dock.Z != home.Dock.Z || hub.Radius != home.Radius || hub.MaximumSpeed != home.MaximumSpeed)
                throw new ArgumentException("Dock contract differs: " + homeId);
            foreach (var outpost in Hubs.Skip(1))
                if (!catalog.Hubs.TryGetValue(outpost.Id, out var known) || known.Dock.RegionId != regionId || known.Dock.X != outpost.Dock.X ||
                    known.Dock.Z != outpost.Dock.Z || known.Radius != outpost.Radius || known.MaximumSpeed != outpost.MaximumSpeed)
                    throw new ArgumentException("Dock contract differs: " + outpost.Id);
            foreach (var site in outposts)
            {
                Finite(site.dock); Finite(site.landmass); Finite(site.landmassSize);
                if (string.IsNullOrWhiteSpace(site.name) || site.landmassSize.x <= 0 || site.landmassSize.y <= 0) throw new ArgumentException("Invalid outpost: " + site.id);
            }
            ValidateIdentities(Identities(name));
            foreach (var island in islands)
            {
                Finite(island.position); Finite(island.size);
                if (island.size.x <= 0 || island.size.y <= 0) throw new ArgumentException("Invalid island size: " + island.id);
            }
            foreach (var site in salvage)
            {
                Finite(site.position);
                if (!catalog.EntityDefinitionIds.Contains(site.definitionId)) throw new ArgumentException("Unknown salvage definition: " + site.definitionId);
                var entity = site.Initial(regionId);
                if (!entity.Loot.Any(p => p.Value > 0) || entity.Loot.Keys.Any(k => !catalog.ResourceWeights.ContainsKey(k)))
                    throw new ArgumentException("Invalid salvage bundle: " + site.id);
            }
            foreach (var site in encounters)
            {
                Finite(site.position);
                if (site.ships < 1 || site.ships > 8) throw new ArgumentException("Invalid encounter size: " + site.id);
            }
            if (route.Length < 2 || route[0] != dock || route[route.Length - 1] != dock) throw new ArgumentException("Route must return home.");
            foreach (var point in route) Finite(point);
        }
        public static void ValidateIdentities(IEnumerable<AuthoredIdentity> identities)
        {
            var duplicates = identities.GroupBy(x => x.Id, StringComparer.Ordinal).Where(g => g.Count() > 1).ToArray();
            if (duplicates.Length > 0) throw new ArgumentException("Duplicate authored IDs: " + string.Join("; ",
                duplicates.Select(g => g.Key + " at " + string.Join(", ", g.Select(x => x.Origin)))));
        }
        private static void Finite(Vector2 p) { Values.Finite(p.x); Values.Finite(p.y); }
    }
}
