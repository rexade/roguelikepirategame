using System.Collections.Generic;
using UnityEngine;

namespace PirateGame.Art.Editor
{
    public enum IsletStyle { Jungle, Rocky, Obsidian }

    // Faceted palms, terraced islets, rocks and obsidian coral.
    public static class Nature
    {
        public static void Palm(Composer c, KitMaterials m, Vector3 at, float height, float leanYaw, float lean, int seed)
        {
            var random = new System.Random(seed);
            const int segments = 7;
            var bend = Quaternion.Euler(0, leanYaw, 0) * Vector3.forward;
            Vector3 Point(float t) => at + Vector3.up * (t * height) + bend * (lean * t * t * height * 0.32f);
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments, t1 = (float)(i + 1) / segments;
                var a = Point(t0); var b = Point(t1);
                float r0 = Mathf.Lerp(0.34f, 0.2f, t0), r1 = Mathf.Lerp(0.34f, 0.2f, t1);
                var segment = Shapes.Lathe(new[] { new Vector2(r0 * 1.12f, 0), new Vector2(r0, 0.12f), new Vector2(r1, (b - a).magnitude) }, 5, i * 0.7f, true);
                c.Add(m.Trunk, segment, Matrix4x4.TRS(a, Quaternion.FromToRotation(Vector3.up, b - a), Vector3.one));
            }
            var crown = Point(1);
            int fronds = 8 + random.Next(2);
            for (int i = 0; i < fronds; i++)
            {
                float yaw = i * 360f / fronds + (float)random.NextDouble() * 18;
                float length = Mathf.Lerp(3.6f, 4.8f, (float)random.NextDouble());
                float rise = Mathf.Lerp(0.4f, 1.3f, (float)random.NextDouble());
                c.Add(i % 2 == 0 ? m.Jungle : m.JungleDark, Frond(length, rise), Matrix4x4.TRS(crown, Quaternion.Euler(0, yaw, 0), Vector3.one));
            }
            for (int i = 0; i < 3; i++)
                c.Add(m.WoodDark, Shapes.Box(Vector3.one * 0.32f, 0.06f), crown + Quaternion.Euler(0, i * 120, 0) * Vector3.forward * 0.35f + Vector3.down * 0.35f, i * 40);
        }

        // One drooping blade along local +z, both faces built so it reads from above and below.
        private static MeshBuilder Frond(float length, float rise)
        {
            var frond = new MeshBuilder();
            const int steps = 5;
            Vector3 Spine(int k) { float t = (float)k / steps; return new Vector3(0, rise * Mathf.Sin(t * Mathf.PI * 0.8f) - t * t * 1.6f, t * length); }
            float Width(int k) => Mathf.Lerp(0.62f, 0.04f, (float)k / steps);
            for (int k = 0; k < steps; k++)
            {
                var a = Spine(k); var b = Spine(k + 1);
                // A shallow V along the midrib gives each blade two lit facets.
                var la = a + new Vector3(-Width(k), -0.12f, 0); var ra = a + new Vector3(Width(k), -0.12f, 0);
                var lb = b + new Vector3(-Width(k + 1), -0.1f, 0); var rb = b + new Vector3(Width(k + 1), -0.1f, 0);
                frond.QuadFacing(a, la, lb, b, Vector3.up); frond.QuadFacing(a, ra, rb, b, Vector3.up);
                frond.QuadFacing(a, la, lb, b, Vector3.down); frond.QuadFacing(a, ra, rb, b, Vector3.down);
            }
            return frond;
        }

