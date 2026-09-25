using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PirateGame.Gameplay.Input;
using PirateGame.Gameplay.Ships;
using PirateGame.Presentation.Ships;
using PirateGame.Rules.Application;
using UnityEngine;
using UnityEngine.Rendering;

namespace PirateGame.Tests.T04
{
    public sealed class ShipCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-t04-fps") >= 0)
                new GameObject("T04 capture").AddComponent<ShipCapture>();
        }
        private IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            int fps = int.Parse(args[Array.IndexOf(args, "-t04-fps") + 1]);
            string output = args[Array.IndexOf(args, "-t04-output") + 1];
            Directory.CreateDirectory(output);
            bool footage = Array.IndexOf(args, "-t04-footage") >= 0;
            if (footage) Time.captureDeltaTime = 1f / fps;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fps;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            target.Create();
            Camera.main.targetTexture = target;
            var renderRequest = new RenderPipeline.StandardRequest { destination = target };
            var simulation = FindFirstObjectByType<ShipSimulation>();
            FindFirstObjectByType<ShipKeyboardMouse>().enabled = false;
            simulation.SetPaused(true);
            yield return new WaitForSecondsRealtime(3);
            simulation.SetPaused(false);
            var frames = new List<string> { "frame,deltaSeconds,tick,x,z,speed" };
            var start = simulation.Session.Tick;
            var bob = FindFirstObjectByType<ShipBobbing>();
            bob.amplitude = fps == 60 ? 0 : 0.5f;
            int index = 0, shot = 0;
            while (simulation.Session.Tick - start < 350)
            {
                simulation.Submit(new InputIntent(1, 0, 1, 0, true, false, false), false);
                yield return new WaitForEndOfFrame();
                RenderPipeline.SubmitRenderRequest(Camera.main, renderRequest);
                var p = simulation.motor.Body.position;
                frames.Add(string.Format(CultureInfo.InvariantCulture, "{0},{1:R},{2},{3:R},{4:R},{5:R}", index++, Time.unscaledDeltaTime, simulation.Session.Tick, p.x, p.z, simulation.motor.Speed));
                if (footage || simulation.Session.Tick - start >= shot * 100)
                {
                    var previous = RenderTexture.active;
                    RenderTexture.active = target;
                    var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                    texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    texture.Apply();
                    File.WriteAllBytes(Path.Combine(output, "frame-" + shot.ToString("D4") + ".jpg"), texture.EncodeToJPG(90));
                    RenderTexture.active = previous;
                    Destroy(texture);
                    shot++;
                }
            }
            simulation.SetPaused(true);
            var pausedPosition = simulation.motor.Body.position;
            var pausedTick = simulation.Session.Tick;
            yield return new WaitForSecondsRealtime(0.3f);
            File.WriteAllLines(Path.Combine(output, "frames.csv"), frames);
            File.WriteAllText(Path.Combine(output, "result.txt"), string.Format(CultureInfo.InvariantCulture,
                "requestedFPS={0}\ntick={1}\nx={2:R}\nz={3:R}\npausePositionStable={4}\npauseTickStable={5}\naim={6}\nbobAmplitude={7}\ndevice={8}\nresolution={9}x{10}\n",
                fps, pausedTick, pausedPosition.x, pausedPosition.z, pausedPosition == simulation.motor.Body.position,
                pausedTick == simulation.Session.Tick, simulation.motor.AimDirection, bob.amplitude, SystemInfo.graphicsDeviceName, target.width, target.height)
                + "offlineFootage=" + footage + "\n");
            Application.Quit();
        }
    }
}
