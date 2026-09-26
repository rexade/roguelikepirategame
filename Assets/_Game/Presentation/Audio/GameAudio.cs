using System;
using System.Collections.Generic;
using UnityEngine;

namespace PirateGame.Presentation.Audio
{
    public enum Sfx { Cannon, Repeater, Impact, Splash, Pickup, Bell, Sink, Brace, Flag, CargoFull, Relight }

    // Procedurally synthesized sound effects and ocean ambience: no audio assets,
    // no paid content. Presentation only; nothing here feeds back into rules.
    public sealed class GameAudio : MonoBehaviour
    {
        [Range(0, 1)] public float master = 0.8f;
        [Range(0, 1)] public float ambience = 0.35f;
        // Zone ambience mix (lagoon gulls, causeway wind, deep groans); eases toward SetAmbience targets.
        [Range(0, 1)] public float gulls, wind, groans;
        [Min(0.01f)] public float ambienceFade = 1.5f;
        public const int Rate = 44100;

        private readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
        private readonly List<AudioSource> voices = new List<AudioSource>();
        private readonly List<AudioSource> ambientVoices = new List<AudioSource>();
        private AudioSource surf, windLoop;
        private AudioClip[] gullCalls, groanCalls;
        private float gullsTarget, windTarget, groansTarget, nextGull, nextGroan;
        private int next, nextAmbient;
        private System.Random random = new System.Random(7);

        private void Awake()
        {
            clips[Sfx.Cannon] = Make("cannon", 1.4f, Cannon);
            clips[Sfx.Repeater] = Make("repeater", 0.4f, Repeater);
            clips[Sfx.Impact] = Make("impact", 0.7f, Impact);
            clips[Sfx.Splash] = Make("splash", 0.9f, Splash);
            clips[Sfx.Pickup] = Make("pickup", 0.7f, t => Chime(t, new[] { 659.25f, 987.77f }, 0.11f));
            clips[Sfx.Flag] = Make("flag", 1.3f, t => Chime(t, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.14f));
            clips[Sfx.CargoFull] = Make("full", 0.45f, t => Chime(t, new[] { 392f, 311.13f }, 0.12f) * 0.8f);
            clips[Sfx.Bell] = Make("bell", 2.6f, Bell);
            clips[Sfx.Sink] = Make("sink", 3f, Sink);
            clips[Sfx.Brace] = Make("brace", 0.8f, Brace);
            clips[Sfx.Relight] = Make("relight", 4.5f, Relight);
            gullCalls = new[] { GullCall("gull-a", 1f, 3, 97), GullCall("gull-b", 0.88f, 4, 101), GullCall("gull-c", 1.1f, 2, 103) };
            groanCalls = new[] { GroanCall("groan-a", 43, 107), GroanCall("groan-b", 35, 109) };
            for (int i = 0; i < 16; i++)
            {
                var source = new GameObject("Voice " + i).AddComponent<AudioSource>();
                source.transform.SetParent(transform, false);
                source.playOnAwake = false; source.spatialBlend = 0.35f; source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 35; source.maxDistance = 160; source.dopplerLevel = 0;
                voices.Add(source);
            }
            // Distant calls are panned around the listener rather than placed in the world.
            for (int i = 0; i < 3; i++)
            {
                var source = new GameObject("Ambient voice " + i).AddComponent<AudioSource>();
                source.transform.SetParent(transform, false);
                source.playOnAwake = false; source.spatialBlend = 0; source.dopplerLevel = 0;
                ambientVoices.Add(source);
            }
            surf = gameObject.AddComponent<AudioSource>();
            surf.clip = MakeSurf(); surf.loop = true; surf.spatialBlend = 0; surf.volume = ambience * master; surf.playOnAwake = false;
            surf.Play();
            windLoop = gameObject.AddComponent<AudioSource>();
            windLoop.clip = MakeWind(); windLoop.loop = true; windLoop.spatialBlend = 0; windLoop.volume = 0; windLoop.playOnAwake = false;
            windLoop.Play();
            nextGull = 2 + (float)random.NextDouble() * 3;
            nextGroan = 6 + (float)random.NextDouble() * 6;
        }

        private void Update()
        {
            float step = Time.unscaledDeltaTime / ambienceFade;
            gulls = Mathf.MoveTowards(gulls, gullsTarget, step);
            wind = Mathf.MoveTowards(wind, windTarget, step);
            groans = Mathf.MoveTowards(groans, groansTarget, step);
            if (windLoop != null) windLoop.volume = wind * master * 0.55f;
            float now = Time.unscaledTime;
            if (now >= nextGull)
            {
                if (gulls > 0.05f) Call(gullCalls[random.Next(gullCalls.Length)], gulls * 0.4f, 0.9f, 1.15f);
                nextGull = now + Mathf.Lerp(9, 3, gulls) * (0.7f + 0.6f * (float)random.NextDouble());
            }
            if (now >= nextGroan)
            {
                if (groans > 0.05f) Call(groanCalls[random.Next(groanCalls.Length)], groans * 0.65f, 0.85f, 1.05f);
                nextGroan = now + Mathf.Lerp(26, 11, groans) * (0.7f + 0.6f * (float)random.NextDouble());
            }
        }

