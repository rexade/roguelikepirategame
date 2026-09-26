using System;
using UnityEngine;

namespace PirateGame.Presentation.World
{
    // One zone's look: light, sky, fog, water colour, grading and ambience mix.
    // Pure presentation (INV-14): no rule, hit box or aim ever reads these values.
    [Serializable]
    public struct AtmosphereState
    {
        [Header("Sun")] public float sunPitch, sunYaw; public Color sunColor; public float sunIntensity;
        [Header("Sky and exposure")] public Color skyTop, skyMiddle, skyBottom; public float skyExposure, exposure;
        [Header("Fog")] public Color fogColor; public float fogDistance, fogHeight;
        [Header("Water")] public Color refraction, scattering; public float absorption, wind, swell0, swell1, foam;
        [Header("Grading")] public float saturation, contrast, temperature, bloom, vignette; public Color filter;
        [Header("Ambience 0..1")] public float gulls, breeze, groans;

        public static AtmosphereState Lerp(AtmosphereState a, AtmosphereState b, float t)
        {
            t = Mathf.Clamp01(t);
            return new AtmosphereState
            {
                sunPitch = Mathf.Lerp(a.sunPitch, b.sunPitch, t), sunYaw = Mathf.LerpAngle(a.sunYaw, b.sunYaw, t),
                sunColor = Color.Lerp(a.sunColor, b.sunColor, t), sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t),
                skyTop = Color.Lerp(a.skyTop, b.skyTop, t), skyMiddle = Color.Lerp(a.skyMiddle, b.skyMiddle, t), skyBottom = Color.Lerp(a.skyBottom, b.skyBottom, t),
                skyExposure = Mathf.Lerp(a.skyExposure, b.skyExposure, t), exposure = Mathf.Lerp(a.exposure, b.exposure, t),
                fogColor = Color.Lerp(a.fogColor, b.fogColor, t), fogDistance = Mathf.Lerp(a.fogDistance, b.fogDistance, t), fogHeight = Mathf.Lerp(a.fogHeight, b.fogHeight, t),
                refraction = Color.Lerp(a.refraction, b.refraction, t), scattering = Color.Lerp(a.scattering, b.scattering, t),
                absorption = Mathf.Lerp(a.absorption, b.absorption, t), wind = Mathf.Lerp(a.wind, b.wind, t),
                swell0 = Mathf.Lerp(a.swell0, b.swell0, t), swell1 = Mathf.Lerp(a.swell1, b.swell1, t), foam = Mathf.Lerp(a.foam, b.foam, t),
                saturation = Mathf.Lerp(a.saturation, b.saturation, t), contrast = Mathf.Lerp(a.contrast, b.contrast, t),
                temperature = Mathf.Lerp(a.temperature, b.temperature, t), bloom = Mathf.Lerp(a.bloom, b.bloom, t),
                vignette = Mathf.Lerp(a.vignette, b.vignette, t), filter = Color.Lerp(a.filter, b.filter, t),
                gulls = Mathf.Lerp(a.gulls, b.gulls, t), breeze = Mathf.Lerp(a.breeze, b.breeze, t), groans = Mathf.Lerp(a.groans, b.groans, t)
            };
        }
    }

    [CreateAssetMenu(menuName = "Pirate Game/Atmosphere Profile")]
    public sealed class AtmosphereProfile : ScriptableObject
    {
        // Shown on the HUD under the zone name, e.g. "Glass-clear shallows".
        public string mood = "";
        // The profile is fully in effect at this z; the look blends between anchors.
        public float anchorZ;
        public AtmosphereState look;
    }
}
