using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PirateGame.Core;
using PirateGame.Presentation.Weather;
using PirateGame.Rules.Application;
using Unity.Profiling;
using UnityEngine;

namespace PirateGame.Composition
{
    // Integrated-slice benchmark (T11) for automated standalone runs:
    //   -benchmark <dir> [-condition daylight|dusk|rough] [-runs 3] [-stress]
    // An autopilot sails the validated T06 route with real physics, AI, combat,
    // salvage, UI and region streaming, docking at the end of every lap. The first
    // lap is warm-up; each further lap is one recorded run with per-frame timing.
    // Benchmark voyages use a sturdier hull so every lap finishes the same route.
    [RequireComponent(typeof(GameDirector))]
    public sealed class GameBenchmark : MonoBehaviour
    {
        private static readonly Vector2[] Route =
        {
            new Vector2(0, 16), new Vector2(0, 48), new Vector2(0, 85), new Vector2(12, 112),
            new Vector2(45, 110), new Vector2(48, 45), new Vector2(25, -12), new Vector2(0, -8)
        };

        private GameDirector director;
        private string output, condition;
        private double loiterUntil;
        private readonly FrameTiming[] timings = new FrameTiming[1];
        private ProfilerRecorder mainThread, gpuRecorder;
        public static bool Active => LaunchOptions.Argument("-benchmark") != null;
        public static bool Stress => LaunchOptions.Flag("-stress");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Active) return;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => Attach();
            if (!Attach())
            {
                LaunchOptions.Mode = LaunchMode.NewCampaign;
                UnityEngine.SceneManagement.SceneManager.LoadScene(LaunchOptions.WorldScene);
            }
        }

        private static bool Attach()
        {
            var existing = FindFirstObjectByType<GameDirector>();
            if (existing == null) return false;
            if (existing.GetComponent<GameBenchmark>() == null) existing.gameObject.AddComponent<GameBenchmark>();
            return true;
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            director = GetComponent<GameDirector>();
            output = Path.GetFullPath(LaunchOptions.Argument("-benchmark"));
            condition = LaunchOptions.Argument("-condition") ?? "daylight";
            int runs = int.TryParse(LaunchOptions.Argument("-runs"), out var n) ? Math.Max(1, n) : 3;
            Directory.CreateDirectory(output);
            string label = (Stress ? "stress-" : "") + condition;
            while (!director.Ready) yield return null;
            director.playerInput.enabled = false;
            director.menus.Activate("continue");
            File.WriteAllText(Path.Combine(output, label + "-environment.json"), JsonUtility.ToJson(new EnvironmentInfo(label), true));
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            gpuRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time", 1);
            var laps = new List<string>();
            for (int lap = 0; lap <= runs; lap++)
            {
                var samples = new List<string>(20000);
                var stats = new LapStats();
                yield return Lap(samples, stats, lap > 0);
                laps.Add(stats.Describe(lap == 0 ? "warm-up" : "run" + lap));
                if (lap == 0) continue;
                File.WriteAllLines(Path.Combine(output, label + "-run" + lap + ".csv"),
                    new[] { "seconds,frame_ms,cpu_ms,gpu_ms,main_ms,allocated_bytes,live_shots,ships,region" }.Concat(samples));
            }
            File.WriteAllLines(Path.Combine(output, label + "-laps.txt"), laps.Concat(director.streamer != null ? director.streamer.LoadLog.Select(l => "region " + l) : new string[0]));
            mainThread.Dispose(); gpuRecorder.Dispose();
            Application.Quit();
        }

        private sealed class LapStats
        {
            public double Seconds; public int Frames, PeakShots, Ships; public bool Docked, Sunk; public double Health;
            public readonly List<string> Trace = new List<string>();
            public string Describe(string name) => name + ": " + Seconds.ToString("0.0", CultureInfo.InvariantCulture) + " s, " + Frames + " frames, peak live shots " +
                PeakShots + ", ships " + Ships + ", docked " + Docked + ", sunk " + Sunk + ", hull " + Health.ToString("0", CultureInfo.InvariantCulture) +
                Environment.NewLine + "    " + string.Join(Environment.NewLine + "    ", Trace);
        }

        private IEnumerator Lap(List<string> samples, LapStats stats, bool record)
        {
            var session = director.Session;
            if (session.Lifecycle != Lifecycle.Docked) yield break;
            var embarked = session.Embark(Guid.NewGuid(), director.PlanEmbark());
            if (!embarked.IsSuccess) { Debug.LogError("Benchmark embark failed: " + embarked.Error); yield break; }
            for (int i = 0; i < 600 && !director.Ready; i++) yield return null;
            ApplyCondition();
            int waypoint = 0;
            loiterUntil = 0;
            double start = Time.realtimeSinceStartupAsDouble, previous = start;
            bool docking = false;
            var voyage = session.Snapshot.Expedition.Id;
            while (Time.realtimeSinceStartupAsDouble - start < 120)
            {
                if (director.CardOpen) director.menus.Activate("continue");
                if (session.Snapshot.Campaign.ResolvedExpeditions.ContainsKey(voyage)) break;
                if (director.Ready && session.Lifecycle == Lifecycle.AtSea && !director.Holding)
                    Steer(ref waypoint, ref docking);
                if (Time.realtimeSinceStartupAsDouble - start > stats.Trace.Count * 5)
                    stats.Trace.Add((Time.realtimeSinceStartupAsDouble - start).ToString("0", CultureInfo.InvariantCulture) + "s " + Where() + " wp " + waypoint +
                        (director.Holding ? " HOLD" : "") + (session.InputLocked ? " LOCK" : "") + " pending=" + director.player.HasPendingStep + " dock " + director.DockingState);
                yield return null;
                double now = Time.realtimeSinceStartupAsDouble;
                int live = director.CombatWorld != null ? director.CombatWorld.Projectiles.Count(p => p.Active) : 0;
                stats.PeakShots = Math.Max(stats.PeakShots, live);
                stats.Ships = director.Enemies.Count;
                if (record)
                {
                    uint count = FrameTimingManager.GetLatestTimings(1, timings);
                    double cpu = count > 0 ? timings[0].cpuFrameTime : -1;
                    double gpu = count > 0 && timings[0].gpuFrameTime > 0 ? timings[0].gpuFrameTime : Recorder(gpuRecorder);
                    string region = director.player.motor.Body != null ? director.RegionIdAt(director.player.motor.Body.position) : "";
                    samples.Add(string.Join(",", (now - start).ToString("R", CultureInfo.InvariantCulture), ((now - previous) * 1000).ToString("R", CultureInfo.InvariantCulture),
                        cpu.ToString("R", CultureInfo.InvariantCulture), gpu.ToString("R", CultureInfo.InvariantCulture), Recorder(mainThread).ToString("R", CultureInfo.InvariantCulture),
                        UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong().ToString(CultureInfo.InvariantCulture), live.ToString(CultureInfo.InvariantCulture),
                        stats.Ships.ToString(CultureInfo.InvariantCulture), region));
                    FrameTimingManager.CaptureFrameTimings();
                }
                stats.Frames++;
                previous = now;
            }
            stats.Seconds = Time.realtimeSinceStartupAsDouble - start;
            var outcome = session.Snapshot.Campaign.ResolvedExpeditions.TryGetValue(voyage, out var o) ? o : (Outcome?)null;
            stats.Docked = outcome == Outcome.Docked; stats.Sunk = outcome == Outcome.Sunk;
            stats.Health = session.Snapshot.Expedition?.Health ?? 0;
            stats.Trace.Add("end at " + Where() + " waypoint " + waypoint + "/" + Route.Length);
            // Guarantee the next lap starts docked even if this one timed out at sea.
            double deadline = Time.realtimeSinceStartupAsDouble + 15, nextTry = 0;
            while (session.Lifecycle != Lifecycle.Docked && Time.realtimeSinceStartupAsDouble < deadline)
            {
                if (director.CardOpen) director.menus.Activate("continue");
                if (session.Lifecycle == Lifecycle.AtSea && Time.realtimeSinceStartupAsDouble > nextTry && Home()) nextTry = Time.realtimeSinceStartupAsDouble + 1.5;
                yield return null;
            }
            for (int i = 0; i < 30; i++) { if (director.CardOpen) director.menus.Activate("continue"); yield return null; }
        }

        private void Steer(ref int waypoint, ref bool docking)
        {
            var body = director.player.motor.Body;
            var position = body.position;
            // Stress only: circle inside the escorted pair's reach for 15 s so every
            // Homeward ship engages at once (sustained projectile workload).
            if (Stress && waypoint == 4 && loiterUntil == 0) loiterUntil = Time.realtimeSinceStartupAsDouble + 15;
            if (Stress && Time.realtimeSinceStartupAsDouble < loiterUntil)
            {
                Fire(position, out var aimAt, out bool shoot, out bool guard);
                director.player.Submit(new InputIntent(0.55, 0.7, aimAt.x, aimAt.z, shoot, guard, false), false);
                return;
            }
            if (waypoint < Route.Length)
            {
                var target = new Vector3(Route[waypoint].x, 0, Route[waypoint].y);
                var to = target - position; to.y = 0;
                if (to.magnitude < 7) waypoint++;
                float angle = Vector3.SignedAngle(director.player.transform.forward, to, Vector3.up);
                double turn = Mathf.Clamp(angle / 30f, -1, 1);
                double throttle = Mathf.Abs(angle) > 80 ? 0.35 : 1;
                Fire(position, out var aim, out bool fire, out bool brace);
                director.player.Submit(new InputIntent(throttle, turn, aim.x, aim.z, fire, brace, false), false);
            }
            else
            {
                // Brake inside the berth and keep pressing Interact until docking starts,
                // as a player would after an early press outside the berth.
                docking = true;
                director.player.Submit(default, true);
                if (Time.frameCount % 30 == 0) director.RequestInteract();
            }
        }

        // Nearest living enemy within cannon range: aim and fire; brace when hurt.
        private void Fire(Vector3 position, out Vector3 aim, out bool fire, out bool brace)
        {
            aim = Vector3.zero; fire = false; brace = false;
            var enemy = director.Enemies.Where(e => e != null && !e.Target.Defeated)
                .OrderBy(e => (e.transform.position - position).sqrMagnitude).FirstOrDefault();
            if (enemy == null) return;
            var to = enemy.transform.position - director.player.motor.weaponOrigin.position; to.y = 0;
            if (to.magnitude > 42) return;
            aim = to.normalized; fire = true;
            var voyage = director.Session.Snapshot.Expedition;
            brace = voyage != null && voyage.Health < director.Session.ShipStats()["health"] * 0.6;
        }

        private string Where()
        {
            var p = director.player.motor.Body != null ? director.player.motor.Body.position : Vector3.zero;
            return "(" + p.x.ToString("0", CultureInfo.InvariantCulture) + "," + p.z.ToString("0", CultureInfo.InvariantCulture) + ")";
        }

        private bool Home()
        {
            var dock = director.Definitions.Hubs[director.homeHub].Dock;
            var sim = director.player;
            if (sim.HasPendingStep || director.Session.InputLocked) return false;
            sim.SetPaused(true);
            sim.motor.Teleport(new Vector3((float)dock.X, 0, (float)dock.Z + 1), 0, 0);
            Physics.SyncTransforms();
            sim.SetPaused(false);
            director.RequestInteract();
            return true;
        }

        private void ApplyCondition()
        {
            if (director.weather == null) return;
            director.weather.Apply(condition == "dusk" ? SeaCondition.Dusk : condition == "rough" ? SeaCondition.Rough : SeaCondition.Daylight);
        }

        private static double Recorder(ProfilerRecorder recorder) =>
            recorder.Valid && recorder.Count > 0 ? recorder.LastValue / 1000000.0 : -1;

        [Serializable]
        private sealed class EnvironmentInfo
        {
            public string label, utc = DateTime.UtcNow.ToString("o"), unity = Application.unityVersion;
            public string cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, driver = SystemInfo.graphicsDeviceVersion, os = SystemInfo.operatingSystem;
            public int ramMB = SystemInfo.systemMemorySize, vramMB = SystemInfo.graphicsMemorySize, width = Screen.width, height = Screen.height;
            public bool development = Debug.isDebugBuild, frameTiming = FrameTimingManager.IsFeatureEnabled(), stress = Stress;
            public int vsync = QualitySettings.vSyncCount, targetFrameRate = Application.targetFrameRate;
            public EnvironmentInfo(string label) { this.label = label; }
        }
    }
}
