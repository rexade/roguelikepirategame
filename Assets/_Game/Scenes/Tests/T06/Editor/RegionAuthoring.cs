using System;
using System.IO;
using System.Linq;
using PirateGame.Content.World;
using PirateGame.Gameplay.Ships;
using PirateGame.Gameplay.World;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PirateGame.Tests.T06.Editor
{
    public sealed class RegionValidation : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => Validate();
        [InitializeOnLoadMethod] private static void Install() => EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            try { Validate(); } catch (Exception e) { EditorApplication.isPlaying = false; Debug.LogException(e); }
        };
        public static void Validate()
        {
            var paths = AssetDatabase.FindAssets("t:FirstRegionAsset").Select(AssetDatabase.GUIDToAssetPath).ToArray();
            FirstRegionAsset.ValidateIdentities(paths.SelectMany(p => AssetDatabase.LoadAssetAtPath<FirstRegionAsset>(p).Identities(p)));
            foreach (var path in paths) { var asset = AssetDatabase.LoadAssetAtPath<FirstRegionAsset>(path); asset.Validate(SalvageFixture.Catalog(asset)); }
        }
    }
    public static class RegionAuthoring
    {
        public const string Content = "Assets/_Game/Content/World/FirstRegion/FirstRegion.asset";
        public const string Scene = "Assets/_Game/Scenes/Tests/T06/Salvage.unity";
        public const string RegionScene = "Assets/_Game/Scenes/Regions/FirstRegion.unity";
        private const string Prefabs = "Assets/_Game/Prefabs/World/";
        public static void Create()
        {
            if (File.Exists(Scene) || File.Exists(Content)) throw new InvalidOperationException("Preserve existing T06 assets.");
            Directory.CreateDirectory(Prefabs); Directory.CreateDirectory("Assets/_Game/Scenes/Regions");
            var content = ScriptableObject.CreateInstance<FirstRegionAsset>();
            content.islands = new[] {
                new IslandSite { id = "first:needle", position = new Vector2(-20, 20), size = new Vector2(16, 22) },
                new IslandSite { id = "first:lookout", position = new Vector2(25, 65), size = new Vector2(20, 24) },
                new IslandSite { id = "first:twin-rock", position = new Vector2(-18, 108), size = new Vector2(22, 18) } };
            content.salvage = new[] {
                Site("first:barrel-01", "barrel", 0, 8, 3, 0), Site("first:barrel-02", "barrel", 0, 38, 2, 0),
                Site("first:wreck-01", "wreck", 0, 82, 2, 2), Site("first:barrel-03", "barrel", 14, 112, 1, 1),
                Site("first:wreck-02", "wreck", 48, 45, 4, 1) };
            content.encounters = new[] { new EncounterSite { id = "first:encounter-01", position = new Vector2(0, 60) },
                new EncounterSite { id = "first:encounter-02", position = new Vector2(42, 95) } };
            content.route = new[] { content.dock, new Vector2(0, 16), new Vector2(0, 48), new Vector2(0, 85),
                new Vector2(12, 112), new Vector2(45, 110), new Vector2(48, 45), new Vector2(25, -12), content.dock };
            AssetDatabase.CreateAsset(content, Content);
            var timber = Material("Salvage timber", new Color(0.38f, 0.24f, 0.16f));
            var iron = Material("Salvage iron", new Color(0.22f, 0.3f, 0.32f));
            var rock = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Prototype/Cool rock.mat");
            var grass = Material("Island green", new Color(0.18f, 0.48f, 0.26f));
            var barrel = new GameObject("Barrel salvage"); barrel.AddComponent<SalvageSource>();
            Shape("Staves", barrel.transform, PrimitiveType.Cylinder, new Vector3(0, 0.5f, 0), new Vector3(1.4f, 0.65f, 1.4f), timber, false);
            Shape("Hoop", barrel.transform, PrimitiveType.Cylinder, new Vector3(0, 0.5f, 0), new Vector3(1.48f, 0.12f, 1.48f), iron, false);
            var barrelPrefab = PrefabUtility.SaveAsPrefabAsset(barrel, Prefabs + "Barrel.prefab"); Object.DestroyImmediate(barrel);
            var wreck = new GameObject("Wreck salvage"); wreck.AddComponent<SalvageSource>();
            for (int i = 0; i < 4; i++) Shape("Broken plank", wreck.transform, PrimitiveType.Cube, new Vector3(i * 0.6f - 0.9f, 0.1f, 0), new Vector3(0.45f, 0.5f, 3 + i % 2), timber, false);
            Shape("Broken mast", wreck.transform, PrimitiveType.Cube, new Vector3(0, 0.8f, 0), new Vector3(0.3f, 2, 0.3f), timber, false);
            var wreckPrefab = PrefabUtility.SaveAsPrefabAsset(wreck, Prefabs + "Wreck.prefab"); Object.DestroyImmediate(wreck);
            var regionScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Homeward Reach"); var region = root.AddComponent<SalvageRegion>();
            region.content = content; region.barrelPrefab = barrelPrefab; region.wreckPrefab = wreckPrefab;
            Island(root.transform, "Home harbor", new Vector2(-8, -33), new Vector2(30, 20), rock, grass);
            Shape("Harbor pier", root.transform, PrimitiveType.Cube, new Vector3(-7, 0.6f, -18), new Vector3(4, 1, 12), timber, true);
            foreach (var island in content.islands) Island(root.transform, island.id, island.position, island.size, rock, grass);
            var dock = new GameObject(content.homeId + " docking zone"); dock.transform.SetParent(root.transform); dock.transform.position = new Vector3(content.dock.x, 0, content.dock.y);
            var zone = dock.AddComponent<SphereCollider>(); zone.isTrigger = true; zone.radius = content.dockRadius;
            foreach (var encounter in content.encounters) { var marker = new GameObject(encounter.id); marker.transform.SetParent(root.transform); marker.transform.position = new Vector3(encounter.position.x, 0, encounter.position.y); }
            Decorate(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "FirstRegion.prefab");
            EditorSceneManager.SaveScene(regionScene, RegionScene);
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Tests/T04/ShipControls.unity");
            EditorSceneManager.SaveScene(scene, Scene);
            foreach (var old in Object.FindObjectsByType<T04.ShipTestScene>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
            foreach (var name in new[] { "Island collision test wall", "Harbor island", "Island variant", "Harbor light", "Harbor reflection", "Menu" })
            { var old = GameObject.Find(name); if (old != null) Object.DestroyImmediate(old); }
            RemoveOldPier(scene);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "FirstRegion.prefab"));
            var simulation = Object.FindFirstObjectByType<ShipSimulation>();
            simulation.transform.position = new Vector3(content.dock.x, 0, content.dock.y);
            var fixture = new GameObject("T06 isolated session").AddComponent<SalvageFixture>(); fixture.simulation = simulation;
            fixture.region = instance.GetComponent<SalvageRegion>();
            var interaction = simulation.gameObject.AddComponent<SalvageInteraction>(); interaction.simulation = simulation; fixture.interaction = interaction;
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); RegionValidation.Validate();
        }
        private static SalvageSite Site(string id, string definition, float x, float z, int wood, int iron) => new SalvageSite { id = id, definitionId = definition, position = new Vector2(x,z), wood = wood, iron = iron };
        private static void RemoveOldPier(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name.StartsWith("Shore foam ", StringComparison.Ordinal) || root.name == "Pier plank" || root.name == "Pier piling" || root.name == "Harbor lamp post" || root.name == "Lantern")
                    Object.DestroyImmediate(root);
        }
        public static void Refine()
        {
            var prefab = PrefabUtility.LoadPrefabContents(Prefabs + "FirstRegion.prefab");
            Decorate(prefab.transform); PrefabUtility.SaveAsPrefabAsset(prefab, Prefabs + "FirstRegion.prefab"); PrefabUtility.UnloadPrefabContents(prefab);
            var region = EditorSceneManager.OpenScene(RegionScene);
            foreach (var root in region.GetRootGameObjects()) Object.DestroyImmediate(root);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "FirstRegion.prefab"));
            EditorSceneManager.SaveScene(region);
            var scene = EditorSceneManager.OpenScene(Scene); RemoveOldPier(scene); EditorSceneManager.SaveScene(scene);
        }
        private static void Decorate(Transform region)
        {
            var previous = region.Find("Landmarks"); if (previous != null) Object.DestroyImmediate(previous.gameObject);
            var parent = new GameObject("Landmarks").transform; parent.SetParent(region, false);
            var rock = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Prototype/Cool rock.mat");
            var timber = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Content/World/FirstRegion/Salvage timber.mat");
            var iron = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Content/World/FirstRegion/Salvage iron.mat");
            Shape("Needle rock", parent, PrimitiveType.Sphere, new Vector3(-21, 5, 22), new Vector3(5, 12, 6), rock, false);
            Shape("Needle foot", parent, PrimitiveType.Sphere, new Vector3(-17, 3, 18), new Vector3(5, 5, 7), rock, false);
            Shape("Lookout tower", parent, PrimitiveType.Cylinder, new Vector3(25, 5, 65), new Vector3(4, 3, 4), rock, false);
            Shape("Lookout roof", parent, PrimitiveType.Cube, new Vector3(25, 8.3f, 65), new Vector3(5, 0.7f, 5), iron, false);
            Shape("Twin rock west", parent, PrimitiveType.Sphere, new Vector3(-23, 5, 109), new Vector3(5, 10, 6), rock, false);
            Shape("Twin rock east", parent, PrimitiveType.Sphere, new Vector3(-14, 4, 108), new Vector3(6, 8, 5), rock, false);
            Shape("Harbor storehouse", parent, PrimitiveType.Cube, new Vector3(-8, 4.5f, -30), new Vector3(9, 4, 6), timber, false);
            Shape("Harbor roof", parent, PrimitiveType.Cube, new Vector3(-8, 6.8f, -30), new Vector3(10, 0.7f, 7), iron, false);
            foreach (float z in new[] { -22f, -18f, -14f })
                foreach (float x in new[] { -8.7f, -5.3f })
                    Shape("Pier post", parent, PrimitiveType.Cylinder, new Vector3(x, 0.6f, z), new Vector3(0.5f, 1.4f, 0.5f), timber, false);
        }
        private static Material Material(string name, Color color)
        { var material = new Material(Shader.Find("HDRP/Lit")) { name = name }; material.SetColor("_BaseColor", color); AssetDatabase.CreateAsset(material, "Assets/_Game/Content/World/FirstRegion/" + name + ".mat"); return material; }
        private static void Island(Transform parent, string name, Vector2 p, Vector2 size, Material rock, Material grass)
        {
            var root = new GameObject(name); root.transform.SetParent(parent); root.transform.position = new Vector3(p.x, 0, p.y);
            Shape("Shore collision", root.transform, PrimitiveType.Cylinder, new Vector3(0, 0.4f, 0), new Vector3(size.x, 2, size.y), rock, true);
            Shape("Grassy crown", root.transform, PrimitiveType.Cylinder, new Vector3(0, 2.3f, 0), new Vector3(size.x * 0.83f, 0.5f, size.y * 0.83f), grass, false);
        }
        private static void Shape(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool solid)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (solid) go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        }
        public static void Build()
        {
            BuildTo("Builds/T06/Salvage.exe");
        }
        public static void BuildR1()
        {
            BuildTo("Builds/T06-R1/Salvage.exe");
        }
        private static void BuildTo(string path)
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = path, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("T06 build failed.");
        }
    }
}
