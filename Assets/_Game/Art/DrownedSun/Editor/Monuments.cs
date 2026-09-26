using System.Collections.Generic;
using UnityEngine;

namespace PirateGame.Art.Editor
{
    public enum ColossusPose { RaisedDisc, Crossed, Broken }

    // The sun-kings and their works: moai-faced colossi, the Sun Gate, the causeway,
    // the offering hand and the Drowned Crown. All sizes in metres.
    public static class Monuments
    {
        // Moai-like sun-king head facing local +z, neck base at the origin (about 11.6 m
        // tall at scale 1, crown rays included it reaches about 15 m).
        public static void ColossusHead(Composer c, KitMaterials m, Matrix4x4 at, float scale = 1)
        {
            var s = Matrix4x4.Scale(Vector3.one * scale);
            void Part(Material material, MeshBuilder part, Vector3 p, Vector3 euler = default) =>
                c.Add(material, part, at * s * Matrix4x4.TRS(p, Quaternion.Euler(euler), Vector3.one));
            Part(m.LimestoneShade, Shapes.Frustum(new Vector2(5.4f, 4.6f), new Vector2(4.6f, 4.1f), 2.6f, default, 0.25f), Vector3.zero);
            Part(m.Limestone, Shapes.Frustum(new Vector2(4.6f, 4.1f), new Vector2(4.4f, 4.4f), 2.0f, new Vector2(0, 0.25f), 0.25f), new Vector3(0, 2.4f, 0));
            Part(m.Limestone, Shapes.Frustum(new Vector2(4.4f, 4.4f), new Vector2(3.9f, 3.6f), 7.4f, new Vector2(0, -0.35f), 0.3f), new Vector3(0, 4.2f, 0));
            Part(m.LimestoneShade, Shapes.Box(new Vector3(4.3f, 0.95f, 1.2f), 0.2f), new Vector3(0, 8.35f, 1.85f));        // brow shelf
            Part(m.Obsidian, Shapes.Box(new Vector3(1.2f, 0.55f, 0.3f), 0.05f), new Vector3(-0.98f, 7.55f, 2.02f));        // eye slots
            Part(m.Obsidian, Shapes.Box(new Vector3(1.2f, 0.55f, 0.3f), 0.05f), new Vector3(0.98f, 7.55f, 2.02f));
            var nose = Shapes.Loft(new List<IReadOnlyList<Vector3>> {
                new[] { new Vector3(-0.62f, 0, 1.85f), new Vector3(0.62f, 0, 1.85f), new Vector3(0, 0, 3.25f) },
                new[] { new Vector3(-0.36f, 3.1f, 1.95f), new Vector3(0.36f, 3.1f, 1.95f), new Vector3(0, 3.1f, 2.55f) } }, true, true);
            Part(m.Limestone, nose, new Vector3(0, 4.85f, 0));
            Part(m.LimestoneShade, Shapes.Box(new Vector3(1.8f, 0.45f, 0.9f), 0.1f), new Vector3(0, 4.65f, 2.15f));          // philtrum ledge
            Part(m.LimestoneShade, Shapes.Box(new Vector3(2.0f, 0.55f, 0.7f), 0.12f), new Vector3(0, 3.85f, 2.2f));          // pursed lips
            Part(m.LimestoneShade, Shapes.Box(new Vector3(0.45f, 3.3f, 1.3f), 0.1f), new Vector3(-2.15f, 7.0f, 0.3f));       // ears
            Part(m.LimestoneShade, Shapes.Box(new Vector3(0.45f, 3.3f, 1.3f), 0.1f), new Vector3(2.15f, 7.0f, 0.3f));
            Part(m.Gold, Shapes.Box(new Vector3(4.2f, 0.62f, 0.4f), 0.08f), new Vector3(0, 9.55f, 1.95f));                 // diadem
            Part(m.Gold, Shapes.Disc(0.62f, 0.2f, 14, 8, 0.32f), new Vector3(0, 9.55f, 2.2f));
            // Sun-ray crown: tapering blades fanned over the head.
            for (int i = 0; i < 9; i++)
            {
                float angle = -64 + i * 16;
                var blade = Shapes.Frustum(new Vector2(0.75f, 0.42f), new Vector2(0.06f, 0.18f), i == 4 ? 4.4f : 3.5f, default, 0.04f);
                Part(m.Gold, blade, new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 1.2f, 10.9f, 0.9f), new Vector3(-12, 0, -angle));
            }
        }

