using System;
using System.IO;
using PirateGame.Content.Definitions;
using PirateGame.Core;
using PirateGame.UI.Harbor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace PirateGame.Tests.T07.Editor
{
    public static class HarborAuthoring
    {
        public const string Scene = "Assets/_Game/Scenes/Tests/T07/Harbor.unity";
        public const string Content = "Assets/_Game/Content/Progression/HarborCatalog.asset";
        public static void Create()
        {
            if (File.Exists(Scene) || File.Exists(Content)) throw new InvalidOperationException("Preserve existing T07 assets.");
            Directory.CreateDirectory(Path.GetDirectoryName(Content)); AssetDatabase.Refresh();
            var catalog = ScriptableObject.CreateInstance<DefinitionCatalogAsset>();
            catalog.resources = new[] { new ResourceRow { id = "wood" }, new ResourceRow { id = "iron", weight = 2 } };
            catalog.hulls = new[] { new HullRow { id = "starter", slots = new[] {
                new SlotRow { id = "weapon", kind = SlotKind.Weapon }, new SlotRow { id = "ability", kind = SlotKind.Ability } },
                stats = new[] { Stat("health", 100, 1, 1000), Stat("cargo", 10, 0, 1000), Stat("speed", 8, 1, 50) } } };
            catalog.equipment = new[] { new EquipmentRow { id = "cannon", kind = SlotKind.Weapon },
                new EquipmentRow { id = "heavy-cannon", kind = SlotKind.Weapon, modifiers = new[] { Flat("health", 20) } },
                new EquipmentRow { id = "dash", kind = SlotKind.Ability } };
            catalog.hubs = new[] { new HubRow { id = "home", regionId = "harbor" }, new HubRow { id = "other", regionId = "harbor", x = 50 } };
            catalog.regionIds = new[] { "harbor" }; catalog.unlockIds = new[] { "storehouse" };
            catalog.upgrades = new[] {
                new UpgradeRow { id = "harbor-storehouse", trackId = "harbor", cost = new[] { new QuantityRow { id = "wood", quantity = 5 } },
                    grants = new[] { "storehouse" }, modifiers = new[] { Flat("cargo", 5) } },
                new UpgradeRow { id = "reinforced-hull", trackId = "ship", cost = new[] { new QuantityRow { id = "wood", quantity = 6 }, new QuantityRow { id = "iron", quantity = 2 } },
                    modifiers = new[] { new ModifierRow { statId = "health", operation = ModifierOperation.Percent, value = 0.5 } } } };
            catalog.Freeze(); AssetDatabase.CreateAsset(catalog, Content);
            var panel = ScriptableObject.CreateInstance<PanelSettings>(); panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 720);
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/_Game/Bootstrap/Theme.tss");
            AssetDatabase.CreateAsset(panel, "Assets/_Game/UI/Harbor/PanelSettings.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Harbor camera").AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.12f, 0.12f);
            var go = new GameObject("Harbor UI");
            var view = go.AddComponent<HarborView>(); view.stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/_Game/UI/Harbor/Harbor.uss");
            var document = go.GetComponent<UIDocument>(); document.panelSettings = panel; EditorUtility.SetDirty(document);
            PrefabUtility.SaveAsPrefabAsset(go, "Assets/_Game/UI/Harbor/Harbor.prefab");
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/UI/Harbor/Harbor.prefab").GetComponent<UIDocument>().panelSettings == null)
                throw new InvalidOperationException("Harbor prefab requires its panel settings.");
            var fixture = new GameObject("T07 isolated composition").AddComponent<HarborFixture>(); fixture.catalog = catalog; fixture.view = view;
            EditorSceneManager.SaveScene(scene, Scene); AssetDatabase.SaveAssets();
        }
        private static StatRow Stat(string id, double value, double minimum, double maximum) => new StatRow { id = id, value = value, minimum = minimum, maximum = maximum };
        private static ModifierRow Flat(string id, double value) => new ModifierRow { statId = id, value = value, operation = ModifierOperation.Flat };
        public static void Build()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = "Builds/T07/Harbor.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("T07 build failed.");
        }
    }
}
