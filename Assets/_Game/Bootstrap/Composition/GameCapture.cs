using System;
using System.Collections;
using System.IO;
using System.Linq;
using PirateGame.Core;
using PirateGame.Rules.Application;
using UnityEngine;

namespace PirateGame.Composition
{
    // Scripted evidence tour for automated standalone runs (-capture <dir>).
    // It drives the same session commands and ship intents a player would, with
    // test-only relocations between scenes; screenshots include all UI overlays.
    [RequireComponent(typeof(GameDirector))]
    public sealed class GameCapture : MonoBehaviour
    {
        private GameDirector director;
        private string output;
        private int shot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (LaunchOptions.Argument("-capture") == null) return;
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
            if (existing.GetComponent<GameCapture>() == null) existing.gameObject.AddComponent<GameCapture>();
            return true;
        }

        private IEnumerator Start()
        {
            director = GetComponent<GameDirector>();
            output = Path.GetFullPath(LaunchOptions.Argument("-capture") ?? "Captures");
            Directory.CreateDirectory(output);
            Log("capture start " + DateTime.UtcNow.ToString("o"));
            while (!director.Ready) yield return null;
            director.playerInput.enabled = false;
            yield return Wait(2.5f);
            yield return Shot("harbor-intro");
            director.menus.Activate("continue");
            yield return Wait(0.8f);
            yield return Shot("harbor");

            var result = director.Session.Embark(Guid.NewGuid(), director.PlanEmbark());
            Log("embark " + result.Error);
            yield return Wait(1.5f);
            yield return Drive(1, 0, 3.2f);
            yield return Shot("cast-off");

            yield return Relocate(new Vector3(-2.5f, 0, 4.5f), 20);
            yield return Drive(0.35f, 0, 0.8f);
            yield return Shot("salvage-prompt");
            director.RequestInteract();
            yield return Wait(0.6f);
            yield return Shot("salvaged");

            // Engage the first encounter from the south and trade shots until it sinks.
            var enemy = director.Enemies.OrderBy(e => Vector3.Distance(e.transform.position, Vector3.zero)).First();
            var at = enemy.transform.position;
            yield return Relocate(at + new Vector3(-6, 0, -26), 15);
            float time = 0; bool mid = false;
            while (!enemy.Target.Defeated && time < 25)
            {
                var aim = enemy.transform.position - director.player.motor.weaponOrigin.position; aim.y = 0; aim.Normalize();
                var toEnemy = enemy.transform.position - director.player.transform.position;
                float turn = Mathf.Clamp(Vector3.SignedAngle(director.player.transform.forward, Quaternion.Euler(0, 70, 0) * toEnemy, Vector3.up) / 40, -1, 1);
                director.player.Submit(new InputIntent(0.45, turn, aim.x, aim.z, true, time > 3 && time < 3.2f, false), false);
                if (!mid && time > 4.5f) { mid = true; yield return Shot("combat"); }
                time += Time.deltaTime;
                yield return null;
            }
            director.player.Submit(default, false);
            Log("enemy defeated " + enemy.Target.Defeated + " after " + time.ToString("0.0") + "s; hull " + director.Session.Snapshot.Expedition?.Health);
            yield return Wait(1.2f);
            yield return Shot("enemy-sinking");
            yield return Wait(1.5f);

            var wreck = director.salvage.Sources.LastOrDefault(s => s != null && s.Id.AuthoredId == null);
            if (wreck != null)
            {
                yield return Relocate(wreck.transform.position + new Vector3(0, 0, -3.5f), 0);
                yield return Wait(0.4f);
                yield return Shot("wreck-prompt");
                director.RequestInteract();
                yield return Wait(0.8f);
            }
            var dock = director.Definitions.Hubs[director.homeHub].Dock;
            yield return Relocate(new Vector3((float)dock.X + 1.5f, 0, (float)dock.Z + 14), 180);
            yield return Drive(0.6f, 0, 2.2f);
            yield return Shot("homecoming");
            director.player.Submit(default, true);
            yield return Relocate(new Vector3((float)dock.X, 0, (float)dock.Z + 1.5f), 180);
            director.RequestInteract();
            for (int i = 0; i < 300 && director.Session.Lifecycle != Lifecycle.Docked; i++) yield return null;
            yield return Wait(1f);
            yield return Shot("docked-results");
            director.menus.Activate("continue");
            yield return Wait(0.5f);
            var bought = director.Session.PurchaseUpgrade(Guid.NewGuid(), "harbor-storehouse");
            Log("purchase storehouse " + bought.Error);
            yield return Wait(0.5f);
            yield return Shot("harbor-after-voyage");

            director.Session.Embark(Guid.NewGuid(), director.PlanEmbark());
            yield return Wait(1f);
            // Open water west of the first encounter, out of every gun's reach.
            yield return Relocate(new Vector3(-50, 0, 40), 30);
            yield return Drive(1, 0.2f, 2.5f);
            yield return Shot("open-water");
            if (director.weather != null)
            {
                var voyageWeather = director.weather.Current;
                director.weather.Apply(PirateGame.Presentation.Weather.SeaCondition.Dusk);
                yield return Wait(1.5f);
                yield return Shot("weather-dusk");
                director.weather.Apply(PirateGame.Presentation.Weather.SeaCondition.Rough);
                yield return Wait(2.5f);
                yield return Shot("weather-rough");
                director.weather.Apply(voyageWeather);
            }

            // Second harbor: sight it, sail into its berth, raise the flag and moor.
            var outpost = director.Definitions.Hubs.Values.FirstOrDefault(h => h.Id == "saltmarsh-harbor");
            if (outpost != null)
            {
                // Approach from the east, clear of the escorted pair guarding the south.
                yield return Relocate(new Vector3((float)outpost.Dock.X + 22, 0, (float)outpost.Dock.Z - 24), -40);
                yield return Drive(0.8f, 0, 2.5f);
                yield return Shot("land-ho");
                yield return Relocate(new Vector3((float)outpost.Dock.X, 0, (float)outpost.Dock.Z - 1.5f), 0);
                yield return Wait(0.3f);
                yield return Shot("claim-prompt");
                director.RequestInteract();
                for (int i = 0; i < 400 && director.Session.Lifecycle != Lifecycle.Docked; i++) yield return null;
                Log("outpost docked " + director.Session.Snapshot.Campaign.CurrentHub);
                yield return Wait(1f);
                yield return Shot("outpost-docked");
                director.menus.Activate("continue");
                yield return Wait(0.6f);
                yield return Shot("outpost-harbor");
            }

            // Galewater Reach: sail north from Saltmarsh across the region border.
            var stormwatch = director.Definitions.Hubs.Values.FirstOrDefault(h => h.Id == "stormwatch-harbor");
            if (stormwatch != null && director.streamer != null)
            {
                var sailed = director.Session.Embark(Guid.NewGuid(), director.PlanEmbark());
                Log("embark for galewater " + sailed.Error);
                for (int i = 0; i < 600 && !director.Ready; i++) yield return null;
                // East of Saltmarsh's landmass, in open water just south of the border.
                yield return Relocate(new Vector3(96, 0, 150), 0);
                for (int i = 0; i < 600 && !director.streamer.IsReady("galewater-reach"); i++) yield return null;
                yield return Drive(1, 0, 3.5f);
                Log("crossed into " + director.Session.Snapshot.Expedition?.Position.RegionId);
                yield return Shot("galewater-border");
                var corsair = director.Enemies.FirstOrDefault(e => e.Definition.Id == "corsair" && !e.Target.Defeated);
                if (corsair != null)
                {
                    yield return Relocate(corsair.transform.position + new Vector3(0, 0, -26), 0);
                    yield return Wait(1.5f);
                    yield return Shot("corsair");
                }
                yield return Relocate(new Vector3((float)stormwatch.Dock.X - 30, 0, (float)stormwatch.Dock.Z - 16), 60);
                yield return Wait(0.8f);
                yield return Shot("stormwatch-approach");
                yield return Relocate(new Vector3((float)stormwatch.Dock.X, 0, (float)stormwatch.Dock.Z - 1.5f), 0);
                director.RequestInteract();
                for (int i = 0; i < 400 && director.Session.Lifecycle != Lifecycle.Docked; i++) yield return null;
                Log("stormwatch docked " + director.Session.Snapshot.Campaign.CurrentHub);
                yield return Wait(1f);
                yield return Shot("stormwatch-docked");
                director.menus.Activate("continue");
                yield return Wait(0.6f);
                yield return Shot("stormwatch-harbor");
                foreach (var line in director.streamer.LoadLog) Log("region " + line);
            }
            Log("capture done");
            yield return Wait(0.5f);
            Application.Quit();
        }

        private IEnumerator Drive(double throttle, double turn, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                director.player.Submit(new InputIntent(throttle, turn, 0, 0, false, false, false), false);
                yield return null;
            }
            director.player.Submit(default, false);
        }

        private IEnumerator Relocate(Vector3 to, float yaw)
        {
            var sim = director.player;
            while (sim.HasPendingStep) yield return new WaitForFixedUpdate();
            sim.SetPaused(true);
            sim.motor.Teleport(to, yaw, 0);
            Physics.SyncTransforms();
            sim.SetPaused(false);
            director.followCamera.Snap();
            yield return Wait(0.25f);
        }

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            // The back buffer at end of frame includes the UI Toolkit overlays.
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            string path = Path.Combine(output, (++shot).ToString("00") + "-" + name + ".png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Destroy(texture);
            Log("shot " + Path.GetFileName(path) + " " + Screen.width + "x" + Screen.height + " lifecycle=" + director.Session.Lifecycle);
        }

        private void Log(string line)
        {
            Debug.Log("[capture] " + line);
            File.AppendAllText(Path.Combine(output, "capture.txt"), line + Environment.NewLine);
        }
    }
}
