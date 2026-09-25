# Technical Architecture

## Scope and principles

This document specifies the gameplay architecture. T01's engine/bootstrap fixture
is implemented and accepted; the gameplay systems below remain planned.
Unity 6 LTS/HDRP is selected under the owner's explicit delegation of D07.
ADR 001 records the decision; production suitability awaits T02 evidence.
The behavioral contract is [INVARIANTS.md](INVARIANTS.md). Its numbered rules
resolve edge cases and are mapped to implementation tasks in [TASKS.md](TASKS.md).
Use ordinary Unity components and small C# classes, engine physics, and the engine
water system. Avoid ECS, networking, a general ability scripting language, custom
physics, and speculative framework work for the first slice.

Separate persistent state, expedition state, and visual presentation. Keep rules
that govern resources, unlocks, equipment, and expedition outcomes testable without
loading a rendered scene. Do not duplicate the engine's movement/physics system
in a separate simulation framework.

## Module boundaries

See [DEPENDENCIES.md](DEPENDENCIES.md) for allowed references, contract ownership,
package selection responsibilities, and task handoffs.

| Module | Owns | Allowed dependencies |
| --- | --- | --- |
| Core | IDs, state records, economy, loadout rules, outcome rules | Standard C# only |
| Application | Expedition lifecycle, commands, save coordination | Core |
| Content | Authored definitions and conversion into rule data | Core, Unity |
| Gameplay | Ship motor, physics, AI, combat, world interaction | Application, Core, Content, Unity |
| Presentation | Water, camera, model animation, VFX, audio | Gameplay read state/events, Core payload types, Unity/HDRP |
| UI | HUD, harbor, loadout, map views | Application queries/commands, Core query types, Unity UI |
| Persistence | Save DTOs, validation, migrations, file storage | Application save contract, Core |
| Bootstrap | Scene composition and concrete dependency wiring | All modules |

Dependency arrows point from consumer to dependency. Core never imports Unity or
HDRP. Application declares its storage interface; Persistence implements it.
Gameplay emits small typed events such as DamageApplied and LootCollected;
Presentation consumes them without owning gameplay outcomes. Use direct references
and scoped event subscriptions, not a global event bus or service locator.

Start with Core, Runtime, and test assemblies. The table defines logical ownership;
do not create a separate assembly or interface for every row without a need.

Proposed layout under Assets/_Game/: Core/, Application/, Content/, Gameplay/,
Presentation/, UI/, Persistence/, Bootstrap/, Scenes/, and Tests/.
Third-party packages live outside that root and keep their original files.

## Scenes and world

- Bootstrap: one campaign/session owner, persistence service, and scene coordinator.
- OceanWorld: persistent ocean, lighting, camera, player, and active region manager.
- Region scenes: authored islands, collision, ports, encounter markers, and decor.
- Harbor UI: overlays the world while docked; no separate copy of campaign state.
- Dedicated visual and gameplay test scenes remain separate from authored content.

Build one region initially. Add additive neighboring region loading when traversal
or profiling requires it. Geography and authored object IDs stay fixed. Persist
region mutations as data; unloading a scene must not restore looted wrecks or
respawn killed enemies within the same expedition. Load ahead of travel, prevent
entry into unloaded collision, and retire old scenes only after state is captured.

A distant map is a simplified presentation of authored geography, not all regions
running simultaneously. Use regional encounter tables and seeded random selection
for voyage variation. Store chosen encounters and their outcomes; physics is not
deterministic and a seed alone is not a complete save.

## Ship movement and water

Use Unity 3D physics at an initial 50 Hz fixed timestep with interpolation. Keep
the gameplay body constrained to XZ translation and yaw. The engine resolves hull
collision; tune acceleration, turn rate, drag, and braking for approachable play.

Put the visible ship beneath a presentation transform. Bob, pitch, and roll that
child from water samples or bounded procedural motion. Wave visuals must not move
the collision body, change weapon aim, or cause seasickness. Weapons resolve from
stable gameplay hardpoints; muzzle flashes follow the visible ship.

Ocean rendering owns waves, foam, wake visuals, and reflections. Region gameplay
owns currents and storm hazards as explicit fields. Wave-rendering quality changes
must not change damage or travel difficulty. Do not synchronously read back GPU
water data every frame. Budget sampling and fall back to bounded visual bobbing.

