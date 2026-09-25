# Project Invariants

Status: specification only. No implementation or verification has passed yet.

These rules govern the first playable slice. CONTEXT.md distinguishes user
requirements from proposed design defaults. Rules about death, cargo, and controls
formalize those defaults; they remain open to deliberate design changes. Update
affected tasks and checks when changing a rule, and record the design reason.

Invariants hold for valid authoritative states and transitions. Visual and
performance gates are acceptance criteria, not mathematical guarantees.

## State and Lifecycle

### INV-01: One campaign owner

Exactly one campaign owner exists per session. Hubs, UI, and scene objects query
it or issue commands. They do not hold independently writable copies of bank,
ownership, or unlocks. Scene loading cannot create a second owner. Read-only
snapshots are allowed.

### INV-02: One expedition and one outcome

Docked has no active expedition; AtSea has exactly one, with a unique ID. Departing
and Resolving are input-locked transitions. An expedition commits Docked or Sunk
exactly once, never both. Old or repeated callbacks cannot resolve it again.
Process damage before docking requests in a simulation tick: zero health selects
sinking. Once resolution starts, stop expedition simulation. A failed save leaves
the pending outcome frozen and retryable, not a dead ship in active play.

### INV-03: Validate before mutation

Commands validate lifecycle, identity, ownership, prerequisites, and quantities
against authoritative state. Rejection leaves state unchanged and returns a reason.
Only one persistent transition is in flight. Publish success after save commit.
Order checkpoints and transition writes so an older snapshot cannot overwrite a
newer committed transition.

## Resources and Progression

### INV-04: Explicit resource accounting

Quantities and costs are nonnegative integers with checked arithmetic. Cargo and
bank are separate ledgers. Salvage credits cargo at sea. Purchases cannot overdraw
the bank. Every resource change has an explicit source or sink: loot, banking,
purchase, or documented death loss. Generated loot need not obey global conservation.

### INV-05: Indivisible pickup transfers

Each source has a stable ID and remaining contents. For the slice, transfer its
entire resource bundle only when it fits cargo; otherwise reject without changing
source or cargo. Capacity usage is sum(quantity * positive integer unit weight).
Transfer and depletion happen in one authoritative update. Duplicate interaction
callbacks cannot credit the same contents twice. Partial transfers are deferred.

### INV-06: Docking banks once

Docking requires an alive ship in an activated hub's docking zone at an allowed
speed; zone and speed thresholds are authored data. Commit complete cargo banking,
expedition/temporary-bonus removal, resolved expedition ID, and current/last-safe
hub together. Overflow or failed validation cannot partially bank resources.

### INV-07: Sinking preserves a playable campaign

Sinking removes cargo and temporary bonuses, not banked resources, owned equipment,
shared tiers, or committed unlocks. Return to the last-safe hub with a usable ship
and loadout and a free path to embark. Repair debt cannot create a progression dead
end. New campaigns always have a valid starter ship and activated home hub.

### INV-08: Shared hub progression

Bank, upgrade tiers, and unlocks belong to the campaign. Buying an upgrade at one
hub affects all hubs. Discovery, activation, and local story flags are hub-specific.
Cost deduction and upgrade grant are atomic; a tier cannot be purchased twice.
Unlocks do not disappear when an expedition ends or a region unloads.

### INV-09: Travel cannot bypass expedition risk

Fast travel requires Docked, no expedition, the global travel unlock, and an
activated destination. Destination/current hub/last-safe hub commit together.
Invalid travel changes nothing. Input remains locked until destination loading
finishes. Failure offers retry/reload without spawning in an incomplete scene.

## World and Content

### INV-10: Stable geography

Island/hub positions, region connections, and authored navigation geometry are
fixed for a content version. Randomness selects encounters and loot, not island
layout. Resume restores selected encounters, outcomes, and RNG state; a seed alone
does not reconstruct the voyage.

### INV-11: Scene lifetime is not entity lifetime

Authored IDs are globally unique and stable. Generated IDs are scoped by expedition
and spawn identity. Reloading a region or save cannot resurrect defeated enemies
or refill loot within an expedition. New expeditions may reset expedition entities,
but not campaign flags. Collision must be ready before entering a region.

