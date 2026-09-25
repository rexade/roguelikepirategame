using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PirateGame.Gameplay.Ships;
using PirateGame.Rules.Application;
using PirateGame.Tests.T03;

// Control-flow probe only: Unity scheduling/physics are not simulated here.
class Program
{
    static void Main()
    {
        foreach (bool failedSave in new[] { false, true })
        {
            var definitions = RuleFixtures.Catalog();
            var initial = RuleFixtures.Initial(definitions);
            var store = new FakeSaveStore { Committed = initial };
            var session = new CampaignSession(definitions, initial, store, new FakeArrival());
            session.RetryArrival();
            var plan = RuleFixtures.Plan(RuleFixtures.Loot("barrel", 1));
            session.Embark(Guid.NewGuid(), plan);
            var simulation = new ShipSimulation { motor = new ShipMotor() };
            simulation.Bind(session);
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var publish = (IEnumerator)typeof(ShipSimulation).GetMethod("PublishTicks", flags).Invoke(simulation, null);
            var step = typeof(ShipSimulation).GetMethod("FixedUpdate", flags);
            publish.MoveNext();
            Action<InputIntent> callback = _ =>
            {
                simulation.AddDamage(100);
                if (failedSave)
                {
                    store.Fail = true;
                    Console.WriteLine("Pickup: " + session.CollectLoot(Guid.NewGuid(), plan.ExpeditionId,
                        PirateGame.Core.EntityId.Authored("barrel")).Error);
                }
                else session.SetPaused(true);
            };
            simulation.TickStarted += callback;
            step.Invoke(simulation, null);
            publish.MoveNext();
            Console.WriteLine($"{(failedSave ? "save failure" : "pause")}: after lethal step tick={session.Tick}, health={session.Snapshot.Expedition.Health}");
            simulation.TickStarted -= callback;
            if (failedSave) { store.Fail = false; session.RetrySave(); }
            else session.SetPaused(false);
            step.Invoke(simulation, null);
            publish.MoveNext();
            Console.WriteLine($"After resume: tick={session.Tick}, health={session.Snapshot.Expedition?.Health}, lifecycle={session.Lifecycle}");
            if (session.Snapshot.Expedition?.Health != 100 || session.Tick != 1)
                throw new Exception("Expected r1 lost-damage reproduction changed; reassess finding.");
        }
    }
}

namespace UnityEngine
{
    public class MonoBehaviour
    {
        protected void StartCoroutine(IEnumerator value) { }
        protected void StopAllCoroutines() { }
    }
    public static class Time { public static float fixedDeltaTime = 0.02f; }
    public class WaitForFixedUpdate { }
    public static class Debug { public static void LogWarning(object value) => Console.WriteLine(value); }
    public struct Vector3 { public float x, y, z; }
    public class Rigidbody { public Vector3 position; }
}
namespace PirateGame.Gameplay.Ships
{
    public class ShipMotor
    {
        public UnityEngine.Rigidbody Body = new UnityEngine.Rigidbody();
        public float Speed => 0;
        public void Configure(IReadOnlyDictionary<string, double> stats) { }
        public void Suspend(bool value) { }
        public void Step(InputIntent intent, bool brake, float delta) { }
    }
}
