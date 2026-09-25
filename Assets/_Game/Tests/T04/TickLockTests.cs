using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PirateGame.Core;
using PirateGame.Gameplay.Ships;
using PirateGame.Rules.Application;
using UnityEngine;
using UnityEngine.TestTools;
using EntityId = PirateGame.Core.EntityId;
using Object = UnityEngine.Object;

namespace PirateGame.Tests.T04
{
    public sealed class LockCollision : MonoBehaviour
    {
        public Action Hit;
        private void OnCollisionEnter(Collision collision) => Hit?.Invoke();
    }

    public sealed class TickLockTests
    {
        private GameObject ship, wall;
        private ShipMotor motor;
        private ShipSimulation simulation;
        private CampaignSession session;
        private Store store;
        private Guid voyage;
        private static readonly EntityId Loot = EntityId.Authored("loot");

        private sealed class Store : ISaveStore
        {
            public bool Fail;
            public readonly List<SaveCandidate> Attempts = new List<SaveCandidate>();
            public RuleResult Commit(SaveCandidate candidate)
            {
                Attempts.Add(candidate);
                return new RuleResult(Fail ? RuleError.SaveFailed : RuleError.None);
            }
        }
        private sealed class Arrival : IWorldArrival
        {
            public RuleResult EnsureReady(ArrivalRequest request) => new RuleResult();
        }

        [SetUp] public void Setup()
        {
            var home = new SeaPosition("sea", 0, 0);
            var catalog = new DefinitionCatalog(new Dictionary<string, int> { ["wood"] = 1 },
                new[] { new HullDefinition("ship", new Dictionary<string, SlotKind>(), new[] {
                    new StatDefinition("health", 100, 1, 100), new StatDefinition("cargo", 10, 0, 100), new StatDefinition("speed", 8, 1, 50) }) },
                Array.Empty<EquipmentDefinition>(), new[] { new HubDefinition("home", home, 5, 1) },
                Array.Empty<UpgradeDefinition>(), Array.Empty<string>(), Array.Empty<AuthoredIdentity>(), new[] { "barrel" }, new[] { "sea" });
            var initial = CampaignSession.NewCampaign(catalog, "home", "ship", new Dictionary<string, string>(), new Dictionary<string, string>());
            store = new Store();
            session = new CampaignSession(catalog, initial, store, new Arrival());
            session.RetryArrival();
            voyage = Guid.NewGuid();
            session.Embark(Guid.NewGuid(), new EmbarkPlan(voyage, 1, "rng", new[] {
                new EntityState(Loot, "barrel", home, 1, false, new Dictionary<string, int> { ["wood"] = 2 }, new Dictionary<string, double>(), "idle")
            }, new Dictionary<string, string>()));
            session.DrainEvents();
            ship = new GameObject("tick lock ship");
            motor = ship.AddComponent<ShipMotor>();
            simulation = ship.AddComponent<ShipSimulation>(); simulation.motor = motor;
            simulation.Bind(session);
            simulation.SetPaused(true);
            simulation.Submit(new InputIntent(1, 0, 0, 1, false, false, false), false);
        }

        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(ship);
            if (wall != null) Object.DestroyImmediate(wall);
        }

        private void Trigger(bool collision, Action action)
        {
            bool invoked = false;
            Action once = () => { if (invoked) return; invoked = true; action(); };
            if (!collision) simulation.TickStarted += _ => once();
            else
            {
                wall = new GameObject("actual collision wall");
                wall.AddComponent<BoxCollider>().size = new Vector3(10, 10, 1);
                wall.transform.position = new Vector3(0, 0, 0.95f);
                ship.AddComponent<LockCollision>().Hit = once;
                Physics.SyncTransforms();
            }
            simulation.SetPaused(false);
            simulation.Submit(new InputIntent(1, 0, 0, 1, false, false, false), false);
        }