        private void Call(AudioClip clip, float volume, float lowPitch, float highPitch)
        {
            var voice = ambientVoices[nextAmbient]; nextAmbient = (nextAmbient + 1) % ambientVoices.Count;
            voice.clip = clip;
            voice.volume = Mathf.Clamp01(volume) * master;
            voice.pitch = Mathf.Lerp(lowPitch, highPitch, (float)random.NextDouble());
            voice.panStereo = (float)(random.NextDouble() * 1.4 - 0.7);
            voice.Play();
        }

        public void Play(Sfx sound, Vector3 position, float volume = 1, float pitchSpread = 0.06f)
        {
            if (!clips.TryGetValue(sound, out var clip)) return;
            var voice = voices[next]; next = (next + 1) % voices.Count;
            voice.transform.position = position;
            voice.pitch = 1 + (float)(random.NextDouble() * 2 - 1) * pitchSpread;
            voice.volume = Mathf.Clamp01(volume) * master;
            voice.clip = clip;
            voice.Play();
        }

        public void PlayUi(Sfx sound, float volume = 1)
        {
            var listener = FindFirstObjectByType<AudioListener>();
            Play(sound, listener != null ? listener.transform.position : Vector3.zero, volume, 0);
        }

        // Surf loop level (unchanged since T08).
        public void SetAmbience(float level)
        {
            ambience = Mathf.Clamp01(level);
            if (surf != null) surf.volume = ambience * master;
        }

        // Zone mix, each 0..1: gull calls (lagoon), warm wind (causeway), distant groans
        // (the deeps). Levels ease over `ambienceFade` seconds, so it can be called every frame.
        public void SetAmbience(float gulls, float wind, float groans)
        {
            gullsTarget = Mathf.Clamp01(gulls);
            windTarget = Mathf.Clamp01(wind);
            groansTarget = Mathf.Clamp01(groans);
        }

        // ------------------------------------------------------------ synthesis

        private AudioClip Make(string name, float seconds, Func<float, float> voice)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            var data = new float[count];
            float peak = 0;
            for (int i = 0; i < count; i++) { data[i] = voice(i / (float)Rate); peak = Mathf.Max(peak, Mathf.Abs(data[i])); }
            // Normalize to a consistent loudness, then fade the tail to avoid clicks.
            float gain = peak > 0 ? 0.85f / peak : 1;
            int fade = Mathf.Min(count, Rate / 50);
            for (int i = 0; i < count; i++)
            {
                data[i] *= gain;
                if (i >= count - fade) data[i] *= (count - i) / (float)fade;
            }
            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Stateful noise sources, rebuilt per clip so every clip is deterministic.
        private sealed class Noise
        {
            private readonly System.Random random;
            private float low, band, brown;
            public Noise(int seed) { random = new System.Random(seed); }
            public float White() => (float)(random.NextDouble() * 2 - 1);
            public float Low(float cutoff) { low += cutoff * (White() - low); return low; }
            public float Brown() { brown = Mathf.Clamp(brown + White() * 0.06f, -1, 1); brown *= 0.996f; return brown; }
            public float Band(float a, float b) { float l = Low(a); band += b * (l - band); return l - band; }
        }

        private static float Decay(float t, float tau) => Mathf.Exp(-t / tau);
        private static float Sine(float t, float hz) => Mathf.Sin(2 * Mathf.PI * hz * t);

        private Noise cannonNoise, repeaterNoise, impactNoise, splashNoise, sinkNoise, braceNoise, relightNoise;

        private float Cannon(float t)
        {
            if (t == 0) cannonNoise = new Noise(11);
            // Pitch falls from ~93 Hz to 38 Hz: integrated phase of the decaying sweep.
            float phase = 2 * Mathf.PI * (38 * t + 55 * 0.12f * (1 - Decay(t, 0.12f)));
            float boom = Mathf.Sin(phase) * Decay(t, 0.32f) * 0.9f;
            float body = cannonNoise.Low(0.05f) * Decay(t, 0.18f) * 2.2f;
            float crack = cannonNoise.White() * Decay(t, 0.012f) * 0.8f;
            float tail = cannonNoise.Brown() * Decay(t, 0.55f) * 0.5f;
            return boom + body + crack + tail;
        }

