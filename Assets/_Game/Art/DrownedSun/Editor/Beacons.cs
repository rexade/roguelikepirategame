using PirateGame.Presentation.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace PirateGame.Art.Editor
{
    // Sun beacons: the bonfires of the drowned empire. Stone and gold are merged
    // meshes; the fire, light pillar and light are separate so BeaconView can relight them.
    public static class Beacons
    {
        public static GameObject Beacon(Transform parent, KitMaterials m, Vector3 at, float yaw, string hubId, string key, float pillarHeight = 260)
        {
            var root = new GameObject("Beacon " + hubId);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var local = Matrix4x4.TRS(at, Quaternion.Euler(0, yaw, 0), Vector3.one);
            var c = new Composer { Stain = m.StainOf };
            void Add(Material material, MeshBuilder part, Vector3 p, Quaternion r) => c.Add(material, part, local * Matrix4x4.TRS(p, r, Vector3.one));
            Add(m.Limestone, Shapes.Lathe(new[] { new Vector2(6.4f, -3.5f), new Vector2(6.4f, 1.0f), new Vector2(5.3f, 1.0f), new Vector2(5.3f, 2.0f),
                new Vector2(4.3f, 2.0f), new Vector2(4.3f, 2.8f), new Vector2(0, 2.8f) }, 8, 0.39f), Vector3.zero, Quaternion.identity);
            Add(m.Limestone, Shapes.Lathe(new[] { new Vector2(2.0f, 2.8f), new Vector2(2.0f, 3.3f), new Vector2(1.65f, 3.6f), new Vector2(1.5f, 12.4f),
                new Vector2(2.0f, 12.8f), new Vector2(2.0f, 13.3f), new Vector2(0, 13.3f) }, 8, 0.39f), Vector3.zero, Quaternion.identity);
            Add(m.Brazier, Shapes.Lathe(new[] { new Vector2(0.9f, 13.3f), new Vector2(1.4f, 13.6f), new Vector2(2.5f, 14.6f), new Vector2(2.8f, 15.0f),
                new Vector2(2.3f, 15.05f), new Vector2(0, 14.75f) }, 10, 0.2f), Vector3.zero, Quaternion.identity);
            Add(m.Gold, Shapes.Box(new Vector3(0.5f, 3.4f, 0.5f), 0.06f), new Vector3(0, 16.6f, -1.5f), Quaternion.identity);
            Add(m.Gold, Shapes.Disc(2.3f, 0.32f, 20, 12, 1.15f), new Vector3(0, 18.2f, -1.55f), Quaternion.identity);
            for (int i = 0; i < 4; i++)
            {
                float a = (45 + i * 90) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(a) * 4.7f, 2.0f, Mathf.Sin(a) * 4.7f);
                Add(m.LimestoneShade, Shapes.Frustum(new Vector2(0.8f, 0.8f), new Vector2(0.35f, 0.35f), 2.4f, default, 0.06f), p, Quaternion.identity);
                Add(m.Gold, Shapes.Frustum(new Vector2(0.35f, 0.35f), new Vector2(0.02f, 0.02f), 0.5f, default, 0, false), p + Vector3.up * 2.4f, Quaternion.identity);
            }
            c.Emit(root.transform, "Stone", key);
            root.transform.Find("Stone").SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Cold brazier: ash and charcoal heaped in the bowl.
            var dark = new Composer();
            var random = new System.Random(key.GetHashCode());
            for (int i = 0; i < 7; i++)
            {
                float a = (float)random.NextDouble() * Mathf.PI * 2, d = (float)random.NextDouble() * 1.5f;
                Nature.Rock(dark, m.Ash, local.MultiplyPoint3x4(new Vector3(Mathf.Cos(a) * d, 14.7f, Mathf.Sin(a) * d)), Mathf.Lerp(0.4f, 0.8f, (float)random.NextDouble()), i + 3);
            }
            var darkParts = dark.Emit(root.transform, "Cold brazier", key + "-ash");
            darkParts.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            var fire = new GameObject("Fire");
            fire.transform.SetParent(root.transform, false);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2 / 6 + 0.3f, d = i == 0 ? 0 : 0.9f;
                float h = i == 0 ? 3.2f : Mathf.Lerp(1.5f, 2.4f, (float)random.NextDouble());
                var flame = KitAssets.Part(fire.transform, "Flame " + i,
                    KitAssets.Mesh("Flame " + (i == 0 ? "tall" : "low"), Shapes.Lathe(new[] { new Vector2(0.62f, 0), new Vector2(0.45f, h * 0.45f), new Vector2(0, h) }, 5, i, true, false, i + 11, 0.18f)),
                    i == 0 ? m.FlameCore : m.Flame, false);
                flame.transform.localPosition = new Vector3(Mathf.Cos(a) * d, 14.65f, Mathf.Sin(a) * d);
                flame.transform.localRotation = Quaternion.Euler(0, i * 37, 0);
                var flicker = flame.AddComponent<Flicker>(); flicker.amount = 0.22f; flicker.speed = 4 + i * 0.7f;
            }
            var pillar = new GameObject("Light pillar");
            pillar.transform.SetParent(fire.transform, false);
            pillar.transform.localPosition = new Vector3(0, 15.4f, 0);
            KitAssets.Part(pillar.transform, "Core", KitAssets.Mesh("Pillar core " + pillarHeight, Cylinder(1.05f, pillarHeight, 10)), m.Pillar, false);
            KitAssets.Part(pillar.transform, "Glow", KitAssets.Mesh("Pillar glow " + pillarHeight, Cylinder(2.8f, pillarHeight * 0.8f, 12)), m.PillarGlow, false);
            var lightObject = new GameObject("Beacon light", typeof(Light), typeof(HDAdditionalLightData));
            lightObject.transform.SetParent(fire.transform, false);
            lightObject.transform.localPosition = new Vector3(0, 16.5f, 0);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Point; light.color = new Color(1, 0.62f, 0.26f); light.range = 55; light.shadows = LightShadows.None;
            light.intensity = 400000;

            var view = root.AddComponent<BeaconView>();
            view.hubId = hubId; view.litParts = fire; view.darkParts = darkParts; view.pillar = pillar.transform; view.fireLight = light;
            view.fireIntensity = 400000;
            view.SetLit(true, false);
            return root;
        }

        // Open cylinder with u around and v up (0 at the base, 1 at the top): the pillar ramp.
        public static MeshBuilder Cylinder(float radius, float height, int sides)
        {
            var mesh = new MeshBuilder();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2 / sides, a1 = (i + 1) * Mathf.PI * 2 / sides;
                var b0 = new Vector3(Mathf.Cos(a0) * radius, 0, Mathf.Sin(a0) * radius); var b1 = new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);
                var t0 = b0 + Vector3.up * height; var t1 = b1 + Vector3.up * height;
                float u0 = (float)i / sides, u1 = (float)(i + 1) / sides;
                mesh.Quad(b0, t0, t1, b1, new Vector2(u0, 0), new Vector2(u0, 1), new Vector2(u1, 1), new Vector2(u1, 0));
            }
            return mesh;
        }
    }
}