        // Standing sun-king. `height` is from the feet (at the origin) to the crown.
        public static void Colossus(Composer c, KitMaterials m, Matrix4x4 at, float height, ColossusPose pose, int seed)
        {
            float k = height / 48f;
            var s = at * Matrix4x4.Scale(Vector3.one * k);
            void Part(Material material, MeshBuilder part, Vector3 p, Vector3 euler = default) =>
                c.Add(material, part, s * Matrix4x4.TRS(p, Quaternion.Euler(euler), Vector3.one));
            foreach (float x in new[] { -3.1f, 3.1f })
            {
                Part(m.LimestoneShade, Shapes.Box(new Vector3(4.6f, 1.6f, 6.4f), 0.3f), new Vector3(x, 0.8f, 0.8f));                   // feet
                Part(m.Limestone, Shapes.Frustum(new Vector2(4.0f, 4.6f), new Vector2(3.5f, 4.0f), 16, default, 0.35f), new Vector3(x, 1.6f, 0));
            }
            Part(m.LimestoneShade, Shapes.Frustum(new Vector2(11.2f, 7.2f), new Vector2(10.2f, 6.6f), 7.2f, default, 0.4f), new Vector3(0, 15.4f, 0));  // kilt
            Part(m.Gold, Shapes.Box(new Vector3(10.6f, 0.9f, 7.0f), 0.15f), new Vector3(0, 22.8f, 0));                             // belt
            if (pose == ColossusPose.Broken)
            {
                Part(m.Limestone, Shapes.Frustum(new Vector2(9.8f, 6.1f), new Vector2(10.4f, 6.3f), 4.5f, default, 0.4f), new Vector3(0, 23.2f, 0));
                c.Add(m.Limestone, Ruins.JaggedTop(5.2f, 27.7f, 7, 0.4f, seed), s * Matrix4x4.Scale(new Vector3(1, 1, 0.62f)));
                return;
            }
            Part(m.Limestone, Shapes.Frustum(new Vector2(9.8f, 6.1f), new Vector2(12.8f, 6.8f), 11, default, 0.45f), new Vector3(0, 23.2f, 0));  // torso
            Part(m.LimestoneShade, Shapes.Box(new Vector3(15.2f, 2.8f, 7.2f), 0.6f), new Vector3(0, 35.4f, 0));                     // shoulders
            Part(m.Gold, Shapes.Disc(1.6f, 0.4f, 16, 12, 0.9f), new Vector3(0, 29.5f, 3.5f));                                        // pectoral sun
            Part(m.Limestone, Shapes.Frustum(new Vector2(3.4f, 3.2f), new Vector2(3.1f, 2.9f), 1.8f, default, 0.2f), new Vector3(0, 36.8f, 0));  // neck
            ColossusHead(c, m, s * Matrix4x4.TRS(new Vector3(0, 38.4f, 0.2f), Quaternion.identity, Vector3.one), 0.66f);
            if (pose == ColossusPose.RaisedDisc)
            {
                // Left arm hangs; right arm raises the cracked sun overhead.
                Part(m.Limestone, Shapes.Frustum(new Vector2(3.4f, 3.4f), new Vector2(3.0f, 3.0f), 12.5f, default, 0.3f), new Vector3(-8.2f, 36, 0), new Vector3(0, 0, 176));
                Part(m.Limestone, Shapes.Frustum(new Vector2(3.1f, 3.1f), new Vector2(2.8f, 2.8f), 11, default, 0.3f), new Vector3(-8.6f, 23.6f, 0), new Vector3(-8, 0, 178));
                Part(m.Limestone, Shapes.Frustum(new Vector2(3.4f, 3.4f), new Vector2(3.0f, 3.0f), 11, default, 0.3f), new Vector3(7.8f, 35.6f, 0), new Vector3(0, 0, -28));
                Part(m.Limestone, Shapes.Frustum(new Vector2(3.1f, 3.1f), new Vector2(2.7f, 2.7f), 10, default, 0.3f), new Vector3(12.9f, 45.2f, 0), new Vector3(0, 0, 18));
                c.Add(m.Gold, Shapes.Disc(6.5f, 0.9f, 22, 12, 3.2f, 0.15f, 25, 300), s * Matrix4x4.TRS(new Vector3(9.5f, 58.5f, 0.6f), Quaternion.Euler(0, 0, -12), Vector3.one));
            }
            else
            {
                // Arms folded over the chest holding a staff: the pose of a king lying in state.
                foreach (float x in new[] { -1, 1 })
                {
                    Part(m.Limestone, Shapes.Frustum(new Vector2(3.4f, 3.4f), new Vector2(3.0f, 3.0f), 9.5f, default, 0.3f), new Vector3(x * 7.9f, 36, 0.4f), new Vector3(8, 0, 180 - x * 6));
                    Part(m.Limestone, Shapes.Frustum(new Vector2(3.0f, 3.0f), new Vector2(2.7f, 2.7f), 9.5f, default, 0.3f), new Vector3(x * 8.4f, 27.2f, 2.2f), new Vector3(0, 0, x * 70));
                }
                Part(m.Gold, Shapes.Lathe(new[] { new Vector2(0.7f, 0), new Vector2(0.7f, 26), new Vector2(0, 27.5f) }, 6), new Vector3(0, 12, 4.4f));
                Part(m.Gold, Shapes.Disc(2.4f, 0.4f, 16, 12, 1.4f), new Vector3(0, 41.5f, 4.4f));
            }
        }

