# Shared Rules Contracts

Contract revision: **T03-r1**, 2026-09-18, with Lead extensions **r1.1 (T08)** and
**r1.2 (T09)** of 2026-09-25 listed at the end of this document. Developer verification is recorded in
[T03 evidence](evidence/T03/REPORT.md). **Lead-reviewed revision: T03-r1**;
accepted 2026-09-18 for downstream task pickup at rule-layer scope.
These contracts now transfer to Lead ownership. Changes must list
affected consumers (T04-T10) and update the fixtures before integration.

Integration follow-up: r1 has no discovery/activation command for inactive hubs.
Before T09, the Lead must publish and test that extension; consumers must not
mutate snapshots or create replacement session owners to activate a destination.
This is outside T03's enumerated command set and does not block T04/T07.

## Ownership and Assemblies

`PirateGame.Core` contains `PirateGame.Core` values/rules and
`PirateGame.Rules.Application` orchestration, with `noEngineReferences: true`.
Application's folder joins that assembly through an asmref. The namespace avoids
shadowing `UnityEngine.Application` in existing `PirateGame` code. Core source
does not reference Application. `PirateGame.Definitions` contains Unity adapters;
its Editor assembly validates authored catalogs before play/build.

Bootstrap creates exactly one `CampaignSession` and injects its references into
consumers. Hubs/UI never create session owners. The class is simulation-thread-only;
the single-owner scene invariant is integrated by T08, not proven by rule tests.
All public state graphs are immutable, including nested collection copies. Runtime
state never retains a ScriptableObject or mutable authored row.

Canonical signatures and complete field declarations:

- [Values](../Assets/_Game/Core/Values.cs): IDs, positions, enums, results.
- [Definitions](../Assets/_Game/Core/Definitions.cs): definitions and stat/loadout rules.
- [State](../Assets/_Game/Core/State.cs): campaign, expedition, entity, snapshot records.
- [Commands](../Assets/_Game/Application/CampaignSession.cs): session and validation.
- [Ports](../Assets/_Game/Application/Ports.cs): storage, arrival, time/input and capture.
- [Authored adapter](../Assets/_Game/Content/Definitions/DefinitionCatalogAsset.cs).
- [Executable fixtures](../Assets/_Game/Tests/T03/RuleFixtures.cs).

## Identity and Definitions

Definition, region, hub, slot, equipment-instance and spawn IDs are nonempty,
case-sensitive ordinal strings. Definition IDs are unique within each catalog
category; renaming an asset does not rename its explicit ID. Authored world IDs
are globally unique across regions. `AuthoredIdentity` pairs the ID with a source
path for diagnostics; duplicate validation reports all conflicting origins.
T06/T10 populate the complete world identity registry. Catalog build validation
also detects duplicates across catalog assets.

Request IDs and expedition IDs are nonempty `Guid`s. Generate a new request ID for
a new command, retain it for its retry. `EntityId.Authored(string)` identifies a
fixed world object; `EntityId.Generated(Guid expedition, string spawn)` scopes a
generated object to a voyage and stable spawn identity. Do not use scene instance
IDs. In Unity consumers use `using EntityId = PirateGame.Core.EntityId;` to avoid
the Unity type of the same name. New voyages may reset world contents; scene
reloads within the same voyage may not.

`DefinitionCatalog` indexes integer resource weights, hulls, equipment, hubs,
upgrades, unlocks, entity definitions and regions. All required references must
resolve before session creation. `Freeze()` on the authored adapter makes a new
immutable graph and throws with invalid authoring; it is not called every frame.
The Editor hook rejects invalid catalog assets before entering play or building.
No production content assets are authored in T03; the complete small fixture is
`RuleFixtures.Catalog()` for downstream isolated tests.

Every hull slot is required in revision r1. Equipment instances map to definitions;
one owned instance can fill only one compatible slot. Empty/optional slots and
rolled equipment stats require a reviewed contract extension. `health` and `cargo`
are reserved hull stat IDs; cargo is bounded by integer weighted quantities.
Other stats are named definition values. Weapon/ability behavior-specific fields
remain T05 content work; these records establish ownership, slots and modifiers.
An upgrade has one positive tier in a named track; tiers are unique/consecutive.
It requires the preceding tier and its listed unlocks, and grants unlocks and
additive stat modifiers. All purchased tiers in a track contribute their modifiers.

