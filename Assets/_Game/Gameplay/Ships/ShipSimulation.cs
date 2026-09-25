using System;
using System.Collections;
using PirateGame.Core;
using PirateGame.Rules.Application;
using UnityEngine;
using EntityId = PirateGame.Core.EntityId;

namespace PirateGame.Gameplay.Ships
{
    public sealed class ShipSimulation : MonoBehaviour
    {
        public ShipMotor motor;
        public CampaignSession Session { get; private set; }
        public InputIntent Intent { get; private set; }
        public bool Brake { get; private set; }
        public bool HasPendingStep => stepped;
        public RuleResult LastTickResult { get; private set; }
        public RuleResult LastPickupResult { get; private set; }
        public event Action<InputIntent> TickStarted;
        private double damage;
        private bool stepped;
        private bool captured;
        private Guid expeditionId;
        private long tick;
        private SeaPosition position;
        private double speed;
        private bool? pauseRequested;
        private Guid pickupRequest;
        private Guid pickupExpedition;
        private EntityId pickupSource;

        public void Bind(CampaignSession session)
        {
            if (stepped) throw new InvalidOperationException("Complete the pending step before rebinding.");
            if (Math.Abs(session.FixedDeltaSeconds - Time.fixedDeltaTime) > 0.000001)
                throw new InvalidOperationException("Session and Unity fixed steps must match.");
            Session = session;
            motor.Configure(session.ShipStats());
            motor.Suspend(session.IsPaused);
        }

        public void Submit(InputIntent intent, bool brake)
        {
            if (Session == null || Session.IsPaused || pauseRequested == true) { Intent = default; Brake = false; return; }
            // Keep edges until a physics step consumes them, including at 120 FPS.
            Intent = new InputIntent(intent.Throttle, intent.Turn, intent.AimX, intent.AimZ,
                intent.Fire, intent.Ability || Intent.Ability, intent.Interact || Intent.Interact);
            Brake = brake;
        }

        public void ClearInput() { Intent = default; Brake = false; }

        public void SetPaused(bool value)
        {
            if (Session == null) throw new InvalidOperationException("Bind a session first.");
            if (value) ClearInput();
            if (stepped)
            {
                pauseRequested = value;
                // Resume can release an unsupported direct pause while the adapter
                // still owns/freeze-guards the unpublished step.
                if (!value) Session.SetPaused(false);
            }
            else { Session.SetPaused(value); motor.Suspend(!isActiveAndEnabled || Session.IsPaused); }
        }

        // A successful pending result acknowledges the queue, not a committed pickup.
        // LastPickupResult and the session's committed events report its eventual result.
        public RuleResult CollectLoot(Guid requestId, Guid voyageId, EntityId source)
        {
            if (Session == null) return new RuleResult(RuleError.WrongLifecycle);
            if (pickupRequest != Guid.Empty || captured) return new RuleResult(RuleError.Busy);
            if (!stepped) return LastPickupResult = Session.CollectLoot(requestId, voyageId, source);
            if (requestId == Guid.Empty || !source.IsValid) return new RuleResult(RuleError.InvalidRequest);
            if (voyageId != expeditionId) return new RuleResult(RuleError.WrongExpedition);
            pickupRequest = requestId; pickupExpedition = voyageId; pickupSource = source;
            return LastPickupResult = new RuleResult(pending: true);
        }

        public void AddDamage(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (stepped && !captured)
            {
                if (double.IsInfinity(damage + amount)) throw new ArgumentOutOfRangeException(nameof(amount));
                damage += amount;
            }
        }

        private void Update()
        {
            if (Session == null || Session.IsPaused) { Intent = default; Brake = false; }
            motor.Suspend(stepped || Session == null || Session.IsPaused);
        }

        private void FixedUpdate()
        {
            // A retained publication owns the next tick; never replay its physics.
            if (stepped) { motor.Suspend(true); return; }
            motor.Suspend(Session == null || Session.IsPaused);
            if (Session == null || Session.IsPaused) return;
            stepped = true;
            captured = false;
            expeditionId = Session.Snapshot.Expedition.Id;
            tick = Session.Tick + 1;
            damage = 0;
            motor.Step(Intent, Brake, Time.fixedDeltaTime);
            TickStarted?.Invoke(Intent);
            Intent = new InputIntent(Intent.Throttle, Intent.Turn, Intent.AimX, Intent.AimZ, Intent.Fire, false, false);
        }

        private void OnEnable() => StartCoroutine(PublishTicks());

        private IEnumerator PublishTicks()
        {
            var boundary = new WaitForFixedUpdate();
            while (true)
            {
                // Unity resumes this after physics/collision callbacks, before rendering.
                yield return boundary;
                if (!stepped) continue;
                if (!captured)
                {
                    var p = motor.Body.position;
                    position = new SeaPosition(Session.Snapshot.Expedition.Position.RegionId, p.x, p.z);
                    speed = motor.Speed;
                    captured = true;
                }
                LastTickResult = Session.CompleteTick(expeditionId, tick, damage, position, speed);
                var current = Session.Snapshot.Expedition;
                // SaveFailed and invalid docking can follow a fully processed tick.
                // Only the authoritative tick/outcome acknowledges consumption.
                bool processed = (current != null && current.Id == expeditionId && current.Tick == tick)
                    || Session.Snapshot.Campaign.ResolvedExpeditions.ContainsKey(expeditionId);
                if (!processed) { motor.Suspend(true); continue; }
                stepped = false;
                captured = false;
                if (pickupRequest != Guid.Empty)
                {
                    LastPickupResult = Session.CollectLoot(pickupRequest, pickupExpedition, pickupSource);
                    pickupRequest = Guid.Empty;
                }
                if (pauseRequested.HasValue)
                {
                    Session.SetPaused(pauseRequested.Value);
                    pauseRequested = null;
                }
                motor.Suspend(!isActiveAndEnabled || Session.IsPaused);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ClearInput();
            if (motor != null && motor.Body != null) motor.Suspend(true);
        }
    }
}