        // A stone hand rising from deep water, palm up, cupping a sun disc (local +z = fingers).
        public static void OfferingHand(Composer c, KitMaterials m, Matrix4x4 at, float scale = 1)
        {
            var s = at * Matrix4x4.Scale(Vector3.one * scale);
            void Part(Material material, MeshBuilder part, Vector3 p, Vector3 euler = default) =>
                c.Add(material, part, s * Matrix4x4.TRS(p, Quaternion.Euler(euler), Vector3.one));
            Part(m.Limestone, Shapes.Frustum(new Vector2(5.2f, 4.6f), new Vector2(4.5f, 4.1f), 15, new Vector2(0, 1.4f), 0.35f), new Vector3(0, -8, -1.5f));  // forearm
            Part(m.LimestoneShade, Shapes.Box(new Vector3(6.8f, 2.4f, 7.2f), 0.35f), new Vector3(0, 8.1f, 1.6f));                   // palm
            float[] xs = { -2.5f, -0.85f, 0.85f, 2.5f };
            float[] lengths = { 2.6f, 3.1f, 3.0f, 2.4f };
            for (int i = 0; i < 4; i++)
            {
                Part(m.Limestone, Shapes.Box(new Vector3(1.45f, 1.45f, lengths[i]), 0.2f), new Vector3(xs[i], 8.9f, 5.4f + lengths[i] * 0.35f), new Vector3(-22, 0, 0));
                Part(m.Limestone, Shapes.Box(new Vector3(1.35f, 1.35f, lengths[i] * 0.9f), 0.2f), new Vector3(xs[i], 10.6f, 7.2f + lengths[i] * 0.3f), new Vector3(-62, 0, 0));
            }
            Part(m.Limestone, Shapes.Box(new Vector3(1.6f, 1.6f, 3.2f), 0.2f), new Vector3(4.1f, 9.2f, 1.9f), new Vector3(-18, -38, 0));   // thumb
            Part(m.Limestone, Shapes.Box(new Vector3(1.45f, 1.45f, 2.6f), 0.2f), new Vector3(4.6f, 10.6f, 3.8f), new Vector3(-48, -20, 0));
            Part(m.Gold, Shapes.Disc(3.1f, 0.5f, 22, 12, 1.6f), new Vector3(0, 13.4f, 1.8f), new Vector3(0, 180, 0));
        }

