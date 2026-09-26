using System.Collections.Generic;
using System.IO;
using System.Linq;
using PirateGame.Art;
using PirateGame.Art.Editor;
using PirateGame.Presentation.Cameras;
using PirateGame.Presentation.Ships;
using PirateGame.Presentation.World;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace PirateGame.Playground.Editor
{
    // Generates the Drowned Sun playground: a vignette of the Gilded Shallows
    // (lagoon shelf and reef rim, the Sunken Forum, the Dawn Watcher, the Sun Gate,
    // Dawnrest Beacon, a relic, a wreck, two ships) under the zone atmospheres.
    public static class DrownedSunPlayground
    {
        public const string Scene = "Assets/_Game/Scenes/Playground/DrownedSun.unity";
        private const string Water = "Assets/_Game/Scenes/Tests/T02/WaterTest.unity";

        [MenuItem("Pirate Game/Drowned Sun/Author Playground")]
        public static void Create()
        {
            KitAssets.ForgetMeshes();
            var scene = EditorSceneManager.OpenScene(Water, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene, Scene);
            scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            var keep = new HashSet<string> { "Main Camera", "Sun", "Environment", "Ocean", "Harbor reflection" };
            foreach (var root in scene.GetRootGameObjects()) if (!keep.Contains(root.name)) Object.DestroyImmediate(root);

            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            var sun = GameObject.Find("Sun").GetComponent<Light>();
            var ocean = Object.FindFirstObjectByType<WaterSurface>();
            var volume = Object.FindFirstObjectByType<Volume>();
            volume.sharedProfile = Atmospheres.Volume(volume.sharedProfile);
            ocean.decalRegionSize = new Vector2(150, 150);
            ocean.caustics = true;
            camera.farClipPlane = 2500;
            camera.fieldOfView = 50;
            var probe = GameObject.Find("Harbor reflection");
            probe.transform.position = new Vector3(0, 6, 40);
            probe.GetComponent<ReflectionProbe>().size = new Vector3(420, 120, 420);

            var m = new KitMaterials();
            var world = new GameObject("Gilded Shallows").transform;
            BuildLagoon(world, m, out var beaconFocus);

            var ship = Ship("Player cutter", m, Vessels.Style.SunCutter, new Vector3(-12, 0, 12), 25);
            Ship("Wrecker", m, Vessels.Style.Wrecker, new Vector3(-30, 0, 62), 210);

            var profiles = Atmospheres.All();
            var atmosphere = new GameObject("Atmosphere").AddComponent<ZoneAtmosphere>();
            atmosphere.sun = sun; atmosphere.volume = volume; atmosphere.ocean = ocean;
            atmosphere.profiles = profiles; atmosphere.follow = ship;
            atmosphere.Apply(profiles[0].look);

            if (!camera.TryGetComponent<ShipFollowCamera>(out var rig)) rig = camera.gameObject.AddComponent<ShipFollowCamera>();
            rig.framings = true; rig.target = ship;
            rig.Snap();

            var tour = new GameObject("Playground tour").AddComponent<PlaygroundTour>();
            tour.rig = rig; tour.ship = ship; tour.atmosphere = atmosphere; tour.moods = profiles;
            tour.beaconFocus = beaconFocus; tour.beaconViewYaw = 180;
            tour.shots = new[]
            {
                Shot("voyage-lagoon", new Vector3(-12, 0, 12), 25),
                Shot("tactical-wrecker", new Vector3(-38, 0, 40), 0, threat: 1),
                Shot("approach-relic", new Vector3(-45, 0, 34), 0, near: 1),
                new PlaygroundTour.Shot { name = "beacon-harbor", ship = new Vector3(0, 0, -28), yaw = 0, harbor = true },
                new PlaygroundTour.Shot { name = "forum-above", ship = new Vector3(-12, 0, 12), yaw = 25, freeCamera = true,
                    cameraPosition = new Vector3(-58, 38, 26), cameraEuler = new Vector3(58, 0, 0) },
                Shot("gate-looking-home", new Vector3(0, 0, 148), 180),
                Shot("voyage-sun-gate", new Vector3(8, 0, 104), 356),
                Shot("causeway-mood", new Vector3(-12, 0, 12), 25, mood: 1),
                Shot("deeps-mood", new Vector3(-12, 0, 12), 25, mood: 2),
            };
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Drowned Sun playground authored: " + Scene);
        }

        private static PlaygroundTour.Shot Shot(string name, Vector3 ship, float yaw, float threat = 0, float near = 0, int mood = 0) =>
            new PlaygroundTour.Shot { name = name, ship = ship, yaw = yaw, threat = threat, near = near, mood = mood };

        private static void BuildLagoon(Transform world, KitMaterials m, out Vector3 beaconFocus)
        {
            // Seabed: the lagoon shelf inside an atoll rim whose only gap is the Sun Gate.
            var center = new Vector2(0, 45);
            var field = Seabed.Field(new Seabed.IFeature[]
            {
                new Seabed.Shelf { center = center, radii = new Vector2(140, 115), depth = -4.4f, edge = 24 },
                new Seabed.Rim { center = center, radii = new Vector2(150, 124), width = 13, crest = Crest },
            }, 11);
            var seabed = new Composer();
            Seabed.Build(seabed, m, new Rect(-180, -105, 360, 305), 5, field, 11);
            seabed.Emit(world, "Seabed", "Playground-seabed");

            var islands = new Composer { Stain = m.StainOf };
            Nature.Islet(islands, m, new Vector2(0, -54), new Vector2(24, 17), 2.2f, 21, IsletStyle.Jungle, 5, 6);
            Nature.Islet(islands, m, new Vector2(48, 38), new Vector2(13, 9), 1.8f, 22, IsletStyle.Jungle, 4, 3);
            Nature.Islet(islands, m, new Vector2(-104, 118), new Vector2(16, 11), 2.4f, 23, IsletStyle.Jungle, 5, 4);
            // Palms along the cays of the rim.
            for (int i = 0; i < 26; i++)
            {
                float a = i * Mathf.PI * 2 / 26 + 0.11f;
                if (Crest(a) < 0.9f) continue;
                var p = new Vector3(center.x + Mathf.Cos(a) * 150, 1.0f, center.y + Mathf.Sin(a) * 124);
                Nature.Palm(islands, m, p, 6.5f + (i % 3), a * Mathf.Rad2Deg + 180, 0.6f, 40 + i);
            }
            islands.Emit(world, "Islets", "Playground-islets");

            // Dawnrest Beacon on the home islet, a stone quay running out to the berth.
            var beacon = Beacons.Beacon(world, m, new Vector3(0, 0.2f, -58), 0, "home-harbor", "Playground-beacon");
            beaconFocus = beacon.transform.position + Vector3.up * 9;
            var quay = new Composer { Stain = m.StainOf };
            quay.Add(m.LimestoneShade, Shapes.Box(new Vector3(4.2f, 5.6f, 15), 0.2f), new Vector3(5.8f, -1.8f, -37));
            foreach (float z in new[] { -42f, -35f, -30.5f })
                quay.Add(m.Brazier, Shapes.Lathe(new[] { new Vector2(0.28f, 0), new Vector2(0.22f, 0.7f), new Vector2(0.32f, 0.85f), new Vector2(0, 0.9f) }, 8), new Vector3(3.9f, 1.0f, z));
            quay.Box(new Vector3(5.8f, 0, -37), new Vector3(4.4f, 6, 15.4f));
            quay.Emit(world, "Dawnrest quay", "Playground-quay");

            // The Sunken Forum: plaza, colonnade, stair to a terrace, an arch facing the lagoon.
            var forum = new Composer { Stain = m.StainOf };
            var plaza = new Vector3(-58, -3.4f, 55);
            Ruins.Plaza(forum, m, plaza, 34, 24, 8, 31);
            var rotation = Quaternion.Euler(0, 8, 0);
            int n = 0;
            foreach (float z in new[] { -10.5f, 10.5f })
                for (float x = -14; x <= 14.1f; x += 7)
                {
                    n++;
                    bool broken = n % 3 == 0 || n == 5;
                    Ruins.Column(forum, m, plaza + rotation * new Vector3(x, 0, z), 10.5f, 0.95f, 50 + n, broken, 8);
                }
            Ruins.FallenColumn(forum, m, plaza + rotation * new Vector3(-6, 0, -3), 40, 9, 0.95f, 61);
            Ruins.FallenColumn(forum, m, plaza + rotation * new Vector3(9, 0, 4), -70, 6, 0.95f, 62);
            Ruins.Stairs(forum, m, plaza + rotation * new Vector3(0, 0, 13), 8, 9, 10, 0.48f, 1.0f, true);
            forum.Add(m.Limestone, Shapes.Box(new Vector3(16, 6, 11), 0.25f), plaza + rotation * new Vector3(0, 1.8f, 28.5f), 8);
            forum.Box(plaza + rotation * new Vector3(0, 1.8f, 28.5f), new Vector3(16, 8, 11), 8);
            Ruins.SunDisc(forum, m, plaza + rotation * new Vector3(0, 9.6f, 28f), rotation, 3.2f, false);
            forum.Add(m.LimestoneShade, Shapes.Box(new Vector3(1.2f, 3.8f, 1.2f), 0.1f), plaza + rotation * new Vector3(0, 6.5f, 28f), 8);
            Ruins.Arch(forum, m, new Vector3(-37, -4.4f, 58), 98, 9, 8, 2.4f, 71, false);
            Ruins.SunDisc(forum, m, new Vector3(-64, -3.0f, 47), Quaternion.Euler(-62, 30, 0), 3.0f, true);
            Ruins.Blocks(forum, m, new Vector3(-74, -4.3f, 60), 8, 7, 81);
            Ruins.Obelisk(forum, m, new Vector3(-86, -4.4f, 42), 13, 20, 11);
            Ruins.Pedestal(forum, m, new Vector3(-45, -4.4f, 42), 91);
            Ruins.Pedestal(forum, m, new Vector3(-70, -3.4f, 66), 92);
            forum.Emit(world, "Sunken Forum", "Playground-forum");
            Relic(world, m, new Vector3(-45, 0, 42));
            Relic(world, m, new Vector3(-70, 0, 66));

            // Landmarks: the Dawn Watcher, the Sun Gate and, far beyond it, the Offering Hand.
            var landmarks = new Composer { Stain = m.StainOf };
            Monuments.ColossusHead(landmarks, m, Matrix4x4.TRS(new Vector3(72, -8.5f, 88), Quaternion.Euler(-7, 90, 6), Vector3.one), 1.7f);
            landmarks.Box(new Vector3(72, 0, 88), new Vector3(12, 10, 12));
            Monuments.SunGate(landmarks, m, new Vector3(0, 0, 169), 0, 36, 30);
            Monuments.OfferingHand(landmarks, m, Matrix4x4.TRS(new Vector3(70, -6, 300), Quaternion.Euler(0, 200, 0), Vector3.one), 1.4f);
            landmarks.Emit(world, "Landmarks", "Playground-landmarks");

            // Dressing the flats: coral heads, a drowned avenue leading to the forum, and a
            // fallen king's head beside the route out of the lagoon.
            var dressing = new Composer { Stain = m.StainOf };
            var random = new System.Random(5);
            for (int i = 0; i < 34; i++)
            {
                float x = Mathf.Lerp(-125, 125, (float)random.NextDouble()), z = Mathf.Lerp(-25, 150, (float)random.NextDouble());
                if (Mathf.Abs(x) < 30 && z < -20) continue;                                   // beacon islet and quay
                if (new Vector2(x + 58, z - 55).magnitude < 24) continue;                     // forum
                if (Mathf.Abs(x) < 10 && z > 120) continue;                                   // the gate channel
                float floor = field(x, z);
                if (floor > -2.2f || floor < -9) continue;
                Nature.CoralHead(dressing, m, new Vector3(x, floor - 0.2f, z), Mathf.Lerp(1.1f, 2.6f, (float)random.NextDouble()), 300 + i);
            }
            for (int i = 0; i < 5; i++)
            {
                var along = Vector2.Lerp(new Vector2(-14, 16), new Vector2(-42, 38), i / 4f);
                var side = new Vector2(-0.62f, -0.78f) * 4.5f;
                foreach (float s in new[] { -1f, 1f })
                {
                    var p = along + side * s;
                    float floor = field(p.x, p.y);
                    // Every third column still stands with its capital above the water; the rest are stumps.
                    int k = i * 2 + (s > 0 ? 1 : 0);
                    bool standing = k % 3 == 0;
                    Ruins.Column(dressing, m, new Vector3(p.x, floor, p.y), standing ? -floor + 3.2f : 3.6f + (k % 2) * 1.8f, 1.05f, 200 + k, !standing, 38);
                }
            }
            var fallen = new Vector3(24, -2.2f, 44);
            Monuments.ColossusHead(dressing, m, Matrix4x4.TRS(fallen, Quaternion.Euler(0, 205, 82), Vector3.one), 0.95f);
            dressing.Box(fallen + Vector3.up * 2, new Vector3(9, 8, 13), 205);
            dressing.Emit(world, "Dressing", "Playground-dressing");

            // A wreck on the eastern reef with its cargo floating beside it.
            var wreck = new Composer();
            Vessels.WreckHull(wreck, m, new Vector3(138, -0.8f, 19), 35, 5);
            wreck.Emit(world, "Wreck", "Playground-wreck");
            var debris = new GameObject("Wreck cargo").transform;
            debris.SetParent(world); debris.position = new Vector3(130, 0, 26);
            Vessels.DebrisVisual(debris, m);
        }

        // Rim crest by angle: cays above water, reef flats just below, the Sun Gate gap in the north.
        public static float Crest(float angle)
        {
            float gate = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, 90));
            if (gate < 9) return -8;
            float crest = 0.35f + 0.8f * Mathf.Sin(3 * angle + 0.7f) + 0.5f * Mathf.Sin(7 * angle + 2.1f);
            return gate < 14 ? Mathf.Lerp(-8, crest, (gate - 9) / 5) : crest;
        }

        private static void Relic(Transform world, KitMaterials m, Vector3 at)
        {
            var root = new GameObject("Relic").transform;
            root.SetParent(world); root.position = at;
            Vessels.RelicVisual(root, m);
        }

        private static Transform Ship(string name, KitMaterials m, Vessels.Style style, Vector3 at, float yaw)
        {
            // The tour moves the root; only the model bobs (ShipBobbing resets its own
            // transform to the start pose every frame).
            var root = new GameObject(name).transform;
            root.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var hull = Vessels.ShipModel(root, m, style);
            var model = root.Find("Model");
            model.gameObject.AddComponent<ShipBobbing>().amplitude = 0.1f;
            var exclusion = new GameObject("Hull water exclusion");
            exclusion.transform.SetParent(model, false);
            exclusion.AddComponent<WaterExcluder>().SetExclusionMesh(hull);
            return root;
        }

        [MenuItem("Pirate Game/Drowned Sun/Build Playground")]
        public static void Build()
        {
            Create();
            Directory.CreateDirectory("Builds/Playground");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene }, locationPathName = "Builds/Playground/DrownedSun.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            Debug.Log("Playground build: " + report.summary.result + " errors=" + report.summary.totalErrors);
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Playground build failed.");
        }
    }
}
