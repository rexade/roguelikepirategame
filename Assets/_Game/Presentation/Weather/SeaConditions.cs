using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace PirateGame.Presentation.Weather
{
    public enum SeaCondition { Daylight, Dusk, Rough }

    // Voyage weather as pure presentation (INV-14): the T02-approved daylight, dusk
    // and rough-water looks. Rules, hit boxes and aim never read these values.
    public sealed class SeaConditions : MonoBehaviour
    {
        public Light sun;
        public Volume volume;
        public WaterSurface ocean;
        public SeaCondition Current { get; private set; } = SeaCondition.Daylight;

        // Deterministic from the saved voyage seed, so a resumed voyage keeps its sky.
        public static SeaCondition ForSeed(int seed)
        {
            uint roll = (uint)seed * 2654435761u % 100u;
            return roll < 55 ? SeaCondition.Daylight : roll < 82 ? SeaCondition.Dusk : SeaCondition.Rough;
        }

        public static string Describe(SeaCondition condition) =>
            condition == SeaCondition.Dusk ? "Dusk" : condition == SeaCondition.Rough ? "Rough seas" : "Fair weather";

        public void Apply(SeaCondition condition)
        {
            Current = condition;
            bool dusk = condition == SeaCondition.Dusk, rough = condition == SeaCondition.Rough;
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(dusk ? 16 : rough ? 40 : 48, dusk ? -65 : -35, 0);
                sun.color = dusk ? new Color(1, 0.78f, 0.6f) : rough ? new Color(0.92f, 0.95f, 1f) : new Color(1, 0.96f, 0.86f);
                sun.intensity = dusk ? 16000 : rough ? 70000 : 100000;
            }
            if (volume != null)
            {
                // Runtime instance: never edits the shared profile asset.
                var profile = volume.profile;
                if (profile.TryGet<Exposure>(out var exposure)) exposure.fixedExposure.Override(dusk ? 10 : rough ? 11.6f : 12);
                if (profile.TryGet<GradientSky>(out var sky))
                {
                    sky.exposure.Override(dusk ? 11.5f : rough ? 12.4f : 13);
                    sky.top.Override(dusk ? new Color(0.13f, 0.16f, 0.3f) : rough ? new Color(0.3f, 0.37f, 0.45f) : new Color(0.18f, 0.38f, 0.65f));
                }
            }
            if (ocean != null)
            {
                ocean.largeWindSpeed = rough ? 45 : 22;
                ocean.largeBand0Multiplier = rough ? 0.45f : 0.12f;
                ocean.largeBand1Multiplier = rough ? 0.5f : 0.2f;
                ocean.simulationFoamAmount = rough ? 0.5f : 0.15f;
            }
        }
    }
}
