using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using PirateGame.Rules.Application;
using UnityEngine;
using UnityEngine.Rendering;

namespace PirateGame.Tests.T06
{
    public sealed class RegionCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-t06-output") >= 0)
                new GameObject("T06 capture").AddComponent<RegionCapture>();
        }
        private IEnumerator Start()
        {
            yield return null;
            var args = Environment.GetCommandLineArgs(); string output = args[Array.IndexOf(args, "-t06-output") + 1];
            Directory.CreateDirectory(output);
            var fixture = FindFirstObjectByType<SalvageFixture>(); fixture.simulation.SetPaused(true);
            yield return new WaitForSecondsRealtime(3);
            var camera = Camera.main;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture = target;
            yield return new WaitForEndOfFrame(); Save(camera, target, Path.Combine(output, "harbor.png"));
            foreach (var component in fixture.simulation.GetComponents<MonoBehaviour>())
                if (component.GetType().Name == "ShipKeyboardMouse") component.enabled = false;
            var trace = new List<string> { "tick,x,z,waypoint" };
            var route = fixture.region.content.route; int waypoint = 1;
            fixture.simulation.SetPaused(false);
            var started = Time.realtimeSinceStartup;
            while (waypoint < route.Length && Time.realtimeSinceStartup - started < 150)
            {
                var position = fixture.simulation.motor.Body.position;
                var delta = new Vector3(route[waypoint].x, 0, route[waypoint].y) - position;
                if (delta.magnitude < 3) { waypoint++; continue; }
                var angle = Vector3.SignedAngle(fixture.simulation.motor.Body.rotation * Vector3.forward, delta, Vector3.up);
                fixture.simulation.Submit(new InputIntent(Math.Abs(angle) > 45 ? 0 : 1, Mathf.Clamp(angle / 25, -1, 1), 0, 1, false, false, true), false);
                yield return new WaitForFixedUpdate();
                if (fixture.simulation.Session.Tick % 10 == 0)
                    trace.Add(string.Format(CultureInfo.InvariantCulture, "{0},{1:R},{2:R},{3}", fixture.simulation.Session.Tick, position.x, position.z, waypoint));
            }
            fixture.simulation.ClearInput();
            for (int i = 0; i < 100 && fixture.simulation.motor.Speed > 0.5f; i++)
            { fixture.simulation.Submit(default, true); yield return new WaitForFixedUpdate(); }
            fixture.simulation.SetPaused(true);
            File.WriteAllLines(Path.Combine(output, "route.csv"), trace);
            File.WriteAllText(Path.Combine(output, "route-result.txt"), "waypoints=" + waypoint + "/" + route.Length + "\ncompleted=" + (waypoint == route.Length) + "\n");
            foreach (var component in camera.GetComponents<MonoBehaviour>())
                if (component.GetType().Name == "ShipFollowCamera") component.enabled = false;
            camera.transform.position = new Vector3(12, 180, -4); camera.transform.rotation = Quaternion.Euler(75, 0, 0); camera.fieldOfView = 62;
            yield return new WaitForSecondsRealtime(2);
            yield return new WaitForEndOfFrame(); Save(camera, target, Path.Combine(output, "region.png"));
            File.WriteAllText(Path.Combine(output, "capture.txt"), "T06-r1\nGPU=" + SystemInfo.graphicsDeviceName + "\n1280x720 HDRP\nSources=" + fixture.region.Sources.Length + "\n");
            Application.Quit(waypoint == route.Length ? 0 : 1);
        }
        private static void Save(Camera camera, RenderTexture target, string path)
        {
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG()); RenderTexture.active = previous; Destroy(image);
        }
    }
}
