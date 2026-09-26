using System;
using System.Collections.Generic;
using UnityEngine;

namespace PirateGame.Art
{
    // Faceted solids for the Drowned Sun kit. Outlines are XZ polygons (x east,
    // z north); every generator returns a MeshBuilder so parts can be merged.
    public static class Shapes
    {
        // Connects consecutive rings (same vertex count, stacked bottom to top) with
        // outward quads and optionally caps both ends. Rings may be any orientation.
        public static MeshBuilder Loft(IReadOnlyList<IReadOnlyList<Vector3>> rings, bool capBottom, bool capTop)
        {
            var mesh = new MeshBuilder();
            if (rings.Count == 0) return mesh;
            int count = rings[0].Count;
            bool reverse = SignedArea(rings[0]) < 0;
            Vector3 At(int ring, int index) => rings[ring][reverse ? count - 1 - index : index];
            for (int r = 0; r + 1 < rings.Count; r++)
                for (int s = 0; s < count; s++)
                {
                    int t = (s + 1) % count;
                    // Rings run counter-clockwise seen from above, so a-d-c-b faces outward.
                    mesh.Quad(At(r, s), At(r + 1, s), At(r + 1, t), At(r, t));
                }
            if (capBottom) mesh.Polygon(rings[0], Vector3.down);
            if (capTop) mesh.Polygon(rings[rings.Count - 1], Vector3.up);
            return mesh;
        }

        public static MeshBuilder Prism(IReadOnlyList<Vector2> outline, float bottom, float top, float chamferTop = 0, float chamferBottom = 0,
            bool capTop = true, bool capBottom = false)
        {
            float room = (top - bottom) * 0.45f;
            float ct = Mathf.Clamp(chamferTop, 0, room), cb = Mathf.Clamp(chamferBottom, 0, room);
            var rings = new List<IReadOnlyList<Vector3>>();
            if (cb > 0) rings.Add(Lift(Inset(outline, cb), bottom));
            rings.Add(Lift(outline, bottom + cb));
            rings.Add(Lift(outline, top - ct));
            if (ct > 0) rings.Add(Lift(Inset(outline, ct), top));
            return Loft(rings, capBottom, capTop);
        }

        // Centred box whose twelve edges are cut by `chamfer`.
        public static MeshBuilder Box(Vector3 size, float chamfer = 0, bool capBottom = true) =>
            Prism(Rectangle(size.x, size.z, chamfer), -size.y * 0.5f, size.y * 0.5f, chamfer, chamfer, true, capBottom);

        // Box from y = 0 to `height` whose top is resized and shifted: heads, torsos, noses.
        public static MeshBuilder Frustum(Vector2 bottom, Vector2 top, float height, Vector2 topOffset = default, float chamfer = 0, bool capBottom = true)
        {
            var low = Rectangle(bottom.x, bottom.y, chamfer);
            var high = Rectangle(top.x, top.y, chamfer * Mathf.Min(top.x / Mathf.Max(bottom.x, 0.001f), 1));
            var rings = new List<IReadOnlyList<Vector3>> { Lift(low, 0) };
            var shifted = new Vector3[high.Length];
            for (int i = 0; i < high.Length; i++) shifted[i] = new Vector3(high[i].x + topOffset.x, height, high[i].y + topOffset.y);
            rings.Add(shifted);
            return Loft(rings, capBottom, true);
        }

        // Revolved profile: (radius, y) from bottom to top. Jitter perturbs each ring
        // vertex's radius deterministically (rocks, weathered stone).
        public static MeshBuilder Lathe(IReadOnlyList<Vector2> profile, int sides, float phase = 0, bool capTop = true, bool capBottom = false,
            int seed = 0, float jitter = 0)
        {
            var random = new System.Random(seed);
            var rings = new List<IReadOnlyList<Vector3>>();
            foreach (var p in profile)
            {
                var ring = new Vector3[sides];
                for (int s = 0; s < sides; s++)
                {
                    float a = phase + s * Mathf.PI * 2 / sides;
                    float r = p.x * (1 + jitter * (float)(random.NextDouble() * 2 - 1));
                    ring[s] = new Vector3(Mathf.Cos(a) * r, p.y, Mathf.Sin(a) * r);
                }
                rings.Add(ring);
            }
            return Loft(rings, capBottom, capTop);
        }

