using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PirateGame.Art;
using UnityEngine;

namespace PirateGame.Tests.T12
{
    public sealed class ArtKitTests
    {
        [Test]
        public void EveryFacetKeepsItsOwnFlatNormal()
        {
            var mesh = Shapes.Box(new Vector3(2, 1, 3), 0.2f).Build("box");
            var vertices = mesh.vertices; var normals = mesh.normals; var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var face = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]).normalized;
                for (int k = 0; k < 3; k++)
                    Assert.That(Vector3.Distance(normals[triangles[i + k]], face), Is.LessThan(1e-4f), "smoothed normal at triangle " + i / 3);
            }
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void ChamferedBoxAndCappedPrismAreClosedAndFaceOutward()
        {
            foreach (var shape in new[] {
                Shapes.Box(new Vector3(2, 1, 3), 0.2f),
                Shapes.Prism(Shapes.Ring(8, 3, 2), 0, 4, 0.3f, 0.2f, true, true),
                Shapes.Lathe(new[] { new Vector2(1, 0), new Vector2(1.2f, 1), new Vector2(0.6f, 2) }, 7, capTop: true, capBottom: true, seed: 3, jitter: 0.2f) })
            {
                AssertClosed(shape);
                AssertOutward(shape);
            }
        }

        [Test]
        public void ClockwiseOutlinesProduceTheSameOutwardSolid()
        {
            var ring = Shapes.Ring(6, 2, 2);
            var reversed = ring.Reverse().ToArray();
            AssertOutward(Shapes.Prism(reversed, 0, 2, 0.2f, 0, true, true));
        }

        [Test]
        public void DiscWithRaysFacesFrontAndBack()
        {
            var disc = Shapes.Disc(2, 0.3f, 16, 12, 0.8f);
            var mesh = disc.Build("disc");
            Assert.That(mesh.normals.Any(n => n.z > 0.99f) && mesh.normals.Any(n => n.z < -0.99f));
            Assert.That(mesh.vertices.Max(p => new Vector2(p.x, p.y).magnitude), Is.EqualTo(2.8f).Within(0.01f), "ray tips reach radius + ray length");
            AssertClosed(disc);
            Object.DestroyImmediate(mesh);
            var broken = Shapes.Disc(2, 0.3f, 16, 12, 0.8f, startAngle: 30, endAngle: 250);
            AssertClosed(broken);
        }

        [Test]
        public void HeightfieldDropsDeepCellsAndFacesUp()
        {
            var area = new Rect(0, 0, 40, 40);
            var field = Shapes.Heightfield(area, 4, (x, z) => x < 20 ? -3 : -40, -25, seed: 1, jitter: 0);
            Assert.That(field.TriangleCount, Is.EqualTo(2 * 5 * 10), "four shallow columns plus the slope column remain");
            var mesh = field.Build("field");
            Assert.That(mesh.normals.All(n => n.y > 0));
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void MirroredAppendKeepsFacetsOutward()
        {
            var part = Shapes.Box(Vector3.one, 0.1f);
            var mirrored = new MeshBuilder().Append(part, Matrix4x4.TRS(new Vector3(5, 0, 0), Quaternion.identity, new Vector3(-1, 1, 1)));
            AssertOutward(mirrored);
            var kept = mirrored.Where((a, b, c) => a.y > 0 && b.y > 0 && c.y > 0);
            Assert.That(kept.TriangleCount, Is.GreaterThan(0).And.LessThan(mirrored.TriangleCount));
        }

        [Test]
        public void BuildIsFiniteAndBoundsContainEveryVertex()
        {
            var mesh = new MeshBuilder().Append(Shapes.Frustum(new Vector2(4, 3), new Vector2(2, 1.5f), 5, new Vector2(0.5f, 0), 0.3f), Vector3.up)
                .Build("frustum");
            Assert.That(mesh.vertices.All(v => mesh.bounds.Contains(v) || (mesh.bounds.ClosestPoint(v) - v).sqrMagnitude < 1e-6f));
            Assert.That(mesh.normals.All(n => Mathf.Abs(n.magnitude - 1) < 1e-3f));
            Object.DestroyImmediate(mesh);
        }

        // Every undirected edge (by rounded position) is used by exactly two triangles.
        private static void AssertClosed(MeshBuilder shape)
        {
            var edges = new Dictionary<(Vector3Int, Vector3Int), int>();
            var v = shape.Vertices;
            for (int i = 0; i < v.Count; i += 3)
                for (int k = 0; k < 3; k++)
                {
                    var a = Key(v[i + k]); var b = Key(v[i + (k + 1) % 3]);
                    var edge = Less(a, b) ? (a, b) : (b, a);
                    edges[edge] = edges.TryGetValue(edge, out var n) ? n + 1 : 1;
                }
            var open = edges.Where(e => e.Value != 2).ToArray();
            Assert.That(open, Is.Empty, "open edges: " + string.Join(", ", open.Take(5).Select(e => e.Key + "x" + e.Value)));
        }

        // Each facet's normal points away from the solid's centre (true for the convex-ish kit parts).
        private static void AssertOutward(MeshBuilder shape)
        {
            var center = shape.Bounds().center;
            var v = shape.Vertices;
            for (int i = 0; i < v.Count; i += 3)
            {
                var n = Vector3.Cross(v[i + 1] - v[i], v[i + 2] - v[i]);
                var mid = (v[i] + v[i + 1] + v[i + 2]) / 3;
                Assert.That(Vector3.Dot(n, mid - center), Is.GreaterThan(-1e-5f), "inward facet " + i / 3);
            }
        }

        private static Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x * 1000), Mathf.RoundToInt(p.y * 1000), Mathf.RoundToInt(p.z * 1000));
        private static bool Less(Vector3Int a, Vector3Int b) => a.x != b.x ? a.x < b.x : a.y != b.y ? a.y < b.y : a.z < b.z;
    }
}
