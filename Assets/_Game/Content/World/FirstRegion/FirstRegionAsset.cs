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
    [Serializable] public sealed class EncounterSite
    {
        public string id;
        public Vector2 position;
    }

    [CreateAssetMenu(menuName = "Pirate Game/First Region")]
    public sealed class FirstRegionAsset : ScriptableObject
    {
        public string regionId = "first-region", homeId = "home-harbor";
        public Vector2 dock = new Vector2(0, -12);
        public float dockRadius = 5, dockMaximumSpeed = 1;
        public IslandSite[] islands = Array.Empty<IslandSite>();
        public SalvageSite[] salvage = Array.Empty<SalvageSite>();
        public EncounterSite[] encounters = Array.Empty<EncounterSite>();
        public Vector2[] route = Array.Empty<Vector2>();
        public HubDefinition Home => new HubDefinition(homeId, new SeaPosition(regionId, dock.x, dock.y), dockRadius, dockMaximumSpeed);

        public IEnumerable<AuthoredIdentity> Identities(string origin)
        {
            yield return new AuthoredIdentity(homeId, origin + "/harbor");
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
            foreach (var site in encounters) Finite(site.position);
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