Start with one directional light, controlled local lights, reflection probes, and
limited post-processing. Evaluate reflections and transparent effects from the
actual camera, including off-screen reflection gaps and shoreline intersections.
No ray tracing dependency. HUD and attack indicators must remain readable in glare.

## Combat and equipment

Author hulls, weapons, abilities, enemies, loot tables, and upgrades as immutable
ScriptableObject definitions with explicit stable IDs. Never mutate those assets
to hold health, cooldowns, quantities, or campaign purchases.

- Hull definition: movement stats, health, cargo capacity, allowed slot layout.
- Weapon definition: targeting mode, range, cooldown, damage, projectile/effect ID.
- Ability definition: activation requirements, cooldown, and a supported effect.
- Runtime equipment instance: definition ID, instance ID when needed, rolled stats.
- Loadout: slot-to-owned-instance assignments, validated by the shared rule layer.

Implement a small set of explicit behaviors: projectile attack, short dash, and
defensive effect are sufficient candidates. Add behaviors when content needs them.
Use the INV-13 formula: clamp((base + sum(flat)) * (1 + sum(percent))).
Persistent equipment and temporary expedition bonuses use the same stat
calculation without sharing lifetime or ownership.

Use engine collision queries with swept projectile checks for fast shots. Pool
projectiles/VFX where profiling warrants it. Hit resolution records one damage
application per valid hit; pooled objects reset subscriptions and state.

Enemy ships use a small state machine and the same motor/weapon components where
practical. Authored sea lanes and engine obstacle casts are sufficient initially;
evaluate an established navigation solution if island avoidance exceeds this.

## State and transactions

| State | Stored information | Lifetime |
| --- | --- | --- |
| CampaignState | Bank, shared hub tiers, unlocks, owned equipment, hub activation, story flags | Persistent |
| ExpeditionState | ID, seed/RNG state, cargo, health, temporary modifiers, selected encounters, entity outcomes, location | Until resolved; resumable |
| PresentationState | Wake particles, camera interpolation, transient sounds | Reconstructed |

Lifecycle: Docked -> Departing -> AtSea -> Resolving -> Docked. Loading and saving
are coordinated transitions. UI cannot bypass validation by directly changing state.

- Embark validates loadout and departure conditions, creates one expedition, and
  saves its start before enabling play.
- Dock validates arrival, banks cargo, clears expedition-only state, and commits
  the entire resulting campaign/save snapshot together.
- Sink discards at-risk cargo and bonuses, restores a usable ship at the last safe
  hub, and commits the resolution in the same way.
- Upgrade validates prerequisites and bank balance, then deducts cost and grants
  the upgrade in one transition. All hubs query the same campaign record.
- Fast travel validates docked state, the global travel unlock, and destination
  activation. Save the new hub/location before resuming input.

Commands reject invalid lifecycle states. Record resolved expedition IDs so repeat
callbacks cannot bank cargo twice. Only one transition may commit at a time; if a
write fails, retain the prior committed state and offer retry while the pending
transition remains frozen. Resolve damage before docking in a simulation tick;
zero health selects sinking. Publish transition effects only after save commit.

## Save and load

Use a versioned save envelope containing campaign plus optional active expedition.
Use serializable DTOs with stable IDs, never scene references. Choose an existing
supported JSON serializer during setup. Write to a temporary file on the same
volume, validate, replace the main save, and retain one known-good backup. Verify
replacement and recovery behavior on the target platform.

Checkpoint periodically at sea, on pause/quit when possible, and on lifecycle
transitions. Capture health, cargo, cooldowns, modifiers, loot depletion, enemy state, and RNG state
together. Suspend gameplay while restoring. A crash may roll back to the last
checkpoint; do not promise protection against save editing or deterministic replay.

Validation rejects malformed data and unknown required IDs with a recoverable
error. Never silently overwrite an unreadable save. Future schema changes require
explicit migration and fixtures for the previous supported schema.

## Verification

Core tests cover banking/death, duplicate resolution, upgrade affordability, shared
hub access, and loadout validation. Persistence tests cover interrupted writes,
backup recovery, and expedition round trips without loot duplication. Play-mode
tests cover docking gates, collision, projectile hits, and unload/reload behavior.

Visual acceptance requires captured gameplay-camera footage, daylight/dusk/rough
water comparisons, and recorded performance in a standalone build. CPU and GPU
timings overlap; do not add them to infer frame time. Use the frame-time distribution
and profiler evidence to identify the limiting side. No performance is verified yet.
