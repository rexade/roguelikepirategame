using System;
using System.Collections;
using System.IO;
using System.Linq;
using PirateGame.Core;
using PirateGame.Rules.Application;
using UnityEngine;

namespace PirateGame.Tests.T06
{
    // Opt-in standalone evidence route; leaves the actual player framebuffer intact.
    public sealed class HudCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-t06-hud-output") >= 0)
                new GameObject("T06 R1 HUD evidence").AddComponent<HudCapture>();
        }
        private IEnumerator Start()
        {
            yield return null;
            var args = Environment.GetCommandLineArgs();
            var output = args[Array.IndexOf(args, "-t06-hud-output") + 1];
            Directory.CreateDirectory(output);
            var fixture = FindFirstObjectByType<SalvageFixture>();
            var simulation = fixture.simulation;
            foreach (var component in simulation.GetComponents<MonoBehaviour>())
                if (component.GetType().Name == "ShipKeyboardMouse") component.enabled = false;
            simulation.SetPaused(true);
            while (simulation.HasPendingStep) yield return new WaitForFixedUpdate();
            yield return new WaitForSecondsRealtime(3);
            File.WriteAllText(Path.Combine(output, "startup-ready.txt"), fixture.HudText() + "\nGPU=" + SystemInfo.graphicsDeviceName);
            while (!File.Exists(Path.Combine(output, "continue.txt"))) yield return null;
            simulation.SetPaused(false);
            float started = Time.realtimeSinceStartup;
            while (simulation.motor.Body.position.z < 78 && Time.realtimeSinceStartup - started < 90)
            {
                simulation.Submit(new InputIntent(1, 0, 0, 1, false, false, true), false);
                yield return new WaitForFixedUpdate();
            }
            simulation.ClearInput();
            simulation.SetPaused(true);
            while (simulation.HasPendingStep) yield return new WaitForFixedUpdate();
            yield return null;
            simulation.SetPaused(false);
            fixture.interaction.TryCollect(Guid.NewGuid());
            simulation.SetPaused(true);
            fixture.Recreate();
            yield return null;
            var wreck = fixture.region.Sources.Single(s => s.Id.AuthoredId == "first:wreck-01").Capture();
            bool valid = fixture.interaction.LastResult?.Error == RuleError.CargoFull
                && Values.Amount(simulation.Session.Snapshot.Expedition.Cargo, "wood") == 5
                && Values.Amount(simulation.Session.Snapshot.Expedition.Cargo, "iron") == 0
                && fixture.region.Sources[0].Depleted && fixture.region.Sources[1].Depleted
                && Values.Amount(wreck.Loot, "wood") == 2 && Values.Amount(wreck.Loot, "iron") == 2;
            File.WriteAllText(Path.Combine(output, "cargo-ready.txt"), fixture.HudText()
                + "\nvalid=" + valid + "\nbarrelsDepleted=" + fixture.region.Sources[0].Depleted + "," + fixture.region.Sources[1].Depleted
                + "\nwreckWood=" + Values.Amount(wreck.Loot, "wood") + "\nwreckIron=" + Values.Amount(wreck.Loot, "iron")
                + "\nposition=" + simulation.motor.Body.position + "\nroute=injected motor intents; no teleportation\n");
            while (!File.Exists(Path.Combine(output, "quit.txt"))) yield return null;
            Application.Quit(valid ? 0 : 1);
        }
    }
}