        private float Repeater(float t)
        {
            if (t == 0) repeaterNoise = new Noise(23);
            return repeaterNoise.White() * Decay(t, 0.01f) * 0.9f + repeaterNoise.Low(0.18f) * Decay(t, 0.05f) * 1.4f
                + Sine(t, 170) * Decay(t, 0.045f) * 0.5f;
        }

        private float Impact(float t)
        {
            if (t == 0) impactNoise = new Noise(31);
            float crunch = impactNoise.Band(0.25f, 0.02f) * Decay(t, 0.09f) * 2f;
            float knock = (Sine(t, 196) + 0.6f * Sine(t, 311)) * Decay(t, 0.07f) * 0.6f;
            float creak = impactNoise.Low(0.03f) * Decay(t, 0.25f) * 1.5f;
            return crunch + knock + creak;
        }

        private float Splash(float t)
        {
            if (t == 0) splashNoise = new Noise(43);
            float attack = Mathf.Clamp01(t / 0.025f);
            float wash = splashNoise.Band(0.35f, 0.01f) * Decay(t, 0.22f) * attack;
            float plop = Sine(t, 120 + 240 * Decay(t, 0.05f)) * Decay(t, 0.06f) * 0.4f;
            float bubbles = splashNoise.Low(0.08f) * (0.5f + 0.5f * Sine(t, 23)) * Decay(t, 0.35f) * 0.6f;
            return wash * 1.6f + plop + bubbles;
        }

        private static float Chime(float t, float[] notes, float step)
        {
            float value = 0;
            for (int i = 0; i < notes.Length; i++)
            {
                float start = i * step; if (t < start) break;
                float local = t - start;
                float env = Mathf.Clamp01(local / 0.006f) * Decay(local, 0.28f);
                value += (Sine(local, notes[i]) + 0.3f * Sine(local, notes[i] * 2) + 0.12f * Sine(local, notes[i] * 3)) * env;
            }
            return value;
        }

        private static float Bell(float t)
        {
            float f = 392f;
            float strike = Mathf.Clamp01(t / 0.004f);
            return strike * (Sine(t, f) * Decay(t, 1.1f) + 0.55f * Sine(t, f * 2.41f) * Decay(t, 0.6f)
                + 0.35f * Sine(t, f * 3.93f) * Decay(t, 0.35f) + 0.2f * Sine(t, f * 5.4f) * Decay(t, 0.18f));
        }

        private float Sink(float t)
        {
            if (t == 0) sinkNoise = new Noise(59);
            float groan = Mathf.Sin(2 * Mathf.PI * (70 * t - 12 * t * t)) * Decay(t, 1.2f) * 0.5f;
            float rumble = sinkNoise.Low(0.012f) * 3 * Decay(t, 1.4f);
            float gurgle = sinkNoise.Band(0.2f, 0.02f) * (0.5f + 0.5f * Sine(t, 7 + 4 * t)) * Decay(t, 0.9f);
            return groan + rumble + gurgle;
        }

        private float Brace(float t)
        {
            if (t == 0) braceNoise = new Noise(67);
            float rise = Mathf.Clamp01(t / 0.15f) * Decay(t, 0.3f);
            return braceNoise.Band(0.05f + 0.3f * Mathf.Clamp01(t / 0.4f), 0.02f) * rise * 1.6f + Sine(t, 110) * rise * 0.35f;
        }

        // A beacon catches fire: a soft low whoosh, then a warm D major chord swells,
        // slightly detuned for warmth, with a high shimmer as it rings out.
        private float Relight(float t)
        {
            if (t == 0) relightNoise = new Noise(89);
            float whoosh = relightNoise.Band(0.05f + 0.1f * Mathf.Clamp01(t / 0.4f), 0.01f) * Mathf.Clamp01(t / 0.08f) * Decay(t, 0.45f) * 1.4f;
            float chord = 0;
            float[] notes = { 146.83f, 220f, 293.66f, 369.99f, 440f, 587.33f };
            for (int i = 0; i < notes.Length; i++)
            {
                float start = 0.12f + i * 0.07f;
                if (t < start) break;
                float local = t - start, f = notes[i];
                float env = Mathf.Clamp01(local / 0.35f) * Decay(local, 1.7f);
                chord += env * (Sine(local, f) + 0.5f * Sine(local, f * 1.004f) + 0.25f * Sine(local, f * 2) + 0.1f * Sine(local, f * 3)) / (1 + i * 0.15f);
            }
            float late = Mathf.Max(0, t - 0.6f);
            float shimmer = Sine(t, 1174.66f) * 0.12f * Mathf.Clamp01(late / 0.3f) * Decay(late, 0.9f);
            return whoosh + chord * 0.5f + shimmer;
        }