### INV-12: Immutable definitions and valid references

Runtime state never modifies authored definitions. Required definition IDs resolve
uniquely; duplicates/missing references fail validation before play/build. Saves
contain IDs and values, never scene references. Asset renames preserve identity.
Removing saved content requires migration or a recoverable load error, never
silent substitution followed by overwriting the original save.

## Ships and Combat

### INV-13: Owned, compatible loadouts

Every equipped instance is owned and slot-compatible; one instance cannot occupy
multiple slots. Loadouts change only while docked. UI and embark share validation.
Stats are finite and use clamp((base + sum(flat)) * (1 + sum(percent))) with
definition-specific limits. Percent values are fractions, e.g. 0.10. Temporary and
persistent modifiers share calculation but retain distinct lifetimes. Unsupported
modifier operations are rejected.

### INV-14: Presentation cannot change simulation

Ship bodies translate on XZ and rotate in yaw. Bobbing affects child models only.
Water quality, shadows, particles, camera settings, and render frame rate do not
determine damage, cooldowns, capacity, or hazard strength. Currents/hazards are
explicit gameplay data. Cooldowns use simulation time and stop when simulation
pauses. This does not promise bit-identical physics across machines.

### INV-15: Bounded combat effects

A non-piercing projectile applies damage at most once before retirement. Multi-hit
abilities explicitly define per-target hit intervals/limits; duplicate callbacks
are not extra hits. Health stays between zero and maximum; death resolves once.
Pool reuse resets hit history, ownership, timers, and subscriptions. Fast shots
use swept collision checks.

## Persistence and Dependencies

### INV-16: Coherent save snapshots

Capture campaign and expedition at one simulation boundary: cargo, health, location,
cooldowns, modifiers, RNG, loot depletion, enemy state, and encounters all describe
that boundary. Isolate the snapshot from later mutations before asynchronous writes.
Serialize only stable Docked or AtSea states; transition candidates serialize their
resulting stable state. Resume cannot refill health, reset cooldowns, or restore loot.

### INV-17: Recoverable save failures

Prepare transition candidates without publishing them. Write/validate a same-volume
temporary file, replace the main save, then publish. Retain a validated previous
backup. Failure preserves the last committed snapshot and retries the same pending
transition. A crash after replacement but before publication loads the new state
without repeating effects. Preserve/report invalid or unsupported saves.
Interrupted checkpoints may roll back to the last valid snapshot. This is local
crash recovery, not anti-cheat or zero-data-loss protection. Test filesystem failure
boundaries on target hardware rather than assuming replacement is sufficient.

### INV-18: Rules independent of rendering

Core and Application have no Unity/HDRP imports. UI issues application commands;
presentation consumes state/events. Persistence implements the application storage
contract; Bootstrap wires implementations. Shared rule changes have one owner and
updated contracts. Visual effects never award loot or unlocks.

## Acceptance Gates

### GATE-01: Ocean quality at gameplay scale

Before production gameplay, review moving gameplay-camera footage with simple ship
and island art, shore foam, wakes, shadows, and shallow/deep contrast. Daylight,
dusk, and rough water preserve ship and attack-marker readability. The game owner
accepts appearance; the lead records defects. Another camera's screenshot is not proof.

### GATE-02: Measured performance

Record revision, engine/packages, hardware, resolution, settings, warmup, and route.
T02/T11 use standalone frame-time distributions, CPU/GPU timings, memory, and stalls.
Targets stay provisional until T00 records agreed hardware/budget. Editor FPS and
averages alone cannot establish a pass.
Owner D02 approves initial prototype targets, not release requirements. Misses
require measurements/bottleneck analysis and a reviewed disposition, not automatic
quality reduction. Accepting a limitation does not mean a numeric target was met.

### GATE-03: Practical asset production

Demonstrate reusable simple ship/island materials and procedural ship motion without
skeletal character animation. Record creation steps and effort so the game owner
can judge feasibility. Unaffordable per-asset work does not establish a usable style.
Owner D04 makes the 2-hour ship/4-hour island figures workflow targets, not hard
product requirements. Overruns trigger analysis/revision, not automatic game failure.
D05's initial camera may change with visual/gameplay evidence.
