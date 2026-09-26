using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PirateGame.Art;
using UnityEngine;

namespace PirateGame.Tests.T12
{
    // The faceted kit must produce closed, outward-facing, flat-shaded solids:
    // holes or inverted facets show up as black or see-through patches in HDRP.
    public sealed class KitMeshTests
    {
        [Test] public void ChamferedBoxIsClosedAndFacesOutward()
        {
            var box = Shapes.Box(new Vector3(4, 2, 3), 0.3f);
            AssertClosed(box);
            AssertConvexOutward(box, Vector3.zero);
        }

        [Test] public void ChamferedPrismIsClosedAndFacesOutward()
        {
            var octagon = Shapes.Ring(8, 3, 3);
            var prism = Shapes.Prism(octagon, 0, 5, chamferTop: 0.4f, chamferBottom: 0.2f, capTop: true, capBottom: true);
            AssertClosed(prism);
            AssertConvexOutward(prism, new Vector3(0, 2.5f, 0));
        }

        [Test] public void ClockwiseOutlinesProduceTheSameOutwardSolid()
        {
            var outline = Shapes.Ring(6, 2, 2).Reverse().ToArray();
            var prism = Shapes.Prism(outline, 0, 1, capTop: true, capBottom: true);
            AssertClosed(prism);
            AssertConvexOutward(prism, new Vector3(0, 0.5f, 0));
        }

        [Test] public void LatheColumnIsClosedAndFacesOutward()
        {
            var column = Shapes.Lathe(new[] { new Vector2(1.2f, 0), new Vector2(1.2f, 0.4f), new Vector2(0.9f, 0.6f), new Vector2(0.9f, 6), new Vector2(0, 6) },
                8, capTop: true, capBottom: true);
            AssertClosed(column);
            // A stepped column is not convex: outward-facing means positive enclosed volume.
            float volume = SignedVolume(column);
            Assert.That(volume, Is.GreaterThan(0));
            Assert.That(volume, Is.EqualTo(Mathf.PI * 0.9f * 0.9f * 5.4f + Mathf.PI * 1.2f * 1.2f * 0.4f).Within(3f), "Roughly the column's volume");
        }

        [Test] public void FrustumIsClosedAndFacesOutward()
        {
            var head = Shapes.Frustum(new Vector2(4, 3), new Vector2(3, 2), 6, new Vector2(0, 0.3f), 0.3f);
            AssertClosed(head);
            AssertConvexOutward(head, new Vector3(0, 3, 0));
        }

        [Test] public void DiscIsClosedAndFacesOutward()
        {
            var disc = Shapes.Disc(2, 0.3f, 16);
            AssertClosed(disc);
            AssertConvexOutward(disc, Vector3.zero);
            var broken = Shapes.Disc(2, 0.3f, 16, startAngle: 30, endAngle: 300);
            AssertClosed(broken);
        }

        [Test] public void HeightfieldDropsDeepCellsAndFacesUp()
        {
            var area = new Rect(-10, -10, 20, 20);
            var field = Shapes.Heightfield(area, 2, (x, z) => x < 0 ? -40 : -3, dropBelow: -30);
            Assert.That(field.TriangleCount, Is.GreaterThan(0));
            Assert.That(field.TriangleCount, Is.LessThan(2 * 10 * 10), "Cells entirely below the drop depth must be omitted");
            var mesh = field.Build("field");
            Assert.That(mesh.normals.All(n => n.y > 0), "Seabed facets face up");
            Object.DestroyImmediate(mesh);
        }

        [Test] public void BuildGivesUnitFlatNormalsAndBounds()
        {
            var builder = Shapes.Box(Vector3.one * 2, 0.2f);
            var mesh = builder.Build("box");
            Assert.AreEqual(builder.TriangleCount * 3, mesh.triangles.Length);
            Assert.Less(mesh.vertexCount, builder.TriangleCount * 3, "Triangles of one planar face share vertices");
            var vertices = mesh.vertices; var normals = mesh.normals; var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var face = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]).normalized;
                for (int k = 0; k < 3; k++)
                {
                    Assert.That(normals[triangles[i + k]].magnitude, Is.EqualTo(1).Within(1e-4f));
                    Assert.That(Vector3.Dot(normals[triangles[i + k]], face), Is.GreaterThan(0.9999f), "Flat shading: vertex normal equals facet normal");
                }
            }
            foreach (var v in vertices) Assert.IsTrue(mesh.bounds.Contains(v) || mesh.bounds.SqrDistance(v) < 1e-8f);
            Object.DestroyImmediate(mesh);
        }

        [Test] public void MirroredAppendKeepsFacesOutward()
        {
            var box = Shapes.Box(Vector3.one, 0.1f);
            var mirrored = new MeshBuilder().Append(box, Matrix4x4.TRS(new Vector3(5, 0, 0), Quaternion.identity, new Vector3(-1, 1, 1)));
            AssertClosed(mirrored);
            AssertConvexOutward(mirrored, new Vector3(5, 0, 0));
        }

        [Test] public void InsetMovesEveryEdgeInward()
        {
            var square = Shapes.Rectangle(4, 4);
            var inset = Shapes.Inset(square, 0.5f);
            foreach (var p in inset) Assert.That(Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)), Is.EqualTo(1.5f).Within(1e-4f));
            var clockwise = Shapes.Inset(square.Reverse().ToArray(), 0.5f);
            foreach (var p in clockwise) Assert.That(Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)), Is.EqualTo(1.5f).Within(1e-4f));
        }

        // Every directed edge must be matched by its reverse exactly once: a closed,
        // consistently wound surface.
        private static void AssertClosed(MeshBuilder builder)
        {
            var edges = new Dictionary<(Vector3Int, Vector3Int), int>();
            var v = builder.Vertices;
            for (int i = 0; i < v.Count; i += 3)
                for (int k = 0; k < 3; k++)
                {
                    var key = (Key(v[i + k]), Key(v[i + (k + 1) % 3]));
                    edges.TryGetValue(key, out int count); edges[key] = count + 1;
                }
            // Fans may split one outline edge into collinear pieces; compare on a vertex basis.
            foreach (var edge in edges)
            {
                Assert.AreEqual(1, edge.Value, "Directed edge used twice (inverted neighbour): " + edge.Key);
                Assert.IsTrue(edges.ContainsKey((edge.Key.Item2, edge.Key.Item1)), "Open edge " + edge.Key);
            }
        }

        private static void AssertConvexOutward(MeshBuilder builder, Vector3 inside)
        {
            var v = builder.Vertices;
            for (int i = 0; i < v.Count; i += 3)
            {
                var normal = Vector3.Cross(v[i + 1] - v[i], v[i + 2] - v[i]);
                var center = (v[i] + v[i + 1] + v[i + 2]) / 3;
                Assert.That(Vector3.Dot(normal, center - inside), Is.GreaterThan(0), "Facet " + i / 3 + " faces inward");
            }
        }

        // Divergence theorem: positive for a closed surface whose facets face outward.
        private static float SignedVolume(MeshBuilder builder)
        {
            var v = builder.Vertices; float sum = 0;
            for (int i = 0; i < v.Count; i += 3) sum += Vector3.Dot(v[i], Vector3.Cross(v[i + 1], v[i + 2]));
            return sum / 6;
        }

        private static Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x * 1000), Mathf.RoundToInt(p.y * 1000), Mathf.RoundToInt(p.z * 1000));
    }
}