Stats use `clamp((base + sum(flat)) * (1 + sum(percent)), min, max)`. Percent is a
fraction. Nonfinite inputs/intermediates and unsupported operations are rejected.
Equipped items, upgrades and expedition modifiers share the calculation, while
temporary modifiers disappear on resolution. There is no repair cost.

## Queries and Commands

The following are public methods on `CampaignSession`; results are `RuleResult`.

```csharp
SessionSnapshot NewCampaign(DefinitionCatalog definitions, string homeHub,
    string hull, IReadOnlyDictionary<string, string> owned,
    IReadOnlyDictionary<string, string> loadout); // static factory
RuleResult ValidateSnapshot(DefinitionCatalog definitions, SessionSnapshot snapshot); // static
RuleResult ValidateLoadout(IReadOnlyDictionary<string, string> loadout);
IReadOnlyDictionary<string, double> ShipStats();
RuleResult Embark(Guid requestId, EmbarkPlan plan);
RuleResult CollectLoot(Guid requestId, Guid expeditionId, EntityId sourceId);
RuleResult RequestDock(Guid requestId, Guid expeditionId, string hubId);
RuleResult CompleteTick(Guid expeditionId, long tick, double damage,
    SeaPosition position, double speed);
RuleResult ResolveSink(Guid requestId, Guid expeditionId);
RuleResult PurchaseUpgrade(Guid requestId, string upgradeId);
RuleResult SetLoadout(Guid requestId, IReadOnlyDictionary<string, string> loadout);
RuleResult FastTravel(Guid requestId, string destinationHub);
RuleResult Checkpoint(Guid requestId);
RuleResult Checkpoint(Guid requestId, ExpeditionState captured);
RuleResult RetrySave();
RuleResult RetryArrival();
void SetPaused(bool value);
IReadOnlyList<TransitionCommitted> DrainEvents();
```

`Snapshot` is current authoritative state. `LastCommitted` is the last storage
acknowledgement, which may predate in-memory damage/movement ticks. `PendingSave`
exposes the immutable candidate during a frozen save failure. `Lifecycle` exposes
Departing/Resolving during a pending transition; snapshots only represent Docked
or AtSea. `InputLocked` additionally covers pause and unfinished world arrival.
New/loaded sessions start arrival-locked; call `RetryArrival()` to restore the
world before accepting commands. Read-only UI snapshots are safe to retain.

| Command | Required state | Additional checks and committed effect |
| --- | --- | --- |
| Embark | Docked | Same loadout validation as UI; new expedition ID, selected encounters/entities, full computed health, empty cargo; save start before arrival/play |
| CollectLoot | AtSea | Matching expedition/source; nonempty remaining bundle; checked weighted capacity; transfer whole bundle and empty source atomically |
| RequestDock | AtSea | Matching expedition, known activated hub; queues one intent, resolved at CompleteTick |
| ResolveSink | AtSea | Matching expedition and zero health; lose cargo/modifiers, record Sunk, return to last-safe hub; bank/ownership/tiers/unlocks retained |
| PurchaseUpgrade | Docked | Known tier, prerequisites, sufficient bank; deduct and grant in one save |
| SetLoadout | Docked | Owned, unique, compatible, complete assignments; save copy |
| FastTravel | Docked | `fast-travel` global unlock, activated destination; save current and last-safe hub together, then ensure arrival |
| Checkpoint | Stable, unlocked | Save coherent current state; no queued docking; capture overload requires same expedition/tick and accounting |

All persistent commands use one ordered writer. CollectLoot also commits in r1:
this deliberately favors an easily verified atomic rule contract over write
frequency. T08 can propose batching with equivalent guarantees after measuring it.
Rejected commands do not mutate state. CompleteTick is a simulation update;
an invalid docking zone/speed rejects the docking intent but retains that tick's
valid damage, movement and cooldown advancement.

## Simulation Boundary

T04 supplies fixed-step input and calls `CompleteTick` exactly once after every
damage producer in the tick has finished. `damage` is the finite nonnegative total
for the player for that tick, `position` is the authoritative XZ position and
`speed` is nonnegative planar speed. The first tick is 1; stale/skipped ticks reject.
`RequestDock` only queues intent, returning `IsPending=true`, never banks immediately.
CompleteTick applies damage first. At zero health it selects sinking, discarding
the dock request even if the requested hub/zone would have been valid. The dock
request ID correlates the resulting Sink event in this case. Otherwise the
position must be inside the authored hub radius and speed at/below its threshold.
The CompleteTick result also reports the queued docking result to its caller.

