using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PirateGame.Presentation.Cameras;
using PirateGame.Presentation.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PirateGame.Playground
{
    // Drowned Sun playground: fixed shots that prove the look (spec §9 DoD).
    // `-capture <dir>` photographs every shot and logs frame times, then quits;
    // otherwise 1-9 jump between shots, WASD sails, T/N toggle threat/approach,
    // Z/X/C pin the shallows/causeway/deeps mood, Esc quits.
    public sealed class PlaygroundTour : MonoBehaviour
    {
        [Serializable]
        public sealed class Shot
        {
            public string name;
            public Vector3 ship;
            public float yaw, threat, near;
            public bool harbor;
            public int mood;          // 0 shallows, 1 causeway, 2 deeps
            public bool freeCamera;   // use `camera` pose instead of the rig
            public Vector3 cameraPosition, cameraEuler;
        }

        public ShipFollowCamera rig;
        public Transform ship;
        public ZoneAtmosphere atmosphere;
        public AtmosphereProfile[] moods = Array.Empty<AtmosphereProfile>();
        public Vector3 beaconFocus;
        public float beaconViewYaw = 180;
        public Shot[] shots = Array.Empty<Shot>();
        private int current;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            yield return null;
            Apply(shots[0]);
            string output = Argument("-capture");
            if (output == null) yield break;
            output = Path.GetFullPath(output);
            Directory.CreateDirectory(output);
            var log = new List<string> { "shot,seconds", "device " + SystemInfo.graphicsDeviceName + " " + Screen.width + "x" + Screen.height };
            for (int i = 0; i < shots.Length; i++)
            {
                Apply(shots[i]);
                for (float t = 0; t < 2.2f; t += Time.unscaledDeltaTime) yield return null;
                yield return new WaitForEndOfFrame();
                var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(output, (i + 1).ToString("00") + "-" + shots[i].name + ".png"), texture.EncodeToPNG());
                Destroy(texture);
                log.Add(shots[i].name + "," + Time.realtimeSinceStartup.ToString("0.0"));
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-no-timing") >= 0)
            {
                File.WriteAllLines(Path.Combine(output, "tour.txt"), log);
                Application.Quit();
                yield break;
            }
            // Frame times while sailing a slow circle in the voyage framing. The first lap
            // is a warm-up (first-use shader and mesh uploads), the second is measured.
            Apply(shots[0]);
            var frames = new List<float>();
            var cpu = new List<float>(); var gpu = new List<float>(); var main = new List<float>();
            var slow = new List<string>();
            var timing = new FrameTiming[1];
            var start = ship.position;
            int collections = 0;
            for (int lap = 0; lap < 2; lap++)
            {
                if (lap == 1) collections = GC.CollectionCount(0);
                for (float t = 0; t < 12; t += Time.unscaledDeltaTime)
                {
                    float a = t * 0.25f;
                    ship.SetPositionAndRotation(start + new Vector3(Mathf.Sin(a) * 30, 0, Mathf.Cos(a) * 30 - 30), Quaternion.Euler(0, a * Mathf.Rad2Deg + 90, 0));
                    FrameTimingManager.CaptureFrameTimings();
                    if (lap == 1)
                    {
                        float ms = Time.unscaledDeltaTime * 1000;
                        frames.Add(ms);
                        if (FrameTimingManager.GetLatestTimings(1, timing) > 0)
                        {
                            cpu.Add((float)timing[0].cpuFrameTime); gpu.Add((float)timing[0].gpuFrameTime); main.Add((float)timing[0].cpuMainThreadFrameTime);
                            if (ms > 33.3f && slow.Count < 40)
                                slow.Add("  slow t=" + t.ToString("0.00") + " dt=" + ms.ToString("0.0") + " cpu=" + timing[0].cpuFrameTime.ToString("0.0") +
                                    " main=" + timing[0].cpuMainThreadFrameTime.ToString("0.0") + " gpu=" + timing[0].gpuFrameTime.ToString("0.0"));
                        }
                    }
                    yield return null;
                }
            }
            string Stats(string name, List<float> values)
            {
                if (values.Count == 0) return name + " n/a";
                values.Sort();
                float Q(float q) => values[Mathf.Clamp(Mathf.RoundToInt(q * (values.Count - 1)), 0, values.Count - 1)];
                return name + " median " + Q(0.5f).ToString("0.00") + " p95 " + Q(0.95f).ToString("0.00") + " max " + values.Last().ToString("0.00");
            }
            log.Add("frames " + frames.Count + " | " + Stats("frame_ms", frames) + " | " + Stats("cpu", cpu) + " | " + Stats("main", main) + " | " + Stats("gpu", gpu)
                + " | gc0 " + (GC.CollectionCount(0) - collections));
            log.AddRange(slow);
            File.WriteAllLines(Path.Combine(output, "tour.txt"), log);
            Application.Quit();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            for (int i = 0; i < Mathf.Min(9, shots.Length); i++)
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame) { current = i; Apply(shots[i]); }
            if (keyboard.escapeKey.wasPressedThisFrame) Application.Quit();
            if (keyboard.tKey.wasPressedThisFrame) rig.Threat = rig.Threat > 0.5f ? 0 : 1;
            if (keyboard.nKey.wasPressedThisFrame) rig.Near = rig.Near > 0.5f ? 0 : 1;
            if (keyboard.zKey.wasPressedThisFrame) Mood(0);
            if (keyboard.xKey.wasPressedThisFrame) Mood(1);
            if (keyboard.cKey.wasPressedThisFrame) Mood(2);
            float throttle = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            float turn = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            if (throttle != 0 || turn != 0)
            {
                rig.HarborFocus = null;
                ship.Rotate(0, turn * 60 * Time.deltaTime, 0);
                ship.position += ship.forward * throttle * 9 * Time.deltaTime;
            }
        }

        private void Apply(Shot shot)
        {
            ship.SetPositionAndRotation(shot.ship, Quaternion.Euler(0, shot.yaw, 0));
            Mood(shot.mood);
            rig.enabled = !shot.freeCamera;
            if (shot.freeCamera)
            {
                rig.transform.SetPositionAndRotation(shot.cameraPosition, Quaternion.Euler(shot.cameraEuler));
                return;
            }
            rig.Threat = shot.threat; rig.Near = shot.near;
            rig.HarborFocus = shot.harbor ? beaconFocus : (Vector3?)null;
            rig.HarborYaw = beaconViewYaw;
            rig.Snap();
        }

        private void Mood(int index)
        {
            if (atmosphere == null || moods.Length == 0) return;
            atmosphere.Pinned = moods[Mathf.Clamp(index, 0, moods.Length - 1)];
            atmosphere.Snap(ship.position);
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
