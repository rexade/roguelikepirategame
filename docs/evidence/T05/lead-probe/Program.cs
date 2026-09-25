using System;
using PirateGame.Gameplay.Combat;
using PirateGame.Rules.Application;
using PirateGame.Tests.T03;

// Replays the actual player-target/session data path, not Unity physics/scheduling.
class Program
{
    static void Main()
    {
        foreach (double damage in new[] { 10.0, 100.0 })
        {
            var catalog = RuleFixtures.Catalog();
            var initial = RuleFixtures.Initial(catalog);
            var session = new CampaignSession(catalog, initial,
                new FakeSaveStore { Committed = initial }, new FakeArrival());
            session.RetryArrival();
            var plan = RuleFixtures.Plan();
            session.Embark(Guid.NewGuid(), plan);
            double collected = 0;
            var player = new CombatTarget();
            player.Initialize("player", 0, 100, amount => collected += amount);
            int deaths = 0;
            player.Died += _ => deaths++;
            // CombatWorld.Tick refreshes the target before shots collect damage.
            player.RestoreHealth(session.Snapshot.Expedition.Health);
            player.ApplyDamage(damage);
            session.CompleteTick(plan.ExpeditionId, 1, collected, RuleFixtures.Home, 0);
            session.SetPaused(true);
            Console.WriteLine($"damage={damage}, lifecycle={session.Lifecycle}, sessionHealth={session.Snapshot.Expedition?.Health.ToString() ?? "resolved"}, targetHealth={player.Health}, targetDefeated={player.Defeated}, targetDeaths={deaths}");
            if (player.Health != 100 || player.Defeated || deaths != 0)
                throw new Exception("Original stale-target reproduction changed; reassess finding.");
        }
    }
}

namespace UnityEngine
{
    public class MonoBehaviour
    {
        public T[] GetComponentsInChildren<T>() => Array.Empty<T>();
    }
    public class Collider { public bool enabled; }
}
namespace PirateGame.Gameplay.Ships
{
    public class ShipMotor { public void Suspend(bool value) { } }
}
