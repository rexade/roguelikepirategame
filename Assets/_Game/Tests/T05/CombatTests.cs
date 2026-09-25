using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PirateGame.Core;
using PirateGame.Content.Combat;
using PirateGame.Content.Definitions;
using PirateGame.Gameplay.Combat;
using PirateGame.Gameplay.AI;
using PirateGame.Gameplay.Ships;
using PirateGame.Rules.Application;
using UnityEngine;
using UnityEngine.TestTools;
using EntityId = PirateGame.Core.EntityId;
using Object = UnityEngine.Object;

namespace PirateGame.Tests.T05
{
    public sealed class PlayerCollisionDamage : MonoBehaviour
    {
        public CombatWorld World;
        public int Calls;
        private void OnCollisionEnter(Collision collision)
        {
            Calls++;
            World.Player.ApplyDamage(10);
            World.Simulation.SetPaused(true);
        }
    }

    public sealed class CombatTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private DefinitionCatalogAsset rules;
        private CombatCatalogAsset authored;
        private CombatCatalog catalog;
        [SetUp] public void Setup()
        {
            rules = CombatFixture.CreateRules(); authored = CombatFixture.CreateCatalog(rules); catalog = authored.Freeze();
        }
        [TearDown] public void Cleanup()
        {
            foreach (var o in objects) if (o != null) Object.DestroyImmediate(o);
            objects.Clear(); Object.DestroyImmediate(authored); Object.DestroyImmediate(rules);
        }
        private GameObject Create(string name) { var go = new GameObject(name); objects.Add(go); return go; }
        private CombatTarget Target(string id, int team, Vector3 position, double health = 100)
        {
            var go = Create(id); go.transform.position = position;
            go.AddComponent<BoxCollider>().size = Vector3.one;
            var target = go.AddComponent<CombatTarget>(); target.Initialize(id, team, health); return target;
        }
        private ShipMotor Motor(string name, Vector3 position)
        {
            var go = Create(name); go.transform.position = position; var motor = go.AddComponent<ShipMotor>();
            motor.weaponOrigin = new GameObject("Muzzle").transform;
            motor.weaponOrigin.SetParent(go.transform, false); motor.weaponOrigin.localPosition = new Vector3(0, 0.6f, 1);
            return motor;
        }
        private EnemyShip Enemy(string id, string definition, Vector3 position)
        {
            var motor = Motor(id, position);
            var enemy = motor.gameObject.AddComponent<EnemyShip>();
            enemy.Initialize(EntityId.Authored(id), "test-region", catalog.Enemies[definition]); return enemy;
        }
        private CombatWorld World(CombatFixture.MemoryStore store = null, params ICombatEnemy[] enemies)
        {
            var motor = Motor("player", new Vector3(7, 0, -9));
            var sim = motor.gameObject.AddComponent<ShipSimulation>(); sim.motor = motor;
            sim.Bind(CombatFixture.CreateSession(catalog, store: store));
            var target = motor.gameObject.AddComponent<CombatTarget>(); target.motor = motor;
            var world = Create("world").AddComponent<CombatWorld>();
            world.Bind(sim, catalog, target, enemies, EntityId.Authored("test-context")); return world;
        }
        private SweptProjectile Shot(string owner = "player", int team = 0)
        {
            var p = new SweptProjectile();
            p.Launch(1, owner, team, new Vector3(0, 0.5f, 0), Vector3.forward, catalog.Weapons["cannon"], 30);
            return p;
        }
        [Test] public void AC13_DuplicateHitsDeathOnceAndNewOwnerOnReuse()
        {
            var target = Target("enemy", 1, Vector3.forward * 4, 30); int deaths = 0;
            target.Died += _ => deaths++;
            var shot = Shot();
            Assert.That(shot.Hit(target, 1), Is.True);
            Assert.That(shot.Hit(target, 1), Is.False); target.ApplyDamage(99);
            Assert.That(target.Health, Is.Zero); Assert.That(deaths, Is.EqualTo(1));
            Assert.That(target.GetComponent<Collider>().enabled, Is.False, "Defeated ships must not leave invisible collision.");
            var player = Target("player", 0, Vector3.zero);
            shot.Launch(2, "enemy", 1, Vector3.one, Vector3.left, catalog.Weapons["repeater"], 8);
            Assert.That(shot.Hit(player, 1), Is.False, "Delayed callback from the previous lease");
            Assert.That(shot.Hit(player, 2), Is.True); Assert.That(shot.Hit(player, 2), Is.False);
            Assert.That(player.Health, Is.EqualTo(92)); Assert.That(shot.Owner, Is.EqualTo("enemy"));
            Assert.That(shot.Remaining, Is.EqualTo(30)); Assert.That(shot.Direction, Is.EqualTo(Vector3.left));
        }
        [Test] public void SweptShotHitsCrossedThinTarget()
        {
            var target = Target("enemy", 1, new Vector3(0, 0.5f, 4));
            target.GetComponent<BoxCollider>().size = new Vector3(1, 1, 0.05f);
            Physics.SyncTransforms(); var shot = Shot(); shot.Step(0.2f);
            Assert.That(target.Health, Is.EqualTo(70)); Assert.That(shot.Active, Is.False);
        }
        [Test] public void WallBlocksTargetAndFriendlyHullIsIgnored()
        {
            var target = Target("enemy", 1, new Vector3(0, 0.5f, 8));
            Target("ally", 0, new Vector3(0, 0.5f, 2));
            var wall = Create("wall"); wall.transform.position = new Vector3(0, 0.5f, 4); wall.AddComponent<BoxCollider>();
            Physics.SyncTransforms(); var shot = Shot(); shot.Step(0.2f);
            Assert.That(target.Health, Is.EqualTo(100)); Assert.That(shot.Active, Is.False);
            Object.DestroyImmediate(wall); Physics.SyncTransforms(); shot = Shot(); shot.Step(0.2f);
            Assert.That(target.Health, Is.EqualTo(70));
        }
        [Test] public void InitialOverlapAndRangeExpiryAreBounded()
        {
            var target = Target("enemy", 1, new Vector3(0, 0.5f, 0));
            Physics.SyncTransforms(); var shot = Shot(); shot.Step(0.02f); shot.Step(0.02f);
            Assert.That(target.Health, Is.EqualTo(70));
            Object.DestroyImmediate(target.gameObject); Physics.SyncTransforms();
            shot = Shot(); shot.Step(10); Assert.That(shot.Active, Is.False); Assert.That(shot.Position.z, Is.EqualTo(45).Within(0.001));
        }
        [Test] public void DefinitionsDetachAndRejectUnknownOrInvalidContent()
        {
            authored.weapons[0].damage = 999;
            Assert.That(catalog.Weapons["cannon"].Damage, Is.EqualTo(30));
            authored.weapons[0].cooldown = float.NaN; Assert.Throws<ArgumentException>(() => authored.Freeze());
            authored.weapons[0].cooldown = 1; authored.enemies[0].weaponId = "missing";
            Assert.Throws<KeyNotFoundException>(() => authored.Freeze());
            authored.enemies[0].weaponId = "cannon"; authored.weapons[1].id = "cannon";
            Assert.Throws<ArgumentException>(() => authored.Freeze());
        }
        [Test] public void DockedLoadoutSelectsDifferentWeaponAndAtSeaRefitRejects()
        {
            var session = CombatFixture.CreateSession(catalog, embark: false);
            Assert.That(catalog.EquippedWeapon(session.Snapshot.Campaign).Id, Is.EqualTo("cannon"));
            var loadout = new Dictionary<string, string> { ["weapon"] = "owned-repeater", ["ability"] = "owned-brace" };
            Assert.That(session.SetLoadout(Guid.NewGuid(), loadout).IsSuccess, Is.True);
            var weapon = catalog.EquippedWeapon(session.Snapshot.Campaign);
            Assert.That(weapon.Damage, Is.EqualTo(8)); Assert.That(weapon.Cooldown, Is.LessThan(catalog.Weapons["cannon"].Cooldown));
            session.Embark(Guid.NewGuid(), new EmbarkPlan(Guid.NewGuid(), 1, "test", Array.Empty<EntityState>(), new Dictionary<string, string>()));
            Assert.That(session.SetLoadout(Guid.NewGuid(), loadout).IsSuccess, Is.False);
        }
        [Test] public void EnemyArchetypesPursueAndRetreatUsingMotor()
        {
            var raider = Enemy("raider", "raider", Vector3.zero);
            var gunner = Enemy("gunner", "gunner", Vector3.right * 100);
            raider.Step(Vector3.forward * 15, 0.02f, (_, __, ___) => {});
            gunner.Step(Vector3.right * 100 + Vector3.forward * 8, 0.02f, (_, __, ___) => {});
            Assert.That(raider.Mode, Is.EqualTo("pursue")); Assert.That(gunner.Mode, Is.EqualTo("retreat"));
            Assert.That(raider.Target.motor.Body.linearVelocity.z, Is.GreaterThan(0));
            Assert.That(gunner.Target.motor.Body.linearVelocity.z, Is.LessThan(0));
        }
        [Test] public void EntityRestoreValidatesBeforeMutationAndDoesNotEmitDeathAgain()
        {
            var enemy = Enemy("enemy", "raider", Vector3.zero); int deaths = 0; enemy.Target.Died += _ => deaths++;
            enemy.Target.ApplyDamage(999); var dead = enemy.Capture();
            Assert.That(enemy.Restore(dead).IsSuccess, Is.True); enemy.Target.ApplyDamage(1);
            Assert.That(deaths, Is.EqualTo(1)); Assert.That(enemy.Mode, Is.EqualTo("defeated"));
            var invalid = new EntityState(dead.Id, dead.DefinitionId, dead.Position, 10, true, dead.Loot, dead.Cooldowns, dead.BehaviorState);
            Assert.That(enemy.Restore(invalid).IsSuccess, Is.False); Assert.That(enemy.Target.Health, Is.Zero);
            Assert.That(enemy.GetComponent<Collider>().enabled, Is.False);
        }
        [UnityTest] public IEnumerator PauseFreezesShotsCooldownsAbilityAndEnemyMotor()
        {
            var enemy = Enemy("enemy", "gunner", new Vector3(20, 0, 20)); var world = World(null, enemy); var sim = world.Simulation;
            bool once = false;
            sim.TickStarted += _ => { if (!once) { once = true; sim.SetPaused(true); } };
            sim.Submit(new InputIntent(0, 0, 1, 0, true, true, false), false);
            yield return new WaitForSeconds(0.1f);
            Assert.That(sim.Session.IsPaused, Is.True); Assert.That(world.WeaponCooldown, Is.GreaterThan(0));
            double cooldown = world.WeaponCooldown, brace = world.BraceRemaining; var position = enemy.Target.motor.Body.position;
            var shots = world.Projectiles.Where(p => p.Active).Select(p => p.Position).ToArray(); var tick = sim.Session.Tick;
            yield return new WaitForSeconds(0.12f);
            Assert.That(sim.Session.Tick, Is.EqualTo(tick)); Assert.That(world.WeaponCooldown, Is.EqualTo(cooldown));
            Assert.That(world.BraceRemaining, Is.EqualTo(brace)); Assert.That(enemy.Target.motor.Body.position, Is.EqualTo(position));
            CollectionAssert.AreEqual(shots, world.Projectiles.Where(p => p.Active).Select(p => p.Position).ToArray());
            sim.SetPaused(false); yield return new WaitForSeconds(0.08f);
            Assert.That(world.WeaponCooldown, Is.LessThan(cooldown));
        }
        [UnityTest] public IEnumerator BraceReducesCollectedDamageAndCooldownPreventsReactivation()
        {
            var world = World(); var sim = world.Simulation; bool first = true;
            sim.TickStarted += _ => { if (first) { first = false; world.Player.ApplyDamage(40); sim.SetPaused(true); } };
            sim.Submit(new InputIntent(0, 0, 1, 0, false, true, false), false);
            yield return new WaitForSeconds(0.08f);
            Assert.That(sim.Session.Snapshot.Expedition.Health, Is.EqualTo(90));
            Assert.That(world.Player.Health, Is.EqualTo(90));
            sim.SetPaused(false); sim.Submit(new InputIntent(0, 0, 1, 0, false, true, false), false);
            yield return new WaitForSeconds(0.08f); sim.SetPaused(true);
            Assert.That(world.AbilityCooldown, Is.LessThan(catalog.AbilityCooldown)); Assert.That(world.BraceRemaining, Is.LessThan(catalog.BraceDuration));
        }
        [UnityTest] public IEnumerator CaptureCheckpointRestoreRetainsShotsTimersEnemyAndRejectsMalformedAtomically()
        {
            var enemy = Enemy("enemy", "gunner", new Vector3(20, 0, 20)); var world = World(null, enemy); var sim = world.Simulation;
            bool once = false; sim.TickStarted += _ => { if (!once) { once = true; sim.SetPaused(true); } };
            sim.Submit(new InputIntent(0, 0, -1, 0, true, true, false), false);
            yield return new WaitForSeconds(0.1f);
            var state = world.Capture(sim.Session.Snapshot.Expedition.Id, sim.Session.Tick);
            Assert.That(state.Entities.Count, Is.EqualTo(2));
            sim.SetPaused(false); Assert.That(sim.Session.Checkpoint(Guid.NewGuid(), state).IsSuccess, Is.True); sim.SetPaused(true);
            enemy.Target.ApplyDamage(30);
            Assert.That(world.Restore(state).IsSuccess, Is.True);
            Assert.That(enemy.Target.Health, Is.EqualTo(state.Entities[enemy.Id].Health));
            Assert.That(world.WeaponCooldown, Is.EqualTo(state.Cooldowns[CombatWorld.WeaponTimer]));
            var context = state.Entities[EntityId.Authored("test-context")];
            var payload = JsonUtility.FromJson<CombatContext>(context.BehaviorState);
            Assert.That(payload.shots.Length, Is.GreaterThan(0));
            Assert.That(world.Projectiles.Count(p => p.Active), Is.EqualTo(payload.shots.Length));
            payload.shots[0].owner = "missing";
            var badContext = new EntityState(context.Id, context.DefinitionId, context.Position, context.Health, false, context.Loot, context.Cooldowns, JsonUtility.ToJson(payload));
            var bad = new ExpeditionState(state.Id, state.Seed, state.RngState, state.Tick, state.Health, state.Speed, state.Position,
                state.Cargo, state.Modifiers, state.Cooldowns, state.Encounters, new[] { state.Entities[enemy.Id], badContext });
            string before = JsonUtility.ToJson(world.Projectiles[0].Capture());
            Assert.That(world.Restore(bad).IsSuccess, Is.False);
            Assert.That(JsonUtility.ToJson(world.Projectiles[0].Capture()), Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator FreshSessionRestoresLiveProjectilesAndDefeatedEnemy()
        {
            var enemy = Enemy("enemy", "raider", new Vector3(20, 0, 20)); var world = World(null, enemy); var sim = world.Simulation;
            bool once = false; sim.TickStarted += _ => { if (!once) { once = true; world.Player.ApplyDamage(40); sim.SetPaused(true); } };
            sim.Submit(new InputIntent(0, 0, -1, 0, true, true, false), false);
            yield return new WaitForSeconds(0.1f);
            enemy.Target.ApplyDamage(999);
            var state = world.Capture(sim.Session.Snapshot.Expedition.Id, sim.Session.Tick);
            sim.SetPaused(false); Assert.That(sim.Session.Checkpoint(Guid.NewGuid(), state).IsSuccess, Is.True); sim.SetPaused(true);
            var snapshot = sim.Session.Snapshot;
            Object.DestroyImmediate(world.gameObject); Object.DestroyImmediate(enemy.gameObject); Object.DestroyImmediate(sim.gameObject);
            var replacement = Enemy("enemy", "raider", Vector3.one * 100);
            var motor = Motor("restored-player", Vector3.zero);
            var restoredSim = motor.gameObject.AddComponent<ShipSimulation>(); restoredSim.motor = motor;
            var loaded = new CampaignSession(catalog.Rules, snapshot, new CombatFixture.MemoryStore(), new CombatFixture.ReadyArrival());
            restoredSim.Bind(loaded);
            var player = motor.gameObject.AddComponent<CombatTarget>(); player.motor = motor;
            var restored = Create("restored-world").AddComponent<CombatWorld>();
            restored.Bind(restoredSim, catalog, player, new[] { replacement }, EntityId.Authored("test-context"));
            int deaths = 0; player.Died += _ => deaths++;
            Assert.That(restored.Player.Health, Is.EqualTo(90), "Binding reads saved health before arrival/input resumes.");
            Assert.That(restored.Restore(state).IsSuccess, Is.True);
            Assert.That(replacement.Target.Defeated, Is.True);
            Assert.That(restored.Projectiles.Any(s => s.Active), Is.True);
            Assert.That(restored.BraceRemaining, Is.EqualTo(state.Cooldowns[CombatWorld.BraceTimer]));
            loaded.RetryArrival(); yield return new WaitForSeconds(0.08f);
            Assert.That(loaded.Tick, Is.GreaterThan(state.Tick)); Assert.That(replacement.Target.Defeated, Is.True);
            restoredSim.SetPaused(true);
            Assert.That(restored.Player.Health, Is.EqualTo(90)); Assert.That(restored.Player.Defeated, Is.False);
            Assert.That(deaths, Is.Zero); Assert.That(loaded.DrainEvents(), Is.Empty);
        }
        [UnityTest] public IEnumerator RetainedTickAndDisabledAdapterDoNotReplayCombat()
        {
            var enemy = Enemy("enemy", "gunner", new Vector3(20, 0, 20)); var world = World(null, enemy); var sim = world.Simulation;
            bool once = false; int shots = 0; world.ShotFired += _ => shots++;
            sim.TickStarted += _ => { if (!once) { once = true; sim.Session.SetPaused(true); } };
            sim.Submit(new InputIntent(0, 0, -1, 0, true, false, false), false);
            yield return new WaitForSeconds(0.1f);
            Assert.That(sim.HasPendingStep, Is.True);
            var position = enemy.Target.motor.Body.position; var count = shots; var cooldown = world.WeaponCooldown;
            sim.enabled = false; yield return new WaitForSeconds(0.08f);
            Assert.That(enemy.Target.motor.Body.position, Is.EqualTo(position)); Assert.That(shots, Is.EqualTo(count));
            sim.enabled = true; sim.SetPaused(false); yield return new WaitForSeconds(0.08f);
            Assert.That(sim.HasPendingStep, Is.False); Assert.That(shots, Is.EqualTo(count)); Assert.That(world.WeaponCooldown, Is.LessThan(cooldown));
        }
        [UnityTest] public IEnumerator FailedSaveFreezesCombatAndRetryDoesNotReplayShots()
        {
            var store = new CombatFixture.MemoryStore(); var world = World(store); var sim = world.Simulation;
            bool once = false; int shots = 0; world.ShotFired += _ => shots++;
            sim.TickStarted += _ => { if (!once) { once = true; sim.SetPaused(true); } };
            sim.Submit(new InputIntent(0, 0, 1, 0, true, false, false), false);
            yield return new WaitForSeconds(0.1f);
            var state = world.Capture(sim.Session.Snapshot.Expedition.Id, sim.Session.Tick);
            sim.SetPaused(false); store.Fail = true;
            Assert.That(sim.Session.Checkpoint(Guid.NewGuid(), state).Error, Is.EqualTo(RuleError.SaveFailed));
            var candidate = sim.Session.PendingSave; double timer = world.WeaponCooldown; var tick = sim.Session.Tick;
            yield return new WaitForSeconds(0.1f);
            Assert.That(sim.Session.Tick, Is.EqualTo(tick)); Assert.That(world.WeaponCooldown, Is.EqualTo(timer)); Assert.That(shots, Is.EqualTo(1));
            Assert.That(sim.Session.RetrySave().Error, Is.EqualTo(RuleError.SaveFailed)); Assert.That(sim.Session.PendingSave, Is.SameAs(candidate));
            store.Fail = false; Assert.That(sim.Session.RetrySave().IsSuccess, Is.True);
            yield return new WaitForSeconds(0.08f);
            Assert.That(shots, Is.EqualTo(1)); Assert.That(world.WeaponCooldown, Is.LessThan(timer));
        }
        [UnityTest] public IEnumerator LethalDamageWithFailedSinkPublishesOneOutcomeWithoutReplay()
        {
            var store = new CombatFixture.MemoryStore(); var world = World(store); var sim = world.Simulation;
            int ticks = 0, deaths = 0; world.Player.Died += _ => deaths++;
            sim.Session.DrainEvents();
            sim.Submit(new InputIntent(1, 0, 1, 0, true, false, false), false);
            sim.TickStarted += _ => { ticks++; store.Fail = true; world.Player.ApplyDamage(200); };
            yield return new WaitForSeconds(0.1f);
            Assert.That(sim.Session.PendingSave.Command, Is.EqualTo("Sink")); Assert.That(sim.HasPendingStep, Is.False);
            Assert.That(sim.Session.Snapshot.Expedition.Health, Is.Zero);
            Assert.That(world.Player.Health, Is.Zero); Assert.That(world.Player.Defeated, Is.True);
            Assert.That(sim.Session.DrainEvents(), Is.Empty); Assert.That(deaths, Is.Zero);
            var pending = sim.Session.PendingSave; var position = sim.motor.Body.position; var timer = world.WeaponCooldown;
            for (int retry = 0; retry < 3; retry++)
            {
                Assert.That(sim.Session.RetrySave().Error, Is.EqualTo(RuleError.SaveFailed));
                yield return new WaitForSeconds(0.06f);
                Assert.That(sim.Session.PendingSave, Is.SameAs(pending));
                Assert.That(world.Player.Health, Is.Zero); Assert.That(world.Player.Defeated, Is.True);
                Assert.That(sim.motor.Body.position, Is.EqualTo(position)); Assert.That(world.WeaponCooldown, Is.EqualTo(timer));
                Assert.That(sim.Session.DrainEvents(), Is.Empty); Assert.That(deaths, Is.Zero);
            }
            yield return new WaitForSeconds(0.08f); Assert.That(ticks, Is.EqualTo(1));
            store.Fail = false; Assert.That(sim.Session.RetrySave().IsSuccess, Is.True);
            Assert.That(world.Player.Health, Is.Zero); Assert.That(world.Player.Defeated, Is.True);
            Assert.That(sim.Session.DrainEvents().Count(e => e.Command == "Sink"), Is.EqualTo(1));
            yield return new WaitForSeconds(0.05f); Assert.That(ticks, Is.EqualTo(1));
            Assert.That(sim.Session.DrainEvents(), Is.Empty); Assert.That(deaths, Is.Zero);
        }

        [UnityTest] public IEnumerator R1_TickDamagePauseReadsProcessedStateWithoutAnotherTick()
        {
            var world = World(); var sim = world.Simulation;
            sim.TickStarted += _ => {
                world.Player.ApplyDamage(4); world.Player.ApplyDamage(6);
                Assert.That(world.Player.Health, Is.EqualTo(100), "Collection has not published damage yet.");
                sim.SetPaused(true);
            };
            yield return new WaitForSeconds(0.1f);
            Assert.That(sim.HasPendingStep, Is.False); Assert.That(sim.Session.Tick, Is.EqualTo(1));
            Assert.That(sim.Session.Snapshot.Expedition.Health, Is.EqualTo(90));
            Assert.That(world.Player.Health, Is.EqualTo(90)); Assert.That(world.Player.Defeated, Is.False);
            yield return new WaitForSeconds(0.1f);
            Assert.That(sim.Session.Tick, Is.EqualTo(1)); Assert.That(world.Player.Health, Is.EqualTo(90));
        }

        [UnityTest] public IEnumerator R1_PhysicalCollisionDamageAndPauseReadsProcessedState()
        {
            var world = World(); var sim = world.Simulation;
            sim.GetComponent<BoxCollider>().size = Vector3.one;
            var callback = sim.gameObject.AddComponent<PlayerCollisionDamage>(); callback.World = world;
            var wall = Create("wall"); wall.transform.position = sim.motor.Body.position + Vector3.forward * 0.8f;
            wall.AddComponent<BoxCollider>().size = Vector3.one;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.15f);
            Assert.That(callback.Calls, Is.EqualTo(1)); Assert.That(sim.Session.IsPaused, Is.True);
            Assert.That(sim.HasPendingStep, Is.False); Assert.That(sim.Session.Snapshot.Expedition.Health, Is.EqualTo(90));
            Assert.That(world.Player.Health, Is.EqualTo(90)); Assert.That(world.Player.Defeated, Is.False);
            var tick = sim.Session.Tick;
            yield return new WaitForSeconds(0.1f);
            Assert.That(sim.Session.Tick, Is.EqualTo(tick)); Assert.That(world.Player.Health, Is.EqualTo(90));
        }

        [UnityTest] public IEnumerator R1_CommittedSinkPersistsReadStateAndNewCompositionStartsHealthy()
        {
            var world = World(); var sim = world.Simulation; var id = sim.Session.Snapshot.Expedition.Id;
            int deaths = 0; world.Player.Died += _ => deaths++;
            sim.TickStarted += _ => world.Player.ApplyDamage(100);
            yield return new WaitForSeconds(0.1f);
            Assert.That(sim.Session.Snapshot.Expedition, Is.Null);
            Assert.That(sim.Session.Snapshot.Campaign.ResolvedExpeditions[id], Is.EqualTo(Outcome.Sunk));
            Assert.That(world.Player.Health, Is.Zero); Assert.That(world.Player.Defeated, Is.True);
            Assert.That(sim.Session.DrainEvents().Count(e => e.Command == "Sink"), Is.EqualTo(1));
            Assert.That(deaths, Is.Zero);
            sim.enabled = false;
            Assert.That(sim.Session.Embark(Guid.NewGuid(), new EmbarkPlan(Guid.NewGuid(), 5, "new", Array.Empty<EntityState>(), new Dictionary<string, string>())).IsSuccess, Is.True);
            var next = Create("next-world").AddComponent<CombatWorld>();
            var target = Create("next-player").AddComponent<CombatTarget>(); target.motor = sim.motor;
            next.Bind(sim, catalog, target, Array.Empty<ICombatEnemy>(), EntityId.Authored("next-context"));
            Assert.That(next.Player.Health, Is.EqualTo(100)); Assert.That(next.Player.Defeated, Is.False);
            Assert.That(world.Player.Defeated, Is.True, "Old target remains scoped to its sunk voyage.");
        }

        [UnityTest] public IEnumerator R1_DockingDoesNotNotifyOrExposePlayerDeath()
        {
            var world = World(); var sim = world.Simulation; int deaths = 0;
            world.Player.Died += _ => deaths++;
            sim.TickStarted += _ => sim.Session.RequestDock(Guid.NewGuid(), sim.Session.Snapshot.Expedition.Id, "home");
            yield return new WaitForSeconds(0.1f);
            Assert.That(sim.Session.Snapshot.Expedition, Is.Null); Assert.That(world.Player.Defeated, Is.False);
            Assert.That(world.Player.Health, Is.GreaterThan(0)); Assert.That(deaths, Is.Zero);
            var events = sim.Session.DrainEvents();
            Assert.That(events.Count(e => e.Command == "Dock"), Is.EqualTo(1));
            Assert.That(events.Any(e => e.Command == "Sink"), Is.False);
        }

        [UnityTest] public IEnumerator R1_DamagedCheckpointFailureAndRetryKeepProcessedHealth()
        {
            var store = new CombatFixture.MemoryStore(); var world = World(store); var sim = world.Simulation;
            bool once = false;
            sim.TickStarted += _ => { if (!once) { once = true; world.Player.ApplyDamage(10); sim.SetPaused(true); } };
            yield return new WaitForSeconds(0.1f);
            var captured = world.Capture(sim.Session.Snapshot.Expedition.Id, sim.Session.Tick);
            sim.SetPaused(false); store.Fail = true;
            Assert.That(sim.Session.Checkpoint(Guid.NewGuid(), captured).Error, Is.EqualTo(RuleError.SaveFailed));
            Assert.That(world.Player.Health, Is.EqualTo(90)); Assert.That(sim.Session.Snapshot.Expedition.Health, Is.EqualTo(90));
            yield return new WaitForSeconds(0.1f);
            Assert.That(sim.Session.RetrySave().Error, Is.EqualTo(RuleError.SaveFailed));
            Assert.That(world.Player.Health, Is.EqualTo(90));
            store.Fail = false; Assert.That(sim.Session.RetrySave().IsSuccess, Is.True);
            Assert.That(world.Player.Health, Is.EqualTo(90)); Assert.That(world.Player.Defeated, Is.False);
        }
    }
}
