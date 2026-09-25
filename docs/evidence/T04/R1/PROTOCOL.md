# T04-r2 Step Protocol

This T04-local protocol supersedes the original T04 report's pause integration
instruction. T03-r1 contracts and Core/Application are unchanged. The same injected
CampaignSession remains authoritative; ShipSimulation is its sole tick publisher.

## Phases and Ownership

1. Idle boundary: no pending step. An unlocked AtSea session allows FixedUpdate
   to begin exactly its next tick. The motor restores suspended velocity before
   applying intent. Save the expedition ID and tick number now.
2. Collecting: the motor steps once, TickStarted runs, then Unity physics and
   collision callbacks run. AddDamage accumulates this step's finite damage.
   Producers must finish in these callbacks, not in later coroutines. This is the
   only phase in which AddDamage accepts damage. RequestDock may queue docking
   through the session; it cannot resolve before CompleteTick processes damage.
3. Publication: WaitForFixedUpdate resumes after physics. Capture XZ position
   and planar speed once. CompleteTick processes the retained identity and total.
4. Pending rejection: if the session has neither advanced that expedition's tick
   nor recorded its outcome, retain all step data and freeze the Rigidbody. Later
   fixed boundaries retry publication; they never run motor.Step or TickStarted.
   Paused/Busy do not acknowledge processing. LastTickResult exposes the rejection.
5. Processed: authoritative tick advancement or a recorded outcome acknowledges
   consumption even when CompleteTick returns SaveFailed, ArrivalFailed, or an
   invalid docking result. Clear the step, attempt a queued pickup, then apply
   requested pause. Session locks continue to freeze the motor. RetrySave handles
   the identical pending candidate; never replay CompleteTick for a processed tick.

The motor owns physical position, yaw and velocity. During suspension its stored
velocity supplies Speed; the kinematic body's zero solver velocity is not a new
simulation speed. It disables interpolation while frozen and restores it on resume
so reactivation cannot substitute an older rendered transform. At unlocked AtSea
boundaries, session XZ/speed match the body within 0.001 m and 0.001 m/s.

## Entry Points for T05/T06 and Integration

- Use ShipSimulation.SetPaused(bool). During collection it defers the flag until
  the step completes; outside a step it freezes immediately. A pause request clears
  input and suppresses new input until applied. Resume also releases an explicit
  direct-session pause while retaining the adapter's pending-step freeze.
- Use ShipSimulation.CollectLoot(requestId, expeditionId, sourceId). During a
  step it queues one pickup and returns IsPending=true (queue acknowledgement,
  not a persistence success). A second queued request returns Busy. Keep the same
  request identity. LastPickupResult reports the attempted command result; session
  DrainEvents remains the only commit-success publication. SaveFailed requires
  Session.RetrySave, not a new pickup. Outside a step the wrapper calls the session
  immediately and returns its result.
- Ordering is damage, docking/outcome, pickup, pause regardless of callback order.
  A lethal or successfully docked step rejects the deferred pickup (Busy while an
  outcome save is pending, WrongLifecycle after commit). It is not retried into a
  later voyage. A failed pickup candidate includes already processed damage,
  position, speed and tick; cargo/source accounting commits atomically.
- Session.RequestDock may be called during TickStarted/collision collection.
  CompleteTick applies damage before docking. Invalid zone/speed consumes the
  movement/damage tick and rejects only docking; LastTickResult exposes the error.
- Session.RetrySave and RetryArrival are supported while locked. Keep simulation
  suspended through scene restoration. T08 still owns real persistence/arrival.
- Direct Session.SetPaused/CollectLoot during collection are unsupported for new
  consumers. Existing direct locks fail safely at publication: the adapter retains
  damage and physical state until resume/save retry. A direct pickup can serialize
  the prior boundary before the pending damage, so it is not the supported coherent
  save path. No claim of crash recovery for this misuse is made. The shared session
  API is not intercepted or weakened; CompleteTick's rejection is recovered.
- Other persistent commands/checkpoint capture must run only at an idle boundary,
  not inside damage callbacks. There must be no second tick publisher or external
  Rigidbody driver, including manual Physics.Simulate while the adapter is active.

## Lifecycle

Disabling the adapter or deactivating its GameObject freezes the body, stops its
publication coroutine and clears input, but retains the in-flight step, damage,
queued pickup and pause request. Re-enable the same object to finish publication
before another step can begin. Do not rebind while HasPendingStep: Bind throws
without replacing the session. Do not destroy/unload a pending adapter; finish its
step first. Destruction/scene transfer is not a persistence or restore protocol.
Damage producers must also stop while the adapter is disabled or publication is
pending. The fixture's Escape and capture pause calls use the new entry point.
