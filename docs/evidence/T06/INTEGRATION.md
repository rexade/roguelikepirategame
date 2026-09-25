# T06-r1 integration handoff

T06-R1 developer revision uses `Builds/T06-R1/Salvage.exe`. Its HUD treats a
null LastResult as neutral (no interaction yet); SalvageInteraction's public
result semantics are unchanged. Cargo still comes from the session snapshot.
See `R1/BEFORE.md` and the appended R1 report for verification.

The authored region is `Assets/_Game/Scenes/Regions/FirstRegion.unity`.
It contains geography, collision, encounter markers and a SalvageRegion adapter;
it does not create a campaign, player, water system or tick publisher.
The identical composition is available as `Prefabs/World/FirstRegion.prefab`.
Load one copy, not both. The isolated playable scene is
`Assets/_Game/Scenes/Tests/T06/Salvage.unity`.

## Lead-owned integration proposal

No shared definition, assembly, input asset, ship prefab, Bootstrap or production
composition was edited. T08 and T10 are the affected consumers of this proposal:

1. Merge the existing `FirstRegionAsset.Home` hub record into the production
   DefinitionCatalog: `home-harbor`, region `first-region`, XZ `(0,-12)`, radius
   `5`, maximum speed `1`. Register entity definitions `barrel`, `wreck` and
   resource weights `wood=1`, `iron=2`. Preserve the production hull/loadout.
   T06's test-only catalog uses a 10-unit hold and an unequipped starter hull.
2. Register `FirstRegionAsset.Identities(assetPath)` with all other authored
   identities. The T06 editor hook scans every FirstRegionAsset and diagnoses
   duplicate IDs with both asset paths and row indices before play/build.
   Later region authoring types must join the same global registry in T10.
3. On a NEW expedition only, seed its ledger from the authored salvage rows'
   `Initial(regionId)`. Existing voyages must use their complete saved entity
   ledger. Encounter marker IDs and positions are provided; no enemies or
   random encounter selections are instantiated by this task.
4. Inject the existing CampaignSession into `SalvageRegion.Recreate(session)`
   while simulation is stopped at a completed boundary. It validates every
   expected source before replacing any objects. Missing/incompatible records
   return InvalidRequest rather than refilling them. Inactive objects are removed
   at the end of the frame; their replacements use the same explicit IDs.
5. Add `SalvageInteraction` to the composed player, bind the existing
   ShipSimulation, and assign `region.Sources`. Its TickStarted subscription
   consumes the accepted Interact edge (E) and chooses the closest nondepleted
   source within 5 horizontal metres of the interaction hardpoint. Pickup uses
   ShipSimulation.CollectLoot; pending only means queued. No trigger grants loot.
6. Capture each source through IEntityStatePort or `SalvageRegion.Capture()`.
   These query authoritative entity records, never a stale scene copy. Merge
   these records with the full expedition including unloaded entities; do not
   replace the ledger with the region subset. Restore the session snapshot first,
   then recreate views. Restore on an existing source accepts that session's
   record only. It cannot mutate campaign state or refill cargo sources.
7. Use ShipSimulation.SetPaused and finish any retained step before capture,
   destruction or scene transfer. SaveFailed retains the candidate and source;
   RetrySave commits that same candidate. Keep the simulation locked throughout.
   The integration event consumer alone drains committed events for success VFX.

## Route and reproduction

Open the T06 Salvage scene and enter Play, or run `Builds/T06/Salvage.exe`.
The isolated fixture starts an expedition automatically and uses memory-only
storage. It does not establish filesystem durability, streaming or progression.

- W/S sail; A/D turn; Space brakes; E collects within range.
- Escape pauses through the ship adapter. R recreates sources at an idle boundary.
- F requests docking; the rules enforce the home zone, speed and damage ordering.
- Enter retries an injected failed save (tests inject failure; normal fixture
  storage succeeds). Relaunching starts a new test expedition.
- Collect barrel `first:barrel-01` at `(0,8)`: 3 wood. The next barrel at `(0,38)`
  adds 2 wood. The wreck at `(0,82)` weighs 6, so the 10-unit hold rejects it after
  those two barrels, displays Cargo full, and leaves the wreck visible/intact.
- Recreate with R after a successful pickup: it remains depleted. For wreck
  recreation specifically, take the first wreck before collecting barrels.

Authored outward/return waypoints (XZ): `(0,-12)`, `(0,16)`, `(0,48)`, `(0,85)`,
`(12,112)`, `(45,110)`, `(48,45)`, `(25,-12)`, `(0,-12)`.
Brake in the home docking zone before F. Island centers are `(-20,20)`, `(25,65)`,
and `(-18,108)`; the home landmass is at `(-8,-33)`. Positions and IDs are content,
not generated from a seed or asset name. The route test sweeps a 3-metre radius
along every segment and checks island collision; this encloses the starter hull.

The barrel/wreck prefabs are reusable visuals with an unbound SalvageSource.
Only a ledger-bound instance is a valid source. Do not assign a prefab-instance
name, Unity instance ID or asset GUID as the gameplay ID. Asset renames must
retain their metadata and the explicit IDs in FirstRegion.asset.
