using System.IO;
using PirateGame.Bootstrap;
using PirateGame.UI.Game;
using PirateGame.UI.Harbor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace PirateGame.UI.Preview.Editor
{
    // Builds the UI playground scene and a standalone player for it:
    //   -executeMethod PirateGame.UI.Preview.Editor.UiPreviewAuthoring.Build
    // then run Builds/UiPreview/UiPreview.exe -capture <dir> -screen-width 1920 -screen-height 1080 -screen-fullscreen 0
    public static class UiPreviewAuthoring
    {
        public const string Folder = "Assets/_Game/UI/Preview/";
        public const string ScenePath = Folder + "UiPreview.unity";
        private const string GamePanel = "Assets/_Game/UI/Harbor/PanelSettings.asset";
        private const string TitlePanel = "Assets/_Game/Bootstrap/Settings/PanelSettings.asset";

        [MenuItem("Pirate Game/UI Preview/Create Scene")]
        public static void Create()
        {
            AssetDatabase.Refresh();
            // Open the scene first: NewScene(Single) unloads unused assets, which would
            // leave references loaded before it pointing at destroyed objects.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var gamePanel = Load<PanelSettings>(GamePanel);
            var titlePanel = Load<PanelSettings>(TitlePanel);
            var style = Load<StyleSheet>("Assets/_Game/UI/Game/Game.uss");
            var backdropPanel = BackdropPanel(gamePanel);
            var camera = new GameObject("Camera", typeof(Camera), typeof(HDAdditionalCameraData));
            var data = camera.GetComponent<HDAdditionalCameraData>();
            data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            data.backgroundColorHDR = new Color(0.04f, 0.14f, 0.19f);

            var backdrop = Document("Backdrop", backdropPanel, 0);
            backdrop.AddComponent<PreviewBackdrop>().image = Load<Texture2D>(Folder + "preview-backdrop.png");
            var title = Document("Title", titlePanel, 10);
            title.AddComponent<BootstrapMenu>();
            var hud = Document("HUD", gamePanel, 0).AddComponent<GameHud>(); hud.stylesheet = style;
            var harbor = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Assets/_Game/UI/Harbor/Harbor.prefab"));
            harbor.GetComponent<UIDocument>().sortingOrder = 1;
            var menus = Document("Menus", gamePanel, 2).AddComponent<GameMenus>(); menus.stylesheet = style;
            var chart = Document("Sea chart", gamePanel, 3).AddComponent<SeaChart>(); chart.stylesheet = style;

            var preview = new GameObject("UI preview").AddComponent<UiPreview>();
            preview.title = title; preview.hud = hud; preview.menus = menus; preview.chart = chart;
            preview.harbor = harbor.GetComponent<HarborView>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("UI preview scene written: " + ScenePath);
        }

        [MenuItem("Pirate Game/UI Preview/Build Player")]
        public static void Build()
        {
            Create();
            string output = "Builds/UiPreview/UiPreview.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            Debug.Log("UI preview build: " + report.summary.result + "; errors=" + report.summary.totalErrors + "; output=" + output);
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("UI preview build failed.");
        }

        // Serialized assignment: the panelSettings property setter does not persist on a
        // document created in a fresh scene (ProjectSetup.AssignPanel does the same).
        private static GameObject Document(string name, PanelSettings panel, int order)
        {
            var go = new GameObject(name);
            var serialized = new SerializedObject(go.AddComponent<UIDocument>());
            serialized.FindProperty("m_PanelSettings").objectReferenceValue = panel;
            serialized.FindProperty("m_SortingOrder").floatValue = order;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        // A separate panel drawn beneath the game panels for the stand-in vista.
        private static PanelSettings BackdropPanel(PanelSettings template)
        {
            string path = Folder + "BackdropPanel.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (panel == null)
            {
                panel = Object.Instantiate(template);
                AssetDatabase.CreateAsset(panel, path);
            }
            panel.sortingOrder = -10;
            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            return panel;
        }

        private static T Load<T>(string path) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new BuildFailedException("Missing " + typeof(T).Name + ": " + path);
    }
}
