using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace PirateGame.Bootstrap.Editor
{
    public static class ProjectSetup
    {
        private const string Settings = "Assets/_Game/Bootstrap/Settings";
        private const string Bootstrap = "Assets/_Game/Scenes/Bootstrap.unity";
        private const string Ocean = "Assets/_Game/Scenes/Tests/T02/WaterTest.unity";

        [MenuItem("Pirate Prototype/Create Initial Assets")]
        public static void CreateInitialAssets()
        {
            // This is a one-time authoring command, never a build-time scene rewrite.
            if (File.Exists(Bootstrap) || File.Exists(Ocean))
                throw new InvalidOperationException("Initial scenes already exist; edit their assets directly.");
            Directory.CreateDirectory(Settings);
            Directory.CreateDirectory(Path.GetDirectoryName(Ocean));
            AssetDatabase.Refresh();
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            PlayerSettings.companyName = "PiratePrototype";
            PlayerSettings.productName = "Pirate Prototype";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            var player = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            player.FindProperty("activeInputHandler").intValue = 1;
            player.ApplyModifiedPropertiesWithoutUndo();
            QualitySettings.vSyncCount = 0;
            Time.fixedDeltaTime = 0.02f;

            // HDRP 17.3 hides its global-settings type; the public factory accepts its Type.
            var globalType = typeof(HDRenderPipelineAsset).Assembly.GetType(
                "UnityEngine.Rendering.HighDefinition.HDRenderPipelineGlobalSettings", true);
            var global = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
            if (global == null)
                global = RenderPipelineGlobalSettingsUtils.Create(globalType, Settings + "/HDRPGlobalSettings.asset");
            if (global == null)
                throw new InvalidOperationException("Could not initialize HDRP global settings.");
            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<HDRenderPipeline>(global);
            var pipeline = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
            var config = pipeline.currentPlatformRenderPipelineSettings;
            config.supportWater = true;
            config.waterSimulationResolution = WaterSimulationResolution.High256;
            config.supportRayTracing = false;
            config.dynamicResolutionSettings.enabled = false;
            pipeline.currentPlatformRenderPipelineSettings = config;
            AssetDatabase.CreateAsset(pipeline, Settings + "/HDRP.asset");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int initialQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
                QualitySettings.vSyncCount = 0;
            }
            QualitySettings.SetQualityLevel(initialQuality, false);

            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            AssetDatabase.CreateAsset(panel, Settings + "/PanelSettings.asset");
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(
                "Assets/_Game/Bootstrap/Theme.tss");
            if (theme == null) throw new InvalidOperationException("Bootstrap UI theme is missing.");
            panel.themeStyleSheet = theme;
            EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();

            CreateScene(false, panel);
            CreateScene(true, panel);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Bootstrap, true),
                new EditorBuildSettingsScene(Ocean, true)
            };
            AssetDatabase.SaveAssets();
            RelocateDefaults();
            EditorSceneManager.OpenScene(Bootstrap);
            Debug.Log("T01 initial assets created.");
        }

        private static void CreateScene(bool ocean, PanelSettings panel)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(Settings + "/PanelSettings.asset");
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.SetPositionAndRotation(new Vector3(0, 35, -20.20726f), Quaternion.Euler(60, 0, 0));
            camera.GetComponent<Camera>().farClipPlane = 1000;
            var hdCamera = camera.AddComponent<HDAdditionalCameraData>();
            hdCamera.allowDynamicResolution = false;
            hdCamera.allowDeepLearningSuperSampling = false;
            hdCamera.allowFidelityFX2SuperResolution = false;
            hdCamera.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;

            var sun = new GameObject("Sun", typeof(Light));
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.intensity = 100000;
            sun.AddComponent<HDAdditionalLightData>();

            var volume = new GameObject("Environment", typeof(Volume)).GetComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            string profilePath = Settings + (ocean ? "/OceanVolume.asset" : "/BootstrapVolume.asset");
            AssetDatabase.CreateAsset(profile, profilePath);
            profile.Add<VisualEnvironment>(true).skyType.Override((int)SkyType.Gradient);
            var sky = profile.Add<GradientSky>(true);
            sky.exposure.Override(12);
            sky.top.Override(new Color(0.18f, 0.38f, 0.65f));
            sky.middle.Override(new Color(0.55f, 0.7f, 0.82f));
            sky.bottom.Override(new Color(0.25f, 0.3f, 0.35f));
            var exposure = profile.Add<Exposure>(true);
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(12);
            profile.Add<WaterRendering>(true).enable.Override(ocean);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            volume.sharedProfile = profile;

            if (ocean)
            {
                if (!EditorApplication.ExecuteMenuItem("GameObject/Water/Surface/Ocean Sea or Lake"))
                    throw new InvalidOperationException("HDRP ocean creation menu is unavailable.");
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "Scale Reference (not a ship)";
                marker.transform.position = new Vector3(0, 1, 0);
                marker.transform.localScale = new Vector3(2, 2, 4);
                var material = new Material(Shader.Find("HDRP/Lit"));
                material.SetColor("_BaseColor", new Color(0.75f, 0.2f, 0.12f));
                AssetDatabase.CreateAsset(material, Settings + "/ReferenceMaterial.mat");
                marker.GetComponent<Renderer>().sharedMaterial = material;
            }
            var menu = new GameObject("Menu", typeof(UIDocument));
            AssignPanel(menu.GetComponent<UIDocument>(), panel);
            menu.AddComponent<BootstrapMenu>();
            EditorSceneManager.SaveScene(scene, ocean ? Ocean : Bootstrap);
        }

        private static void AssignPanel(UIDocument document, PanelSettings panel)
        {
            if (panel == null) throw new InvalidOperationException("UI panel settings asset is missing.");
            var serialized = new SerializedObject(document);
            serialized.FindProperty("m_PanelSettings").objectReferenceValue = panel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void BuildWindows()
        {
            Validate();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "Builds/Windows/PiratePrototype.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            Debug.Log($"T01 build: {report.summary.result}; errors={report.summary.totalErrors}; bytes={report.summary.totalSize}");
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("T01 Windows build failed. See build log.");
            CopyPackageNotices();
        }

        private static void CopyPackageNotices()
        {
            foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                string destination = Path.Combine("Builds/Windows/ThirdPartyNotices", package.name);
                foreach (string source in Directory.GetFiles(package.resolvedPath, "*", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(source);
                    if (!name.StartsWith("LICENSE", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith("Third Party Notices", StringComparison.OrdinalIgnoreCase)) continue;
                    if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                    Directory.CreateDirectory(destination);
                    File.Copy(source, Path.Combine(destination, name), true);
                }
            }
        }

        public static void RelocateDefaults()
        {
            const string source = "Assets/HDRPDefaultResources";
            if (!AssetDatabase.IsValidFolder(source)) return;
            string originalGlobal = source + "/HDRenderPipelineGlobalSettings.asset";
            var original = AssetDatabase.LoadAssetAtPath<RenderPipelineGlobalSettings>(originalGlobal);
            var active = GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
            if (original != null && original != active)
                AssetDatabase.DeleteAsset(originalGlobal);
            string error = AssetDatabase.MoveAsset(source, Settings + "/Defaults");
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            AssetDatabase.SaveAssets();
        }

        public static void FinalizeBootstrap()
        {
            RelocateDefaults();
            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;
            foreach (string name in new[] { "BootstrapVolume", "OceanVolume" })
            {
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Settings + "/" + name + ".asset");
                if (profile.TryGet<GradientSky>(out var sky))
                {
                    sky.exposure.Override(12);
                    EditorUtility.SetDirty(sky);
                }
            }
            AssetDatabase.SaveAssets();
            foreach (string scenePath in new[] { Bootstrap, Ocean })
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(Settings + "/PanelSettings.asset");
                foreach (var ui in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<UIDocument>()))
                    AssignPanel(ui, panel);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Validate();
        }

        public static void Validate()
        {
            if (Application.unityVersion != "6000.3.24f1")
                throw new BuildFailedException("Use the pinned Unity editor.");
            var pipeline = GraphicsSettings.defaultRenderPipeline as HDRenderPipelineAsset;
            if (pipeline == null || !pipeline.currentPlatformRenderPipelineSettings.supportWater ||
                pipeline.currentPlatformRenderPipelineSettings.supportRayTracing ||
                pipeline.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.enabled)
                throw new BuildFailedException("Reference HDRP configuration is invalid.");
            if (!File.Exists(Bootstrap) || !File.Exists(Ocean))
                throw new BuildFailedException("Required authored scenes are missing.");
            string[] scenePaths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (!scenePaths.SequenceEqual(new[] { Bootstrap, Ocean }))
                throw new BuildFailedException("Build must contain Bootstrap followed by WaterTest.");
            var oceanProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Settings + "/OceanVolume.asset");
            if (oceanProfile == null || !oceanProfile.TryGet<WaterRendering>(out var water) ||
                !water.enable.overrideState || !water.enable.value)
                throw new BuildFailedException("Water rendering must be enabled in the ocean volume.");
            var scene = EditorSceneManager.OpenScene(Ocean);
            if (!scene.GetRootGameObjects().Any(go => go.GetComponent<WaterSurface>() != null))
                throw new BuildFailedException("The ocean scene has no HDRP water surface.");
            if (scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<UIDocument>())
                .Any(ui => ui.panelSettings == null || ui.panelSettings.themeStyleSheet == null))
                throw new BuildFailedException("A UI panel is missing its settings or theme.");
            EditorSceneManager.OpenScene(Bootstrap);
            const string original = "{\"label\":\"harbor\",\"count\":7}";
            var sample = JsonConvert.DeserializeObject<SerializerProbe>(original);
            var roundTrip = JsonConvert.DeserializeObject<SerializerProbe>(JsonConvert.SerializeObject(sample));
            if (roundTrip == null || roundTrip.label != "harbor" || roundTrip.count != 7)
                throw new BuildFailedException("JSON serializer smoke check failed.");
            Debug.Log("T01 configuration and JSON round-trip checks passed.");
        }

        private sealed class SerializerProbe
        {
            public string label;
            public int count;
        }
    }
}
