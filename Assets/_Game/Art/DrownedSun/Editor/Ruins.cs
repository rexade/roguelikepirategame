using System.Collections.Generic;
using UnityEngine;

namespace PirateGame.Art.Editor
{
    // Aurelian masonry: every piece is chamfered, faceted limestone with gold accents.
    // Positions are world space; `at.y` is the ground (seabed or deck) the piece stands on.
    public static class Ruins
    {
        public static void Column(Composer c, KitMaterials m, Vector3 at, float height, float radius, int seed, bool broken, float yaw = 0)
        {
            var random = new System.Random(seed);
            float phase = (float)random.NextDouble();
            c.Add(m.LimestoneShade, Shapes.Box(new Vector3(radius * 2.7f, 0.7f, radius * 2.7f), 0.12f), at + Vector3.up * 0.35f, yaw);
            float top = broken ? height * Mathf.Lerp(0.3f, 0.75f, (float)random.NextDouble()) : height - 1.0f;
            var profile = new List<Vector2> { new Vector2(radius * 1.25f, 0.7f), new Vector2(radius * 1.25f, 0.95f), new Vector2(radius * 1.02f, 1.2f), new Vector2(radius, 1.35f),
                new Vector2(radius * 0.92f, top) };
            if (!broken) { profile.Add(new Vector2(radius * 1.22f, height - 0.55f)); profile.Add(new Vector2(radius * 1.22f, height - 0.45f)); profile.Add(new Vector2(0, height - 0.45f)); }
            c.Add(m.Limestone, Shapes.Lathe(profile, 8, phase, capTop: false), at, yaw);
            if (broken) c.Add(m.Limestone, JaggedTop(radius * 0.92f, top, 8, phase, seed), at, yaw);
            else c.Add(m.LimestoneShade, Shapes.Box(new Vector3(radius * 2.4f, 0.45f, radius * 2.4f), 0.08f), at + Vector3.up * (height - 0.2f), yaw);
        }

        // Break line of a snapped shaft: raised shards around a sunken core.
        public static MeshBuilder JaggedTop(float radius, float y, int sides, float phase, int seed)
        {
            var random = new System.Random(seed * 31 + 7);
            var ring = new Vector3[sides]; var raised = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = phase + i * Mathf.PI * 2 / sides;
                ring[i] = new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
                float shard = (float)random.NextDouble();
                raised[i] = new Vector3(Mathf.Cos(a) * radius * 0.96f, y + 0.1f + shard * shard * radius * 1.2f, Mathf.Sin(a) * radius * 0.96f);
            }
            return Shapes.Loft(new List<IReadOnlyList<Vector3>> { ring, raised }, false, true);
        }

        // Drums of a toppled column scattered along `yaw`, half buried in the sand.
        public static void FallenColumn(Composer c, KitMaterials m, Vector3 at, float yaw, float length, float radius, int seed)
        {
            var random = new System.Random(seed);
            float drum = radius * 2.1f;
            int count = Mathf.Max(2, Mathf.RoundToInt(length / drum));
            var direction = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var side = Quaternion.Euler(0, yaw + 90, 0) * Vector3.forward;
            var cylinder = Shapes.Lathe(new[] { new Vector2(radius, 0), new Vector2(radius, drum) }, 8, 0, true, true);
            for (int i = 0; i < count; i++)
            {
                float drift = ((float)random.NextDouble() - 0.5f) * radius * 0.8f * i / count;
                var p = at + direction * (i * drum * 1.04f) + side * drift + Vector3.up * radius * 0.75f;
                var rotation = Quaternion.Euler(0, yaw + ((float)random.NextDouble() - 0.5f) * 14, 0) * Quaternion.Euler(90, 0, (float)random.NextDouble() * 45);
                c.Add(m.Limestone, cylinder, p, rotation, Vector3.one);
            }
        }

        // Steps rising along the local +z from `at` (their foot) by `rise` each.
        public static void Stairs(Composer c, KitMaterials m, Vector3 at, float yaw, float width, int steps, float rise, float run, bool collide = false)
        {
            var rotation = Quaternion.Euler(0, yaw, 0);
            for (int i = 0; i < steps; i++)
            {
                float h = rise * (i + 1);
                var block = Shapes.Box(new Vector3(width, h, run), 0.06f);
                c.Add(i % 2 == 0 ? m.Limestone : m.LimestoneShade, block, Matrix4x4.TRS(at + rotation * new Vector3(0, h * 0.5f, (i + 0.5f) * run), rotation, Vector3.one));
            }
            // Side walls (cheeks) frame the flight.
            float total = rise * steps, depth = run * steps;
            foreach (float x in new[] { -1, 1 })
                c.Add(m.LimestoneShade, Shapes.Frustum(new Vector2(0.9f, depth), new Vector2(0.9f, run * 1.2f), total + 0.4f, new Vector2(0, depth * 0.5f - run * 0.6f), 0.08f),
                    Matrix4x4.TRS(at + rotation * new Vector3(x * (width * 0.5f + 0.45f), 0, depth * 0.5f), rotation, Vector3.one));
            if (collide && total > 0.5f) c.Box(at + rotation * new Vector3(0, total * 0.5f, depth * 0.5f), new Vector3(width + 1.8f, Mathf.Max(total, 3), depth), yaw);
        }

