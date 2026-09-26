using UnityEngine;

namespace PirateGame.Art.Editor
{
    public enum SailStyle { Sun, Wrecker, Gunboat, Corsair }

    // Procedural textures: sails with the sun and eclipse emblems, the light pillar ramp.
    public static class Textures
    {
        public static Texture2D PillarGradient() =>
            KitAssets.Texture("Pillar gradient", 8, 256, (x, y) =>
            {
                float v = (y + 0.5f) / 256f;
                float fadeIn = Mathf.SmoothStep(0, 1, v / 0.03f);
                float value = Mathf.Pow(1 - v, 1.7f) * fadeIn;
                return new Color(value, value, value, value);
            }, srgb: false, wrap: TextureWrapMode.Clamp, mipmaps: false);

        public static Texture2D Sail(string name, SailStyle style) =>
            KitAssets.Texture(name, 256, 256, (x, y) => SailPixel(x, y, style), srgb: true, wrap: TextureWrapMode.Clamp, mipmaps: true,
                alpha: style == SailStyle.Wrecker || style == SailStyle.Gunboat);

        private static Color SailPixel(int x, int y, SailStyle style)
        {
            Color canvas = style == SailStyle.Sun ? new Color(0.95f, 0.90f, 0.78f)
                : style == SailStyle.Wrecker ? new Color(0.44f, 0.17f, 0.12f)
                : style == SailStyle.Gunboat ? new Color(0.27f, 0.19f, 0.30f)
                : new Color(0.10f, 0.10f, 0.11f);
            float grain = Mathf.PerlinNoise(x * 0.09f, y * 0.35f) * 0.08f - 0.04f;
            var color = canvas * (1 + grain);
            if (x % 43 < 2) color *= 0.86f;                                    // panel seams
            if (x < 6 || x > 249 || y < 6 || y > 249) color *= 0.82f;          // hem
            float dx = x - 128, dy = y - 136, r = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx);
            if (style == SailStyle.Sun)
            {
                var gold = new Color(0.86f, 0.60f, 0.17f);
                var ochre = new Color(0.60f, 0.37f, 0.10f);
                float ray = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, Mathf.Round(angle * Mathf.Rad2Deg / 30f) * 30f));
                float rayWidth = Mathf.Lerp(9f, 0f, Mathf.InverseLerp(54, 98, r));
                if (r < 44) color = gold;
                else if (r < 49) color = ochre;
                else if (r > 54 && r < 98 && ray < rayWidth) color = gold;
            }
            else
            {
                var ring = style == SailStyle.Corsair ? new Color(0.92f, 0.70f, 0.22f) : new Color(0.72f, 0.54f, 0.18f);
                if (r < 44) color = new Color(0.04f, 0.04f, 0.05f);
                else if (r < 51) color = ring;
            }
            float alpha = 1;
            if (style == SailStyle.Wrecker || style == SailStyle.Gunboat)
            {
                // Tattered foot and a few holes: wreckers sail on stolen rags.
                float edge = 14 + 12 * Mathf.PerlinNoise(x * 0.07f, 3.1f) + 10 * Mathf.PerlinNoise(x * 0.31f, 7.7f);
                if (y < edge) alpha = 0;
                foreach (var hole in new[] { new Vector3(52, 200, 9), new Vector3(200, 84, 7), new Vector3(70, 60, 6) })
                    if ((x - hole.x) * (x - hole.x) + (y - hole.y) * (y - hole.y) < hole.z * hole.z * (0.7f + 0.6f * Mathf.PerlinNoise(x * 0.2f, y * 0.2f))) alpha = 0;
            }
            color.a = alpha;
            return color;
        }
    }
}
