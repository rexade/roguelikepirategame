using System;
using System.Collections.Generic;
using UnityEngine;

namespace PirateGame.Art.Editor
{
    // Seabed height fields: the shallows that let the drowned empire show through
    // the water. Heights combine features by maximum over a deep floor; anything
    // below `Deep` is not built at all, so open water keeps its dark scattering colour.
    public static class Seabed
    {
        public const float Deep = -34;
        public const float Floor = -48;

        public interface IFeature { float Height(float x, float z); }

        // Flat-topped shelf (plateau) with a sloping rim of `edge` metres.
        public sealed class Shelf : IFeature
        {
            public Vector2 center, radii; public float depth = -4.5f, edge = 22;
            public float Height(float x, float z)
            {
                float d = Mathf.Sqrt(Sq((x - center.x) / radii.x) + Sq((z - center.y) / radii.y));
                float outside = (d - 1) * Mathf.Min(radii.x, radii.y);
                return outside <= 0 ? depth : Mathf.Lerp(depth, Floor, Mathf.SmoothStep(0, 1, outside / edge));
            }
        }

        // Atoll rim: a band around an ellipse whose crest height varies with angle
        // (cays above water, reef flats just below, gaps for passages).
        public sealed class Rim : IFeature
        {
            public Vector2 center, radii; public float width = 16;
            public Func<float, float> crest;   // angle (radians, 0 = east, counter-clockwise) -> crest height
            public float Height(float x, float z)
            {
                float dx = (x - center.x) / radii.x, dz = (z - center.y) / radii.y;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                float across = Mathf.Abs(d - 1) * Mathf.Min(radii.x, radii.y);
                if (across > width * 2.2f) return Floor;
                float top = crest(Mathf.Atan2(dz, dx));
                float t = across / width;
                return t <= 1 ? Mathf.Lerp(top, top - 2.5f, t * t) : Mathf.Lerp(top - 2.5f, Floor, Mathf.SmoothStep(0, 1, (t - 1) / 1.2f));
            }
        }

        // A drowned road or reef ridge along a polyline.
        public sealed class Ridge : IFeature
        {
            public Vector2[] points; public float halfWidth = 12, depth = -5, edge = 14;
            public float Height(float x, float z)
            {
                float best = float.MaxValue;
                var p = new Vector2(x, z);
                for (int i = 0; i + 1 < points.Length; i++) best = Mathf.Min(best, Distance(p, points[i], points[i + 1]));
                float outside = best - halfWidth;
                return outside <= 0 ? depth : Mathf.Lerp(depth, Floor, Mathf.SmoothStep(0, 1, outside / edge));
            }
        }

        // `swell` adds broad undulation (sand bars and pools about 80 m across) so the
        // shallows show bands of bright and deeper turquoise instead of one flat tint.
        public static Func<float, float, float> Field(IReadOnlyList<IFeature> features, int seed, float roughness = 0.45f, float swell = 1.6f)
        {
            float ox = seed * 17.13f, oz = seed * 7.71f;
            return (x, z) =>
            {
                float h = Floor;
                foreach (var feature in features) h = Mathf.Max(h, feature.Height(x, z));
                if (h > Deep)
                {
                    // Full swell on the flats, fading out on steep rims so cays and channels keep their shape.
                    float flat = Mathf.Clamp01((h - Deep) / 20f) * Mathf.Clamp01((-1.5f - h) / 2.5f);
                    h += (Mathf.PerlinNoise(x * 0.013f + ox * 0.3f, z * 0.013f + oz * 0.3f) - 0.5f) * 2 * swell * flat
                       + (Mathf.PerlinNoise(x * 0.045f + ox, z * 0.045f + oz) - 0.5f) * 2 * roughness
                       + (Mathf.PerlinNoise(x * 0.17f + oz, z * 0.17f + ox) - 0.5f) * roughness * 0.6f;
                }
                return h;
            };
        }

