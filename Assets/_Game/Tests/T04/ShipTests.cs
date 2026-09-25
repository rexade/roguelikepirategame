using System.Collections;
using NUnit.Framework;
using PirateGame.Gameplay.Ships;
using PirateGame.Gameplay.Input;
using PirateGame.Presentation.Ships;
using PirateGame.Rules.Application;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace PirateGame.Tests.T04
{
    public sealed class ShipTests
    {
        private GameObject ship;
        private ShipMotor motor;
        [SetUp] public void Setup()
        {
            ship = new GameObject("test ship");
            motor = ship.AddComponent<ShipMotor>();
            ship.GetComponent<BoxCollider>().size = new Vector3(1.8f, 1, 4.8f);
            motor.Configure(ShipTestScene.CreateSession().ShipStats());
            motor.weaponOrigin = new GameObject("weapon").transform;
            motor.weaponOrigin.SetParent(ship.transform, false);
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(ship); }

        [TestCase(8, false)] [TestCase(50, false)] [TestCase(50, true)]
        public void CollisionAccelerationBrakingAndPlanarConstraints(float speed, bool shoreline)
        {
            motor.Configure(new System.Collections.Generic.Dictionary<string, double> { ["speed"] = speed });
            motor.acceleration = 100;
            var previous = Physics.simulationMode;
            var wall = new GameObject("island");
            wall.AddComponent<BoxCollider>().size = new Vector3(20, 10, 1);
            wall.transform.position = new Vector3(0, 0, 12);
            if (shoreline)
            {
                Object.DestroyImmediate(wall);
                wall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.DestroyImmediate(wall.GetComponent<Collider>());
                wall.AddComponent<MeshCollider>().sharedMesh = wall.GetComponent<MeshFilter>().sharedMesh;
                wall.transform.position = new Vector3(0, -0.2f, 30);
                wall.transform.localScale = new Vector3(28, 3, 36);
            }
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                Physics.SyncTransforms();
                for (int i = 0; i < 400; i++) { motor.Step(new InputIntent(1, 0, 1, 0, false, false, false), false, 0.02f); Physics.Simulate(0.02f); }
                Assert.That(motor.Body.position.z, Is.InRange(5f, shoreline ? 10f : 9.2f));
                Assert.That(motor.Body.position.y, Is.EqualTo(0).Within(0.0001));
                Assert.That(motor.AimDirection, Is.EqualTo(Vector3.right));
                Object.DestroyImmediate(wall); wall = null;
                for (int i = 0; i < 100; i++) { motor.Step(new InputIntent(1, 1, 0, 0, false, false, false), false, 0.02f); Physics.Simulate(0.02f); }
                Assert.That(Quaternion.Angle(Quaternion.identity, motor.Body.rotation), Is.GreaterThan(90));
                for (int i = 0; i < 300; i++) { motor.Step(default, true, 0.02f); Physics.Simulate(0.02f); }
                Assert.That(motor.Speed, Is.LessThan(0.01));
            }
            finally { Physics.simulationMode = previous; if (wall != null) Object.DestroyImmediate(wall); }
        }

        [UnityTest] public IEnumerator PauseFreezesBodyAndPublishedClockThenResumes()
        {
            var simulation = ship.AddComponent<ShipSimulation>(); simulation.motor = motor;
            var session = ShipTestScene.CreateSession(); simulation.Bind(session);
            simulation.Submit(new InputIntent(1, 0, 0, 1, false, false, false), false);
            yield return new WaitForSeconds(0.2f);
            Assert.That(session.Tick, Is.GreaterThan(0));
            session.SetPaused(true);
            yield return null;
            var position = motor.Body.position; var tick = session.Tick;
            yield return new WaitForSeconds(0.15f);
            Assert.That(motor.Body.position, Is.EqualTo(position)); Assert.That(session.Tick, Is.EqualTo(tick));
            session.SetPaused(false);
            yield return new WaitForSeconds(0.1f);
            Assert.That(session.Tick, Is.GreaterThan(tick));
        }

        [UnityTest] public IEnumerator BobbingDoesNotMoveAimOrHitbox()
        {
            float originalLodBias = QualitySettings.lodBias;
            var visual = new GameObject("visual"); visual.transform.SetParent(ship.transform, false);
            var bob = visual.AddComponent<ShipBobbing>(); bob.amplitude = 0.5f;
            var p = motor.Body.position; var aim = motor.weaponOrigin.position;
            try
            {
                QualitySettings.lodBias = 0.25f;
                yield return null; yield return null;
                Assert.That(visual.transform.localPosition.sqrMagnitude, Is.GreaterThan(0));
                Assert.That(motor.Body.position, Is.EqualTo(p)); Assert.That(motor.weaponOrigin.position, Is.EqualTo(aim));
                Assert.That(ship.GetComponent<BoxCollider>().size, Is.EqualTo(new Vector3(1.8f, 1, 4.8f)));
            }
            finally { QualitySettings.lodBias = originalLodBias; }
        }

        [UnityTest] public IEnumerator KeyboardMouseBindingsAndEdges()
        {
            var oldBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var keyboard = InputSystem.AddDevice<Keyboard>(); var mouse = InputSystem.AddDevice<Mouse>();
            var simulation = ship.AddComponent<ShipSimulation>(); simulation.motor = motor;
            simulation.Bind(ShipTestScene.CreateSession());
            var input = ship.AddComponent<ShipKeyboardMouse>(); input.simulation = simulation;
            var cameraObject = new GameObject("aim camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            camera.pixelRect = new Rect(0, 0, 800, 600);
            camera.transform.SetPositionAndRotation(new Vector3(10, 10, 10), Quaternion.Euler(90, 0, 0));
            input.aimCamera = camera;
            int abilities = 0, interactions = 0; bool fired = false, steered = false;
            simulation.TickStarted += intent => { if (intent.Ability) abilities++; if (intent.Interact) interactions++; fired |= intent.Fire; steered |= intent.Throttle > 0 && intent.Turn > 0; };
            try
            {
                yield return null;
                input.SendMessage("OnApplicationFocus", true);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D, Key.E));
                InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(400, 300) }.WithButton(MouseButton.Left).WithButton(MouseButton.Right));
                yield return new WaitForSeconds(0.15f);
                Assert.That(steered, Is.True, "W/D reach fixed-step intent"); Assert.That(fired, Is.True, "Mouse fire reaches fixed-step intent");
                Assert.That(abilities, Is.EqualTo(1)); Assert.That(interactions, Is.EqualTo(1));
                Assert.That(motor.AimDirection.x, Is.GreaterThan(0.3f));
                Assert.That(motor.AimDirection.z, Is.GreaterThan(0.3f));
                Assert.That(motor.AimDirection.magnitude, Is.EqualTo(1).Within(0.0001));
                input.SendMessage("OnApplicationFocus", false);
                yield return null;
                Assert.That(simulation.Intent.Throttle, Is.Zero);
                Assert.That(simulation.Intent.Fire, Is.False);
                simulation.Session.SetPaused(true); yield return null;
                Assert.That(simulation.Intent.Throttle, Is.Zero); Assert.That(simulation.Intent.Fire, Is.False);
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
                Object.DestroyImmediate(cameraObject);
                InputSystem.settings.backgroundBehavior = oldBackground;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
#endif
            }
        }
    }
}
