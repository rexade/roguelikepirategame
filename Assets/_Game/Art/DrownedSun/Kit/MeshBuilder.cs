using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PirateGame.Art
{
    // Accumulates triangles for the Drowned Sun's faceted look. Every triangle owns
    // its three vertices, so built meshes are flat shaded (one normal per facet).
    // Winding follows Unity: clockwise when seen from the front.
    public sealed class MeshBuilder
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();

        public int TriangleCount => vertices.Count / 3;
        public IReadOnlyList<Vector3> Vertices => vertices;

        public MeshBuilder Triangle(Vector3 a, Vector3 b, Vector3 c) => Triangle(a, b, c, Planar(a, b, c, a), Planar(a, b, c, b), Planar(a, b, c, c));

        public MeshBuilder Triangle(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc)
        {
            // Degenerate facets (for example a lathe closing to a point) add nothing.
            if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-12f) return this;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            uvs.Add(ua); uvs.Add(ub); uvs.Add(uc);
            return this;
        }

        // Adds the triangle wound so that its front faces along `outward`.
        public MeshBuilder Facing(Vector3 a, Vector3 b, Vector3 c, Vector3 outward) =>
            Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0 ? Triangle(a, c, b) : Triangle(a, b, c);

        public MeshBuilder Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) => Triangle(a, b, c).Triangle(a, c, d);

        public MeshBuilder Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud) =>
            Triangle(a, b, c, ua, ub, uc).Triangle(a, c, d, ua, uc, ud);

        public MeshBuilder QuadFacing(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward) =>
            Facing(a, b, c, outward).Facing(a, c, d, outward);

        // Convex fan around the ring's centroid, facing `outward`.
        public MeshBuilder Polygon(IReadOnlyList<Vector3> ring, Vector3 outward)
        {
            var center = Vector3.zero;
            foreach (var p in ring) center += p;
            center /= ring.Count;
            for (int i = 0; i < ring.Count; i++) Facing(center, ring[i], ring[(i + 1) % ring.Count], outward);
            return this;
        }

        public MeshBuilder Append(MeshBuilder other, Matrix4x4 transform)
        {
            bool mirrored = transform.determinant < 0;
            for (int i = 0; i < other.vertices.Count; i += 3)
            {
                var a = transform.MultiplyPoint3x4(other.vertices[i]);
                var b = transform.MultiplyPoint3x4(other.vertices[i + 1]);
                var c = transform.MultiplyPoint3x4(other.vertices[i + 2]);
                if (mirrored) Triangle(a, c, b, other.uvs[i], other.uvs[i + 2], other.uvs[i + 1]);
                else Triangle(a, b, c, other.uvs[i], other.uvs[i + 1], other.uvs[i + 2]);
            }
            return this;
        }

        public MeshBuilder Append(MeshBuilder other, Vector3 position, Quaternion rotation, Vector3 scale) =>
            Append(other, Matrix4x4.TRS(position, rotation, scale));

        public MeshBuilder Append(MeshBuilder other, Vector3 position) => Append(other, Matrix4x4.Translate(position));

        // Keeps only the triangles the predicate accepts (for example collision above the waterline).
        public MeshBuilder Where(Func<Vector3, Vector3, Vector3, bool> keep)
        {
            var result = new MeshBuilder();
            for (int i = 0; i < vertices.Count; i += 3)
                if (keep(vertices[i], vertices[i + 1], vertices[i + 2]))
                    result.Triangle(vertices[i], vertices[i + 1], vertices[i + 2], uvs[i], uvs[i + 1], uvs[i + 2]);
            return result;
        }

        public Bounds Bounds()
        {
            if (vertices.Count == 0) return default;
            var bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (var v in vertices) bounds.Encapsulate(v);
            return bounds;
        }

        // Flat normals per facet; vertices are shared only where position, normal and
        // UV all match (the triangles of one planar face), which keeps stored meshes small.
        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            var positions = new List<Vector3>(vertices.Count);
            var normals = new List<Vector3>(vertices.Count);
            var coordinates = new List<Vector2>(vertices.Count);
            var indices = new int[vertices.Count];
            var shared = new Dictionary<(Vector3Int, Vector3Int, Vector2Int), int>();
            for (int i = 0; i < vertices.Count; i += 3)
            {
                var n = Vector3.Cross(vertices[i + 1] - vertices[i], vertices[i + 2] - vertices[i]).normalized;
                for (int k = 0; k < 3; k++)
                {
                    var p = vertices[i + k]; var uv = uvs[i + k];
                    var key = (Quantize(p, 1e4f), Quantize(n, 1e4f), new Vector2Int(Mathf.RoundToInt(uv.x * 1e4f), Mathf.RoundToInt(uv.y * 1e4f)));
                    if (!shared.TryGetValue(key, out int index))
                    {
                        index = positions.Count; shared[key] = index;
                        positions.Add(p); normals.Add(n); coordinates.Add(uv);
                    }
                    indices[i + k] = index;
                }
            }
            if (positions.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, coordinates);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3Int Quantize(Vector3 v, float scale) =>
            new Vector3Int(Mathf.RoundToInt(v.x * scale), Mathf.RoundToInt(v.y * scale), Mathf.RoundToInt(v.z * scale));

        // Box projection along the facet's dominant axis; world metres / 4 per tile.
        private static Vector2 Planar(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            var n = Vector3.Cross(b - a, c - a);
            float x = Mathf.Abs(n.x), y = Mathf.Abs(n.y), z = Mathf.Abs(n.z);
            var uv = y >= x && y >= z ? new Vector2(p.x, p.z) : x >= z ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
            return uv * 0.25f;
        }
    }
}