        // Paved plaza whose slab tops sit at at.y; some slabs are missing or heaved.
        public static void Plaza(Composer c, KitMaterials m, Vector3 at, float width, float depth, float yaw, int seed)
        {
            var random = new System.Random(seed);
            var rotation = Quaternion.Euler(0, yaw, 0);
            const float tile = 2.3f;
            int nx = Mathf.Max(1, Mathf.FloorToInt(width / tile)), nz = Mathf.Max(1, Mathf.FloorToInt(depth / tile));
            var slab = Shapes.Box(new Vector3(tile - 0.12f, 0.4f, tile - 0.12f), 0.07f);
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float edge = Mathf.Min(Mathf.Min(i, nx - 1 - i), Mathf.Min(j, nz - 1 - j));
                    if (random.NextDouble() < (edge < 1 ? 0.35f : 0.08f)) continue;
                    var local = new Vector3((i - (nx - 1) * 0.5f) * tile, -0.2f + ((float)random.NextDouble() - 0.5f) * 0.12f, (j - (nz - 1) * 0.5f) * tile);
                    var tilt = random.NextDouble() < 0.12 ? Quaternion.Euler(((float)random.NextDouble() - 0.5f) * 14, 0, ((float)random.NextDouble() - 0.5f) * 14) : Quaternion.identity;
                    c.Add((i + j) % 3 == 0 ? m.LimestoneShade : m.Limestone, slab, Matrix4x4.TRS(at + rotation * local, rotation * tilt, Vector3.one));
                }
            // Kerb along the long sides.
            foreach (float z in new[] { -1, 1 })
                c.Add(m.LimestoneShade, Shapes.Box(new Vector3(nx * tile + 1, 0.7f, 0.9f), 0.1f),
                    Matrix4x4.TRS(at + rotation * new Vector3(0, -0.1f, z * (nz * tile * 0.5f + 0.5f)), rotation, Vector3.one));
        }

        // Round arch of wedge voussoirs on two piers; the keystone carries a gold sun.
        public static void Arch(Composer c, KitMaterials m, Vector3 at, float yaw, float span, float springHeight, float depth, int seed, bool broken, bool collide = true)
        {
            var random = new System.Random(seed);
            var rotation = Quaternion.Euler(0, yaw, 0);
            float pierWidth = Mathf.Max(1.4f, span * 0.2f), radius = span * 0.5f, thickness = pierWidth * 0.85f;
            foreach (float side in new[] { -1, 1 })
            {
                var center = new Vector3(side * (radius + pierWidth * 0.5f), 0, 0);
                float h = springHeight;
                if (broken && side > 0) h *= Mathf.Lerp(0.45f, 0.8f, (float)random.NextDouble());
                c.Add(m.LimestoneShade, Shapes.Box(new Vector3(pierWidth * 1.35f, 1.0f, depth * 1.25f), 0.12f), Matrix4x4.TRS(at + rotation * (center + Vector3.up * 0.5f), rotation, Vector3.one));
                c.Add(m.Limestone, Shapes.Frustum(new Vector2(pierWidth, depth), new Vector2(pierWidth * 0.94f, depth * 0.94f), h - 1, default, 0.12f),
                    Matrix4x4.TRS(at + rotation * (center + Vector3.up * 1f), rotation, Vector3.one));
                if (broken && side > 0) c.Add(m.Limestone, JaggedTop(pierWidth * 0.55f, h, 6, 0.3f, seed), Matrix4x4.TRS(at + rotation * center, rotation, Vector3.one));
                if (collide) c.Box(at + rotation * (center + Vector3.up * h * 0.5f), new Vector3(pierWidth * 1.35f, Mathf.Max(h, 4), depth * 1.25f), yaw);
            }
            const int stones = 9;
            int missingFrom = broken ? 4 + random.Next(3) : stones;
            var arch = Quaternion.Euler(-90, 0, 0);
            for (int i = 0; i < stones; i++)
            {
                if (i >= missingFrom) break;
                float a0 = Mathf.PI * (stones - i) / stones, a1 = Mathf.PI * (stones - i - 1) / stones;
                float r1 = radius + thickness * (i == stones / 2 ? 1.18f : 1f);
                var outline = new[] {
                    new Vector2(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius), new Vector2(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius),
                    new Vector2(Mathf.Cos(a1) * r1, Mathf.Sin(a1) * r1), new Vector2(Mathf.Cos(a0) * r1, Mathf.Sin(a0) * r1) };
                var stone = Shapes.Prism(outline, -depth * 0.5f, depth * 0.5f, 0.06f, 0.06f, true, true);
                c.Add(i == stones / 2 ? m.LimestoneShade : m.Limestone, stone, Matrix4x4.TRS(at + rotation * new Vector3(0, springHeight, 0), rotation * arch, Vector3.one));
            }
            if (!broken)
            {
                var keystone = at + rotation * new Vector3(0, springHeight + radius + thickness * 0.55f, depth * 0.5f + 0.05f);
                SunDisc(c, m, keystone, rotation, thickness * 0.42f, false);
            }
        }

        // Standing sun disc facing the rotation's forward.
        public static void SunDisc(Composer c, KitMaterials m, Vector3 center, Quaternion rotation, float radius, bool cracked, bool patina = false)
        {
            var disc = cracked ? Shapes.Disc(radius, radius * 0.14f, 20, 12, radius * 0.55f, 0.16f, 20, 290)
                               : Shapes.Disc(radius, radius * 0.14f, 20, 12, radius * 0.55f);
            c.Add(patina ? m.Patina : m.Gold, disc, center, rotation, Vector3.one);
        }

        // Column stump whose capital breaks the surface: the altar a relic rests on.
        public static void Pedestal(Composer c, KitMaterials m, Vector3 at, int seed, float top = 0.35f)
        {
            float height = top - at.y;
            var random = new System.Random(seed);
            float phase = (float)random.NextDouble();
            c.Add(m.Limestone, Shapes.Lathe(new[] { new Vector2(1.05f, 0), new Vector2(0.85f, 0.5f), new Vector2(0.78f, height - 0.35f) }, 8, phase, false), at);
            c.Add(m.LimestoneShade, Shapes.Box(new Vector3(1.8f, 0.35f, 1.8f), 0.08f), new Vector3(at.x, top - 0.175f, at.z), (float)random.NextDouble() * 40);
            c.Box(new Vector3(at.x, 0, at.z), new Vector3(1.6f, 4, 1.6f));
        }

        // Scattered ashlar blocks resting on the ground around `at`.
        public static void Blocks(Composer c, KitMaterials m, Vector3 at, float spread, int count, int seed)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                var size = new Vector3(Mathf.Lerp(1.2f, 2.8f, (float)random.NextDouble()), Mathf.Lerp(0.8f, 1.5f, (float)random.NextDouble()), Mathf.Lerp(1.0f, 2.2f, (float)random.NextDouble()));
                float a = (float)random.NextDouble() * Mathf.PI * 2, d = spread * Mathf.Sqrt((float)random.NextDouble());
                var p = at + new Vector3(Mathf.Cos(a) * d, size.y * 0.35f, Mathf.Sin(a) * d);
                var rotation = Quaternion.Euler(((float)random.NextDouble() - 0.5f) * 30, (float)random.NextDouble() * 360, ((float)random.NextDouble() - 0.5f) * 30);
                c.Add(random.NextDouble() < 0.5 ? m.Limestone : m.LimestoneShade, Shapes.Box(size, 0.12f), p, rotation, Vector3.one);
            }
        }

        // Tapered needle with a gilded pyramidion; `tilt` leans it (degrees).
        public static void Obelisk(Composer c, KitMaterials m, Vector3 at, float height, float yaw, float tilt)
        {
            var rotation = Quaternion.Euler(tilt, yaw, 0);
            float w = height * 0.11f;
            c.Add(m.LimestoneShade, Shapes.Box(new Vector3(w * 2.2f, w * 0.8f, w * 2.2f), w * 0.1f), at + Vector3.up * w * 0.4f, yaw);
            c.Add(m.Limestone, Shapes.Frustum(new Vector2(w, w), new Vector2(w * 0.66f, w * 0.66f), height, default, w * 0.06f), Matrix4x4.TRS(at + Vector3.up * w * 0.8f, rotation, Vector3.one));
            c.Add(m.Gold, Shapes.Frustum(new Vector2(w * 0.66f, w * 0.66f), new Vector2(0.02f, 0.02f), w * 0.9f, default, 0, false),
                Matrix4x4.TRS(at + Vector3.up * w * 0.8f + rotation * Vector3.up * height, rotation, Vector3.one));
        }
    }
}
