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
        public static bool IsSalvage(string definitionId) => definitionId == "barrel" || definitionId == "wreck";

        // Authored salvage must exist in the ledger; generated salvage (for example
        // wrecks left by defeated ships) is recreated from whatever the ledger holds.
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
            states.AddRange(expedition.Entities.Values.Where(e => e.Id.AuthoredId == null && IsSalvage(e.DefinitionId) &&
                    e.Position.RegionId == content.regionId)
                .OrderBy(e => e.Id.SpawnId, StringComparer.Ordinal));
            Clear();
            Sources = states.Select(state => {
                var prefab = state.DefinitionId == "wreck" ? wreckPrefab : barrelPrefab;
                var instance = Instantiate(prefab, transform);
                instance.name = state.Id.AuthoredId ?? state.Id.SpawnId;
                var source = instance.GetComponent<SalvageSource>();
                source.Bind(session, state); return source;
            }).ToArray();
            return new RuleResult();
        }

        // Adds a view for one generated ledger entry without rebuilding the others.
        public SalvageSource Add(CampaignSession session, EntityState state)
        {
            if (state == null || !IsSalvage(state.DefinitionId)) throw new ArgumentException("Not a salvage entity.");
            var instance = Instantiate(state.DefinitionId == "wreck" ? wreckPrefab : barrelPrefab, transform);
            instance.name = state.Id.AuthoredId ?? state.Id.SpawnId;
            var source = instance.GetComponent<SalvageSource>();
            source.Bind(session, state);
            Sources = Sources.Concat(new[] { source }).ToArray();
            return source;
        }

        public void Clear()
        {
            foreach (var source in Sources) if (source != null) { source.gameObject.SetActive(false); Destroy(source.gameObject); }
            Sources = Array.Empty<SalvageSource>();
        }
    }
}
