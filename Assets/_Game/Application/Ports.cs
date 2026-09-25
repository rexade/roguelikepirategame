using System;
using System.Collections.Generic;
using PirateGame.Core;

namespace PirateGame.Rules.Application
{
    public sealed class SaveCandidate
    {
        public Guid RequestId { get; }
        public long ExpectedRevision { get; }
        public SessionSnapshot Snapshot { get; }
        public string Command { get; }
        public SaveCandidate(Guid requestId, long expectedRevision, SessionSnapshot snapshot, string command)
        {
            if (requestId == Guid.Empty || snapshot.Revision != checked(expectedRevision + 1)) throw new ArgumentException("Invalid save candidate.");
            RequestId = requestId; ExpectedRevision = expectedRevision; Snapshot = snapshot; Command = Values.Id(command);
        }
    }

    // A single writer must compare ExpectedRevision and acknowledge only a committed
    // snapshot. Retrying an acknowledged RequestId/revision must be idempotent.
    public interface ISaveStore { RuleResult Commit(SaveCandidate candidate); }

    public sealed class ArrivalRequest
    {
        public Guid RequestId { get; }
        public long Revision { get; }
        public SeaPosition Destination { get; }
        public string HubId { get; }
        public Guid? ExpeditionId { get; }
        public ArrivalRequest(Guid requestId, long revision, SeaPosition destination, string hubId, Guid? expeditionId)
        { RequestId = requestId; Revision = revision; Destination = destination; HubId = hubId; ExpeditionId = expeditionId; }
    }

    // Restore/reposition the session's existing player. Ready means collision and
    // all required entity state are restored. Never create a second campaign owner.
    public interface IWorldArrival { RuleResult EnsureReady(ArrivalRequest request); }

    public interface IGameplayClock
    {
        long Tick { get; }
        double FixedDeltaSeconds { get; }
        bool IsPaused { get; }
    }

    public readonly struct InputIntent
    {
        public double Throttle { get; }
        public double Turn { get; }
        public double AimX { get; }
        public double AimZ { get; }
        public bool Fire { get; }
        public bool Ability { get; }
        public bool Interact { get; }
        public InputIntent(double throttle, double turn, double aimX, double aimZ, bool fire, bool ability, bool interact)
        {
            if (Math.Abs(Values.Finite(throttle)) > 1 || Math.Abs(Values.Finite(turn)) > 1) throw new ArgumentException("Input axes out of range.");
            Throttle = throttle; Turn = turn; AimX = Values.Finite(aimX); AimZ = Values.Finite(aimZ);
            Fire = fire; Ability = ability; Interact = interact;
        }
    }

    public interface IEntityStatePort
    {
        EntityId Id { get; }
        EntityState Capture();
        RuleResult Restore(EntityState state);
    }

    // Called at a frozen simulation boundary by the future T08/T10 coordinator.
    // Capture includes unloaded entities, selected encounters, RNG and cooldowns;
    // restore finishes before clock/input resume. Scene references never cross it.
    public interface IExpeditionCapture
    {
        ExpeditionState Capture(Guid expeditionId, long tick);
        RuleResult Restore(ExpeditionState state);
    }

    public sealed class TransitionCommitted
    {
        public Guid RequestId { get; }
        public string Command { get; }
        public SessionSnapshot Snapshot { get; }
        public TransitionCommitted(SaveCandidate candidate)
        { RequestId = candidate.RequestId; Command = candidate.Command; Snapshot = candidate.Snapshot; }
    }

    public sealed class EmbarkPlan
    {
        public Guid ExpeditionId { get; }
        public int Seed { get; }
        public string RngState { get; }
        public IReadOnlyList<EntityState> Entities { get; }
        public IReadOnlyDictionary<string, string> Encounters { get; }
        public EmbarkPlan(Guid expeditionId, int seed, string rngState, IEnumerable<EntityState> entities,
            IEnumerable<KeyValuePair<string, string>> encounters)
        {
            if (expeditionId == Guid.Empty) throw new ArgumentException("Expedition ID required.");
            ExpeditionId = expeditionId; Seed = seed; RngState = Values.Id(rngState);
            Entities = Values.List(entities); Encounters = Values.Map(encounters);
        }
    }
}
