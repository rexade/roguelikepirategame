using UnityEngine;

namespace PirateGame.Presentation.World
{
    // Faceted flame: wobbles its scale and an optional light with smooth noise.
    public sealed class Flicker : MonoBehaviour
    {
        [Min(0)] public float amount = 0.18f;
        [Min(0)] public float speed = 5;
        public Light glow;
        private Vector3 baseScale;
        private float baseIntensity, seed;

        private void Awake()
        {
            baseScale = transform.localScale;
            seed = (transform.position.x * 0.37f + transform.position.z * 0.13f) % 97f;
            if (glow != null) baseIntensity = glow.intensity;
        }

        private void Update()
        {
            float t = Time.time * speed + seed;
            float height = 1 + amount * (Mathf.PerlinNoise(t, seed) * 2 - 1);
            float width = 1 + amount * 0.5f * (Mathf.PerlinNoise(seed, t * 1.3f) * 2 - 1);
            transform.localScale = new Vector3(baseScale.x * width, baseScale.y * height, baseScale.z * width);
            if (glow != null && glow.intensity > 0) glow.intensity = baseIntensity * (0.85f + 0.3f * Mathf.PerlinNoise(t * 0.7f, seed + 3));
        }
    }
}