An outcome begins a frozen Resolving phase. Failure leaves the old committed save
intact and the exact candidate retryable. It does not resume a zero-health ship.
Old expedition callbacks and repeated request IDs reject. Resolved IDs and
committed request IDs are saved so reload cannot replay a bank/purchase effect.
Request history is unbounded in this initial prototype; retention/compaction is
a future explicit persistence policy, not an implicit expiry of idempotence.

`IGameplayClock` has `long Tick`, `double FixedDeltaSeconds` (default 0.02), and
`bool IsPaused`. Pause includes transition/arrival locks and Docked state. There
is no wall-clock delta in rules. CompleteTick advances cooldowns only on accepted
ticks. Rendering quality, frame rate and visual wave motion have no rule input.
On pause, checkpoint the frozen simulation boundary before setting the explicit
pause flag; alternatively clear that flag while an external coordinator keeps
simulation suspended. The same-thread command contract prevents interleaved ticks.

`InputIntent` is immutable: throttle/turn in [-1,1], finite XZ aim direction,
Fire/Ability/Interact booleans. Zero aim means retain previous aim; T04 normalizes
nonzero aim. Input adapters suppress intents while `InputLocked` or docked; they
own physical bindings and button edge detection. No Unity input type crosses this port.

## Save and Publication

```csharp
interface ISaveStore { RuleResult Commit(SaveCandidate candidate); }
// candidate: RequestId, ExpectedRevision, Snapshot, Command
// snapshot: Revision, Campaign, optional Expedition, CommittedRequests
```

The synchronous call is deliberate: no second write or simulation step can pass
a pending write. A store may perform asynchronous work internally but must not
acknowledge before completion. ExpectedRevision is the previous committed revision;
the candidate revision is exactly one greater, with checked arithmetic. Storage
must compare revisions, reject stale candidates, and make retries of the identical
request/revision idempotent. Reentrant commands are Busy. T03's fake proves this
protocol only; it does not implement disk durability.

A failed/throwing storage call returns SaveFailed, retains PendingSave and locks
input. RetrySave resubmits the same object. No successful event or candidate state
is published before acknowledgement. After commit, Snapshot/LastCommitted advance
and one `TransitionCommitted(RequestId, Command, Snapshot)` is queued. DrainEvents
consumes the queue, avoiding subscriber exceptions/reentrancy inside publication.
There are no global event buses. Presentation cannot award loot or upgrades.

Arrival failure is different from save failure: the transition is already saved
and its committed event is valid, but input remains locked. UI must inspect the
error and lock rather than interpreting a commit event as scene readiness.
Reload after a commit restores the saved destination/outcome without emitting
the command's success effect again. T08 implements versioned serialization,
filesystem replacement, backup/recovery and migrations; those are not T03 claims.
ValidateSnapshot returns recoverable errors for unknown/invalid content. The
constructor refuses invalid state. Never silently substitute IDs and overwrite a
save. Malformed DTOs must first pass structural validation in the storage adapter.

## Capture and Arrival Ports

```csharp
interface IEntityStatePort {
    EntityId Id { get; }
    EntityState Capture();
    RuleResult Restore(EntityState state);
}
interface IExpeditionCapture {
    ExpeditionState Capture(Guid expeditionId, long tick);
    RuleResult Restore(ExpeditionState state);
}
interface IWorldArrival { RuleResult EnsureReady(ArrivalRequest request); }
```

EntityState contains stable identity, definition ID, region/XZ position, health,
defeat state, remaining loot, cooldowns and an explicit behavior-state string.
ExpeditionState also carries seed, complete RNG state, tick, player health/speed/
position, cargo, temporary modifiers, player cooldowns, chosen encounter IDs and
the complete entity ledger including unloaded regions. These are data, not scene
objects or deterministic physics replay promises. T05/T06 implement capture and
restore; T08 coordinates a frozen boundary, and T10 retains unloaded state.

The captured checkpoint overload accepts updated cooldowns, modifiers, RNG,
enemy state and newly discovered entities. It requires the same current tick,
player position/health/speed, seed and cargo. Existing entities cannot disappear,
change definition, refill/change remaining loot, or resurrect a defeated entity;
chosen encounters cannot change or disappear. This prevents a stale scene capture
from overriding the application's accounting. Capture must be atomic on the
simulation thread; Restore must validate identity and finish before resuming it.