        // Two towering pillars flanking a channel (local x), each crowned with a sun disc
        // facing local -z (into the lagoon); the fallen lintel lies broken to the sides.
        public static void SunGate(Composer c, KitMaterials m, Vector3 center, float yaw, float gap, float height)
        {
            var rotation = Quaternion.Euler(0, yaw, 0);
            foreach (float side in new[] { -1, 1 })
            {
                var p = center + rotation * new Vector3(side * (gap * 0.5f + 3.5f), 0, 0);
                c.Add(m.LimestoneShade, Shapes.Box(new Vector3(9, 4, 9), 0.4f), p + Vector3.up * -1.5f, yaw);
                c.Add(m.Limestone, Shapes.Frustum(new Vector2(6.2f, 6.2f), new Vector2(5.2f, 5.2f), height - 3, default, 0.35f), p + Vector3.up * 0.5f, yaw);
                foreach (float band in new[] { 0.33f, 0.66f })
                    c.Add(m.LimestoneShade, Shapes.Box(new Vector3(6.4f, 0.9f, 6.4f), 0.15f), p + Vector3.up * (0.5f + (height - 3) * band), yaw);
                c.Add(m.LimestoneShade, Shapes.Box(new Vector3(7.8f, 2.2f, 7.8f), 0.3f), p + Vector3.up * (height - 1.4f), yaw);
                Ruins.SunDisc(c, m, p + Vector3.up * (height + 3.4f), rotation * Quaternion.Euler(0, 180, 0), 3.0f, false);
                c.Add(m.Gold, Shapes.Box(new Vector3(0.8f, 2.4f, 0.8f), 0.1f), p + Vector3.up * (height + 1.0f), yaw);
                c.Box(p + Vector3.up * 4, new Vector3(9, 12, 9), yaw);
            }
            // The lintel fell and snapped: one half leans on the west pillar, one lies east.
            var west = center + rotation * new Vector3(-gap * 0.5f + 1.5f, 0.2f, 7);
            c.Add(m.Limestone, Shapes.Box(new Vector3(4.2f, 3.4f, 15), 0.3f), west, rotation * Quaternion.Euler(-14, 16, 6), Vector3.one);
            c.Box(west, new Vector3(5, 6, 15), yaw + 16);
            var east = center + rotation * new Vector3(gap * 0.5f - 0.5f, -0.6f, -8);
            c.Add(m.Limestone, Shapes.Box(new Vector3(4.2f, 3.4f, 13), 0.3f), east, rotation * Quaternion.Euler(8, -22, -10), Vector3.one);
            c.Box(east, new Vector3(5, 6, 13), yaw - 22);
        }

