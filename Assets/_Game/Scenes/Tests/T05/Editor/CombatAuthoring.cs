using System;
using System.IO;
using PirateGame.Content.Combat;
using PirateGame.Gameplay.AI;
using PirateGame.Gameplay.Ships;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PirateGame.Tests.T05.Editor
{
    public sealed class CombatValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => Validate();
        [InitializeOnLoadMethod] private static void Install() => EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            try { Validate(); } catch (Exception e) { EditorApplication.isPlaying = false; Debug.LogException(e); }
        };
        public static void Validate()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CombatCatalogAsset"))
                AssetDatabase.LoadAssetAtPath<CombatCatalogAsset>(AssetDatabase.GUIDToAssetPath(guid)).Freeze();
        }
    }
    public static class CombatAuthoring
    {
        public const string Scene = "Assets/_Game/Scenes/Tests/T05/Combat.unity";
        public static void Create()
        {
            if (File.Exists(Scene)) throw new InvalidOperationException("Preserve existing T05 scene.");
            Directory.CreateDirectory("Assets/_Game/Prefabs/Combat");
            var rules = CombatFixture.CreateRules();
            AssetDatabase.CreateAsset(rules, "Assets/_Game/Content/Combat/CombatRules.asset");
            var catalog = CombatFixture.CreateCatalog(rules);
            AssetDatabase.CreateAsset(catalog, "Assets/_Game/Content/Combat/CombatCatalog.asset");
            catalog.Freeze();
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Tests/T04/ShipControls.unity");
            EditorSceneManager.SaveScene(scene, Scene);
            foreach (var old in Object.FindObjectsByType<T04.ShipTestScene>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
            Object.DestroyImmediate(GameObject.Find("Island collision test wall"));
            var simulation = Object.FindFirstObjectByType<ShipSimulation>();
            var fixture = new GameObject("T05 combat fixture").AddComponent<CombatFixture>();
            fixture.catalog = catalog; fixture.simulation = simulation;
            fixture.pursuer = Enemy("Raider", new Vector3(15, 0, 8), new Color(0.8f, 0.15f, 0.12f));
            fixture.gunner = Enemy("Gunner", new Vector3(27, 0, 12), new Color(0.5f, 0.14f, 0.65f));
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        private static EnemyShip Enemy(string name, Vector3 position, Color color)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Ships/PlayerCutter.prefab");
            var ship = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(ship, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            ship.name = name; ship.transform.position = position; ship.transform.rotation = Quaternion.Euler(0, 180, 0);
            Object.DestroyImmediate(ship.GetComponent<ShipSimulation>());
            var enemy = ship.AddComponent<EnemyShip>();
            var material = new Material(Shader.Find("HDRP/Lit")); material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, "Assets/_Game/Content/Combat/" + name + ".mat");
            foreach (var renderer in ship.GetComponentsInChildren<Renderer>())
                if (renderer.name.IndexOf("sail", StringComparison.OrdinalIgnoreCase) >= 0) renderer.sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(ship, "Assets/_Game/Prefabs/Combat/" + name + ".prefab");
            return enemy;
        }
        public static void Build()
        {
            CombatValidation.Validate();
            var args = Environment.GetCommandLineArgs();
            int output = Array.IndexOf(args, "-t05-build");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene },
                locationPathName = output >= 0 ? args[output + 1] : "Builds/T05/Combat.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Combat build failed.");
        }
        public static void Refine()
        {
            var scene = EditorSceneManager.OpenScene(Scene);
            var fixture = Object.FindFirstObjectByType<CombatFixture>();
            fixture.pursuer.transform.position = new Vector3(15, 0, 8);
            fixture.gunner.transform.position = new Vector3(27, 0, 12);
            foreach (var row in fixture.catalog.enemies) row.reloadScale = 3;
            EditorUtility.SetDirty(fixture.catalog);
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
        }
    }
}
