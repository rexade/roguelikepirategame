using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PirateGame.Core
{
    public static class Values
    {
        public static string Id(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stable ID is required.");
            return value;
        }

        public static double Finite(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentException("Value must be finite.");
            return value;
        }

        public static IReadOnlyDictionary<string, T> Map<T>(IEnumerable<KeyValuePair<string, T>> values)
        {
            var copy = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (var pair in values)
            {
                if (ReferenceEquals(pair.Value, null)) throw new ArgumentException("Null map values are not supported.");
                copy.Add(Id(pair.Key), pair.Value);
            }
            return new ReadOnlyDictionary<string, T>(copy);
        }

        public static IReadOnlyList<T> List<T>(IEnumerable<T> values)
        {
            var copy = values.ToArray();
            if (copy.Any(v => ReferenceEquals(v, null))) throw new ArgumentException("Null list values are not supported.");
            return Array.AsReadOnly(copy);
        }
        public static IReadOnlyDictionary<string, int> Bundle(IEnumerable<KeyValuePair<string, int>> values)
        {
            var copy = Map(values);
            if (copy.Values.Any(v => v < 0)) throw new ArgumentException("Quantities must be nonnegative.");
            return copy;
        }

        public static Dictionary<string, T> Copy<T>(IReadOnlyDictionary<string, T> source) =>
            source.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);

        public static int Amount(IReadOnlyDictionary<string, int> ledger, string id) =>
            ledger.TryGetValue(id, out var value) ? value : 0;
    }

    public readonly struct SeaPosition
    {
        public string RegionId { get; }
        public double X { get; }
        public double Z { get; }
        public SeaPosition(string regionId, double x, double z)
        { RegionId = Values.Id(regionId); X = Values.Finite(x); Z = Values.Finite(z); }
    }

    public readonly struct EntityId : IEquatable<EntityId>
    {
        public string AuthoredId { get; }
        public Guid ExpeditionId { get; }
        public string SpawnId { get; }
        private EntityId(string authoredId, Guid expeditionId, string spawnId)
        { AuthoredId = authoredId; ExpeditionId = expeditionId; SpawnId = spawnId; }
        public static EntityId Authored(string id) => new EntityId(Values.Id(id), Guid.Empty, null);
        public static EntityId Generated(Guid expedition, string spawn)
        {
            if (expedition == Guid.Empty) throw new ArgumentException("Expedition ID required.");
            return new EntityId(null, expedition, Values.Id(spawn));
        }
        public bool IsValid => AuthoredId != null || (ExpeditionId != Guid.Empty && SpawnId != null);
        public bool Equals(EntityId other) => AuthoredId == other.AuthoredId && ExpeditionId == other.ExpeditionId && SpawnId == other.SpawnId;
        public override bool Equals(object obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => (AuthoredId ?? "").GetHashCode() ^ ExpeditionId.GetHashCode() ^ (SpawnId ?? "").GetHashCode();
        public override string ToString() => AuthoredId != null ? "authored:" + AuthoredId : "spawn:" + ExpeditionId + ":" + SpawnId;
    }

    public enum SlotKind { Weapon, Ability, Passive }
    public enum ModifierOperation { Flat, Percent }
    public enum Lifecycle { Docked, Departing, AtSea, Resolving }
    public enum Outcome { Docked, Sunk }
    public enum RuleError
    {
        None, InvalidRequest, WrongLifecycle, Busy, Paused, UnknownId, WrongExpedition,
        DuplicateRequest, AlreadyResolved, Depleted, CargoFull, Overflow, InsufficientBank,
        PrerequisiteMissing, TierAlreadyOwned, InvalidLoadout, NotInDockZone, TooFast,
        HubInactive, TravelLocked, NotSunk, SaveFailed, ArrivalFailed, StaleTick
    }

    public sealed class RuleResult
    {
        public RuleError Error { get; }
        public bool IsSuccess => Error == RuleError.None;
        public bool IsPending { get; }
        public string Detail { get; }
        public RuleResult(RuleError error = RuleError.None, bool pending = false, string detail = "")
        { Error = error; IsPending = pending; Detail = detail; }
    }
}