        // A gull: a long rising-falling "kyow" then shorter "kek"s, harmonic-rich and
        // slightly rasping. Phase is integrated so the glides stay smooth.
        private AudioClip GullCall(string name, float pitch, int yelps, int seed)
        {
            var noise = new Noise(seed);
            float phase = 0;
            const float yelp = 0.2f, gap = 0.07f;
            return Make(name, yelps * (yelp + gap) + 0.1f, t =>
            {
                int index = (int)(t / (yelp + gap));
                float local = t - index * (yelp + gap);
                if (index >= yelps || local > yelp) return 0;
                float u = local / yelp;
                float hz = pitch * (index == 0 ? 1250 : 1100 - 40 * index) * (0.72f + 0.45f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u * 1.25f)));
                phase += 2 * Mathf.PI * hz / Rate;
                float env = Mathf.Clamp01(local / 0.015f) * Mathf.Pow(1 - u, 1.4f);
                float tone = Mathf.Sin(phase) + 0.55f * Mathf.Sin(2 * phase + 0.3f) + 0.3f * Mathf.Sin(3 * phase) + 0.18f * Mathf.Sin(4 * phase);
                return tone * (1 + 0.35f * noise.Low(0.3f)) * env;
            });
        }

        // Something vast shifting in the deep: two detuned low tones whose spectral
        // peak rises and falls, over a slow rumble; long swell, longer fade.
        private AudioClip GroanCall(string name, float baseHz, int seed)
        {
            var noise = new Noise(seed);
            float p1 = 0, p2 = 0;
            var weights = new float[6];
            int sample = 0;
            return Make(name, 6.5f, t =>
            {
                float hz = baseHz * (1.12f - 0.18f * Mathf.Clamp01(t / 5f)) * (1 + 0.02f * Mathf.Sin(2 * Mathf.PI * 0.7f * t));
                p1 += 2 * Mathf.PI * hz / Rate;
                p2 += 2 * Mathf.PI * hz * 1.007f / Rate;
                // The spectral peak moves slowly; refresh the harmonic weights every 256 samples.
                if ((sample++ & 255) == 0)
                {
                    float peak = 2 + 2.5f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 5f));
                    for (int k = 1; k <= 5; k++) weights[k] = Mathf.Exp(-(k - peak) * (k - peak) / 3f) / k;
                }
                float tone = Mathf.Sin(p2) * 0.6f;
                for (int k = 1; k <= 5; k++) tone += weights[k] * Mathf.Sin(k * p1);
                float env = Mathf.Clamp01(t / 1.2f) * Decay(Mathf.Max(0, t - 1.2f), 2.2f);
                return tone * env + noise.Low(0.004f) * 4f * env;
            });
        }

        // Warm wind: band-limited noise whose brightness and level follow slow gusts,
        // crossfaded into a seamless loop like the surf.
        private AudioClip MakeWind()
        {
            const float seconds = 16;
            int count = (int)(seconds * Rate), blend = Rate;
            var noise = new Noise(83);
            var raw = new float[count + blend];
            for (int i = 0; i < raw.Length; i++)
            {
                float t = i / (float)Rate;
                float gust = 0.5f + 0.3f * Mathf.Sin(2 * Mathf.PI * t / 7f) + 0.2f * Mathf.Sin(2 * Mathf.PI * t / 3.1f + 0.7f);
                raw[i] = noise.Band(0.03f + 0.08f * gust, 0.006f) * (0.4f + 0.6f * gust);
            }
            var data = new float[count];
            float peak = 0;
            for (int i = 0; i < count; i++)
            {
                data[i] = i < blend ? Mathf.Lerp(raw[count + i], raw[i], i / (float)blend) : raw[i];
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            for (int i = 0; i < count; i++) data[i] *= 0.7f / Mathf.Max(peak, 0.0001f);
            var clip = AudioClip.Create("wind", count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Surf: layered low noise with slow swells, crossfaded into a seamless loop.
        private AudioClip MakeSurf()
        {
            const float seconds = 12;
            int count = (int)(seconds * Rate), blend = Rate;
            var noise = new Noise(71);
            var raw = new float[count + blend];
            for (int i = 0; i < raw.Length; i++)
            {
                float t = i / (float)Rate;
                float swell = 0.55f + 0.3f * Mathf.Sin(2 * Mathf.PI * t / 6f) + 0.15f * Mathf.Sin(2 * Mathf.PI * t / 2.4f + 1.3f);
                raw[i] = (noise.Low(0.02f) * 3.5f + noise.Band(0.12f, 0.01f) * 0.35f) * swell;
            }
            var data = new float[count];
            float peak = 0;
            for (int i = 0; i < count; i++)
            {
                data[i] = i < blend ? Mathf.Lerp(raw[count + i], raw[i], i / (float)blend) : raw[i];
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            for (int i = 0; i < count; i++) data[i] *= 0.7f / Mathf.Max(peak, 0.0001f);
            var clip = AudioClip.Create("surf", count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
