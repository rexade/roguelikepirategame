# T09 Report: Shared Hubs and Fast Travel

Task: [T09](../../tasks/T09-shared-hubs-travel.md). Implemented 2026-09-25 by the
Lead (Claude Opus 5.5) on top of the T08 integration (commit `3a9a816` and the
harbor layout follow-up). Status: **review**; owner play/visual review pending.

## Contract Extension (Lead-owned)

T03-r1 had no way to activate a hub. Added, with tests, before any consumer:

```csharp
RuleResult ActivateHub(Guid requestId, Guid expeditionId, string hubId);
// RuleError.AlreadyActivated (appended to the enum)
```

| Required state | Checks and committed effect |
| --- | --- |
| AtSea | Matching expedition, no queued docking, known hub not yet active, authoritative position inside the hub's authored docking zone (same ellipse test as docking). Commits discovered + activated in one save. Does not dock, bank, or change the current/last-safe hub. |

Rationale: "raise your flag in the berth" is an explicit, validated world action;
docking keeps its INV-06 requirement of an activated hub. Hub state was already
part of the saved campaign; `CampaignDraft` now copies it.

## Delivered Behaviour

- Second harbor **Saltmarsh Harbor** (`saltmarsh-harbor`) in the first region, dock
  (58, 124), authored in `FirstRegionAsset.outposts` with display names and
  mooring headings; the production catalog registers every region hub.
- Sailing near an unclaimed harbor shows "Land ho!"; in its berth the prompt
  reads "[E] Raise your flag at Saltmarsh Harbor and moor". One press takes a
  coherent checkpoint, commits `ActivateHub`, then runs the normal assisted docking.
- Both harbors read the same campaign: bank, upgrade tiers and unlocks are shared
  (one session, one save). The harbor screen title follows the current hub.
- **Navigator's Charts** (6 wood, 3 iron) grants the `fast-travel` unlock. The
  harbor screen's Fast travel column lists claimed harbors ("Travel to …"),
  disabled until unlocked, and shows unclaimed ones as uncharted.
- Travel commits current + last-safe hub together, then arrival places the ship at
  the destination berth. A failed arrival keeps input locked with a Retry card.
- The HUD marker points to the nearest claimed harbor, labelled with its name.

## Acceptance Checks

| Check | Result | Evidence |
| --- | --- | --- |
| Both hubs show identical shared progression; activation stays local | Passed | EditMode `AC07_ProgressIsSharedAcrossHubsAndTravelMovesTheSafeHub`, `NewCampaignsKnowOnlyTheirHomeHub`; PlayMode `ClaimSaltmarshDockThereAndFastTravelHome` (bank unchanged by travel) |
| Travel at sea or to inactive hubs rejected without mutation (AC-08) | Passed | EditMode `AC08_TravelIsRejectedAtSeaLockedOrToInactiveHubs` |
| Save/reload keeps destination and safe hub; failed arrival never resumes incomplete-world input (AC-18) | Passed | EditMode `AC18_FailedDestinationLoadKeepsInputLockedUntilRetry` (same request retried, one publication); PlayMode reload after travel |
| Activation rules | Passed | `ActivatingInsideTheBerthCommitsDiscoveryWithoutDocking`, `ActivationRejectsOutsideTheBerthRepeatsAndDockedShips`, `DockingNeedsActivationAndThenMakesTheOutpostSafe`, `SinkingReturnsToTheLastDockedHubEvenAfterActivatingAnother` |
| Standalone | Passed (automated tour) | `docs/evidence/T09/captures-r2/13-land-ho.png` … `16-outpost-harbor.png`; `capture.txt`: "outpost docked saltmarsh-harbor" |

Scope note: T09 places both hubs in one region, as its packet specifies.
Cross-region travel belongs to T11 after T10's region loading.

## Known Limitations

- No map screen; discovery is by sight ("Land ho!") and the berth ring.
- Owner review of the new harbor art and the claim/travel flow is pending.