        [UnityTest] public IEnumerator DirectPauseFromTick() => DirectLock(false, false);
        [UnityTest] public IEnumerator DirectPauseFromCollision() => DirectLock(true, false);
        [UnityTest] public IEnumerator DirectPickupFromTick() => DirectLock(false, true);
        [UnityTest] public IEnumerator DirectPickupFromCollision() => DirectLock(true, true);
        [UnityTest] public IEnumerator DirectPauseNonlethalFromTick() => DirectLock(false, false, 10);
        [UnityTest] public IEnumerator DirectPauseNonlethalFromCollision() => DirectLock(true, false, 10);
        [UnityTest] public IEnumerator DirectPickupNonlethalFromTick() => DirectLock(false, true, 10);
        [UnityTest] public IEnumerator DirectPickupNonlethalFromCollision() => DirectLock(true, true, 10);

        private IEnumerator DirectLock(bool collision, bool pickup, double damage = 100)
        {
            bool called = false;
            Trigger(collision, () => {
                called = true;
                simulation.AddDamage(damage);
                if (pickup) { store.Fail = true; Assert.That(session.CollectLoot(Guid.NewGuid(), voyage, Loot).Error, Is.EqualTo(RuleError.SaveFailed)); }
                else session.SetPaused(true);
            });
            yield return new WaitForSeconds(0.1f);
            Assert.That(called, Is.True, "Real scheduler/collision callback must run");
            var position = motor.Body.position;
            var tick = session.Tick;
            yield return new WaitForSeconds(0.1f);
            Assert.That(motor.Body.position, Is.EqualTo(position));
            Assert.That(session.Tick, Is.EqualTo(tick));
            Assert.That(simulation.HasPendingStep, Is.True);
            if (pickup)
            {
                var candidate = session.PendingSave;
                Assert.That(session.RetrySave().Error, Is.EqualTo(RuleError.SaveFailed));
                Assert.That(session.PendingSave, Is.SameAs(candidate));
                Assert.That(session.DrainEvents(), Is.Empty);
                store.Fail = false; session.RetrySave();
            }
            else simulation.SetPaused(false);
            yield return new WaitForSeconds(0.1f);
            if (damage == 100)
            {
                Assert.That(session.Snapshot.Campaign.ResolvedExpeditions.ContainsKey(voyage), Is.True, "Lethal in-flight damage must survive the lock");
                Assert.That(session.Snapshot.Campaign.ResolvedExpeditions[voyage], Is.EqualTo(Outcome.Sunk));
            }
            else
            {
                Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(90));
                AssertAgreement();
                if (pickup)
                {
                    Assert.That(Values.Amount(session.Snapshot.Expedition.Cargo, "wood"), Is.EqualTo(2));
                    Assert.That(Values.Amount(session.Snapshot.Expedition.Entities[Loot].Loot, "wood"), Is.Zero);
                }
            }
        }

        private static readonly int[] Orders = { 0, 1, 2, 3, 4, 5, 6, 7 };
        [UnityTest] public IEnumerator SupportedPauseOrdering([ValueSource(nameof(Orders))] int variant)
        {
            bool collision = (variant & 1) != 0, beforeDamage = (variant & 2) != 0;
            double damage = (variant & 4) != 0 ? 100 : 10;
            bool called = false;
            Trigger(collision, () => {
                called = true;
                if (beforeDamage) simulation.SetPaused(true);
                simulation.AddDamage(damage);
                if (!beforeDamage) simulation.SetPaused(true);
            });
            yield return new WaitForSeconds(0.1f);
            Assert.That(called, Is.True);
            Assert.That(simulation.HasPendingStep, Is.False);
            Assert.That(session.IsPaused, Is.True);
            var position = motor.Body.position; var tick = session.Tick;
            yield return new WaitForSeconds(0.1f);
            Assert.That(motor.Body.position, Is.EqualTo(position));
            Assert.That(session.Tick, Is.EqualTo(tick));
            if (damage == 100)
            {
                Assert.That(session.Snapshot.Campaign.ResolvedExpeditions[voyage], Is.EqualTo(Outcome.Sunk));
                Assert.That(session.DrainEvents().Count, Is.EqualTo(1));
            }
            else
            {
                Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(90));
                AssertAgreement();
                simulation.SetPaused(false);
                AssertAgreement();
                yield return new WaitForSeconds(0.1f);
                Assert.That(session.Tick, Is.GreaterThan(tick));
                Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(90));
                AssertAgreement();
            }
        }

        [UnityTest] public IEnumerator SupportedPickupOrdering([ValueSource(nameof(Orders))] int variant)
        {
            bool collision = (variant & 1) != 0, beforeDamage = (variant & 2) != 0;
            double damage = (variant & 4) != 0 ? 100 : 10;
            var request = Guid.NewGuid();
            Trigger(collision, () => {
                store.Fail = true;
                if (beforeDamage) Assert.That(simulation.CollectLoot(request, voyage, Loot).IsPending, Is.True);
                simulation.AddDamage(damage);
                if (!beforeDamage) Assert.That(simulation.CollectLoot(request, voyage, Loot).IsPending, Is.True);
                Assert.That(simulation.CollectLoot(request, voyage, Loot).Error, Is.EqualTo(RuleError.Busy));
            });
            yield return new WaitForSeconds(0.1f);
            Assert.That(session.PendingSave, Is.Not.Null);
            var candidate = session.PendingSave;
            Assert.That(candidate.Command, Is.EqualTo(damage == 100 ? "Sink" : "CollectLoot"));
            Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(100 - damage));
            Assert.That(session.Snapshot.Expedition.Tick, Is.EqualTo(1));
            Assert.That(Values.Amount(session.Snapshot.Expedition.Cargo, "wood"), Is.Zero);
            Assert.That(Values.Amount(session.Snapshot.Expedition.Entities[Loot].Loot, "wood"), Is.EqualTo(2));
            if (damage == 10)
            {
                Assert.That(candidate.Snapshot.Expedition.Health, Is.EqualTo(90));
                Assert.That(Values.Amount(candidate.Snapshot.Expedition.Cargo, "wood"), Is.EqualTo(2));
                Assert.That(Values.Amount(candidate.Snapshot.Expedition.Entities[Loot].Loot, "wood"), Is.Zero);
                AssertAgreement();
            }
            var position = motor.Body.position;
            for (int i = 0; i < 2; i++)
            {
                Assert.That(session.RetrySave().Error, Is.EqualTo(RuleError.SaveFailed));
                Assert.That(store.Attempts[store.Attempts.Count - 1], Is.SameAs(candidate));
                Assert.That(session.DrainEvents(), Is.Empty);
                yield return new WaitForSeconds(0.06f);
                Assert.That(motor.Body.position, Is.EqualTo(position));
                Assert.That(session.Tick, Is.EqualTo(1));
            }
            store.Fail = false;
            Assert.That(session.RetrySave().IsSuccess, Is.True);
            Assert.That(session.DrainEvents().Count, Is.EqualTo(1));
            Assert.That(session.RetrySave().Error, Is.EqualTo(RuleError.InvalidRequest));
            Assert.That(session.DrainEvents(), Is.Empty);
            simulation.SetPaused(false);
            yield return new WaitForSeconds(0.08f);
            if (damage == 100)
            {
                Assert.That(session.Snapshot.Campaign.ResolvedExpeditions[voyage], Is.EqualTo(Outcome.Sunk));
                Assert.That(Values.Amount(session.Snapshot.Campaign.Bank, "wood"), Is.Zero);
                Assert.That(simulation.LastPickupResult.IsSuccess, Is.False);
            }
            else
            {
                Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(90));
                Assert.That(Values.Amount(session.Snapshot.Expedition.Cargo, "wood"), Is.EqualTo(2));
                Assert.That(simulation.CollectLoot(request, voyage, Loot).Error, Is.EqualTo(RuleError.DuplicateRequest));
                AssertAgreement();
            }
        }

        private static readonly int[] Docks = { 0, 1, 2, 3, 4, 5 };
        [UnityTest] public IEnumerator DockOrderingAndProcessedFailures([ValueSource(nameof(Docks))] int variant)
        {
            bool lethal = variant == 2 || variant == 3;
            bool fail = variant == 1 || variant == 3;
            session.SetPaused(false);
            Assert.That(session.CollectLoot(Guid.NewGuid(), voyage, Loot).IsSuccess, Is.True);
            session.SetPaused(true);
            session.DrainEvents();
            if (variant == 4) motor.Body.position = new Vector3(10, 0, 0);
            Trigger(false, () => {
                store.Fail = fail;
                Assert.That(session.RequestDock(Guid.NewGuid(), voyage, "home").IsPending, Is.True);
                simulation.AddDamage(lethal ? 100 : 10);
                if (!fail) simulation.SetPaused(true);
            });
            if (variant == 5) motor.Body.linearVelocity = Vector3.forward * 3;
            yield return new WaitForSeconds(0.1f);
            Assert.That(simulation.HasPendingStep, Is.False);
            if (variant >= 4)
            {
                Assert.That(simulation.LastTickResult.Error, Is.EqualTo(variant == 4 ? RuleError.NotInDockZone : RuleError.TooFast));
                Assert.That(session.Tick, Is.EqualTo(1));
                Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(90));
                AssertAgreement();
                simulation.SetPaused(false);
                yield return new WaitForSeconds(0.1f);
                Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(90));
                Assert.That(session.Tick, Is.GreaterThan(1));
                yield break;
            }
            if (fail)
            {
                var candidate = session.PendingSave;
                Assert.That(candidate.Command, Is.EqualTo(lethal ? "Sink" : "Dock"));
                var position = motor.Body.position;
                for (int i = 0; i < 2; i++)
                {
                    Assert.That(session.RetrySave().Error, Is.EqualTo(RuleError.SaveFailed));
                    Assert.That(store.Attempts[store.Attempts.Count - 1], Is.SameAs(candidate));
                    Assert.That(session.DrainEvents(), Is.Empty);
                    yield return new WaitForSeconds(0.06f);
                    Assert.That(session.Tick, Is.EqualTo(1));
                    Assert.That(motor.Body.position, Is.EqualTo(position));
                    Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(lethal ? 0 : 90));
                }
                store.Fail = false; Assert.That(session.RetrySave().IsSuccess, Is.True);
            }
            Assert.That(session.Snapshot.Campaign.ResolvedExpeditions[voyage], Is.EqualTo(lethal ? Outcome.Sunk : Outcome.Docked));
            Assert.That(Values.Amount(session.Snapshot.Campaign.Bank, "wood"), Is.EqualTo(lethal ? 0 : 2));
            Assert.That(session.DrainEvents().Count, Is.EqualTo(1));
            Assert.That(session.RetrySave().Error, Is.EqualTo(RuleError.InvalidRequest));
            Assert.That(session.DrainEvents(), Is.Empty);
        }

        private static readonly int[] Lifecycles = { 0, 1, 2, 3 };
        [UnityTest] public IEnumerator DisableRetainsStep([ValueSource(nameof(Lifecycles))] int variant)
        {
            bool wholeObject = (variant & 1) != 0, collision = (variant & 2) != 0;
            Trigger(collision, () => {
                simulation.AddDamage(10);
                session.SetPaused(true);
                if (wholeObject) ship.SetActive(false); else simulation.enabled = false;
            });
            yield return new WaitForSeconds(0.1f);
            Assert.That(simulation.HasPendingStep, Is.True);
            Assert.Throws<InvalidOperationException>(() => simulation.Bind(session));
            var position = motor.Body.position;
            yield return new WaitForSeconds(0.1f);
            Assert.That(motor.Body.position, Is.EqualTo(position));
            Assert.That(session.Tick, Is.Zero);
            if (wholeObject) ship.SetActive(true); else simulation.enabled = true;
            yield return new WaitForSeconds(0.08f);
            Assert.That(session.Tick, Is.Zero);
            Assert.That(motor.Body.position, Is.EqualTo(position));
            simulation.SetPaused(false);
            yield return new WaitForSeconds(0.1f);
            Assert.That(simulation.HasPendingStep, Is.False);
            Assert.That(session.Snapshot.Expedition.Health, Is.EqualTo(90));
            AssertAgreement();
        }

        private void AssertAgreement()
        {
            var expedition = session.Snapshot.Expedition;
            Assert.That(expedition.Position.X, Is.EqualTo(motor.Body.position.x).Within(0.001));
            Assert.That(expedition.Position.Z, Is.EqualTo(motor.Body.position.z).Within(0.001));
            Assert.That(expedition.Speed, Is.EqualTo(motor.Speed).Within(0.001));
            if (!motor.Body.isKinematic)
                Assert.That(expedition.Speed, Is.EqualTo(new Vector2(motor.Body.linearVelocity.x, motor.Body.linearVelocity.z).magnitude).Within(0.001));
        }
    }
}