        // Upright disc facing +z (front at +thickness/2) with optional triangular rays.
        public static MeshBuilder Disc(float radius, float thickness, int sides, int rays = 0, float rayLength = 0, float rayWidth = 0.18f,
            float startAngle = 0, float endAngle = 360)
        {
            var mesh = new MeshBuilder();
            float half = thickness * 0.5f;
            bool full = endAngle - startAngle >= 359.9f;
            var front = new List<Vector3>(); var back = new List<Vector3>();
            int steps = full ? sides : Mathf.Max(2, Mathf.RoundToInt(sides * (endAngle - startAngle) / 360f));
            for (int i = 0; i < (full ? steps : steps + 1); i++)
            {
                float a = (startAngle + (endAngle - startAngle) * i / steps) * Mathf.Deg2Rad;
                front.Add(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, half));
                back.Add(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, -half));
            }
            for (int i = 0; i < (full ? front.Count : front.Count - 1); i++)
            {
                int j = (i + 1) % front.Count;
                mesh.Facing(new Vector3(0, 0, half), front[i], front[j], Vector3.forward);
                mesh.Facing(new Vector3(0, 0, -half), back[i], back[j], Vector3.back);
                var mid = (front[i] + front[j]) * 0.5f; mid.z = 0;
                mesh.QuadFacing(front[i], front[j], back[j], back[i], mid);
            }
            if (!full)
            {
                // Close the cut faces of a broken disc.
                mesh.QuadFacing(new Vector3(0, 0, half), front[0], back[0], new Vector3(0, 0, -half), Cut(startAngle, -1));
                mesh.QuadFacing(new Vector3(0, 0, half), front[front.Count - 1], back[back.Count - 1], new Vector3(0, 0, -half), Cut(endAngle, 1));
            }
            for (int k = 0; k < rays; k++)
            {
                float a = (startAngle + (k + 0.5f) * 360f / rays) * Mathf.Deg2Rad;
                if (!full && (a * Mathf.Rad2Deg > endAngle || a * Mathf.Rad2Deg < startAngle)) continue;
                float t = half * 0.6f;
                Vector3 P(float angle, float r, float z) => new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, z);
                var l0 = P(a - rayWidth, radius * 0.96f, t); var r0 = P(a + rayWidth, radius * 0.96f, t); var tip0 = P(a, radius + rayLength, t);
                var l1 = P(a - rayWidth, radius * 0.96f, -t); var r1 = P(a + rayWidth, radius * 0.96f, -t); var tip1 = P(a, radius + rayLength, -t);
                mesh.Facing(l0, r0, tip0, Vector3.forward);
                mesh.Facing(l1, r1, tip1, Vector3.back);
                var outward = P(a, 1, 0);
                mesh.QuadFacing(l0, tip0, tip1, l1, outward + P(a - Mathf.PI / 2, 1, 0));
                mesh.QuadFacing(r0, tip0, tip1, r1, outward + P(a + Mathf.PI / 2, 1, 0));
                // Base hidden inside the disc; keeps each ray a closed solid.
                mesh.QuadFacing(l0, r0, r1, l1, -outward);
            }
            return mesh;
        }

        // Terrain grid with slightly jittered vertices; cells whose corners are all
        // below `dropBelow` are omitted (deep water shows no seabed at all).
        public static MeshBuilder Heightfield(Rect area, float cell, Func<float, float, float> height, float dropBelow, int seed = 0, float jitter = 0.3f)
        {
            int nx = Mathf.Max(1, Mathf.CeilToInt(area.width / cell)), nz = Mathf.Max(1, Mathf.CeilToInt(area.height / cell));
            var points = new Vector3[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++)
                for (int j = 0; j <= nz; j++)
                {
                    float x = area.xMin + i * cell, z = area.yMin + j * cell;
                    if (i > 0 && i < nx) x += Hash(i, j, seed) * cell * jitter;
                    if (j > 0 && j < nz) z += Hash(j, i, seed + 7) * cell * jitter;
                    points[i, j] = new Vector3(x, height(x, z), z);
                }
            var mesh = new MeshBuilder();
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    Vector3 p00 = points[i, j], p10 = points[i + 1, j], p11 = points[i + 1, j + 1], p01 = points[i, j + 1];
                    if (p00.y < dropBelow && p10.y < dropBelow && p11.y < dropBelow && p01.y < dropBelow) continue;
                    if (((i + j) & 1) == 0) { mesh.Facing(p00, p01, p11, Vector3.up); mesh.Facing(p00, p11, p10, Vector3.up); }
                    else { mesh.Facing(p00, p01, p10, Vector3.up); mesh.Facing(p10, p01, p11, Vector3.up); }
                }
            return mesh;
        }

        // Irregular closed outline (x, z) around the origin, counter-clockwise from above.
        public static Vector2[] Ring(int sides, float rx, float rz, int seed = 0, float jitter = 0, float phase = 0)
        {
            var random = new System.Random(seed);
            var points = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = phase + i * Mathf.PI * 2 / sides;
                float r = 1 + jitter * (float)(random.NextDouble() * 2 - 1);
                points[i] = new Vector2(Mathf.Cos(a) * rx * r, Mathf.Sin(a) * rz * r);
            }
            return points;
        }

        // Rectangle outline (x by z) with its corners cut by `chamfer`.
        public static Vector2[] Rectangle(float x, float z, float chamfer = 0)
        {
            float hx = x * 0.5f, hz = z * 0.5f, c = Mathf.Clamp(chamfer, 0, Mathf.Min(hx, hz) * 0.9f);
            if (c <= 0) return new[] { new Vector2(-hx, -hz), new Vector2(hx, -hz), new Vector2(hx, hz), new Vector2(-hx, hz) };
            return new[] { new Vector2(-hx + c, -hz), new Vector2(hx - c, -hz), new Vector2(hx, -hz + c), new Vector2(hx, hz - c),
                new Vector2(hx - c, hz), new Vector2(-hx + c, hz), new Vector2(-hx, hz - c), new Vector2(-hx, -hz + c) };
        }

        // Moves each vertex inward along its miter so edges shift by `distance`.
        public static Vector2[] Inset(IReadOnlyList<Vector2> outline, float distance)
        {
            int n = outline.Count;
            float sign = SignedArea(outline) >= 0 ? 1 : -1;
            var result = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                var prev = outline[(i - 1 + n) % n]; var p = outline[i]; var next = outline[(i + 1) % n];
                var inPrev = Inward(p - prev, sign); var inNext = Inward(next - p, sign);
                var miter = (inPrev + inNext).normalized;
                float along = Vector2.Dot(miter, inNext);
                result[i] = p + miter * (distance / Mathf.Max(0.3f, along));
            }
            return result;
        }

        public static Vector2 Centroid(IReadOnlyList<Vector2> outline)
        {
            var sum = Vector2.zero;
            foreach (var p in outline) sum += p;
            return sum / Mathf.Max(1, outline.Count);
        }

        public static Vector3[] Lift(IReadOnlyList<Vector2> outline, float y)
        {
            var ring = new Vector3[outline.Count];
            for (int i = 0; i < outline.Count; i++) ring[i] = new Vector3(outline[i].x, y, outline[i].y);
            return ring;
        }

        // Shoelace area in the XZ plane: positive when counter-clockwise from above.
        public static float SignedArea(IReadOnlyList<Vector2> outline)
        {
            float area = 0;
            for (int i = 0; i < outline.Count; i++)
            {
                var a = outline[i]; var b = outline[(i + 1) % outline.Count];
                area += a.x * b.y - b.x * a.y;
            }
            return area * 0.5f;
        }

        public static float SignedArea(IReadOnlyList<Vector3> ring)
        {
            float area = 0;
            for (int i = 0; i < ring.Count; i++)
            {
                var a = ring[i]; var b = ring[(i + 1) % ring.Count];
                area += a.x * b.z - b.x * a.z;
            }
            return area * 0.5f;
        }

        // Deterministic value in [-1, 1] for grid jitter.
        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(seed * 83492791);
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                return (h & 0xFFFFFF) / (float)0x7FFFFF - 1f;
            }
        }

        private static Vector2 Inward(Vector2 edge, float sign)
        {
            // For a counter-clockwise outline the interior lies to the left of each edge.
            var left = new Vector2(-edge.y, edge.x).normalized;
            return left * sign;
        }

        private static Vector3 Cut(float degrees, float side)
        {
            float a = degrees * Mathf.Deg2Rad + side * Mathf.PI / 2;
            return new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
        }
    }
}
