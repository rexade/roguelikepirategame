using System.Collections.Generic;
using PirateGame.Presentation.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace PirateGame.Art.Editor
{
    // Salvage visuals, wrecks and ship dressing. Salvage roots sit at the waterline
    // (y = 0); relics rest on a pedestal whose top is at y = 0.35.
    public static class Vessels
    {
        public static void RelicVisual(Transform root, KitMaterials m)
        {
            var c = new Composer();
            c.Add(m.Gold, Shapes.Box(new Vector3(0.66f, 0.3f, 0.44f), 0.05f), new Vector3(0, 0.5f, 0), 15);
            c.Add(m.Gold, Shapes.Disc(0.44f, 0.11f, 14, 10, 0.28f), new Vector3(0, 1.14f, 0), Quaternion.Euler(0, 15, 0), Vector3.one);
            c.Add(m.Gold, Shapes.Box(new Vector3(0.4f, 0.14f, 0.19f), 0.03f), new Vector3(0.55f, 0.42f, 0.28f), 28);
            c.Add(m.Gold, Shapes.Box(new Vector3(0.4f, 0.14f, 0.19f), 0.03f), new Vector3(0.5f, 0.42f, -0.26f), -12);
            c.Add(m.Gold, Shapes.Box(new Vector3(0.4f, 0.14f, 0.19f), 0.03f), new Vector3(0.53f, 0.56f, 0.02f), 60);
            var amphora = Shapes.Lathe(new[] { new Vector2(0.06f, 0), new Vector2(0.19f, 0.1f), new Vector2(0.25f, 0.36f), new Vector2(0.13f, 0.62f),
                new Vector2(0.09f, 0.72f), new Vector2(0.14f, 0.78f), new Vector2(0, 0.8f) }, 8, 0, true, true);
            c.Add(m.Terracotta, amphora, new Vector3(-0.42f, 0.6f, 0.05f), Quaternion.Euler(0, 30, 82), Vector3.one);
            // Scaled up so the gold reads at gameplay distance through the water's glare.
            var relic = c.Emit(root, "Relic", "Relic").transform;
            relic.localScale = Vector3.one * 1.35f;
            relic.localPosition = new Vector3(0, 0.35f - 0.35f * 1.35f, 0);   // still resting on the pedestal top
            var glint = KitAssets.Part(root, "Glint", KitAssets.Mesh("Glint star", Star(0.5f, 0.09f)), m.Glint, false);
            glint.transform.localPosition = new Vector3(0, 2.5f, 0);
            glint.transform.localScale = Vector3.one * 1.3f;
            glint.AddComponent<Glint>();
        }

        public static void DebrisVisual(Transform root, KitMaterials m)
        {
            var c = new Composer();
            c.Add(m.Wood, Shapes.Box(Vector3.one * 0.9f, 0.06f), new Vector3(0.4f, 0.12f, 0.3f), Quaternion.Euler(8, 25, -6), Vector3.one);
            c.Add(m.Wood, Shapes.Box(Vector3.one * 0.7f, 0.05f), new Vector3(-0.7f, 0.08f, 0.9f), Quaternion.Euler(-10, -40, 12), Vector3.one);
            c.Add(m.WoodDark, Shapes.Lathe(new[] { new Vector2(0.32f, 0), new Vector2(0.4f, 0.45f), new Vector2(0.32f, 0.9f) }, 8, 0, true, true),
                new Vector3(-0.3f, 0.05f, -0.8f), Quaternion.Euler(90, 60, 0), Vector3.one);
            for (int i = 0; i < 3; i++)
                c.Add(m.WoodDark, Shapes.Box(new Vector3(0.3f, 0.08f, 2.3f), 0.02f), new Vector3(1.1f - i * 0.5f, 0.02f, -0.4f + i * 0.3f), Quaternion.Euler(0, 70 + i * 23, 3), Vector3.one);
            c.Add(m.Wood, Shapes.Lathe(new[] { new Vector2(0.17f, 0), new Vector2(0.15f, 3.2f) }, 6, 0, true, true), new Vector3(-1.2f, 0.05f, 0.1f), Quaternion.Euler(90, -20, 0), Vector3.one);
            c.Add(m.Canvas, Shapes.Box(new Vector3(1.5f, 0.04f, 1.1f), 0), new Vector3(0.9f, 0.04f, 1.1f), Quaternion.Euler(4, 10, -3), Vector3.one);
            c.Emit(root, "Debris", "Debris");
        }

        // A broken hull lying tilted on a reef, bow snapped off; static set dressing.
        public static void WreckHull(Composer c, KitMaterials m, Vector3 at, float yaw, int seed, float length = 11)
        {
            var random = new System.Random(seed);
            var rings = new List<IReadOnlyList<Vector3>>();
            const int stations = 7;
            for (int k = 0; k <= stations; k++)
            {
                float t = (float)k / stations * 0.64f;
                float w = 2.1f * (0.8f + 0.2f * Mathf.Sin(Mathf.PI * t)), y = t * length;
                float jag = k == stations ? 1 : 0;
                var ring = new[] { new Vector2(-w, 1.2f), new Vector2(-w * 0.96f, 0.2f), new Vector2(-w * 0.55f, -1.3f), new Vector2(0, -1.6f),
                    new Vector2(w * 0.55f, -1.3f), new Vector2(w * 0.96f, 0.2f), new Vector2(w, 1.2f) };
                var points = new Vector3[ring.Length];
                for (int i = 0; i < ring.Length; i++) points[i] = new Vector3(ring[i].x, y + jag * ((float)random.NextDouble() - 0.3f) * 1.6f, ring[i].y);
                rings.Add(points);
            }
            var hull = Shapes.Loft(rings, true, true);
            var pose = Matrix4x4.TRS(at, Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(-9, 0, 24) * Quaternion.Euler(-90, 0, 0), Vector3.one);
            c.Add(m.WoodDark, hull, pose);
            var deck = pose * Matrix4x4.Translate(new Vector3(0, length * 0.3f, 1.25f));
            c.Add(m.Wood, Shapes.Lathe(new[] { new Vector2(0.25f, 0), new Vector2(0.22f, 3.4f) }, 6, 0, true, false),
                deck * Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0)));
            c.Add(m.Wood, Shapes.Lathe(new[] { new Vector2(0.2f, 0), new Vector2(0.17f, 7.5f) }, 6, 0, true, true),
                pose * Matrix4x4.TRS(new Vector3(2.4f, length * 0.25f, 1.6f), Quaternion.Euler(20, 0, -70), Vector3.one));
            for (int i = 0; i < 4; i++)
                c.Add(m.Wood, Shapes.Box(new Vector3(0.28f, 0.07f, 2.6f), 0.02f),
                    new Vector3(at.x, 0.05f, at.z) + Quaternion.Euler(0, yaw + 40 + i * 35, 0) * Vector3.forward * (4.5f + i),
                    Quaternion.Euler(2, yaw + i * 47, 0), Vector3.one);
            c.Box(at + Vector3.up * 0.5f, new Vector3(5, 5, length * 0.7f), yaw);
        }

        // Gold trim, bow sun and sail/hull materials for a T02/T04 cutter instance.
        public static void Dress(GameObject ship, KitMaterials m, Material sail, bool player)
        {
            foreach (var renderer in ship.GetComponentsInChildren<Renderer>(true))
            {
                switch (renderer.name)
                {
                    case "Faceted hull": renderer.sharedMaterial = player ? m.Hull : m.WoodDark; break;
                    case "Raised deck": case "Mast": case "Cross spar": renderer.sharedMaterial = m.Wood; break;
                    case "Square sail": renderer.sharedMaterial = sail; break;
                    case "Pennant": renderer.sharedMaterial = player ? m.Gold : m.Iron; break;
                    case "Cabin": renderer.sharedMaterial = player ? m.Wood : m.WoodDark; break;
                    case "Dummy cannon": renderer.sharedMaterial = m.Iron; break;
                }
            }
            foreach (var old in new[] { "Gold trim", "Bow sun" })
            {
                var existing = ship.transform.Find(old);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
            }
            Vector2[] outline = { new Vector2(-0.75f, -2.4f), new Vector2(0.75f, -2.4f), new Vector2(1, -0.8f), new Vector2(0.9f, 1.2f), new Vector2(0, 2.6f),
                new Vector2(-0.9f, 1.2f), new Vector2(-1, -0.8f) };
            var band = new Vector2[outline.Length];
            for (int i = 0; i < band.Length; i++) band[i] = outline[i] * 1.04f;
            var trim = KitAssets.Part(ship.transform, "Gold trim", KitAssets.Mesh("Ship trim", Shapes.Prism(band, 0.64f, 0.84f, 0, 0, false, false)),
                player ? m.Gold : m.Iron, false);
            trim.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            if (player)
            {
                var sun = KitAssets.Part(ship.transform, "Bow sun", KitAssets.Mesh("Bow sun", Shapes.Disc(0.3f, 0.08f, 12, 8, 0.2f)), m.Gold, false);
                sun.transform.localPosition = new Vector3(0, 0.95f, 2.62f);
                sun.transform.localRotation = Quaternion.Euler(-20, 0, 0);
            }
        }

        public enum Style { SunCutter, Wrecker, Gunboat, Corsair }

        // Faceted ship model (about 5.5 m bow to stern, bow toward +z, waterline at y = 0)
        // under `parent`: lofted hull with a flush deck, stern castle, mast and yard, a
        // bellied square sail carrying its emblem, bowsprit, figurehead and cannons.
        // Returns the hull mesh (for a WaterExcluder). Gameplay transforms are untouched.
        public static Mesh ShipModel(Transform parent, KitMaterials m, Style style)
        {
            bool player = style == Style.SunCutter;
            var hullMaterial = player ? m.Hull : style == Style.Corsair ? m.Iron : m.WoodDark;
            var trim = player ? m.Gold : style == Style.Corsair ? m.Gold : m.Iron;
            var sail = style == Style.SunCutter ? m.SunSail : style == Style.Wrecker ? m.WreckerSail : style == Style.Gunboat ? m.GunboatSail : m.CorsairSail;
            string key = "Ship-" + style;
            var c = new Composer();

            // Stations from stern to bow: z, half beam, keel depth, sheer height.
            float[,] stations = { { -2.5f, 0.74f, -0.5f, 1.08f }, { -1.7f, 0.96f, -0.66f, 0.92f }, { -0.5f, 1.03f, -0.72f, 0.84f },
                { 0.7f, 0.97f, -0.7f, 0.85f }, { 1.7f, 0.74f, -0.6f, 0.95f }, { 2.4f, 0.36f, -0.42f, 1.08f }, { 2.75f, 0.05f, -0.22f, 1.2f } };
            var rings = new List<IReadOnlyList<Vector3>>();
            for (int i = 0; i < stations.GetLength(0); i++)
            {
                float z = stations[i, 0], w = stations[i, 1], keel = stations[i, 2], sheer = stations[i, 3];
                var section = new[] { new Vector2(-w, sheer), new Vector2(-w * 1.02f, 0.35f), new Vector2(-w * 0.8f, -0.2f), new Vector2(-w * 0.35f, keel * 0.85f),
                    new Vector2(0, keel), new Vector2(w * 0.35f, keel * 0.85f), new Vector2(w * 0.8f, -0.2f), new Vector2(w * 1.02f, 0.35f), new Vector2(w, sheer) };
                // Loft space: (x, station, -y), rotated so the station axis runs along +z.
                var ring = new Vector3[section.Length];
                for (int k = 0; k < section.Length; k++) ring[k] = new Vector3(section[k].x, z, -section[k].y);
                rings.Add(ring);
            }
            var hull = new MeshBuilder().Append(Shapes.Loft(rings, true, true), Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0)));
            bool Deck(Vector3 a, Vector3 b, Vector3 d) => Vector3.Cross(b - a, d - a).normalized.y > 0.8f;
            c.Add(hullMaterial, hull.Where((a, b, d) => !Deck(a, b, d)), Matrix4x4.identity);
            c.Add(m.Wood, hull.Where(Deck), Matrix4x4.identity);
            // Gilded (or iron) band along the sheer line, following the hull outline.
            var band = new Vector2[stations.GetLength(0) * 2];
            for (int i = 0; i < stations.GetLength(0); i++)
            {
                band[i] = new Vector2(stations[i, 1] + 0.05f, stations[i, 0]);
                band[band.Length - 1 - i] = new Vector2(-stations[i, 1] - 0.05f, stations[i, 0]);
            }
            c.Add(trim, Shapes.Prism(band, 0.62f, 0.74f, 0, 0, false, false), Matrix4x4.identity);
            // Stern castle with a gilded rail.
            c.Add(hullMaterial, Shapes.Box(new Vector3(1.5f, 0.62f, 1.05f), 0.06f), new Vector3(0, 1.22f, -1.95f));
            c.Add(m.Wood, Shapes.Box(new Vector3(1.56f, 0.06f, 1.1f), 0.02f), new Vector3(0, 1.55f, -1.95f));
            foreach (float x in new[] { -0.8f, 0.8f })
                c.Add(trim, Shapes.Box(new Vector3(0.07f, 0.12f, 1.14f), 0.01f), new Vector3(x, 1.6f, -1.95f));
            c.Add(trim, Shapes.Box(new Vector3(1.64f, 0.12f, 0.07f), 0.01f), new Vector3(0, 1.6f, -2.5f));
            c.Add(m.WoodDark, Shapes.Box(new Vector3(0.9f, 0.22f, 0.04f), 0.02f), new Vector3(0, 1.2f, -2.49f));
            // Mast, yard and the bellied sail.
            c.Add(m.Wood, Shapes.Lathe(new[] { new Vector2(0.11f, 0), new Vector2(0.08f, 3.55f) }, 7, 0, true), new Vector3(0, 0.8f, 0.15f));
            c.Add(m.Wood, Shapes.Lathe(new[] { new Vector2(0.06f, 0), new Vector2(0.05f, 2.9f) }, 6, 0, true, true),
                Matrix4x4.TRS(new Vector3(-1.45f, 3.82f, 0.28f), Quaternion.Euler(0, 0, -90), Vector3.one));
            c.Add(sail, SailMesh(2.6f, 2.1f, 0.34f), new Vector3(0, 1.7f, 0.3f));
            c.Add(m.Wood, Shapes.Lathe(new[] { new Vector2(0.065f, 0), new Vector2(0.04f, 1.5f) }, 6, 0, true, true),
                Matrix4x4.TRS(new Vector3(0, 1.02f, 2.6f), Quaternion.Euler(64, 0, 0), Vector3.one));
            // Pennant at the masthead.
            var pennant = new MeshBuilder();
            pennant.Facing(new Vector3(0, 0, 0), new Vector3(0, -0.34f, 0), new Vector3(0, -0.17f, -0.85f), Vector3.right);
            pennant.Facing(new Vector3(0, 0, 0), new Vector3(0, -0.34f, 0), new Vector3(0, -0.17f, -0.85f), Vector3.left);
            c.Add(player ? m.Gold : m.Iron, pennant, new Vector3(0, 4.35f, 0.12f));
            if (player)
                c.Add(m.Gold, Shapes.Disc(0.28f, 0.07f, 12, 8, 0.2f), Matrix4x4.TRS(new Vector3(0, 1.18f, 2.78f), Quaternion.Euler(-24, 0, 0), Vector3.one));
            else
                c.Add(m.Iron, Shapes.Disc(0.26f, 0.08f, 12), Matrix4x4.TRS(new Vector3(0, 1.15f, 2.76f), Quaternion.Euler(-24, 0, 0), Vector3.one));
            // Two guns a side.
            foreach (float side in new[] { -1f, 1f })
                foreach (float z in new[] { 0.45f, -0.75f })
                    c.Add(m.Iron, Shapes.Lathe(new[] { new Vector2(0.15f, 0), new Vector2(0.11f, 0.32f), new Vector2(0.09f, 0.72f), new Vector2(0.11f, 0.78f) }, 7, 0, true),
                        Matrix4x4.TRS(new Vector3(side * 0.62f, 0.98f, z), Quaternion.Euler(0, 0, -side * 90), Vector3.one));
            var model = c.Emit(parent, "Model", key);
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity;
            return KitAssets.Mesh(key + "-exclusion", hull);
        }

        // Square sail as a bellied grid (both faces), UV 0..1 across its width and height;
        // local origin at the foot centre, belly bulging toward +z.
        public static MeshBuilder SailMesh(float width, float height, float belly)
        {
            var sail = new MeshBuilder();
            const int nx = 6, ny = 4;
            Vector3 P(int i, int j)
            {
                float u = (float)i / nx, v = (float)j / ny;
                return new Vector3((u - 0.5f) * width * (1 - 0.08f * (1 - v)), v * height, belly * Mathf.Sin(Mathf.PI * u) * Mathf.Sin(Mathf.PI * (0.2f + 0.7f * v)));
            }
            Vector2 U(int i, int j) => new Vector2((float)i / nx, (float)j / ny);
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < ny; j++)
                {
                    sail.Quad(P(i, j), P(i, j + 1), P(i + 1, j + 1), P(i + 1, j), U(i, j), U(i, j + 1), U(i + 1, j + 1), U(i + 1, j));
                    sail.Quad(P(i, j), P(i + 1, j), P(i + 1, j + 1), P(i, j + 1), U(i, j), U(i + 1, j), U(i + 1, j + 1), U(i, j + 1));
                }
            return sail;
        }

        // Four-point star, flat, both faces (the relic glint).
        public static MeshBuilder Star(float radius, float waist)
        {
            var star = new MeshBuilder();
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2, b = a + Mathf.PI / 4, c = a - Mathf.PI / 4;
                var tip = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0);
                var left = new Vector3(Mathf.Cos(b) * waist, Mathf.Sin(b) * waist, 0);
                var right = new Vector3(Mathf.Cos(c) * waist, Mathf.Sin(c) * waist, 0);
                star.Facing(Vector3.zero, right, tip, Vector3.forward).Facing(Vector3.zero, tip, left, Vector3.forward);
                star.Facing(Vector3.zero, right, tip, Vector3.back).Facing(Vector3.zero, tip, left, Vector3.back);
            }
            // A second, turned plane so the glint reads from every side.
            return new MeshBuilder().Append(star, Matrix4x4.identity).Append(star, Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0)));
        }
    }
}
