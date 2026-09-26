using System.IO;
using System.Linq;
using PirateGame.Presentation.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace PirateGame.Art.Editor
{
    // The three zone moods and the volume profile they drive. Paradise in the
    // lagoon, golden haze over the causeway, cold mist among the colossi.
    public static class Atmospheres
    {
        public const string Folder = "Assets/_Game/Content/World/DrownedSun/Atmosphere/";
        public const string VolumePath = "Assets/_Game/Content/World/DrownedSun/DrownedSunVolume.asset";

        public static AtmosphereProfile[] All() => new[] { Shallows(), Causeway(), Deeps() };

        public static AtmosphereProfile Shallows() => Profile("Shallows", "Glass-clear shallows", 0, new AtmosphereState
        {
            sunPitch = 54, sunYaw = -32, sunColor = new Color(1f, 0.96f, 0.88f), sunIntensity = 110000,
            skyTop = new Color(0.08f, 0.30f, 0.70f), skyMiddle = new Color(0.34f, 0.64f, 0.92f), skyBottom = new Color(0.76f, 0.87f, 0.95f),
            skyExposure = 13.0f, exposure = 12.75f,
            fogColor = new Color(0.66f, 0.82f, 0.95f), fogDistance = 1100, fogHeight = 70,
            refraction = new Color(0.06f, 0.70f, 0.80f), scattering = new Color(0.01f, 0.16f, 0.30f), absorption = 14,
            wind = 18, swell0 = 0.1f, swell1 = 0.16f, foam = 0.12f,
            saturation = 4, contrast = 14, temperature = 8, bloom = 0.22f, vignette = 0.2f, filter = Color.white,
            gulls = 0.85f, breeze = 0.2f, groans = 0
        });

        public static AtmosphereProfile Causeway() => Profile("Causeway", "Golden haze", 580, new AtmosphereState
        {
            sunPitch = 17, sunYaw = 62, sunColor = new Color(1f, 0.70f, 0.42f), sunIntensity = 42000,
            skyTop = new Color(0.20f, 0.28f, 0.50f), skyMiddle = new Color(0.88f, 0.62f, 0.42f), skyBottom = new Color(1f, 0.80f, 0.58f),
            skyExposure = 12.2f, exposure = 11.45f,
            fogColor = new Color(0.96f, 0.72f, 0.50f), fogDistance = 460, fogHeight = 90,
            refraction = new Color(0.08f, 0.50f, 0.60f), scattering = new Color(0.04f, 0.16f, 0.27f), absorption = 8,
            wind = 24, swell0 = 0.16f, swell1 = 0.24f, foam = 0.18f,
            saturation = 8, contrast = 14, temperature = 22, bloom = 0.34f, vignette = 0.24f, filter = new Color(1f, 0.95f, 0.88f),
            gulls = 0.25f, breeze = 0.75f, groans = 0.1f
        });

        public static AtmosphereProfile Deeps() => Profile("Deeps", "Sea mist", 1150, new AtmosphereState
        {
            sunPitch = 36, sunYaw = -18, sunColor = new Color(0.82f, 0.88f, 0.96f), sunIntensity = 30000,
            skyTop = new Color(0.25f, 0.29f, 0.35f), skyMiddle = new Color(0.50f, 0.55f, 0.60f), skyBottom = new Color(0.66f, 0.70f, 0.72f),
            skyExposure = 11.8f, exposure = 10.6f,
            fogColor = new Color(0.58f, 0.63f, 0.67f), fogDistance = 170, fogHeight = 110,
            refraction = new Color(0.05f, 0.30f, 0.38f), scattering = new Color(0.02f, 0.08f, 0.12f), absorption = 5,
            wind = 40, swell0 = 0.4f, swell1 = 0.45f, foam = 0.45f,
            saturation = -24, contrast = 18, temperature = -10, bloom = 0.3f, vignette = 0.34f, filter = new Color(0.92f, 0.96f, 1f),
            gulls = 0, breeze = 0.6f, groans = 0.75f
        });

        private static AtmosphereProfile Profile(string name, string mood, float anchor, AtmosphereState look)
        {
            Directory.CreateDirectory(Folder);
            string path = Folder + name + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<AtmosphereProfile>(path);
            if (profile == null) { profile = ScriptableObject.CreateInstance<AtmosphereProfile>(); AssetDatabase.CreateAsset(profile, path); }
            profile.mood = mood; profile.anchorZ = anchor; profile.look = look;
            EditorUtility.SetDirty(profile);
            return profile;
        }

        // Copies `source` (the approved T02 profile) once, then makes sure every
        // override the atmosphere drives exists: fog, bloom, grading, vignette, shadows.
        public static VolumeProfile Volume(VolumeProfile source)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(VolumePath));
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
            if (profile == null)
            {
                profile = Object.Instantiate(source);
                profile.components = profile.components.Select(Object.Instantiate).ToList();
                AssetDatabase.CreateAsset(profile, VolumePath);
                foreach (var component in profile.components) { component.name = component.name.Replace("(Clone)", ""); AssetDatabase.AddObjectToAsset(component, profile); }
            }
            Ensure<Fog>(profile).enabled.Override(true);
            var bloom = Ensure<Bloom>(profile); bloom.threshold.Override(1.0f); bloom.scatter.Override(0.72f); bloom.intensity.Override(0.22f);
            Ensure<ColorAdjustments>(profile);
            Ensure<WhiteBalance>(profile);
            var vignette = Ensure<Vignette>(profile); vignette.smoothness.Override(0.45f); vignette.color.Override(new Color(0.05f, 0.08f, 0.1f));
            var shadows = Ensure<HDShadowSettings>(profile); shadows.maxShadowDistance.Override(320); shadows.cascadeShadowSplitCount.Override(4);
            if (profile.TryGet<Tonemapping>(out var tone)) tone.mode.Override(TonemappingMode.ACES);
            if (profile.TryGet<Exposure>(out var exposure)) exposure.mode.Override(ExposureMode.Fixed);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T Ensure<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing)) return existing;
            var added = profile.Add<T>(true);
            added.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(added, profile);
            return added;
        }
    }
}
