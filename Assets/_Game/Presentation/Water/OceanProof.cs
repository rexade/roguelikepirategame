using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace PirateGame.Presentation.Water
{
    // Temporary presentation fixture. It owns no gameplay state or production controls.
    public sealed class OceanProof : MonoBehaviour
    {
        public Transform routeRoot;
        public Transform visibleShip;
        public Transform attackMarker;
        public Camera gameplayCamera;
        public WaterSurface ocean;
        public WaterDecal wake;
        public Light sun;
        public Volume environment;
        public GameObject bootstrapNavigation;
        public float bobAmplitude = 0.12f;
        private double routeStart;
        private string condition;
        private string captureMode;
        private RenderTexture captureTarget;
        private readonly FrameTiming[] timings = new FrameTiming[1];
        private ProfilerRecorder mainThread, renderThread, gpu;

        public static Vector3 Route(float seconds)
        {
            float t = Mathf.Repeat(seconds, 60);
            if (t < 15) return new Vector3(7, 0, -9);
            if (t < 30) return Vector3.Lerp(new Vector3(7, 0, -9), new Vector3(7, 0, 12), (t - 15) / 15);
            if (t < 45)
            {
                float a = (t - 30) / 15 * Mathf.PI;
                return new Vector3(16 - 9 * Mathf.Cos(a), 0, 12 + 9 * Mathf.Sin(a));
            }
            return Vector3.Lerp(new Vector3(25, 0, 12), new Vector3(7, 0, -9), (t - 45) / 15);
        }

        public static string Argument(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            if (bootstrapNavigation && !Application.CanStreamedLevelBeLoaded("Bootstrap"))
                bootstrapNavigation.SetActive(false);
            condition = Argument("-t02-condition", "daylight");
            SetCondition(condition);
            routeStart = Time.realtimeSinceStartupAsDouble;
            string mode = Argument("-t02-mode", "preview");
            captureMode = mode;
            if (mode == "smoke") routeStart -= 30;
            if (mode == "smoke" || mode == "footage")
            {
                captureTarget = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                captureTarget.Create();
                gameplayCamera.targetTexture = captureTarget;
            }
            if (mode == "preview") yield break;
            string output = Path.GetFullPath(Argument("-t02-output", "T02Capture"));
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "environment.json"), JsonUtility.ToJson(new EnvironmentInfo(), true));
            bool benchmark = mode == "benchmark" || mode == "overhead";
            float warmup = benchmark ? 60 : 10;
            yield return new WaitForSecondsRealtime(warmup);
            if (benchmark)
            {
                bool diagnostics = mode == "benchmark";
                if (diagnostics)
                {
                    mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
                    renderThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread", 1);
                    gpu = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time", 1);
                }
                for (int run = 1; run <= (diagnostics ? 3 : 1); run++)
                {
                    var samples = new List<Sample>(20000);
                    routeStart = Time.realtimeSinceStartupAsDouble;
                    double previous = routeStart;
                    while (Time.realtimeSinceStartupAsDouble - routeStart < 60)
                    {
                        yield return null;
                        double now = Time.realtimeSinceStartupAsDouble;
                        uint count = diagnostics ? FrameTimingManager.GetLatestTimings(1, timings) : 0;
                        double cpuMs = count > 0 ? timings[0].cpuFrameTime : -1;
                        double gpuMs = count > 0 && timings[0].gpuFrameTime > 0 ? timings[0].gpuFrameTime : RecorderMs(gpu);
                        samples.Add(new Sample { seconds=now-routeStart, frame=(now-previous)*1000, cpu=cpuMs, gpu=gpuMs,
                            main=RecorderMs(mainThread), render=RecorderMs(renderThread),
                            allocated=diagnostics ? UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() : -1,
                            reserved=diagnostics ? UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() : -1 });
                        previous = now;
                        if (diagnostics) FrameTimingManager.CaptureFrameTimings();
                    }
                    var rows = new List<string>(samples.Count+1) { "route_seconds,frame_ms,cpu_frame_ms,gpu_frame_ms,main_thread_ms,render_thread_ms,allocated_bytes,reserved_bytes" };
                    foreach (var s in samples) rows.Add(string.Join(",", new[] {s.seconds,s.frame,s.cpu,s.gpu,s.main,s.render,(double)s.allocated,s.reserved}.MapInvariant()));
                    File.WriteAllLines(Path.Combine(output, condition + "-run" + run + ".csv"), rows);
                }
                if (diagnostics) { mainThread.Dispose(); renderThread.Dispose(); gpu.Dispose(); }
            }
            else if (mode == "footage")
            {
                // Deterministic offline capture is explicitly separate from real-time performance.
                Time.captureDeltaTime = 1f / 30;
                for (int frame = 0; frame < 1800; frame++)
                {
                    Pose(frame / 30f);
                    yield return new WaitForEndOfFrame();
                    Capture(Path.Combine(output, condition + "-" + frame.ToString("D4") + ".jpg"));
                }
                Time.captureDeltaTime = 0;
                yield return null;
            }
            else if (mode == "smoke")
            {
                Pose((float)(Time.realtimeSinceStartupAsDouble-routeStart));
                yield return new WaitForEndOfFrame();
                Capture(Path.Combine(output, condition + ".png"));
                yield return new WaitForSecondsRealtime(2);
            }
            Application.Quit();
        }

        private static double RecorderMs(ProfilerRecorder recorder) => recorder.Valid && recorder.Count > 0 ? recorder.LastValue / 1000000.0 : -1;

        private struct Sample
        {
            public double seconds, frame, cpu, gpu, main, render;
            public long allocated, reserved;
        }

        private void Capture(string path)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = captureTarget;
            var texture = new Texture2D(captureTarget.width, captureTarget.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, captureTarget.width, captureTarget.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, path.EndsWith(".jpg", StringComparison.Ordinal) ? texture.EncodeToJPG(95) : texture.EncodeToPNG());
            RenderTexture.active = previous;
            Destroy(texture);
        }

        private void Update()
        {
            if (captureMode != "footage") Pose((float)(Time.realtimeSinceStartupAsDouble - routeStart));
        }

        private void Pose(float t)
        {
            routeRoot.position = Route(t);
            Vector3 direction = Route(t + 0.05f) - Route(t);
            if (direction.sqrMagnitude > 0.00001f) routeRoot.rotation = Quaternion.LookRotation(direction);
            visibleShip.localPosition = new Vector3(0, Mathf.Sin(t * 1.7f) * bobAmplitude, 0);
            visibleShip.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.3f) * 2, 0, Mathf.Sin(t * 1.9f) * 3);
            attackMarker.position = routeRoot.position + routeRoot.forward * 6 + Vector3.up * 0.8f;
            attackMarker.rotation = Quaternion.Euler(0, routeRoot.eulerAngles.y, 0);
            wake.surfaceFoamDimmer = direction.sqrMagnitude > 0.00001f ? 1 : 0;
            gameplayCamera.transform.position = routeRoot.position + new Vector3(0, 49, -28.29016f);
        }

        private void SetCondition(string value)
        {
            bool dusk = value == "dusk";
            bool rough = value == "rough";
            sun.transform.rotation = Quaternion.Euler(dusk ? 16 : 48, dusk ? -65 : -35, 0);
            sun.color = dusk ? new Color(1, 0.78f, 0.6f) : new Color(1, 0.96f, 0.86f);
            sun.intensity = dusk ? 16000 : 100000;
            var profile = environment.profile;
            profile.TryGet<Exposure>(out var exposure);
            exposure.fixedExposure.Override(dusk ? 10 : 12);
            profile.TryGet<GradientSky>(out var sky);
            sky.exposure.Override(dusk ? 11.5f : 13);
            sky.top.Override(dusk ? new Color(0.13f, 0.16f, 0.3f) : new Color(0.18f, 0.38f, 0.65f));
            ocean.largeWindSpeed = rough ? 45 : 22;
            ocean.largeBand0Multiplier = rough ? 0.45f : 0.12f;
            ocean.largeBand1Multiplier = rough ? 0.5f : 0.2f;
            ocean.simulationFoamAmount = rough ? 0.5f : 0.15f;
            bobAmplitude = rough ? 0.2f : 0.12f;
        }

        [Serializable]
        private sealed class EnvironmentInfo
        {
            public string utc = DateTime.UtcNow.ToString("O");
            public string unity = Application.unityVersion;
            public string cpu = SystemInfo.processorType;
            public string gpu = SystemInfo.graphicsDeviceName;
            public string driver = SystemInfo.graphicsDeviceVersion;
            public string os = SystemInfo.operatingSystem;
            public int ramMB = SystemInfo.systemMemorySize;
            public int vramMB = SystemInfo.graphicsMemorySize;
            public int width = Screen.width, height = Screen.height;
            public bool development = Debug.isDebugBuild;
            public bool frameTimingEnabled = FrameTimingManager.IsFeatureEnabled();
            public string missingTiming = "-1 means unavailable; timings overlap and must not be added";
        }
    }

    internal static class TimingFormat
    {
        internal static string[] MapInvariant(this double[] values)
        {
            var result = new string[values.Length];
            for (int i = 0; i < values.Length; i++) result[i] = values[i].ToString("R", CultureInfo.InvariantCulture);
            return result;
        }
    }
}
