using System.IO;
using PirateGame.Gameplay.Ships;
using PirateGame.Gameplay.Input;
using PirateGame.Presentation.Ships;
using PirateGame.Presentation.Cameras;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PirateGame.Tests.T04.Editor
{
    public static class ShipTestAuthoring
    {
        public const string ScenePath = "Assets/_Game/Scenes/Tests/T04/ShipControls.unity";
        public static void Build()
        {
            var args = System.Environment.GetCommandLineArgs();
            var outputIndex = System.Array.IndexOf(args, "-t04-build-output");
            var output = outputIndex >= 0 ? args[outputIndex + 1] : "Builds/T04/ShipControls.exe";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.InvalidOperationException("T04 build failed: " + report.summary.result);
        }
        public static void RefineCollision()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            AddIslandCollision();
            EditorSceneManager.SaveScene(scene);
        }
        private static void AddIslandCollision()
        {
            // T02 art is deliberately visual-only. Add static collision in our scene.
            foreach (var mesh in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (mesh.name == "Shore" && mesh.GetComponent<Collider>() == null)
                    mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.sharedMesh;
                if (mesh.name == "Pier plank" && mesh.GetComponent<Collider>() == null)
                    mesh.gameObject.AddComponent<BoxCollider>();
            }
        }
        [MenuItem("Pirate Prototype/T04/Author Ship Fixture")]
        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new System.InvalidOperationException("T04 scene already exists; preserve authored assets.");
            Directory.CreateDirectory("Assets/_Game/Prefabs/Ships");
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Tests/T02/WaterTest.unity");
            EditorSceneManager.SaveScene(scene, ScenePath);
            foreach (var component in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (component.GetType().Name == "OceanProof") Object.DestroyImmediate(component.gameObject);
            Object.DestroyImmediate(GameObject.Find("Route Root (XZ and yaw only)"));
            Object.DestroyImmediate(GameObject.Find("Stable dummy attack marker"));
            var ship = new GameObject("Player cutter");
            ship.transform.position = new Vector3(7, 0, -9);
            var body = ship.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var collider = ship.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.8f, 1, 4.8f);
            var motor = ship.AddComponent<ShipMotor>();
            motor.weaponOrigin = Child("WeaponOrigin", ship.transform, new Vector3(0, 0.6f, 2.4f));
            motor.interactionOrigin = Child("InteractionOrigin", ship.transform, Vector3.zero);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Art/Prototype/StarterCutter.prefab"));
            model.transform.SetParent(ship.transform, false);
            foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            model.AddComponent<ShipBobbing>();
            var simulation = ship.AddComponent<ShipSimulation>();
            simulation.motor = motor;
            PrefabUtility.SaveAsPrefabAsset(ship, "Assets/_Game/Prefabs/Ships/PlayerCutter.prefab");
            var input = ship.AddComponent<ShipKeyboardMouse>();
            input.simulation = simulation;
            input.aimCamera = Camera.main;
            var follow = Camera.main.gameObject.AddComponent<ShipFollowCamera>();
            follow.target = ship.transform;
            Camera.main.fieldOfView = 50;
            new GameObject("T04 fixture session").AddComponent<ShipTestScene>().simulation = simulation;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Island collision test wall";
            wall.transform.position = new Vector3(7, 0, 22);
            wall.transform.localScale = new Vector3(20, 5, 2);
            wall.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Prototype/Cool rock.mat");
            AddIslandCollision();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        private static Transform Child(string name, Transform parent, Vector3 position)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false); child.localPosition = position; return child;
        }
    }
}
