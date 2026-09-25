# T10 Report: Region Continuity and Voyage Variation

Task: [T10](../../tasks/T10-region-streaming.md). Implemented 2026-09-25 by the Lead
(Claude Opus 5.5), commit `75093d8` plus the capture follow-up `f487957`.
Status: **review**; owner play/visual review pending.

## Delivered Behaviour

- **Galewater Reach** (`galewater-reach`), a second region north of Homeward Reach
  (z 165–400): four islands, iron-rich salvage (six sites), three encounter sites
  with variable sizes, and **Stormwatch Harbor** to claim. Content lives in
  `Content/World/Galewater/GalewaterReach.asset` (the `FirstRegionAsset` type now
  describes any region: bounds, display name, additive scene, weighted encounter
  table, `minShips..ships` encounter sizes).
- **Corsair** enemy (140 hull, fast-reloading cannon, pursues to 14 m, wreck
  4 wood 4 iron) appears only in Galewater's table (raider 3 : gunner 3 : corsair 4).
  Homeward keeps raiders/gunners and its fixed 1 + 2 ship encounters.
- **Streaming** (`Gameplay/World/Streaming/RegionStreamer`): each region's art
  and collision is an additive scene (`Scenes/Regions/HomewardReach.unity`,
  `GalewaterReach.unity`). Regions within 70 m of the ship load ahead; regions
  more than 110 m away are retired. Before retiring, the director captures every
  ship of that region into the session ledger with a coherent checkpoint, then
  detaches and destroys them; returning re-populates from the ledger. A region
  keeps being loaded while one of its un-sunk ships is within 60 m of the player.
- **Readiness**: if the ship reaches a region whose collision is not ready, the
  simulation is held at the edge (at most one tick past the border) and released
  when the region is ready. Arrivals (resume, sinking, docking, travel) return a
  pending result until their regions are loaded and are retried through the
  existing `RetryArrival` contract, so no world is restored without collision.
- **Rules adapter**: `ShipSimulation.RegionOf` publishes the region of each tick's
  position, so docking zones, hubs and saves carry the right region.
  `CombatWorld.Attach/Detach` add or remove streamed ships at idle boundaries; a
  departing ship's in-flight shots are retired. On resume, shots whose owner is
  outside the loaded world are dropped before `CombatWorld.Restore`.
- **Variation**: the voyage seed draws each region's encounters from its table
  (types and, where authored, counts). Chosen spawns are recorded in the voyage's
  `Encounters`; the RNG state is saved. Islands, harbors and salvage never move.

## Acceptance Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Crossing, returning and save/reload do not duplicate loot/enemies | Passed | PlayMode `LeavingAndReturningKeepsGalewaterStateWithoutDuplicates` (wounded corsair/gunner keeps its damage, salvaged barrel stays empty, cargo credited once, exactly one ship per ledger entry); `ResumeInsideGalewaterWaitsForTheRegionAndRestoresIt`; `ApproachingGalewaterStreamsItAndCrossingChangesTheVoyageRegion` |
| New expeditions vary encounters without moving islands; campaign flags survive resets | Passed | EditMode `SameSeedSamePlanDifferentSeedsVaryEncounters`, `GeographyAndSalvageNeverMoveBetweenVoyages`, `EncounterTablesAndCountsAreRespectedPerRegion`; hub activation persists across voyages (T09 tests) |
| Duplicate IDs fail validation (AC-15 across regions) | Passed | EditMode `AC15_DuplicateIdAcrossRegionsNamesBothOrigins` (both asset origins named); build/play hooks scan every region asset; `RegionsTileWithoutOverlapAndOwnTheirContent` |
| Region-readiness failure handled without entering incomplete collision | Passed | PlayMode `UnreadyRegionHoldsTheShipAtItsEdge` (blocked region: ship held ≤ 1 tick past the border, no ticks while held, released after retry) |
| Streaming stalls recorded | Recorded (standalone, one run) | `docs/evidence/T10/captures/capture.txt`: Homeward ready in 1087 ms at startup (first load, includes asset warm-up, arrival waits for it), Galewater ready in 12.8 ms while streaming at sea; Homeward unloaded once far behind. Frame-time impact is measured in T11. |

Suite totals after T10: EditMode 69/69, PlayMode 81/81.

Standalone tour screenshots (not in Git): `docs/evidence/T10/captures/19-…23-*.png`
(Galewater border, corsairs, Stormwatch approach, claim and harbor screen).

## Known Limitations

- Region boundaries are straight lines between authored rectangles; beyond the
  authored map the nearest region owns the water (no world edge yet).
- Only two regions; cross-region fast travel works (Stormwatch ↔ Homeward) but a
  dedicated cross-region travel test belongs to T11.
- The first region load at startup takes about a second (hidden behind the
  arrival lock); later streaming loads were measured in milliseconds on this PC.
