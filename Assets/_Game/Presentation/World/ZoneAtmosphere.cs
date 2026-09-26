using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace PirateGame.Presentation.World
{
    // Blends the zone profiles along z by the followed position (the zones are
    // bands running north), thickens the fog toward the world's limits ("the
    // Veil") and drives the sun, volume and water surface. Presentation only.
    public sealed class ZoneAtmosphere : MonoBehaviour
    {
        public Light sun;
        public Volume volume;
        public WaterSurface ocean;
        public AtmosphereProfile[] profiles = Array.Empty<AtmosphereProfile>();
        public Transform follow;
        [Min(0.01f)] public float response = 1.2f;
        [Header("The Veil (world limits)")]
        public Rect limits = new Rect(-300, -200, 1000, 1650);
        [Min(1)] public float veilWidth = 110;
        [Min(1)] public float veilFog = 38;
        public Color veilColor = new Color(0.66f, 0.72f, 0.76f);

        public AtmosphereState Current { get; private set; }
        public string Mood { get; private set; } = "";
        // Capture/preview override: holds one profile regardless of position.
        public AtmosphereProfile Pinned { get; set; }
        private bool initialised;

        public static AtmosphereState Evaluate(IReadOnlyList<AtmosphereProfile> profiles, float z, out AtmosphereProfile nearest)
        {
            var sorted = profiles.Where(p => p != null).OrderBy(p => p.anchorZ).ToArray();
            if (sorted.Length == 0) throw new InvalidOperationException("No atmosphere profiles.");
            nearest = sorted.OrderBy(p => Mathf.Abs(p.anchorZ - z)).First();
            if (z <= sorted[0].anchorZ) return sorted[0].look;
            for (int i = 0; i + 1 < sorted.Length; i++)
            {
                var a = sorted[i]; var b = sorted[i + 1];
                if (z > b.anchorZ) continue;
                float t = Mathf.InverseLerp(a.anchorZ, b.anchorZ, z);
                return AtmosphereState.Lerp(a.look, b.look, t * t * (3 - 2 * t));
            }
            return sorted[sorted.Length - 1].look;
        }

        // 0 well inside the limits, 1 at the edge.
        public static float VeilFactor(Rect limits, Vector3 position, float width)
        {
            float inside = Mathf.Min(Mathf.Min(position.x - limits.xMin, limits.xMax - position.x), Mathf.Min(position.z - limits.yMin, limits.yMax - position.z));
            return 1 - Mathf.Clamp01(inside / Mathf.Max(1, width));
        }

        public AtmosphereState Target(Vector3 position)
        {
            if (Pinned != null) { Mood = Pinned.mood; return Pinned.look; }
            var state = Evaluate(profiles, position.z, out var nearest);
            Mood = nearest.mood;
            float veil = VeilFactor(limits, position, veilWidth);
            if (veil > 0)
            {
                state.fogDistance = Mathf.Lerp(state.fogDistance, veilFog, veil);
                state.fogColor = Color.Lerp(state.fogColor, veilColor, veil * 0.7f);
            }
            return state;
        }

        public void Snap(Vector3 position)
        {
            Current = Target(position);
            initialised = true;
            Apply(Current);
            applied = true;
        }

        private void LateUpdate()
        {
            if (follow == null || profiles.Length == 0) return;
            var target = Target(follow.position);
            var next = initialised ? AtmosphereState.Lerp(Current, target, 1 - Mathf.Exp(-Time.unscaledDeltaTime / response)) : target;
            // Settle exactly on the target and skip unchanged frames: every sky or
            // volume change makes HDRP re-render the sky and ambient lighting.
            if (AtmosphereState.Close(next, target)) next = target;
            bool changed = !initialised || !AtmosphereState.Close(next, Current, 0) || !applied;
            Current = next;
            initialised = true;
            if (changed) { Apply(Current); applied = true; }
        }

        private bool applied;

        public void Apply(AtmosphereState s)
        {
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(s.sunPitch, s.sunYaw, 0);
                sun.color = s.sunColor;
                sun.intensity = s.sunIntensity;
            }
            if (volume != null)
            {
                // Runtime instance in play mode; the shared asset only when baking in the editor.
                var profile = Application.isPlaying ? volume.profile : volume.sharedProfile;
                if (profile != null) Grade(profile, s);
            }
            if (ocean != null)
            {
                ocean.refractionColor = s.refraction;
                ocean.scatteringColor = s.scattering;
                ocean.absorptionDistance = s.absorption;
                ocean.largeWindSpeed = s.wind;
                ocean.largeBand0Multiplier = s.swell0;
                ocean.largeBand1Multiplier = s.swell1;
                ocean.simulationFoamAmount = s.foam;
            }
        }

        private static void Grade(VolumeProfile profile, AtmosphereState s)
        {
            if (profile.TryGet<Exposure>(out var exposure)) { exposure.mode.Override(ExposureMode.Fixed); exposure.fixedExposure.Override(s.exposure); }
            if (profile.TryGet<GradientSky>(out var sky))
            {
                sky.top.Override(s.skyTop); sky.middle.Override(s.skyMiddle); sky.bottom.Override(s.skyBottom);
                sky.exposure.Override(s.skyExposure);
            }
            if (profile.TryGet<Fog>(out var fog))
            {
                fog.enabled.Override(true);
                // Sky-coloured fog tinted by the zone: a constant fog colour is not scaled
                // by exposure and paints a dark band along the horizon.
                fog.colorMode.Override(FogColorMode.SkyColor);
                float peak = Mathf.Max(0.001f, Mathf.Max(s.fogColor.r, Mathf.Max(s.fogColor.g, s.fogColor.b)));
                fog.tint.Override(new Color(s.fogColor.r / peak, s.fogColor.g / peak, s.fogColor.b / peak));
                fog.color.Override(s.fogColor);
                fog.meanFreePath.Override(Mathf.Max(1, s.fogDistance));
                fog.baseHeight.Override(0);
                fog.maximumHeight.Override(Mathf.Max(1, s.fogHeight));
            }
            if (profile.TryGet<Bloom>(out var bloom)) bloom.intensity.Override(s.bloom);
            if (profile.TryGet<ColorAdjustments>(out var grade))
            {
                grade.saturation.Override(s.saturation);
                grade.contrast.Override(s.contrast);
                grade.colorFilter.Override(s.filter.a <= 0 ? Color.white : s.filter);
            }
            if (profile.TryGet<WhiteBalance>(out var balance)) balance.temperature.Override(s.temperature);
            if (profile.TryGet<Vignette>(out var vignette)) vignette.intensity.Override(s.vignette);
        }
    }
}