        // Terraced islet: beach skirt, rock band and plateau, each a chamfered prism of a
        // jittered outline. Returns the waterline outline (for collision and the chart).
        public static Vector2[] Islet(Composer c, KitMaterials m, Vector2 center, Vector2 radii, float height, int seed, IsletStyle style,
            int palms, int rocks, float skirtDepth = -6)
        {
            var random = new System.Random(seed);
            var outline = Shapes.Ring(13, radii.x, radii.y, seed, 0.16f, (float)random.NextDouble());
            Vector2[] Scaled(float k, float jitter)
            {
                var ring = new Vector2[outline.Length];
                for (int i = 0; i < ring.Length; i++) ring[i] = outline[i] * (k * (1 + jitter * ((float)random.NextDouble() * 2 - 1)));
                return ring;
            }
            var origin = new Vector3(center.x, 0, center.y);
            var beachMaterial = style == IsletStyle.Obsidian ? m.Rock : m.Sand;
            c.Add(style == IsletStyle.Obsidian ? m.Obsidian : m.Seabed, Shapes.Prism(Scaled(1.45f, 0.05f), skirtDepth, -1.2f, 2.2f), origin);
            c.Add(beachMaterial, Shapes.Prism(outline, -1.4f, 0.45f, 1.1f), origin);
            var band = style == IsletStyle.Jungle ? m.LimestoneShade : style == IsletStyle.Rocky ? m.Rock : m.Obsidian;
            c.Add(band, Shapes.Prism(Scaled(0.8f, 0.06f), 0.2f, Mathf.Min(1.8f, height * 0.4f), 0.35f), origin);
            if (style == IsletStyle.Jungle)
            {
                c.Add(m.Jungle, Shapes.Prism(Scaled(0.64f, 0.08f), 1.2f, height, 0.9f), origin);
                // Faceted mounds of dense green break the flat plateau into a skyline.
                int mounds = 2 + random.Next(3);
                for (int i = 0; i < mounds; i++)
                {
                    float a = (float)random.NextDouble() * Mathf.PI * 2, d = (float)random.NextDouble() * 0.38f;
                    float r = Mathf.Min(radii.x, radii.y) * Mathf.Lerp(0.28f, 0.45f, (float)random.NextDouble());
                    float h = Mathf.Lerp(1.8f, 3.6f, (float)random.NextDouble());
                    var mound = Shapes.Lathe(new[] { new Vector2(r, 0), new Vector2(r * 0.82f, h * 0.45f), new Vector2(r * 0.42f, h * 0.9f), new Vector2(0, h) },
                        7, (float)random.NextDouble(), true, false, seed * 17 + i, 0.22f);
                    c.Add(i % 2 == 0 ? m.JungleDark : m.Jungle, mound, origin + new Vector3(Mathf.Cos(a) * radii.x * d, height - 0.3f, Mathf.Sin(a) * radii.y * d));
                }
            }
            else
                c.Add(style == IsletStyle.Rocky ? m.Rock : m.Obsidian, Shapes.Prism(Scaled(0.55f, 0.14f), 1.2f, height, 1.6f), origin);
            for (int i = 0; i < palms; i++)
            {
                float a = (float)random.NextDouble() * Mathf.PI * 2, d = Mathf.Lerp(0.15f, 0.62f, (float)random.NextDouble());
                var p = origin + new Vector3(Mathf.Cos(a) * radii.x * d, style == IsletStyle.Jungle ? height : 0.6f, Mathf.Sin(a) * radii.y * d);
                Palm(c, m, p, Mathf.Lerp(5.5f, 8.5f, (float)random.NextDouble()), a * Mathf.Rad2Deg + 90, Mathf.Lerp(0.2f, 0.9f, (float)random.NextDouble()), seed * 7 + i);
            }
            for (int i = 0; i < rocks; i++)
            {
                float a = (float)random.NextDouble() * Mathf.PI * 2, d = Mathf.Lerp(0.85f, 1.05f, (float)random.NextDouble());
                var p = origin + new Vector3(Mathf.Cos(a) * radii.x * d, -0.4f, Mathf.Sin(a) * radii.y * d);
                Rock(c, style == IsletStyle.Obsidian ? m.Obsidian : m.Rock, p, Mathf.Lerp(1.4f, 3.2f, (float)random.NextDouble()), seed * 13 + i);
            }
            var world = new Vector2[outline.Length];
            for (int i = 0; i < outline.Length; i++) world[i] = outline[i] * 0.97f + center;
            c.Solid(Seabed.Fence(world, -4, 4, true), Matrix4x4.identity);
            return world;
        }

        public static void Rock(Composer c, Material material, Vector3 at, float size, int seed)
        {
            var random = new System.Random(seed);
            var rock = Shapes.Lathe(new[] { new Vector2(1, 0), new Vector2(0.92f, 0.45f), new Vector2(0.5f, 0.9f), new Vector2(0.1f, 1.05f) },
                6, (float)random.NextDouble(), true, false, seed, 0.28f);
            c.Add(material, rock, at, Quaternion.Euler(((float)random.NextDouble() - 0.5f) * 20, (float)random.NextDouble() * 360, ((float)random.NextDouble() - 0.5f) * 20),
                new Vector3(size, size * Mathf.Lerp(0.6f, 1.1f, (float)random.NextDouble()), size * Mathf.Lerp(0.8f, 1.2f, (float)random.NextDouble())));
        }

        // Living coral heads on the lagoon floor: rounded, faceted lumps in rose, cream
        // and green that read through the water as colour, not detail.
        public static void CoralHead(Composer c, KitMaterials m, Vector3 at, float size, int seed)
        {
            var random = new System.Random(seed);
            int lumps = 3 + random.Next(3);
            for (int i = 0; i < lumps; i++)
            {
                float r = size * Mathf.Lerp(0.35f, 0.7f, (float)random.NextDouble());
                float h = r * Mathf.Lerp(0.7f, 1.3f, (float)random.NextDouble());
                var lump = Shapes.Lathe(new[] { new Vector2(r * 0.7f, 0), new Vector2(r, h * 0.35f), new Vector2(r * 0.85f, h * 0.75f), new Vector2(r * 0.4f, h), new Vector2(0, h * 1.05f) },
                    7, (float)random.NextDouble(), true, false, seed * 31 + i, 0.2f);
                float a = (float)random.NextDouble() * Mathf.PI * 2, d = (float)random.NextDouble() * size * 0.8f;
                var material = i % 3 == 0 ? m.ReefCoral : i % 3 == 1 ? m.LimestoneShade : m.Seagrass;
                c.Add(material, lump, at + new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d));
            }
        }

        // A cluster of black spikes: obsidian coral overtaking the northern ruins.
        public static void Coral(Composer c, KitMaterials m, Vector3 at, float height, int seed)
        {
            var random = new System.Random(seed);
            int spikes = 4 + random.Next(4);
            for (int i = 0; i < spikes; i++)
            {
                float h = height * Mathf.Lerp(0.45f, 1f, (float)random.NextDouble());
                float r = h * 0.14f;
                var spike = Shapes.Lathe(new[] { new Vector2(r, 0), new Vector2(r * 0.55f, h * 0.6f), new Vector2(0, h) }, 5, (float)random.NextDouble(), true, false, seed + i, 0.2f);
                float a = (float)random.NextDouble() * Mathf.PI * 2;
                var p = at + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (float)random.NextDouble() * height * 0.3f;
                c.Add(i % 3 == 0 ? m.Coral : m.Obsidian, spike, p, Quaternion.Euler(Mathf.Sin(a) * 18, 0, Mathf.Cos(a) * -18), Vector3.one);
            }
        }
    }
}
