using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PirateGame.Content.Combat;
using PirateGame.Content.Definitions;
using PirateGame.Content.World;
using PirateGame.Core;
using PirateGame.Gameplay.AI;
using PirateGame.Gameplay.Combat;
using PirateGame.Gameplay.Input;
using PirateGame.Gameplay.Ships;
using PirateGame.Gameplay.World;
using PirateGame.Presentation.Audio;
using PirateGame.Presentation.Cameras;
using PirateGame.Presentation.Weather;
using PirateGame.Presentation.Combat;
using PirateGame.Presentation.Ships;
using PirateGame.UI.Game;
using PirateGame.UI.Harbor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace PirateGame.Composition.Editor
{
    // Regenerates the production content, prefabs and OceanWorld scene from
    // authored data (FirstRegion.asset, the T02 art kit and the T04-T07 parts).
    // Re-running replaces generated assets in place, preserving their GUIDs.
    public static class WorldAuthoring
    {
        public const string WorldScene = "Assets/_Game/Scenes/OceanWorld.unity";
        public const string BootstrapScene = "Assets/_Game/Scenes/Bootstrap.unity";
        public const string OceanTestScene = "Assets/_Game/Scenes/Tests/T02/WaterTest.unity";
        public const string Content = "Assets/_Game/Content/Production/";
        public const string Prefabs = "Assets/_Game/Prefabs/Production/";
        private const string Art = "Assets/_Game/Art/Prototype/";
        private const string RegionAsset = "Assets/_Game/Content/World/FirstRegion/FirstRegion.asset";

        [MenuItem("Pirate Game/Author Production World")]
        public static void CreateAll()
        {
            Directory.CreateDirectory(Content); Directory.CreateDirectory(Prefabs);
            AssetDatabase.Refresh();
            var region = AssetDatabase.LoadAssetAtPath<FirstRegionAsset>(RegionAsset);
            AuthorRegionContent(region);
            var rules = AuthorRules(region);
            var combat = AuthorCombat(rules);
            var kit = new Kit();
            var raider = EnemyPrefab("Raider", kit);
            var gunner = EnemyPrefab("Gunner", kit);
            AuthorScene(region, rules, combat, raider, gunner, kit);
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("Production world authored: " + WorldScene);
        }

        // ------------------------------------------------------------ content

        private static void AuthorRegionContent(FirstRegionAsset region)
        {
            // Second encounter is an escorted pair; everything else keeps T06's layout.
            foreach (var site in region.encounters) site.ships = site.id == "first:encounter-02" ? 2 : 1;
            region.homeName = "Homeward Harbor";
            // T09: a second harbor in the same region, north-east beyond the escorted pair.
            region.outposts = new[] { new HarborSite { id = "saltmarsh-harbor", name = "Saltmarsh Harbor", dock = new Vector2(58, 124),
                dockRadius = 5, dockMaximumSpeed = 1, landmass = new Vector2(66, 145), landmassSize = new Vector2(26, 18) } };
            EditorUtility.SetDirty(region);
        }

        private static DefinitionCatalogAsset AuthorRules(FirstRegionAsset region)
        {
            var rules = LoadOrCreate<DefinitionCatalogAsset>(Content + "GameCatalog.asset");
            rules.resources = new[] { new ResourceRow { id = "wood", weight = 1 }, new ResourceRow { id = "iron", weight = 2 } };
            rules.hulls = new[] { new HullRow { id = "cutter",
                slots = new[] { new SlotRow { id = "weapon", kind = SlotKind.Weapon }, new SlotRow { id = "ability", kind = SlotKind.Ability } },
                stats = new[] { Stat("health", 100, 1, 1000), Stat("cargo", 10, 0, 1000), Stat("speed", 8, 1, 30), Stat("damage-scale", 1, 0.1, 4) } } };
            rules.equipment = new[] { new EquipmentRow { id = "cannon", kind = SlotKind.Weapon },
                new EquipmentRow { id = "repeater", kind = SlotKind.Weapon }, new EquipmentRow { id = "brace", kind = SlotKind.Ability } };
            rules.hubs = region.Hubs.Select(h => new HubRow { id = h.Id, regionId = h.Dock.RegionId, x = h.Dock.X, z = h.Dock.Z, radius = h.Radius, maximumSpeed = h.MaximumSpeed }).ToArray();
            rules.upgrades = new[] {
                Upgrade("harbor-storehouse", "harbor", 1, new[] { Cost("wood", 5) }, new string[0], new[] { "storehouse" }, Mod("cargo", ModifierOperation.Flat, 5)),
                Upgrade("shipwright-slip", "harbor", 2, new[] { Cost("wood", 8), Cost("iron", 3) }, new[] { "storehouse" }, new string[0], Mod("speed", ModifierOperation.Percent, 0.15)),
                Upgrade("reinforced-hull", "ship", 1, new[] { Cost("wood", 6), Cost("iron", 2) }, new string[0], new string[0], Mod("health", ModifierOperation.Percent, 0.5)),
                Upgrade("iron-bound-guns", "ship", 2, new[] { Cost("wood", 8), Cost("iron", 4) }, new string[0], new string[0], Mod("damage-scale", ModifierOperation.Percent, 0.35)),
                Upgrade("navigators-charts", "charts", 1, new[] { Cost("wood", 6), Cost("iron", 3) }, new string[0], new[] { "fast-travel" }) };
            rules.unlockIds = new[] { "storehouse", "fast-travel" };
            rules.entityDefinitionIds = new[] { "barrel", "wreck", "raider", "gunner", CombatCatalog.ContextDefinition };
            rules.regionIds = new[] { region.regionId };
            rules.worldIdentities = region.Identities(RegionAsset).Select(i => new IdentityRow { id = i.Id, origin = i.Origin }).ToArray();
            EditorUtility.SetDirty(rules);
            region.Validate(rules.Freeze());
            return rules;
        }

        private static CombatCatalogAsset AuthorCombat(DefinitionCatalogAsset rules)
        {
            var combat = LoadOrCreate<CombatCatalogAsset>(Content + "GameCombat.asset");
            combat.rules = rules;
            combat.weapons = new[] {
                new WeaponRow { id = "cannon", damage = 30, cooldown = 1.1f, speed = 65, range = 45, radius = 0.24f },
                new WeaponRow { id = "repeater", damage = 8, cooldown = 0.22f, speed = 90, range = 30, radius = 0.13f } };
            combat.enemies = new[] {
                new EnemyRow { id = "raider", weaponId = "repeater", tactic = EnemyTactic.Pursue, health = 70, speed = 5, engagementRange = 38, preferredRange = 9, reloadScale = 3,
                    wreckLoot = new[] { Cost("wood", 2), Cost("iron", 1) } },
                new EnemyRow { id = "gunner", weaponId = "cannon", tactic = EnemyTactic.KeepRange, health = 90, speed = 3.5f, engagementRange = 42, preferredRange = 23, reloadScale = 3,
                    wreckLoot = new[] { Cost("wood", 3), Cost("iron", 2) } } };
            combat.abilityId = "brace"; combat.abilityCooldown = 6; combat.braceDuration = 2; combat.damageMultiplier = 0.25f;
            EditorUtility.SetDirty(combat);
            combat.Freeze();
            return combat;
        }

        private static StatRow Stat(string id, double value, double min, double max) => new StatRow { id = id, value = value, minimum = min, maximum = max };
        private static QuantityRow Cost(string id, int quantity) => new QuantityRow { id = id, quantity = quantity };
        private static ModifierRow Mod(string stat, ModifierOperation op, double value) => new ModifierRow { statId = stat, operation = op, value = value };
        private static UpgradeRow Upgrade(string id, string track, int tier, QuantityRow[] cost, string[] requires, string[] grants, params ModifierRow[] mods) =>
            new UpgradeRow { id = id, trackId = track, tier = tier, cost = cost, requiredUnlocks = requires, grants = grants, modifiers = mods };

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ------------------------------------------------------------ materials and art kit

        private sealed class Kit
        {
            public readonly Material Timber = Load("Painted hull"), Deck = Load("Deck and pier"), Sail = Load("Sail linen"),
                Rock = Load("Cool rock"), Grass = Load("Island grass"), Sand = Load("Shallow sand"), Iron = Load("Iron"),
                Amber = Load("Attack amber"), Foam = Load("Foam");
            public readonly Mesh Shore = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "Shore mesh.asset"),
                Crown = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "Grassy crown mesh.asset"),
                Shelf = AssetDatabase.LoadAssetAtPath<Mesh>(Art + "Submerged shelf mesh.asset");
            public readonly Material FriendlyShot = Unlit("Shot friendly", new Color(1f, 0.82f, 0.25f)),
                HostileShot = Unlit("Shot hostile", new Color(1f, 0.25f, 0.16f)),
                Flash = Unlit("Muzzle flash", new Color(1f, 0.62f, 0.22f)),
                Brace = Unlit("Brace ring", new Color(0.25f, 1f, 0.85f)),
                DockRing = Unlit("Dock ring", new Color(0.95f, 0.9f, 0.7f)),
                AimLine = Unlit("Aim line", new Color(1f, 0.55f, 0.2f)),
                Lamp = Unlit("Lantern glow", new Color(1f, 0.7f, 0.3f)),
                Smoke = Lit("Gun smoke", new Color(0.42f, 0.42f, 0.41f), 0.1f),
                Splash = Lit("Splash", new Color(0.9f, 0.95f, 0.97f), 0.5f),
                Roof = Lit("Roof tiles", new Color(0.55f, 0.22f, 0.16f), 0.2f),
                Plaster = Lit("Plaster", new Color(0.86f, 0.8f, 0.68f), 0.15f),
                RaiderSail = Lit("Raider sail", new Color(0.72f, 0.14f, 0.12f), 0.2f),
                GunnerSail = Lit("Gunner sail", new Color(0.36f, 0.16f, 0.52f), 0.2f),
                Cloth = Lit("Awning cloth", new Color(0.2f, 0.45f, 0.5f), 0.2f);
            private static Material Load(string name) =>
                AssetDatabase.LoadAssetAtPath<Material>(Art + name + ".mat") ?? throw new InvalidOperationException("Missing art material " + name);
        }

        private static Material Unlit(string name, Color color) => MaterialAsset(name, "HDRP/Unlit", "_UnlitColor", color, 0);
        private static Material Lit(string name, Color color, float smoothness) => MaterialAsset(name, "HDRP/Lit", "_BaseColor", color, smoothness);

        private static Material MaterialAsset(string name, string shader, string property, Color color, float smoothness)
        {
            Directory.CreateDirectory(Content + "Materials");
            string path = Content + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find(shader)) { name = name }; AssetDatabase.CreateAsset(material, path); }
            material.shader = Shader.Find(shader);
            material.SetColor(property, color);
            if (shader == "HDRP/Lit") material.SetFloat("_Smoothness", smoothness);
            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------ prefabs

        private static GameObject EnemyPrefab(string name, Kit kit)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Combat/" + name + ".prefab");
            var ship = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(ship, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            ship.name = name;
            ship.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var sail = name == "Raider" ? kit.RaiderSail : kit.GunnerSail;
            foreach (var renderer in ship.GetComponentsInChildren<Renderer>())
                if (renderer.name == "Square sail" || renderer.name == "Pennant") renderer.sharedMaterial = sail;
            AddWake(ship, kit, 0.8f);
            var target = ship.GetComponent<CombatTarget>(); target.motor = ship.GetComponent<ShipMotor>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(ship, Prefabs + name + ".prefab");
            Object.DestroyImmediate(ship);
            return prefab;
        }

        private static void AddWake(GameObject ship, Kit kit, float strength)
        {
            var existing = ship.transform.Find("Stern wake");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var go = new GameObject("Stern wake");
            go.transform.SetParent(ship.transform, false);
            go.transform.localPosition = new Vector3(0, 0, -3.2f);
            var decal = go.AddComponent<WaterDecal>();
            decal.material = kit.Foam; decal.regionSize = new Vector2(2.2f, 4.8f); decal.surfaceFoamDimmer = 0; decal.deepFoamDimmer = 0;
            if (!ship.TryGetComponent<ShipWake>(out var wake)) wake = ship.AddComponent<ShipWake>();
            wake.motor = ship.GetComponent<ShipMotor>(); wake.wake = decal; wake.strength = strength;
        }

        // ------------------------------------------------------------ scene

        private static void AuthorScene(FirstRegionAsset region, DefinitionCatalogAsset rules, CombatCatalogAsset combat,
            GameObject raider, GameObject gunner, Kit kit)
        {
            var scene = EditorSceneManager.OpenScene(OceanTestScene, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene, WorldScene);
            scene = EditorSceneManager.OpenScene(WorldScene, OpenSceneMode.Single);
            var keep = new HashSet<string> { "Main Camera", "Sun", "Environment", "Ocean", "Harbor reflection", "Harbor light" };
            foreach (var root in scene.GetRootGameObjects())
                if (!keep.Contains(root.name)) Object.DestroyImmediate(root);

            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            var sun = GameObject.Find("Sun").GetComponent<Light>();
            var ocean = Object.FindFirstObjectByType<WaterSurface>();
            var volume = Object.FindFirstObjectByType<Volume>();
            ConfigureEnvironment(sun, volume, ocean);

            var world = new GameObject("World").transform;
            var art = new GameObject("Region art").transform; art.SetParent(world);
            BuildHarbor(art, region.homeName, region.homeLandmass, region.homeLandmassSize, region.dock, kit, kit.Amber, 1);
            int seed = 3;
            foreach (var island in region.islands) BuildIsland(art, island.id, island.position, island.size, kit, seed++, true);
            var dock = new Vector3(region.dock.x, 0, region.dock.y);
            DockRing(art, dock, region.dockRadius, kit);
            var probe = GameObject.Find("Harbor reflection").transform; probe.SetParent(world); probe.position = new Vector3(-4, 5, -22);
            var lamp = GameObject.Find("Harbor light"); lamp.transform.SetParent(world);
            lamp.transform.position = LampLight(region.homeLandmass, region.dock);
            var light = lamp.GetComponent<Light>(); light.intensity = 3000; light.range = 16;
            foreach (var outpost in region.outposts)
            {
                BuildHarbor(art, outpost.name, outpost.landmass, outpost.landmassSize, outpost.dock, kit, kit.Cloth, seed++);
                DockRing(art, new Vector3(outpost.dock.x, 0, outpost.dock.y), outpost.dockRadius, kit);
                var outpostLamp = Object.Instantiate(lamp, world);
                outpostLamp.name = outpost.name + " light";
                outpostLamp.transform.position = LampLight(outpost.landmass, outpost.dock);
            }

            var salvageRoot = new GameObject("Salvage").AddComponent<SalvageRegion>();
            salvageRoot.transform.SetParent(world);
            salvageRoot.content = region;
            salvageRoot.barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/World/Barrel.prefab");
            salvageRoot.wreckPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/World/Wreck.prefab");

            // Player: the accepted T04 cutter with input, combat target, salvage reach and wake.
            var playerObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Ships/PlayerCutter.prefab"));
            PrefabUtility.UnpackPrefabInstance(playerObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            playerObject.name = "Player cutter";
            playerObject.transform.SetPositionAndRotation(dock, Quaternion.identity);
            var simulation = playerObject.GetComponent<ShipSimulation>();
            var input = playerObject.AddComponent<ShipKeyboardMouse>(); input.simulation = simulation; input.aimCamera = camera;
            var target = playerObject.AddComponent<CombatTarget>(); target.motor = simulation.motor;
            var interaction = playerObject.AddComponent<SalvageInteraction>(); interaction.simulation = simulation; interaction.collectOnInteractIntent = false;
            interaction.range = 5.5f;
            AddWake(playerObject, kit, 1f);

            if (!camera.TryGetComponent<ShipFollowCamera>(out var follow)) follow = camera.gameObject.AddComponent<ShipFollowCamera>();
            follow.target = playerObject.transform;
            camera.transform.SetPositionAndRotation(dock + follow.offset, Quaternion.Euler(60, 0, 0));
            camera.fieldOfView = 50;

            var effects = new GameObject("Combat presentation").AddComponent<CombatPresenter>();
            effects.friendlyShot = kit.FriendlyShot; effects.hostileShot = kit.HostileShot; effects.flash = kit.Flash;
            effects.smoke = kit.Smoke; effects.splash = kit.Splash; effects.braceRing = kit.Brace;
            var aim = new GameObject("Aim marker").AddComponent<AimMarker>();
            aim.ringMaterial = kit.Amber; aim.lineMaterial = kit.AimLine;
            var audio = new GameObject("Audio").AddComponent<GameAudio>();
            if (!camera.TryGetComponent<AudioListener>(out _)) camera.gameObject.AddComponent<AudioListener>();
            var skies = new GameObject("Weather").AddComponent<SeaConditions>();
            skies.sun = sun; skies.volume = volume; skies.ocean = ocean;

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/_Game/UI/Harbor/PanelSettings.asset");
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/_Game/UI/Game/Game.uss");
            var ui = new GameObject("UI").transform;
            var hudObject = new GameObject("HUD"); hudObject.transform.SetParent(ui);
            var hudDocument = hudObject.AddComponent<UIDocument>(); hudDocument.panelSettings = panel; hudDocument.sortingOrder = 0;
            var hud = hudObject.AddComponent<GameHud>(); hud.stylesheet = style;
            var harborObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/UI/Harbor/Harbor.prefab"));
            harborObject.transform.SetParent(ui);
            harborObject.GetComponent<UIDocument>().sortingOrder = 1;
            var menuObject = new GameObject("Menus"); menuObject.transform.SetParent(ui);
            var menuDocument = menuObject.AddComponent<UIDocument>(); menuDocument.panelSettings = panel; menuDocument.sortingOrder = 2;
            var menus = menuObject.AddComponent<GameMenus>(); menus.stylesheet = style;

            var director = new GameObject("Game director").AddComponent<GameDirector>();
            director.rules = rules; director.combatContent = combat; director.region = region;
            director.homeHub = region.homeId;
            director.player = simulation; director.playerInput = input; director.playerTarget = target; director.interaction = interaction;
            director.salvage = salvageRoot; director.raiderPrefab = raider; director.gunnerPrefab = gunner;
            director.followCamera = follow; director.worldCamera = camera; director.presenter = effects; director.aimMarker = aim;
            director.sound = audio; director.weather = skies;
            director.harbor = harborObject.GetComponent<HarborView>(); director.hud = hud; director.menus = menus;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureEnvironment(Light sun, Volume volume, WaterSurface ocean)
        {
            // Bake T02's approved daylight condition into the production scene.
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            sun.color = new Color(1, 0.96f, 0.86f);
            sun.intensity = 100000;
            string path = Content + "OceanWorldVolume.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = Object.Instantiate(volume.sharedProfile);
                profile.components = profile.components.Select(Object.Instantiate).ToList();
                AssetDatabase.CreateAsset(profile, path);
                foreach (var component in profile.components) { component.name = component.name.Replace("(Clone)", ""); AssetDatabase.AddObjectToAsset(component, profile); }
            }
            if (profile.TryGet<Exposure>(out var exposure)) exposure.fixedExposure.Override(12);
            if (profile.TryGet<GradientSky>(out var sky))
            {
                sky.exposure.Override(13);
                sky.top.Override(new Color(0.18f, 0.38f, 0.65f));
            }
            EditorUtility.SetDirty(profile);
            volume.sharedProfile = profile;
            ocean.largeWindSpeed = 22; ocean.largeBand0Multiplier = 0.12f; ocean.largeBand1Multiplier = 0.2f; ocean.simulationFoamAmount = 0.15f;
            ocean.decalRegionSize = new Vector2(180, 180);
        }

        private static GameObject Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material,
            PrimitiveType type = PrimitiveType.Cube, Mesh mesh = null, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            if (mesh != null) go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        // T02 island look (faceted shelf, shore and crown, scattered rocks, shore foam),
        // sized so its waterline matches the authored collision footprint.
        private static void BuildIsland(Transform parent, string name, Vector2 center, Vector2 size, Kit kit, int seed, bool lookout)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent); root.position = new Vector3(center.x, 0, center.y);
            var shore = new Vector3(size.x / 0.76f, 3, size.y / 0.76f);
            Part("Submerged shelf", root, new Vector3(0, -1.7f, 0), new Vector3(shore.x * 1.22f, 2, shore.z * 1.2f), kit.Sand, PrimitiveType.Cube, kit.Shelf);
            Part("Shore", root, new Vector3(0, -0.2f, 0), shore, kit.Sand, PrimitiveType.Cube, kit.Shore);
            Part("Grassy crown", root, new Vector3(-shore.x * 0.03f, 0.7f, shore.z * 0.03f), new Vector3(shore.x * 0.82f, 4, shore.z * 0.8f), kit.Grass, PrimitiveType.Cube, kit.Crown);
            var random = new System.Random(seed);
            int rocks = Mathf.Clamp(Mathf.RoundToInt((size.x + size.y) / 5), 5, 10);
            for (int i = 0; i < rocks; i++)
            {
                float a = i * 2.399f + (float)random.NextDouble() * 0.4f;
                var p = new Vector3(Mathf.Cos(a) * size.x * 0.42f, 1.3f, Mathf.Sin(a) * size.y * 0.42f);
                var s = new Vector3(2.2f + (float)random.NextDouble() * 2, 2.2f + (float)random.NextDouble() * 1.8f, 2.4f + (float)random.NextDouble() * 1.5f);
                Part("Rock " + i, root, p, s, kit.Rock, rotation: Quaternion.Euler(12 * i, 31 * i + seed * 17, 15));
            }
            if (lookout)
            {
                var spot = new Vector3(size.x * (random.NextDouble() > 0.5 ? 0.12f : -0.14f), 0, size.y * 0.1f);
                Part("Lookout", root, spot + new Vector3(0, 3.6f, 0), new Vector3(2.6f, 5, 2.6f), kit.Rock, PrimitiveType.Cylinder);
                Part("Lookout roof", root, spot + new Vector3(0, 6.5f, 0), new Vector3(3.6f, 0.5f, 3.6f), kit.Timber);
            }
            // Collision: the authored footprint, matching the validated T06 route clearances.
            var collision = new GameObject("Shore collision");
            collision.transform.SetParent(root, false);
            collision.transform.localPosition = new Vector3(0, 0.4f, 0);
            collision.transform.localScale = new Vector3(size.x, 2, size.y);
            var cylinder = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            collision.AddComponent<MeshCollider>().sharedMesh = cylinder;
            FoamRing(root, size, kit);
        }

        private static void FoamRing(Transform root, Vector2 size, Kit kit)
        {
            float rx = size.x * 0.5f * 1.08f, rz = size.y * 0.5f * 1.08f;
            float perimeter = Mathf.PI * (3 * (rx + rz) - Mathf.Sqrt((3 * rx + rz) * (rx + 3 * rz)));
            int count = Mathf.Clamp(Mathf.RoundToInt(perimeter / 3.2f), 12, 32);
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                var go = new GameObject("Shore foam " + i);
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(Mathf.Cos(a) * rx, 0, Mathf.Sin(a) * rz);
                go.transform.localRotation = Quaternion.Euler(0, -90 - a * Mathf.Rad2Deg, 0);
                var decal = go.AddComponent<WaterDecal>();
                decal.material = kit.Foam; decal.regionSize = new Vector2(4.2f, 2.2f); decal.surfaceFoamDimmer = 1; decal.deepFoamDimmer = 0;
            }
        }

        // A harbor is a landmass with a pier running out to its berth. Layout is
        // expressed relative to the landmass and mirrored so the waterfront faces the dock.
        private static Transform BuildHarbor(Transform parent, string name, Vector2 landmass, Vector2 size, Vector2 dock, Kit kit, Material flag, int seed)
        {
            BuildIsland(parent, name + " landmass", landmass, size, kit, seed, false);
            var harbor = new GameObject(name).transform; harbor.SetParent(parent);
            float front = dock.y >= landmass.y ? 1 : -1, side = dock.x >= landmass.x ? 1 : -1;
            Vector3 At(float dx, float y, float dz) => new Vector3(landmass.x + dx * side, y, landmass.y + dz * front);
            float pierX = dock.x - 7 * side;
            float pierStart = landmass.y + front * (size.y * 0.5f - 1), pierEnd = dock.y + front * 0.2f;
            int planks = Mathf.Max(4, Mathf.RoundToInt(Mathf.Abs(pierEnd - pierStart) / 0.62f));
            for (int i = 0; i < planks; i++)
                Part("Pier plank", harbor, new Vector3(pierX, 0.95f, Mathf.Lerp(pierStart, pierEnd, (i + 0.5f) / planks)), new Vector3(2.6f, 0.2f, 0.58f), kit.Deck);
            for (int i = 0; i < 4; i++)
                foreach (float x in new[] { -1.1f, 1.1f })
                    Part("Pier piling", harbor, new Vector3(pierX + x, 0, Mathf.Lerp(pierStart + front * 0.6f, pierEnd - front * 0.6f, i / 3f)),
                        new Vector3(0.28f, 3.2f, 0.28f), kit.Timber, PrimitiveType.Cylinder);
            var pierCollision = new GameObject("Pier collision");
            pierCollision.transform.SetParent(harbor, false);
            pierCollision.transform.localPosition = new Vector3(pierX, 0.5f, (pierStart + pierEnd) * 0.5f);
            pierCollision.AddComponent<BoxCollider>().size = new Vector3(2.8f, 1.4f, Mathf.Abs(pierEnd - pierStart) + 0.6f);
            var lampAt = new Vector3(pierX + 1.1f * side, 0, dock.y - front * 0.6f);
            Part("Harbor lamp post", harbor, lampAt + Vector3.up * 2.4f, new Vector3(0.18f, 3, 0.18f), kit.Iron);
            Part("Lantern", harbor, lampAt + Vector3.up * 3.95f, new Vector3(0.55f, 0.7f, 0.55f), kit.Lamp);
            Part("Crate", harbor, new Vector3(pierX - 0.6f * side, 1.45f, pierStart + front * 2.2f), new Vector3(0.9f, 0.8f, 0.9f), kit.Deck, rotation: Quaternion.Euler(0, 20, 0));
            Part("Crate", harbor, new Vector3(pierX + 0.5f * side, 1.4f, pierStart + front * 3.1f), new Vector3(0.7f, 0.7f, 0.7f), kit.Deck, rotation: Quaternion.Euler(0, -12, 0));
            Part("Mooring barrel", harbor, new Vector3(pierX - 0.8f * side, 1.45f, pierEnd - front * 2.1f), new Vector3(0.7f, 0.45f, 0.7f), kit.Timber, PrimitiveType.Cylinder);
            // Waterfront: storehouse, cottages and an awning above the shore.
            float turn = front > 0 ? 0 : 180;
            Building(harbor, "Storehouse", At(-1, 2.2f, 3.5f), new Vector3(8, 3.2f, 5), kit.Deck, kit.Roof, turn);
            Building(harbor, "Harbor master", At(6.5f, 2.2f, 2), new Vector3(4.2f, 2.8f, 3.6f), kit.Plaster, kit.Roof, turn + 8 * side);
            Building(harbor, "Cottage", At(-8, 2.3f, -1), new Vector3(3.6f, 2.6f, 3.4f), kit.Plaster, kit.Roof, turn - 14 * side);
            Building(harbor, "Cottage", At(-4.5f, 2.5f, -5), new Vector3(3.2f, 2.4f, 3.2f), kit.Plaster, kit.Roof, turn + 21 * side);
            Part("Awning", harbor, At(3.8f, 3.1f, 6.2f), new Vector3(3.2f, 0.12f, 2.2f), kit.Cloth, rotation: Quaternion.Euler(-12 * front, 0, 0));
            Part("Awning post", harbor, At(2.4f, 2.2f, 7.1f), new Vector3(0.12f, 1.8f, 0.12f), kit.Timber);
            Part("Awning post", harbor, At(5.2f, 2.2f, 7.1f), new Vector3(0.12f, 1.8f, 0.12f), kit.Timber);
            Part("Flag pole", harbor, At(9.8f, 4.2f, 4.6f), new Vector3(0.12f, 4.4f, 0.12f), kit.Iron);
            Part("Flag", harbor, At(10.45f, 6.1f, 4.6f), new Vector3(1.2f, 0.7f, 0.05f), flag);
            return harbor;
        }

        private static void Building(Transform parent, string name, Vector3 position, Vector3 size, Material wall, Material roof, float yaw)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false);
            root.localPosition = position; root.localRotation = Quaternion.Euler(0, yaw, 0);
            Part("Walls", root, Vector3.zero, size, wall);
            // Two sloped slabs form a simple gable.
            float half = size.x * 0.5f;
            var slab = new Vector3(size.x * 0.62f, 0.18f, size.z * 1.12f);
            Part("Roof", root, new Vector3(-half * 0.5f, size.y * 0.5f + half * 0.28f, 0), slab, roof, rotation: Quaternion.Euler(0, 0, 32));
            Part("Roof", root, new Vector3(half * 0.5f, size.y * 0.5f + half * 0.28f, 0), slab, roof, rotation: Quaternion.Euler(0, 0, -32));
            Part("Door", root, new Vector3(0, -size.y * 0.5f + 0.8f, size.z * 0.5f + 0.02f), new Vector3(0.8f, 1.6f, 0.05f), roof);
        }

        private static Vector3 LampLight(Vector2 landmass, Vector2 dock)
        {
            float front = dock.y >= landmass.y ? 1 : -1, side = dock.x >= landmass.x ? 1 : -1;
            return new Vector3(dock.x - 7 * side + 1.1f * side + 0.2f * side, 3.9f, dock.y - front * 0.6f);
        }

        private static void DockRing(Transform parent, Vector3 center, float radius, Kit kit)
        {
            var ring = new GameObject("Dock zone ring").AddComponent<LineRenderer>();
            ring.transform.SetParent(parent, false);
            ring.transform.position = center + Vector3.up * 0.35f;
            ring.useWorldSpace = false; ring.loop = true; ring.widthMultiplier = 0.14f; ring.positionCount = 64;
            ring.sharedMaterial = kit.DockRing; ring.shadowCastingMode = ShadowCastingMode.Off;
            for (int i = 0; i < 64; i++) { float a = i * Mathf.PI * 2 / 64; ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius)); }
            // Four buoys mark the berth at a glance.
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI * 0.25f + i * Mathf.PI * 0.5f;
                var buoy = Part("Berth buoy", parent, center + new Vector3(Mathf.Cos(a) * (radius + 0.6f), 0.2f, Mathf.Sin(a) * (radius + 0.6f)),
                    new Vector3(0.5f, 0.45f, 0.5f), kit.Amber, PrimitiveType.Cylinder);
                buoy.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        // ------------------------------------------------------------ build

        private static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[] { BootstrapScene, WorldScene, OceanTestScene }
                .Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        }

        public static void Validate()
        {
            var scene = EditorSceneManager.OpenScene(WorldScene, OpenSceneMode.Single);
            var director = Object.FindFirstObjectByType<GameDirector>();
            if (director == null || director.rules == null || director.combatContent == null || director.player == null || director.harbor == null ||
                director.hud == null || director.menus == null || director.raiderPrefab == null || director.gunnerPrefab == null)
                throw new BuildFailedException("OceanWorld composition is incomplete.");
            if (Object.FindObjectsByType<GameDirector>(FindObjectsSortMode.None).Length != 1)
                throw new BuildFailedException("OceanWorld must contain exactly one campaign owner.");
            if (Object.FindFirstObjectByType<WaterSurface>() == null) throw new BuildFailedException("OceanWorld has no water surface.");
            var definitions = director.rules.Freeze();
            director.region.Validate(definitions);
            director.combatContent.Freeze();
            if (director.combatContent.rules != director.rules) throw new BuildFailedException("Combat content must use the production rule catalog.");
            if (Mathf.Abs(director.followCamera.transform.eulerAngles.x - 60) > 0.01f) throw new BuildFailedException("D05 camera pitch changed.");
            Debug.Log("OceanWorld validation passed.");
        }

        [MenuItem("Pirate Game/Build Game (Windows)")]
        public static void BuildGame()
        {
            Validate();
            string output = LaunchOptions.Argument("-build-output") ?? "Builds/Game/PiratePrototype.exe";
            bool development = LaunchOptions.Flag("-development");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None
            });
            Debug.Log("Game build: " + report.summary.result + "; errors=" + report.summary.totalErrors + "; bytes=" + report.summary.totalSize + "; output=" + output);
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Game build failed.");
        }
    }
}
