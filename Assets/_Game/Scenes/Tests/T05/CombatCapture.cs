using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PirateGame.Gameplay.Input;
using PirateGame.Rules.Application;
using UnityEngine;
using UnityEngine.Rendering;

namespace PirateGame.Tests.T05
{
    public sealed class CombatCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-t05-output") >= 0)
                new GameObject("Combat capture").AddComponent<CombatCapture>();
        }
        private IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            string output = args[Array.IndexOf(args, "-t05-output") + 1]; Directory.CreateDirectory(output);
            var fixture = FindFirstObjectByType<CombatFixture>();
            var world = fixture.World; var sim = fixture.simulation;
            FindFirstObjectByType<ShipKeyboardMouse>().enabled = false;
            Time.captureDeltaTime = 1f / 30; QualitySettings.vSyncCount = 0; Application.targetFrameRate = 30;
            sim.SetPaused(true);
            yield return new WaitForSecondsRealtime(2);
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); target.Create(); Camera.main.targetTexture = target;
            var request = new RenderPipeline.StandardRequest { destination = target };
            var rows = new List<string> { "frame,tick,health,weaponCooldown,brace,raiderHealth,gunnerHealth,activeShots,targetHealth,targetDefeated" };
            int shots = 0; world.ShotFired += _ => shots++;
            Action<InputIntent> damageAndPause = _ => { world.Player.ApplyDamage(10); sim.SetPaused(true); };
            sim.TickStarted += damageAndPause;
            sim.SetPaused(false);
            yield return new WaitForSecondsRealtime(0.2f);
            sim.TickStarted -= damageAndPause;
            long pausedTick = sim.Session.Tick;
            if (sim.HasPendingStep || !sim.Session.IsPaused || world.Player.Health != 90 || world.Player.Defeated)
                throw new InvalidOperationException("Standalone processed damage/pause read state failed.");
            yield return new WaitForSecondsRealtime(0.2f);
            if (sim.Session.Tick != pausedTick || world.Player.Health != 90)
                throw new InvalidOperationException("Standalone paused state changed.");
            sim.SetPaused(false);
            for (int frame = 0; frame < 330; frame++)
            {
                var enemy = world.Enemies.FirstOrDefault(e => !e.Target.Defeated);
                var aim = enemy == null ? Vector3.forward : enemy.Target.motor.Body.position - sim.motor.weaponOrigin.position;
                aim.y = 0; aim.Normalize();
                sim.Submit(new InputIntent(0, 0, aim.x, aim.z, true, frame == 12 || frame == 210, false), true);
                yield return new WaitForEndOfFrame();
                RenderPipeline.SubmitRenderRequest(Camera.main, request);
                var previous = RenderTexture.active; RenderTexture.active = target;
                var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); texture.Apply();
                File.WriteAllBytes(Path.Combine(output, "frame-" + frame.ToString("D4") + ".jpg"), texture.EncodeToJPG(88));
                Destroy(texture); RenderTexture.active = previous;
                if (world.Player.Health != (sim.Session.Snapshot.Expedition?.Health ?? 0))
                    throw new InvalidOperationException("Standalone player read state diverged.");
                rows.Add(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", frame, sim.Session.Tick,
                    sim.Session.Snapshot.Expedition?.Health ?? 0, world.WeaponCooldown, world.BraceRemaining,
                    fixture.pursuer.Target.Health, fixture.gunner.Target.Health, world.Projectiles.Count(s => s.Active), world.Player.Health, world.Player.Defeated));
            }
            File.WriteAllLines(Path.Combine(output, "combat.csv"), rows);
            File.WriteAllText(Path.Combine(output, "result.txt"), "weapon=" + world.Weapon.Id + "\nshots=" + shots +
                "\nraiderHealth=" + fixture.pursuer.Target.Health + "\ngunnerHealth=" + fixture.gunner.Target.Health +
                "\ndevice=" + SystemInfo.graphicsDeviceName + "\nresolution=1280x720\nofflineFootage=true\n" +
                "hud=False\npausedReadState=passed\nframeReadState=passed\n");
            Application.Quit();
        }
    }
}
