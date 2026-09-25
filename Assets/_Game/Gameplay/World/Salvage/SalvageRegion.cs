using System;
using System.Collections.Generic;
using System.Linq;
using PirateGame.Content.World;
using PirateGame.Core;
using PirateGame.Rules.Application;
using UnityEngine;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Gameplay.World
{
    public sealed class SalvageRegion : MonoBehaviour
    {
        public FirstRegionAsset content;
        public GameObject barrelPrefab, wreckPrefab;
        public SalvageSource[] Sources { get; private set; } = Array.Empty<SalvageSource>();

        public EntityState[] Capture() => Sources.Select(s => s.Capture()).ToArray();
        public RuleResult Recreate(CampaignSession session)
        {
            var expedition = session.Snapshot.Expedition;
            if (expedition == null) return new RuleResult(RuleError.WrongLifecycle);
            var states = new List<EntityState>();
            // Validate the entire set before replacing any objects. Missing state is
            // a recoverable integration error, never permission to seed fresh loot.
            foreach (var site in content.salvage)
            {
                if (!expedition.Entities.TryGetValue(EntityId.Authored(site.id), out var state) ||
                    state.DefinitionId != site.definitionId || state.Position.RegionId != content.regionId ||
                    state.Position.X != site.position.x || state.Position.Z != site.position.y)
                    return new RuleResult(RuleError.InvalidRequest, detail: "Missing or incompatible salvage: " + site.id);
                states.Add(state);
            }
            foreach (var source in Sources) if (source != null) { source.gameObject.SetActive(false); Destroy(source.gameObject); }
            Sources = states.Select(state => {
                var prefab = state.DefinitionId == "wreck" ? wreckPrefab : barrelPrefab;
                var instance = Instantiate(prefab, transform);
                instance.name = state.Id.AuthoredId;
                var source = instance.GetComponent<SalvageSource>();
                source.Bind(session, state); return source;
            }).ToArray();
            return new RuleResult();
        }
    }
}
