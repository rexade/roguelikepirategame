using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace PirateGame.Presentation.Water.Editor
{
    public static class OceanProofAuthoring
    {
        private const string Art = "Assets/_Game/Art/Prototype/";
        private const string Scene = "Assets/_Game/Scenes/Tests/T02/WaterTest.unity";
        private static Material timber, deck, sail, stone, grass, sand, metal, marker;

        [MenuItem("Pirate Prototype/T02/Author Ocean Proof")]
        public static void Create()
        {
            Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(Scene);
            if (UnityEngine.Object.FindFirstObjectByType<OceanProof>() != null)
                throw new InvalidOperationException("T02 already authored; edit existing assets instead of overwriting them.");
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Scale Reference (not a ship)"));
            // This scene is T02-owned; its test-only player starts directly without bootstrap UI.
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Menu"));
            timber = Material("Painted hull", new Color(0.16f, 0.3f, 0.28f));
            deck = Material("Deck and pier", new Color(0.52f, 0.35f, 0.19f));
            sail = Material("Sail linen", new Color(0.9f, 0.85f, 0.69f));
            stone = Material("Cool rock", new Color(0.3f, 0.36f, 0.4f));
            grass = Material("Island grass", new Color(0.3f, 0.48f, 0.2f));
            sand = Material("Shallow sand", new Color(0.68f, 0.62f, 0.4f));
            metal = Material("Iron", new Color(0.08f, 0.1f, 0.12f));
            marker = Material("Attack amber", new Color(1, 0.35f, 0.04f), true);
            var fixture = new GameObject("T02 Ocean Proof").AddComponent<OceanProof>();
            fixture.gameplayCamera = Camera.main;
            fixture.gameplayCamera.fieldOfView = 50;
            fixture.gameplayCamera.transform.SetPositionAndRotation(new Vector3(7, 49, -37.29016f), Quaternion.Euler(60, 0, 0));
            fixture.sun = GameObject.Find("Sun").GetComponent<Light>();
            fixture.environment = UnityEngine.Object.FindFirstObjectByType<Volume>();
            var profile = UnityEngine.Object.Instantiate(fixture.environment.sharedProfile);
            profile.components = profile.components.Select(UnityEngine.Object.Instantiate).ToList();
            AssetDatabase.CreateAsset(profile, Art + "OceanProofVolume.asset");
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            fixture.environment.sharedProfile = profile;
            fixture.ocean = UnityEngine.Object.FindFirstObjectByType<WaterSurface>();
            fixture.ocean.foam = true;
            fixture.ocean.foamPersistenceMultiplier = 0.8f;
            fixture.ocean.decalRegionSize = new Vector2(150, 150);
            fixture.ocean.refractionColor = new Color(0.08f, 0.62f, 0.59f);
            fixture.ocean.scatteringColor = new Color(0.02f, 0.22f, 0.27f);
            fixture.ocean.absorptionDistance = 8;
            fixture.ocean.maxRefractionDistance = 3;
            fixture.ocean.largeWindSpeed = 22;
            fixture.ocean.largeBand0Multiplier = 0.12f;
            fixture.ocean.largeBand1Multiplier = 0.2f;
            fixture.ocean.caustics = true;
            fixture.ocean.causticsDirectionalShadow = true;

            var foamShader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipelineResources/ShaderGraph/Water Decal Sample.shadergraph");
            var foam = new Material(foamShader) { name = "Engine disk foam" };
            foam.SetFloat("_AffectDeformation", 0);
            foam.SetFloat("_TYPE", 0);
            MaterialEditor.ApplyMaterialPropertyDrawers(foam);
            HDMaterial.ValidateMaterial(foam);
            AssetDatabase.CreateAsset(foam, Art + "Foam.mat");

            var root = new GameObject("Route Root (XZ and yaw only)").transform;
            root.position = new Vector3(7, 0, -9);
            root.gameObject.AddComponent<BoxCollider>().size = new Vector3(1.8f, 1, 4.8f);
            fixture.routeRoot = root;
            fixture.visibleShip = Ship("Starter cutter", false).transform;
            fixture.visibleShip.SetParent(root, false);
            PrefabUtility.SaveAsPrefabAsset(fixture.visibleShip.gameObject, Art + "StarterCutter.prefab");
            var alternate = Ship("Ship variant - red pennant", true);
            PrefabUtility.SaveAsPrefabAsset(alternate, Art + "CutterVariant.prefab");
            UnityEngine.Object.DestroyImmediate(alternate);
            fixture.wake = Foam("Stern wake", root, new Vector3(0, 0, -2.5f), new Vector2(2, 3), foam, 0.7f);
            fixture.attackMarker = new GameObject("Stable dummy attack marker").transform;
            Ring(fixture.attackMarker, metal, 1.4f, 0.22f);
            Ring(fixture.attackMarker, marker, 1.4f, 0.11f);
            fixture.attackMarker.position = new Vector3(7, 0.8f, -3);

            var island = Island("Harbor island", false);
            island.transform.position = new Vector3(-16, 0, 4);
            PrefabUtility.SaveAsPrefabAsset(island, Art + "HarborIsland.prefab");
            var variant = Island("Island variant", true);
            PrefabUtility.SaveAsPrefabAsset(variant, Art + "IslandVariant.prefab");
            variant.transform.position = new Vector3(35, 0, 40);
            variant.transform.localScale = Vector3.one * 0.65f;
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI * 2 / 24;
                Foam("Shore foam " + i, null, new Vector3(-16 + Mathf.Cos(a) * 14, 0, 4 + Mathf.Sin(a) * 18), new Vector2(4, 4), foam, 0.15f);
            }
            for (int i = 0; i < 12; i++)
                Part("Pier plank", null, new Vector3(-7 + i * 0.7f, 0.9f, -9), new Vector3(0.65f, 0.22f, 2.4f), deck);
            for (int i = 0; i < 4; i++)
                Part("Pier piling", null, new Vector3(-7 + i * 2.5f, 0, -10.2f), new Vector3(0.25f, 3, 0.25f), timber);
            Part("Harbor lamp post", null, new Vector3(-5, 2.4f, -8), new Vector3(0.2f, 3, 0.2f), metal);
            Part("Lantern", null, new Vector3(-5, 4, -8), new Vector3(0.6f, 0.8f, 0.6f), marker);
            var lamp = new GameObject("Harbor light", typeof(Light), typeof(HDAdditionalLightData));
            lamp.transform.position = new Vector3(-5, 4, -8);
            var light = lamp.GetComponent<Light>();
            light.type = LightType.Point; light.color = new Color(1, 0.56f, 0.18f);
            light.intensity = 4500; light.range = 18; light.shadows = LightShadows.Soft;
            var probe = new GameObject("Harbor reflection", typeof(ReflectionProbe));
            probe.transform.position = new Vector3(-3, 5, 0);
            var reflection = probe.GetComponent<ReflectionProbe>();
            reflection.mode = ReflectionProbeMode.Realtime;
            reflection.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            reflection.size = new Vector3(120, 60, 120);
            reflection.resolution = 128;
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Verify();
        }

        private static Material Material(string name, Color color, bool unlit = false)
        {
            var material = new Material(Shader.Find(unlit ? "HDRP/Unlit" : "HDRP/Lit")) { name = name };
            material.SetColor(unlit ? "_UnlitColor" : "_BaseColor", color);
            material.SetFloat("_Smoothness", 0.25f);
            HDMaterial.ValidateMaterial(material);
            AssetDatabase.CreateAsset(material, Art + name + ".mat");
            return material;
        }

        private static GameObject Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material, PrimitiveType type = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Ship(string name, bool variant)
        {
            var go = new GameObject(name);
            // A closed faceted hull and a matching exclusion volume protect the deck in rough water.
            var mesh = HullMesh();
            string meshPath = Art + (variant ? "VariantHull.asset" : "Hull.asset");
            AssetDatabase.CreateAsset(mesh, meshPath);
            var hull = new GameObject("Faceted hull", typeof(MeshFilter), typeof(MeshRenderer));
            hull.transform.SetParent(go.transform, false);
            hull.GetComponent<MeshFilter>().sharedMesh = mesh;
            hull.GetComponent<MeshRenderer>().sharedMaterial = timber;
            Part("Raised deck", go.transform, new Vector3(0, 0.85f, -0.25f), new Vector3(1.65f, 0.2f, 3.5f), deck);
            Part("Mast", go.transform, new Vector3(0, 2, 0), new Vector3(0.12f, 3, 0.12f), deck);
            Part("Cross spar", go.transform, new Vector3(0, 3.1f, 0), new Vector3(2.5f, 0.1f, 0.1f), deck);
            Part("Square sail", go.transform, new Vector3(0, 2.55f, 0.12f), new Vector3(2.3f, 1.15f, 0.07f), sail);
            Part("Pennant", go.transform, new Vector3(0.35f, 3.55f, 0), new Vector3(0.65f, 0.3f, 0.05f), variant ? marker : timber);
            Part("Cabin", go.transform, new Vector3(0, 1.15f, -1.4f), new Vector3(1.2f, 0.5f, 0.8f), variant ? stone : timber);
            for (int i = -1; i <= 1; i += 2)
                Part("Dummy cannon", go.transform, new Vector3(i * 0.8f, 1.05f, 0.5f), new Vector3(0.7f, 0.22f, 0.25f), metal);
            var exclusion = new GameObject("Hull water exclusion");
            exclusion.transform.SetParent(go.transform, false);
            exclusion.AddComponent<WaterExcluder>().SetExclusionMesh(mesh);
            return go;
        }

        private static Mesh HullMesh()
        {
            Vector2[] outline = { new Vector2(-0.75f,-2.4f), new Vector2(0.75f,-2.4f), new Vector2(1,-0.8f), new Vector2(0.9f,1.2f), new Vector2(0,2.6f), new Vector2(-0.9f,1.2f), new Vector2(-1,-0.8f) };
            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();
            Action<Vector3,Vector3,Vector3> tri = (a,b,c) => { int n = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); triangles.Add(n); triangles.Add(n+1); triangles.Add(n+2); };
            for (int i = 0; i < outline.Length; i++)
            {
                var a = outline[i]; var b = outline[(i+1)%outline.Length];
                var topA = new Vector3(a.x,0.8f,a.y); var topB = new Vector3(b.x,0.8f,b.y);
                var lowA = new Vector3(a.x*0.65f,-0.65f,a.y*0.85f); var lowB = new Vector3(b.x*0.65f,-0.65f,b.y*0.85f);
                tri(topA,topB,lowA); tri(topB,lowB,lowA);
                tri(new Vector3(0,0.8f,0),topB,topA);
                tri(new Vector3(0,-0.65f,0),lowA,lowB);
            }
            var mesh = new Mesh { name = "Seven sided closed cutter hull" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject Island(string name, bool variant)
        {
            var go = new GameObject(name);
            Part("Submerged shelf", go.transform, new Vector3(0,-1.7f,0), new Vector3(34,2,43), sand, PrimitiveType.Sphere);
            Part("Shore", go.transform, new Vector3(0,-0.2f,0), new Vector3(28,3,36), sand, PrimitiveType.Sphere);
            Part("Grassy crown", go.transform, new Vector3(-1,0.7f,1), new Vector3(23,4,29), grass, PrimitiveType.Sphere);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.399f;
                var rock = Part("Rock " + i, go.transform, new Vector3(Mathf.Cos(a)*9, 1.4f, Mathf.Sin(a)*12), new Vector3(3 + i%3, 3 + i%2, 3), stone);
                rock.transform.localRotation = Quaternion.Euler(12*i, 31*i, 15);
            }
            Part("Lookout", go.transform, new Vector3(variant ? 3 : -4, 4, 4), new Vector3(3,6,3), stone, PrimitiveType.Cylinder);
            Part("Lookout roof", go.transform, new Vector3(variant ? 3 : -4, 7.2f, 4), new Vector3(4,0.5f,4), timber);
            return go;
        }

        private static WaterDecal Foam(string name, Transform parent, Vector3 position, Vector2 size, Material material, float amount)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var decal = go.AddComponent<WaterDecal>();
            decal.material = material; decal.regionSize = size; decal.surfaceFoamDimmer = amount; decal.deepFoamDimmer = 0;
            return decal;
        }

        private static void Ring(Transform parent, Material material, float radius, float width)
        {
            var go = new GameObject(material.name, typeof(LineRenderer)); go.transform.SetParent(parent, false);
            var line = go.GetComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true;
            line.sharedMaterial = material; line.widthMultiplier = width; line.positionCount = 48;
            for (int i=0;i<48;i++) { float a=i*Mathf.PI*2/48; line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius, material == marker ? 0.02f : 0, Mathf.Sin(a)*radius)); }
        }

        public static void Verify()
        {
            EditorSceneManager.OpenScene(Scene);
            var fixture = UnityEngine.Object.FindFirstObjectByType<OceanProof>();
            if (!fixture || !fixture.ocean || !fixture.visibleShip || !fixture.wake || !fixture.attackMarker)
                throw new BuildFailedException("Incomplete ocean proof fixture.");
            if (Mathf.Abs(fixture.gameplayCamera.transform.eulerAngles.x - 60) > 0.01f)
                throw new BuildFailedException("D05 camera pitch changed.");
            for (int i=0;i<=6000;i++) if (OceanProof.Route(i/100f).y != 0) throw new BuildFailedException("Route leaves XZ.");
            if (fixture.visibleShip.parent != fixture.routeRoot || fixture.attackMarker.IsChildOf(fixture.visibleShip))
                throw new BuildFailedException("Visual bobbing would affect root or aim.");
            var pose = typeof(OceanProof).GetMethod("Pose", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (float t in new[] { 0f, 18f, 34f, 43f, 57f })
            {
                fixture.bobAmplitude = 0;
                fixture.ocean.largeBand0Multiplier = 0;
                pose.Invoke(fixture, new object[] { t });
                var rootMatrix = fixture.routeRoot.localToWorldMatrix;
                var markerMatrix = fixture.attackMarker.localToWorldMatrix;
                var colliderSize = fixture.routeRoot.GetComponent<BoxCollider>().size;
                fixture.bobAmplitude = 2;
                fixture.ocean.largeBand0Multiplier = 1;
                fixture.ocean.ripples = false;
                pose.Invoke(fixture, new object[] { t });
                if (fixture.routeRoot.localToWorldMatrix != rootMatrix || fixture.attackMarker.localToWorldMatrix != markerMatrix || fixture.routeRoot.GetComponent<BoxCollider>().size != colliderSize)
                    throw new BuildFailedException("AC-14 failed: visual variation changed root, collider or aim.");
            }
            Debug.Log("T02 AC-14 passed: bob 0 vs 2m, swell 0 vs 1 and ripples disabled; identical route root, collider and marker at five route points. No production simulation exists yet.");
            // Discard verification-only mutations; build the authored scene.
            EditorSceneManager.OpenScene(Scene);
            Debug.Log("T02 fixture checks passed: references, D05 pitch, 6001 XZ route samples, independent aim and model child.");
        }

        public static void Refine()
        {
            var scene = EditorSceneManager.OpenScene(Scene);
            foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (filter.name != "Shore" && filter.name != "Grassy crown" && filter.name != "Submerged shelf") continue;
                string path = Art + filter.name + " mesh.asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (!mesh)
                {
                    var v = new System.Collections.Generic.List<Vector3>();
                    var indices = new System.Collections.Generic.List<int>();
                    bool grassTop = filter.name == "Grassy crown";
                    const int count = 20;
                    for (int i=0;i<count;i++)
                    {
                        float a = i * Mathf.PI * 2 / count, b = (i+1) * Mathf.PI * 2 / count;
                        Func<float,float,Vector3> point = (angle, radius) => new Vector3(Mathf.Cos(angle)*radius*(1+0.09f*Mathf.Sin(angle*3)), 0, Mathf.Sin(angle)*radius*(1+0.07f*Mathf.Cos(angle*5)));
                        Vector3 p = point(a,0.5f) + Vector3.down*(grassTop ? 0 : 0.5f);
                        Vector3 q = point(b,0.5f) + Vector3.down*(grassTop ? 0 : 0.5f);
                        Vector3 innerP = point(a,0.32f) + Vector3.up*0.35f;
                        Vector3 innerQ = point(b,0.32f) + Vector3.up*0.35f;
                        Vector3 center = Vector3.up*0.46f;
                        foreach(var vertex in new[]{p,innerP,q,q,innerP,innerQ,innerP,center,innerQ}) { indices.Add(v.Count); v.Add(vertex); }
                    }
                    mesh = new Mesh { name = filter.name + " faceted coastline" };
                    mesh.SetVertices(v); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh,path);
                }
                filter.sharedMesh = mesh;
            }
            var fixture = UnityEngine.Object.FindFirstObjectByType<OceanProof>();
            var profile = fixture.environment.sharedProfile;
            var reflection = GameObject.Find("Harbor reflection").GetComponent<ReflectionProbe>();
            reflection.size = new Vector3(500, 100, 500);
            reflection.blendDistance = 40;
            var cameraData = fixture.gameplayCamera.GetComponent<HDAdditionalCameraData>();
            cameraData.customRenderingSettings = true;
            foreach (var field in new[] { FrameSettingsField.WaterDecals, FrameSettingsField.WaterExclusion })
            {
                cameraData.renderingPathCustomFrameSettings.SetEnabled(field, true);
                cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
            }
            if (!profile.TryGet<Tonemapping>(out var tone)) { tone = profile.Add<Tonemapping>(true); AssetDatabase.AddObjectToAsset(tone, profile); }
            tone.mode.Override(TonemappingMode.ACES);
            profile.TryGet<Exposure>(out var exposure); exposure.fixedExposure.Override(12);
            fixture.ocean.scatteringColor = new Color(0.04f, 0.35f, 0.42f);
            foreach (var decal in UnityEngine.Object.FindObjectsByType<WaterDecal>(FindObjectsSortMode.None))
            {
                if (!decal.name.StartsWith("Shore foam ", StringComparison.Ordinal)) continue;
                int index = int.Parse(decal.name.Substring("Shore foam ".Length));
                float a = index * Mathf.PI * 2 / 24;
                decal.transform.position = new Vector3(-16+Mathf.Cos(a)*12.3f*(1+0.09f*Mathf.Sin(a*3)),0,4+Mathf.Sin(a)*15.8f*(1+0.07f*Mathf.Cos(a*5)));
                decal.transform.rotation = Quaternion.Euler(0,-90-a*Mathf.Rad2Deg,0);
                decal.regionSize = new Vector2(4,2);
                decal.surfaceFoamDimmer = 1;
            }
            var foam = AssetDatabase.LoadAssetAtPath<Material>(Art + "Foam.mat");
            foam.shader = Shader.Find("PiratePrototype/WaterFoamStamp");
            foam.shaderKeywords = Array.Empty<string>();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "FoamFootprint.asset");
            if (!texture)
            {
                texture = new Texture2D(64,64,TextureFormat.RGBA32,false,true) { name="Soft irregular foam footprint", wrapMode=TextureWrapMode.Clamp };
                AssetDatabase.CreateAsset(texture,Art+"FoamFootprint.asset");
            }
                for (int y=0;y<64;y++) for(int x=0;x<64;x++)
                {
                    float u=(x+0.5f)/32-1, v=(y+0.5f)/32-1;
                    float radius=Mathf.Sqrt(u*u+v*v);
                    float edge=Mathf.Clamp01((0.95f-radius)/0.35f);
                    float grain=0.5f+0.5f*Mathf.PerlinNoise(x*0.27f,y*0.27f);
                    texture.SetPixel(x,y,new Color(Mathf.Clamp01(edge*edge*grain*3),0,0,1));
                }
                texture.Apply(); EditorUtility.SetDirty(texture);
            foam.SetTexture("_Foam_Texture",texture);
            EditorUtility.SetDirty(foam);
            EditorUtility.SetDirty(tone); EditorUtility.SetDirty(exposure); EditorUtility.SetDirty(profile);
            foreach (string name in new[] { "Harbor island", "Island variant" })
            {
                var instance = GameObject.Find(name);
                PrefabUtility.SaveAsPrefabAsset(instance, Art + (name == "Harbor island" ? "HarborIsland.prefab" : "IslandVariant.prefab"));
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Verify();
        }

        public static void Build()
        {
            Verify();
            Directory.CreateDirectory("Builds/T02");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = "Builds/T02/OceanProof.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (result.summary.result != BuildResult.Succeeded) throw new BuildFailedException("T02 build failed");
            Debug.Log("T02 build succeeded: " + result.summary.totalSize + " bytes");
        }

        public static void DemonstrateVariants()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var ship = PrefabUtility.LoadPrefabContents(Art + "StarterCutter.prefab");
            ship.name = "Ship variant - red pennant";
            ship.transform.Find("Pennant").GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "Attack amber.mat");
            ship.transform.Find("Cabin").GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "Cool rock.mat");
            PrefabUtility.SaveAsPrefabAsset(ship,Art+"CutterVariant.prefab");
            PrefabUtility.UnloadPrefabContents(ship);
            double shipSeconds = watch.Elapsed.TotalSeconds;
            watch.Restart();
            var island = PrefabUtility.LoadPrefabContents(Art + "HarborIsland.prefab");
            island.name = "Island variant";
            island.transform.position = Vector3.zero;
            island.transform.localScale = Vector3.one*0.65f;
            island.transform.Find("Lookout").localPosition = new Vector3(3,4,4);
            island.transform.Find("Lookout roof").localPosition = new Vector3(3,7.2f,4);
            PrefabUtility.SaveAsPrefabAsset(island,Art+"IslandVariant.prefab");
            PrefabUtility.UnloadPrefabContents(island);
            AssetDatabase.SaveAssets();
            double islandSeconds = watch.Elapsed.TotalSeconds;
            File.WriteAllText("docs/evidence/T02/variant-effort.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                utc=DateTime.UtcNow, shipAuthoringSeconds=shipSeconds, islandAuthoringSeconds=islandSeconds,
                method="Automated Unity prefab load, material/transform edit, save/import. Setup and shared rendered QA excluded and recorded separately. Not novice editor timing."
            },Newtonsoft.Json.Formatting.Indented));
            Debug.Log("T02 repeatable variant authoring recorded; human workflow acceptance remains pending.");
        }

        public static void RestoreNavigation()
        {
            var scene = EditorSceneManager.OpenScene(Scene);
            var fixture = UnityEngine.Object.FindFirstObjectByType<OceanProof>();
            if (!fixture.bootstrapNavigation)
            {
                var source = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Bootstrap.unity", OpenSceneMode.Additive);
                var original = source.GetRootGameObjects().Single(go => go.name == "Menu");
                var menu = UnityEngine.Object.Instantiate(original);
                menu.name = "Menu";
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(menu, scene);
                fixture.bootstrapNavigation = menu;
                EditorSceneManager.CloseScene(source, true);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            Verify();
        }
    }
}