        // Builds the shelf mesh (sand below water, beach and jungle above) and the
        // waterline fence that stops ships where the seabed rises above `ground`.
        public static void Build(Composer c, KitMaterials m, Rect area, float cell, Func<float, float, float> height, int seed, float ground = -1.3f)
        {
            var field = Shapes.Heightfield(area, cell, height, Deep, seed, 0.3f);
            // One sand material: per-facet seagrass read as hard-edged flat stains, so the
            // colour variation comes from depth (the field's swell) instead.
            c.Add(m.Seabed, field.Where((a, b, d) => Mathf.Max(a.y, Mathf.Max(b.y, d.y)) < 0.35f), Matrix4x4.identity);
            c.Add(m.Sand, field.Where((a, b, d) => { float top = Mathf.Max(a.y, Mathf.Max(b.y, d.y)); return top >= 0.35f && Mathf.Min(a.y, Mathf.Min(b.y, d.y)) < 1.1f; }), Matrix4x4.identity);
            c.Add(m.Jungle, field.Where((a, b, d) => Mathf.Max(a.y, Mathf.Max(b.y, d.y)) >= 0.35f && Mathf.Min(a.y, Mathf.Min(b.y, d.y)) >= 1.1f), Matrix4x4.identity);
            foreach (var segment in Contour(area, cell, height, ground))
                c.Solid(Fence(new[] { segment.Item1, segment.Item2 }, -4, 4, false), Matrix4x4.identity);
        }

        // Marching squares at `level` on a regular grid: segments where the seabed crosses it.
        public static List<(Vector2, Vector2)> Contour(Rect area, float cell, Func<float, float, float> height, float level)
        {
            int nx = Mathf.CeilToInt(area.width / cell), nz = Mathf.CeilToInt(area.height / cell);
            var h = new float[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++)
                for (int j = 0; j <= nz; j++) h[i, j] = height(area.xMin + i * cell, area.yMin + j * cell);
            var segments = new List<(Vector2, Vector2)>();
            var crossings = new List<Vector2>(4);
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    crossings.Clear();
                    Vector2 P(int a, int b) => new Vector2(area.xMin + a * cell, area.yMin + b * cell);
                    void Edge(int ai, int aj, int bi, int bj)
                    {
                        float ha = h[ai, aj] - level, hb = h[bi, bj] - level;
                        if ((ha < 0) == (hb < 0)) return;
                        crossings.Add(Vector2.Lerp(P(ai, aj), P(bi, bj), ha / (ha - hb)));
                    }
                    Edge(i, j, i + 1, j); Edge(i + 1, j, i + 1, j + 1); Edge(i + 1, j + 1, i, j + 1); Edge(i, j + 1, i, j);
                    if (crossings.Count >= 2) segments.Add((crossings[0], crossings[1]));
                    if (crossings.Count == 4) segments.Add((crossings[2], crossings[3]));
                }
            return segments;
        }

        // Vertical wall along a polyline (closed when `loop`), both faces, for MeshColliders.
        public static MeshBuilder Fence(IReadOnlyList<Vector2> points, float bottom, float top, bool loop)
        {
            var fence = new MeshBuilder();
            int count = loop ? points.Count : points.Count - 1;
            for (int i = 0; i < count; i++)
            {
                var a = points[i]; var b = points[(i + 1) % points.Count];
                if ((b - a).sqrMagnitude < 1e-6f) continue;
                var a0 = new Vector3(a.x, bottom, a.y); var b0 = new Vector3(b.x, bottom, b.y);
                var a1 = new Vector3(a.x, top, a.y); var b1 = new Vector3(b.x, top, b.y);
                var normal = new Vector3(b.y - a.y, 0, a.x - b.x);
                fence.QuadFacing(a0, b0, b1, a1, normal);
                fence.QuadFacing(a0, b0, b1, a1, -normal);
            }
            return fence;
        }

        private static float Sq(float v) => v * v;

        private static float Distance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
