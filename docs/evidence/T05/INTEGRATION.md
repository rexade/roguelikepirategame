# T05 Integration Notes

Output: T05-r2 (T05-R1 remediation). Consumes accepted T03-r1 and T04-r2. No shared contract or
shared ship-prefab modification is requested. Production composition belongs to
the Lead/T08; this task delivers an isolated scene and reusable combat components.

## Composition

1. Freeze the T05 CombatCatalogAsset once, alongside its referenced rule catalog.
   Register its equipment and entity definition IDs in the eventual production
   catalog. Do not replace an existing production catalog with the test catalog.
2. Bind the existing ShipSimulation to the session. Add a CombatTarget to the
   player's stable gameplay body, referencing its existing ShipMotor.
3. Instantiate the Raider/Gunner prefabs under Prefabs/Combat. Initialize EnemyShip
   with a unique authored/generated EntityId, region ID, and frozen EnemySpec.
4. Bind CombatWorld with the same simulation/session, the frozen catalog, player
   target, enemies and a unique context EntityId. All enemies in this prototype
   share team 1; the player is team 0. Bind once per expedition composition.
5. Use T04 input: WASD, Space brake, left mouse fire, right mouse brace. The fixture
   also uses Escape through ShipSimulation.SetPaused. Only the fixture creates an
   in-memory session; production must inject its existing owner.

CombatWorld subscribes to TickStarted, runs timers, abilities, AI motor steps and
swept shots during that collection callback. Enemy bodies suspend after physics;
the next accepted TickStarted resumes them. It never publishes a campaign tick.
Disabled adapters, pending publication and failed saves cannot replay combat steps.
Complete a pending T04 step before destroying, unloading, rebinding or restoring
the composition. Use the adapter's pause/pickup entry points from R1/PROTOCOL.md.

## State and Saves

### Player Read State and Notifications (r2)

`CombatWorld.Player.Health` queries the bound session and expedition identity on
every read; `Defeated` derives from that value. During collection it still reports
the preceding published boundary. After T04 processes the step (HasPendingStep
becomes false), reads immediately reflect the new authoritative state, including
pause and save locks. No next tick, Update, or combat coroutine is required.
WaitForFixedUpdate consumers must check HasPendingStep rather than assume coroutine
ordering. An Update/render consumer observes the completed fixed-step publication.

A failed Sink save leaves Snapshot.Expedition at zero health: Defeated is true,
but no committed lifecycle success exists yet. After acknowledgement removes the
expedition, its entry in Campaign.ResolvedExpeditions keeps this target at zero for
Outcome.Sunk. A Docked outcome exposes the composition's maximum health as its
terminal healthy representation; the resolved voyage no longer has persisted
health. This is not an at-sea heal or a persisted damage value. Dispose the old
composition after resolution; bind a fresh combat world to each active expedition.
An old sunk target remains defeated even after a subsequent embark.

`CombatTarget.Died` is enemy-only. Player outcome consumers must use the session's
committed `TransitionCommitted` with Command == "Sink", correlated with the
resolved expedition. The existing composition owner drains and distributes those
events; combat never drains them or adds another event publisher. Defeated means
processed zero health, not successful persistence. Repeated reads, failed retries,
and restoring a session do not publish notifications. Enemy death-once and enemy
collider disabling retain their existing semantics; T04 suspension and expedition
composition own the player's physical lifecycle.

The optional CombatTarget.Initialize healthQuery supports this read-only player
binding. RestoreHealth rejects query-backed targets: restore the authoritative
session first. Bind immediately exposes its saved health while arrival is locked,
before CombatWorld.Restore finishes timers/projectiles and before input resumes.
T05 CombatView now reads the target. T06/T07/T08 consumers need no shared API change;
they must follow these read-boundary and committed-notification distinctions.

CombatWorld implements IExpeditionCapture. EnemyShip implements IEntityStatePort.
Capture at an idle simulation boundary, while an external coordinator prevents a
new tick. To checkpoint, pass the captured ExpeditionState to the existing
CampaignSession.Checkpoint(requestId, captured). Do not use the parameterless
checkpoint for a live combat checkpoint: the session alone does not contain the
latest scene-owned enemy/projectile state.

The capture preserves existing unloaded entities, encounters, RNG, cargo,
modifiers and authoritative health/position/speed. It overlays loaded enemy state,
preserving application-owned loot depletion. No loot/economy is introduced by T05.

Player timers use expedition Cooldowns keys combat.weapon, combat.ability and
combat.brace. Enemy cooldowns use weapon. Enemy BehaviorState is version-1 JSON
containing mode, yaw and planar velocity/angular velocity. The combat context is
an EntityState with definition combat-context-v1; its version-1 BehaviorState
contains weapon ID, player yaw, shot generation and all active shots (owner/team,
definition, position/direction, remaining range, damage and generation). Retired
shots do not need persistent tombstones: stale callbacks are generation-guarded,
and scene restore creates new shot objects. The context entity itself remains in
the ledger, with health 1 and empty loot.

Restore the authoritative session first, keep it arrival/pause locked, initialize
the same stable identities, then call CombatWorld.Restore. The full capture is
validated before modifying any enemy, timer or projectile. Unknown versions,
owners, definitions, foreign voyages and invalid enemy state reject. Restore
does not emit damage, death or shot events. Release arrival/pause only after all
other required world restoration succeeds.

T03 persists scalar player speed rather than the player's complete solver
velocity. Player restore uses saved yaw and speed, with zero angular velocity;
it does not promise preservation of lateral/reverse momentum or deterministic
physics replay. Enemy motion and in-flight projectiles are captured explicitly.

All scene combat mutations must be captured before a durable at-sea checkpoint.
T08 must coordinate other at-sea writes (including T03's committing pickup) with
capture; this task does not extend that shared transaction API. Outcome saves
discard combat with the expedition. RetrySave resubmits a frozen candidate and
must never resimulate its tick.

## Presentation and Content

Cannon: 30 damage, 1.1-second reload, 65 m/s, 45 m range.
Repeater: 8 damage, 0.22-second reload, 90 m/s, 30 m range.
Brace: 75% damage reduction for 2 seconds, 6-second cooldown.
The optional computed hull stat damage-scale scales player shot damage using the
existing ShipStats modifier calculation. Absent that stat, weapon damage is used.

Raider pursues to 9 m; gunner maintains roughly 23 m and backs away when too close.
Both use the accepted motor, stable muzzle and engine obstacle casts.
Enemy reload is an authored multiplier (3 in the fixture), including the initial
reload. This is local obstacle avoidance, not an archipelago navigation system.

The fixture's CombatView supplies colored projectiles, brace ring, enemy health
bars, cooldown/health HUD and defeat visibility. Defeated colliders are disabled;
restoring a live entity restores its original collider enabled state. Production
presentation can consume the read state, ShotFired and enemy target Died events without
owning outcomes. No sounds or elaborate impact/death effects were authored.

Run Builds/T05/Combat.exe for the cannon, or pass -t05-repeater for the alternative
owned loadout selected before embark. Refit at sea remains rejected by T03.