        // A line of arches carrying a ceremonial road; `broken` spans lost their arch.
        public static void Causeway(Composer c, KitMaterials m, Vector3 start, Vector3 end, float span, ICollection<int> broken, int seed, ICollection<int> platforms = null)
        {
            var random = new System.Random(seed);
            var direction = end - start; direction.y = 0;
            float length = direction.magnitude;
            direction /= length;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0, yaw, 0);
            int piers = Mathf.Max(2, Mathf.RoundToInt(length / span) + 1);
            // Pier footprint: `across` the road by `along` it. Open spandrels, aqueduct style:
            // the arch crown carries the deck and the sky shows between the haunches.
            const float along = 7, across = 10, deck = 25.5f, ring = 2.6f;
            float radius = (span - along) * 0.5f, spring = deck - 0.7f - radius - ring;
            for (int i = 0; i < piers; i++)
            {
                var p = start + direction * (i * span);
                bool lost = broken.Contains(i) && broken.Contains(i - 1);
                bool platform = platforms != null && platforms.Contains(i);
                float top = lost ? Mathf.Lerp(6, 16, (float)random.NextDouble()) : deck;
                var size = platform ? new Vector2(22, 22) : new Vector2(across, along);
                c.Add(m.LimestoneShade, Shapes.Box(new Vector3(size.x + 2, 3, size.y + 2), 0.3f), p + Vector3.up * -4.5f, yaw);
                c.Add(m.Limestone, Shapes.Frustum(size, size * 0.96f, top + 3, default, 0.3f), p + Vector3.up * -3, yaw);
                if (lost) c.Add(m.Limestone, Ruins.JaggedTop(Mathf.Min(size.x, size.y) * 0.5f, top, 7, (float)random.NextDouble(), seed + i), Matrix4x4.TRS(p, rotation, Vector3.one));
                c.Box(p + Vector3.up * 3, new Vector3(size.x + 0.5f, 12, size.y + 0.5f), yaw);
                if (i + 1 >= piers) break;
                if (broken.Contains(i))
                {
                    Ruins.Blocks(c, m, p + direction * span * 0.5f + Vector3.down * 5.5f, 9, 6, seed + i * 13);
                    continue;
                }
                var mid = p + direction * (span * 0.5f);
                const int stones = 11;
                for (int k = 0; k < stones; k++)
                {
                    float a0 = Mathf.PI * (stones - k) / stones, a1 = Mathf.PI * (stones - k - 1) / stones, r1 = radius + ring;
                    var outline = new[] {
                        new Vector2(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius), new Vector2(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius),
                        new Vector2(Mathf.Cos(a1) * r1, Mathf.Sin(a1) * r1), new Vector2(Mathf.Cos(a0) * r1, Mathf.Sin(a0) * r1) };
                    // Outline in the arch plane (x along the road, z up), extruded across the road.
                    c.Add(k % 2 == 0 ? m.Limestone : m.LimestoneShade, Shapes.Prism(outline, -across * 0.45f, across * 0.45f, 0.08f, 0.08f, true, true),
                        Matrix4x4.TRS(mid + Vector3.up * spring, Quaternion.Euler(0, yaw - 90, 0) * Quaternion.Euler(-90, 0, 0), Vector3.one));
                }
                c.Add(m.LimestoneShade, Shapes.Box(new Vector3(across + 1, 1.4f, span + 0.2f), 0.25f), mid + Vector3.up * (deck + 0.7f), yaw);
                foreach (float x in new[] { -1, 1 })
                    c.Add(m.Limestone, Shapes.Box(new Vector3(0.7f, 1.2f, span), 0.15f), mid + rotation * new Vector3(x * (across * 0.5f + 0.1f), deck + 2, 0), yaw);
                if (i % 2 == 1)
                    Ruins.SunDisc(c, m, mid + rotation * new Vector3(across * 0.45f + 0.35f, spring + radius + ring * 0.5f, 0), rotation * Quaternion.Euler(0, 90, 0),
                        1.3f, random.NextDouble() < 0.4, random.NextDouble() < 0.5);
            }
        }

        // The Drowned Crown on the horizon: a ring of snapped spires around a split dome.
        public static void Crown(Composer c, KitMaterials m, Vector3 center, float radius, int seed)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2 / 9 + (float)random.NextDouble() * 0.2f;
                var p = center + new Vector3(Mathf.Cos(a) * radius, -12, Mathf.Sin(a) * radius);
                float h = Mathf.Lerp(70, 130, (float)random.NextDouble());
                var lean = Quaternion.Euler(Mathf.Sin(a) * -9, 0, Mathf.Cos(a) * 9);
                c.Add(m.Obsidian, Shapes.Frustum(new Vector2(11, 11), new Vector2(3, 3), h, default, 0.6f), Matrix4x4.TRS(p, lean, Vector3.one));
                if (random.NextDouble() < 0.6)
                    c.Add(m.Gold, Shapes.Disc(4.5f, 0.8f, 16, 12, 2.4f, 0.15f, 30, 250),
                        Matrix4x4.TRS(p + lean * Vector3.up * (h * 0.72f), Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))), Vector3.one));
            }
            c.Add(m.Obsidian, Shapes.Lathe(new[] { new Vector2(52, 0), new Vector2(50, 14), new Vector2(42, 30), new Vector2(28, 42), new Vector2(10, 48), new Vector2(0, 49) }, 12),
                center + Vector3.down * 12);
            c.Add(m.Obsidian, Shapes.Frustum(new Vector2(8, 8), new Vector2(1, 1), 60, default, 0.4f), center + Vector3.up * 36);
        }
    }
}
