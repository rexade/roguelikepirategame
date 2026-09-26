using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace PirateGame.Art.Editor
{
    // Persists generated kit content (materials, textures, meshes, prefabs) under
    // Art/DrownedSun/Generated. Regeneration updates assets in place so GUIDs and
    // scene references survive.
    public static class KitAssets
    {
        public const string Root = "Assets/_Game/Art/DrownedSun/Generated/";
        private static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

        public static void Folders()
        {
            foreach (var folder in new[] { "Materials", "Meshes", "Textures", "Prefabs" }) Directory.CreateDirectory(Root + folder);
            AssetDatabase.Refresh();
        }

        // `glow` is a display-referred emissive tint (exposure weight 0), so gold keeps
        // reading as gold in shade and in the darker zones without blowing out.
        public static Material Lit(string name, Color color, float smoothness = 0.12f, float metallic = 0, Texture2D map = null, bool clip = false,
            Color? glow = null)
        {
            var material = MaterialAt(name, "HDRP/Lit");
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseColorMap", map);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_AlphaCutoffEnable", clip ? 1 : 0);
            material.SetFloat("_AlphaCutoff", 0.5f);
            material.SetFloat("_DoubleSidedEnable", clip ? 1 : 0);
            material.SetFloat("_UseEmissiveIntensity", 0);
            material.SetColor("_EmissiveColor", glow ?? Color.black);
            material.SetFloat("_EmissiveExposureWeight", 0);
            return Finish(material);
        }

        // Display-referred colour (HDRP de-exposes unlit colour): values above 1 bloom.
        public static Material Unlit(string name, Color color, bool additive = false, Texture2D map = null, bool doubleSided = false)
        {
            var material = MaterialAt(name, "HDRP/Unlit");
            material.SetColor("_UnlitColor", color);
            material.SetTexture("_UnlitColorMap", map);
            material.SetFloat("_SurfaceType", additive ? 1 : 0);
            material.SetFloat("_BlendMode", additive ? 1 : 0);
            material.SetFloat("_DoubleSidedEnable", doubleSided || additive ? 1 : 0);
            material.SetFloat("_ZWrite", additive ? 0 : 1);
            material.SetFloat("_TransparentZWrite", 0);
            return Finish(material);
        }

        private static Material MaterialAt(string name, string shader)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shader)) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.shader.name != shader) material.shader = Shader.Find(shader);
            return material;
        }

        private static Material Finish(Material material)
        {
            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Stores (or refreshes in place) a mesh asset; one build per key per session.
        public static Mesh Mesh(string key, MeshBuilder builder)
        {
            string path = Root + "Meshes/" + Safe(key) + ".asset";
            if (meshes.TryGetValue(path, out var cached) && cached != null) return cached;
            var built = builder.Build(key);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(built, path);
                existing = built;
            }
            else
            {
                existing.Clear();
                existing.indexFormat = built.indexFormat;
                existing.SetVertices(built.vertices);
                existing.SetNormals(built.normals);
                existing.SetUVs(0, built.uv);
                existing.SetTriangles(built.triangles, 0);
                existing.RecalculateBounds();
                existing.name = key;
                Object.DestroyImmediate(built);
                EditorUtility.SetDirty(existing);
            }
            meshes[path] = existing;
            return existing;
        }

        public static void ForgetMeshes() => meshes.Clear();

        public static Texture2D Texture(string name, int width, int height, Func<int, int, Color> pixel, bool srgb = true,
            TextureWrapMode wrap = TextureWrapMode.Clamp, bool mipmaps = true, bool alpha = false)
        {
            string path = Root + "Textures/" + name + ".png";
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false, !srgb);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) pixels[y * width + x] = pixel(x, y);
            image.SetPixels(pixels);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = srgb;
            importer.wrapMode = wrap;
            importer.mipmapEnabled = mipmaps;
            importer.alphaIsTransparency = alpha;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static GameObject Part(Transform parent, string name, Mesh mesh, Material material, bool shadows = true)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        public static GameObject SavePrefab(GameObject instance, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, Root + "Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(instance);
            return prefab;
        }

        public static string Safe(string key)
        {
            var chars = key.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '-' && chars[i] != '_') chars[i] = '-';
            return new string(chars);
        }
    }

    // Collects parts per material and emits one merged mesh per material, plus
    // collision (boxes and fence meshes). Parts are baked in world space.
    public sealed class Composer
    {
        private readonly Dictionary<Material, MeshBuilder> parts = new Dictionary<Material, MeshBuilder>();
        private readonly List<Material> order = new List<Material>();
        private readonly MeshBuilder solids = new MeshBuilder();
        private readonly List<(Vector3 center, Vector3 size, float yaw)> boxes = new List<(Vector3, Vector3, float)>();

        // Optional tide mark: facets wholly below `StainBelow` switch to the stained
        // variant of their material (stone goes green-grey, gold goes to patina).
        public Func<Material, Material> Stain;
        public float StainBelow = 0.6f;

        public void Add(Material material, MeshBuilder part, Matrix4x4 transform)
        {
            var placed = new MeshBuilder().Append(part, transform);
            var stained = Stain?.Invoke(material);
            if (stained == null) { Builder(material).Append(placed, Matrix4x4.identity); return; }
            Builder(stained).Append(placed.Where((a, b, c) => Mathf.Max(a.y, Mathf.Max(b.y, c.y)) < StainBelow), Matrix4x4.identity);
            Builder(material).Append(placed.Where((a, b, c) => Mathf.Max(a.y, Mathf.Max(b.y, c.y)) >= StainBelow), Matrix4x4.identity);
        }

        private MeshBuilder Builder(Material material)
        {
            if (!parts.TryGetValue(material, out var builder)) { builder = new MeshBuilder(); parts[material] = builder; order.Add(material); }
            return builder;
        }

        public void Add(Material material, MeshBuilder part, Vector3 position, float yaw = 0) =>
            Add(material, part, Matrix4x4.TRS(position, Quaternion.Euler(0, yaw, 0), Vector3.one));

        public void Add(Material material, MeshBuilder part, Vector3 position, Quaternion rotation, Vector3 scale) =>
            Add(material, part, Matrix4x4.TRS(position, rotation, scale));

        public void Box(Vector3 center, Vector3 size, float yaw = 0) => boxes.Add((center, size, yaw));

        // Collision geometry merged into one MeshCollider (fences, hulls).
        public void Solid(MeshBuilder part, Matrix4x4 transform) => solids.Append(part, transform);

        public bool IsEmpty => parts.Count == 0 && boxes.Count == 0 && solids.TriangleCount == 0;

        public GameObject Emit(Transform parent, string name, string assetKey, bool shadows = true)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            foreach (var material in order)
            {
                if (parts[material].TriangleCount == 0) continue;
                var mesh = KitAssets.Mesh(assetKey + "-" + material.name, parts[material]);
                KitAssets.Part(root.transform, material.name, mesh, material, shadows);
            }
            if (solids.TriangleCount > 0)
            {
                var collision = new GameObject("Collision");
                collision.transform.SetParent(root.transform, false);
                collision.AddComponent<MeshCollider>().sharedMesh = KitAssets.Mesh(assetKey + "-collision", solids);
            }
            for (int i = 0; i < boxes.Count; i++)
            {
                var box = new GameObject("Collision box " + i);
                box.transform.SetParent(root.transform, false);
                box.transform.SetPositionAndRotation(boxes[i].center, Quaternion.Euler(0, boxes[i].yaw, 0));
                box.AddComponent<BoxCollider>().size = boxes[i].size;
            }
            return root;
        }
    }
}