ArrivalRequest contains request/revision identity, destination region/XZ position,
hub ID and optional expedition ID. EnsureReady is idempotent for that identity and
restores/repositions the existing player, including required collision and entity
state, before success. It never creates a second campaign owner. Failure or a
pending response keeps the lock and returns ArrivalFailed; RetryArrival uses the
same request without saving or publishing again. Freshly loaded sessions issue a
new arrival request for the saved location. Scene loading itself belongs to T08/T09.

## Errors

`RuleResult` exposes `Error`, `IsSuccess`, `IsPending`, and diagnostic `Detail`.
Only queued docking uses a successful pending result; callers await CompleteTick.
Ports must return non-pending success only when their operation is complete.

| Error | Meaning |
| --- | --- |
| InvalidRequest | Empty request, malformed values/capture, or retry with nothing pending |
| WrongLifecycle / Busy / Paused | Command cannot run in the current lifecycle/lock |
| UnknownId / WrongExpedition | Unknown required content/entity or stale/foreign active voyage |
| DuplicateRequest / AlreadyResolved | Replayed request or resolved voyage identity |
| Depleted / CargoFull / Overflow | No remaining loot, indivisible bundle exceeds capacity, or checked accounting overflow |
| InsufficientBank / PrerequisiteMissing / TierAlreadyOwned | Invalid purchase |
| InvalidLoadout | Missing/unknown slot, unowned instance, duplicate instance or incompatible kind |
| NotInDockZone / TooFast / HubInactive | Dock/travel eligibility failure |
| TravelLocked / NotSunk / StaleTick | Missing global unlock, nonlethal sink callback or invalid simulation boundary |
| SaveFailed / ArrivalFailed | Frozen retryable storage failure / saved destination not yet ready |

Errors are prioritized by lock, request replay, lifecycle, then command-specific
validation. A repeated old callback while Docked may report WrongLifecycle rather
than AlreadyResolved; neither applies effects. Construction/authoring uses argument
exceptions for invalid data; application commands return structured errors.

## Lead Extensions 2026-09-25

Backwards-compatible additions; existing consumers and fixtures are unchanged
(r1.3 for T10 is listed at the end).
Affected consumers were re-tested (T03-T09 suites, 141 tests).

### r1.1 (T08 integration)

- `bool CampaignSession.ArrivalPending`, `bool ExplicitlyPaused`: read-only queries so
  composition can distinguish a committed-but-not-arrived transition from a pause.
- Persistence implements `ISaveStore` as `PirateGame.Persistence.JsonSaveStore`
  (schema-1 envelope, `SaveMapper`, backup recovery); see the T08 report.
- Generated salvage: a defeated enemy may add a `wreck` entity with ID
  `EntityId.Generated(voyage, "wreck/<enemy spawn id>")` through the captured
  `Checkpoint` overload (new entities are allowed there). Loot comes from
  `EnemyRow.wreckLoot`. Salvage views recreate generated entries from the ledger.
- Coherence rule for composition: take `CombatWorld.Capture` and commit
  `Checkpoint(request, captured)` at the same idle boundary immediately before
  `CollectLoot`/`ActivateHub`, so each at-sea save describes one simulation instant.

### r1.2 (T09 hubs)

```csharp
RuleResult ActivateHub(Guid requestId, Guid expeditionId, string hubId);
```

| Command | Required state | Additional checks and committed effect |
| --- | --- | --- |
| ActivateHub | AtSea | Matching expedition, no queued docking, known hub not yet activated (`AlreadyActivated`), position inside the hub's docking zone (`NotInDockZone`); commits discovered + activated in one save; does not dock, bank or change current/last-safe hub |

`RuleError.AlreadyActivated` is appended to the enum. Regions may author further
harbors (`FirstRegionAsset.outposts`); every region hub must exist in the catalog
with identical dock data, which `FirstRegionAsset.Validate` enforces.

### r1.3 (T10 regions)

- `ShipSimulation.RegionOf : Func<Vector3, string>` (optional): the region ID
  published with each tick's position. Unset keeps the voyage's current region.
- `CombatWorld.Attach(ICombatEnemy)` / `Detach(EntityId)` at idle boundaries only.
  Take a captured checkpoint before detaching so the ledger holds the ship's last
  state; detaching retires that ship's in-flight shots (`SweptProjectile.Retire`).
- `IWorldArrival.EnsureReady` may return a successful *pending* result while the
  destination region loads; the session treats it as `ArrivalFailed`, keeps input
  locked and the composition calls `RetryArrival` when the regions are ready.
- A region's enemies keep the region ID of their spawn in their `EntityState`
  position; generated wrecks take the region of the water they lie in.
