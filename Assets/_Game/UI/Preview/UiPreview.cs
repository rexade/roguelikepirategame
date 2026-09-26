using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PirateGame.Core;
using PirateGame.Rules.Application;
using PirateGame.UI.Game;
using PirateGame.UI.Harbor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace PirateGame.UI.Preview
{
    // UI playground for the Drowned Sun reskin: every themed panel with realistic
    // fake data over a stand-in vista, no world required. Keys 1-8 switch panels;
    // `-capture <dir>` photographs each one and quits (automation).
    public sealed class UiPreview : MonoBehaviour
    {
        public GameObject title;
        public GameHud hud;
        public GameMenus menus;
        public SeaChart chart;
        public HarborView harbor;

        private CampaignSession session;
        private readonly Dictionary<string, string> names = new Dictionary<string, string>
        { ["home-harbor"] = "Dawnrest Beacon", ["saltmarsh-harbor"] = "Causeway Beacon", ["stormwatch-harbor"] = "Kingsfall Beacon" };

        private sealed class MemoryStore : ISaveStore { public RuleResult Commit(SaveCandidate candidate) => new RuleResult(); }
        private sealed class ReadyArrival : IWorldArrival { public RuleResult EnsureReady(ArrivalRequest request) => new RuleResult(); }

        private IEnumerator Start()
        {
            var catalog = Catalog();
            session = new CampaignSession(catalog, Campaign(catalog), new MemoryStore(), new ReadyArrival());
            session.RetryArrival();
            harbor.Bind(session, catalog, () => new EmbarkPlan(Guid.NewGuid(), 1, "preview", Array.Empty<EntityState>(), new Dictionary<string, string>()),
                id => names.TryGetValue(id, out var name) ? name : id);
            yield return null;
            Show(1);
            var output = Argument("-capture");
            if (output != null) yield return Capture(Path.GetFullPath(output));
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            for (int i = 1; i <= 8; i++)
                if (keyboard[(Key)((int)Key.Digit1 + i - 1)].wasPressedThisFrame) Show(i);
            if (keyboard.escapeKey.wasPressedThisFrame) Application.Quit();
        }

        private IEnumerator Capture(string output)
        {
            Directory.CreateDirectory(output);
            string[] shots = { "title", "hud", "zone-title", "card-sunk", "card-relit", "card-pause", "chart", "ledger" };
            for (int i = 0; i < shots.Length; i++)
            {
                Show(i + 1);
                for (float t = 0; t < 2.2f; t += Time.unscaledDeltaTime) yield return null;
                yield return new WaitForEndOfFrame();
                var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(output, (i + 1).ToString("00") + "-" + shots[i] + ".png"), texture.EncodeToPNG());
                Destroy(texture);
                Debug.Log("[ui-preview] shot " + shots[i] + " " + Screen.width + "x" + Screen.height);
            }
            Application.Quit();
        }

        // 1 title, 2 HUD + inscription, 3 zone title, 4-6 cards, 7 chart, 8 ledger.
        private void Show(int panel)
        {
            title.SetActive(panel == 1);
            hud.SetVisible(panel == 2 || panel == 3);
            menus.Hide();
            chart.Hide();
            harbor.Root.style.display = panel == 8 ? DisplayStyle.Flex : DisplayStyle.None;
            switch (panel)
            {
                case 2:
                    RenderHud();
                    hud.Toast("Salvaged +2 bronze", ToastKind.Gain, 30);
                    hud.Toast("Wreckers anchored by the Sunken Forum", ToastKind.Warning, 30);
                    hud.Inscription("THE DAWN WATCHER", "His gaze still turned to the dawn that drowned him.", 30);
                    break;
                case 3:
                    RenderHud();
                    hud.ZoneTitle("The Broken Causeway", "Golden haze over the fallen road of kings", 30);
                    break;
                case 4:
                    menus.Show(CardTone.Dire, "THE SEA CLAIMS YOU",
                        "Your cutter breaks apart on the black water. The crew is pulled from the sea and brought back to Dawnrest Beacon.",
                        new[] { "Lost cargo: 3 timber, 2 bronze.", "Your stores, the beacon works and your equipment are safe." },
                        new MenuAction("Return to the beacon", "continue", null, true));
                    break;
                case 5:
                    menus.Show(CardTone.Radiant, "BEACON RELIT", "Fire climbs the Causeway Beacon for the first time in three hundred years.",
                        new[] { "Moor beneath it to bank your hold and refit.", "Beacon Paths can now carry you here from any lit beacon." },
                        new MenuAction("Moor at the beacon", "continue", null, true), new MenuAction("Sail on", "sail", null));
                    break;
                case 6:
                    menus.Show("PAUSED", "Voyage saved.", null,
                        new MenuAction("Resume", "resume", null, true), new MenuAction("Save and exit to title", "exit", null),
                        new MenuAction("Quit game", "quit", null));
                    break;
                case 7:
                    chart.Show(ChartData());
                    break;
            }
        }

        private void RenderHud()
        {
            hud.Render(new HudModel
            {
                Health = 64, MaxHealth = 150, CargoUsed = 7, CargoCapacity = 15, CargoDetail = "3 timber, 2 bronze",
                WeaponName = "CANNON", WeaponCooldown = 0.4, WeaponCooldownMax = 1.1, AbilityName = "BRACE", AbilityCooldown = 0, AbilityCooldownMax = 6,
                Region = "THE GILDED SHALLOWS", Condition = "Glass-clear shallows under a high sun",
                Objective = "Relics glint among the ruins. Bring what you find to a lit beacon.",
                Prompt = "[E]  Salvage the relic  —  2 bronze"
            });
            hud.RenderBars(new[] {
                new HudMarker { Screen = new Vector2(Screen.width * 0.62f, Screen.height * 0.56f), Fill = 0.7f },
                new HudMarker { Screen = new Vector2(Screen.width * 0.7f, Screen.height * 0.48f), Fill = 0.3f } });
            hud.RenderHome(true, new Vector2(Screen.width * 0.05f, Screen.height * 0.42f), -100, "Dawnrest Beacon  212 m");
        }

        // The spec's map (2026-09-26 design, section 3), approximated for preview only;
        // composition fills the real model from content.
        private static ChartModel ChartData()
        {
            var data = new ChartModel { Limits = Rect.MinMaxRect(-300, -200, 700, 1450), ShipVisible = true, Ship = new Vector2(40, 210), ShipYaw = 30 };
            data.Zones.Add(new ChartModel.Zone { Name = "The Gilded Shallows", Center = new Vector2(-190, 40), Mood = "paradise, still" });
            data.Zones.Add(new ChartModel.Zone { Name = "The Broken Causeway", Center = new Vector2(560, 470), Mood = "golden haze" });
            data.Zones.Add(new ChartModel.Zone { Name = "The Colossus Deeps", Center = new Vector2(-80, 1300), Mood = "mist and old kings" });
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Shallows, Ring(new Vector2(0, 40), 150, 128, 28, 3, 0.05f)));
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Reef, Ring(new Vector2(0, 40), 132, 112, 40, 5, 0.03f)));
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Land, Ring(new Vector2(0, -52), 22, 14, 10, 7, 0.2f)));
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Land, Ring(new Vector2(-70, 60), 16, 10, 9, 9, 0.25f)));
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Land, Ring(new Vector2(95, 10), 12, 18, 9, 11, 0.25f)));
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Land, Ring(new Vector2(-110, 120), 14, 10, 8, 13, 0.3f)));
            // The causeway's shallow ridge and a few islets along it.
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Shallows, new[] { new Vector2(80, 400), new Vector2(130, 395), new Vector2(505, 700),
                new Vector2(500, 770), new Vector2(450, 760), new Vector2(75, 460) }));
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Land, Ring(new Vector2(300, 575), 18, 12, 9, 17, 0.2f)));
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Land, Ring(new Vector2(200, 520), 10, 8, 8, 19, 0.3f)));
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Land, Ring(new Vector2(430, 690), 12, 9, 8, 23, 0.3f)));
            // Kingsfall basin: obsidian crags in deep water.
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Reef, Ring(new Vector2(120, 1150), 120, 100, 36, 29, 0.08f)));
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Land, Ring(new Vector2(60, 1090), 16, 12, 8, 31, 0.3f)));
            data.Shapes.Add(Shape(ChartModel.ShapeKind.Land, Ring(new Vector2(210, 1210), 12, 10, 7, 37, 0.35f)));
            data.Road.AddRange(new[] { new Vector2(480, 760), new Vector2(400, 830), new Vector2(300, 910), new Vector2(200, 980), new Vector2(140, 1030) });
            data.Landmarks.Add(new ChartModel.Landmark { Name = "The Sun Gate", Position = new Vector2(0, 160), Kind = ChartModel.LandmarkKind.Gate });
            data.Landmarks.Add(new ChartModel.Landmark { Name = "The Dawn Watcher", Position = new Vector2(70, 80), Kind = ChartModel.LandmarkKind.Colossus });
            data.Landmarks.Add(new ChartModel.Landmark { Name = "The Offering Hand", Position = new Vector2(70, 300), Kind = ChartModel.LandmarkKind.Hand });
            data.Landmarks.Add(new ChartModel.Landmark { Name = "The Broken Causeway", Position = new Vector2(250, 600), Kind = ChartModel.LandmarkKind.Arches });
            data.Landmarks.Add(new ChartModel.Landmark { Name = "The Sun-Kings", Position = new Vector2(160, 1180), Kind = ChartModel.LandmarkKind.Colossus });
            data.Landmarks.Add(new ChartModel.Landmark { Name = "The Drowned Crown", Position = new Vector2(-150, 1700), Kind = ChartModel.LandmarkKind.Crown });
            data.Harbors.Add(new ChartModel.Harbor { Name = "Dawnrest Beacon", Position = new Vector2(0, -28), Claimed = true });
            data.Harbors.Add(new ChartModel.Harbor { Name = "Causeway Beacon", Position = new Vector2(300, 560), Claimed = true, Current = false });
            data.Harbors.Add(new ChartModel.Harbor { Name = "Kingsfall Beacon", Position = new Vector2(100, 1100) });
            return data;
        }

        private static ChartModel.Shape Shape(ChartModel.ShapeKind kind, Vector2[] points) => new ChartModel.Shape { Kind = kind, Points = points };

        private static Vector2[] Ring(Vector2 center, float rx, float rz, int sides, int seed, float jitter)
        {
            var random = new System.Random(seed);
            var points = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                float r = 1 + jitter * (float)(random.NextDouble() * 2 - 1);
                points[i] = center + new Vector2(Mathf.Cos(a) * rx * r, Mathf.Sin(a) * rz * r);
            }
            return points;
        }

        // Production-like rules (same IDs and numbers as WorldAuthoring.AuthorRules).
        private static DefinitionCatalog Catalog()
        {
            var none = Array.Empty<StatModifier>();
            UpgradeDefinition Upgrade(string id, string track, int tier, Dictionary<string, int> cost, string[] requires, string[] grants, params StatModifier[] mods) =>
                new UpgradeDefinition(id, track, tier, cost, requires, grants, mods);
            return new DefinitionCatalog(
                new Dictionary<string, int> { ["wood"] = 1, ["iron"] = 2 },
                new[] { new HullDefinition("cutter", new Dictionary<string, SlotKind> { ["weapon"] = SlotKind.Weapon, ["ability"] = SlotKind.Ability },
                    new[] { new StatDefinition("health", 100, 1, 1000), new StatDefinition("cargo", 10, 0, 1000), new StatDefinition("speed", 8, 1, 30),
                        new StatDefinition("damage-scale", 1, 0.1, 4) }) },
                new[] { new EquipmentDefinition("cannon", SlotKind.Weapon, none), new EquipmentDefinition("repeater", SlotKind.Weapon, none),
                    new EquipmentDefinition("brace", SlotKind.Ability, none) },
                new[] { new HubDefinition("home-harbor", new SeaPosition("gilded-shallows", 0, -28), 5, 1),
                    new HubDefinition("saltmarsh-harbor", new SeaPosition("broken-causeway", 300, 560), 5, 1),
                    new HubDefinition("stormwatch-harbor", new SeaPosition("colossus-deeps", 100, 1100), 5, 1) },
                new[] {
                    Upgrade("harbor-storehouse", "harbor", 1, new Dictionary<string, int> { ["wood"] = 5 }, new string[0], new[] { "storehouse" },
                        new StatModifier("cargo", ModifierOperation.Flat, 5)),
                    Upgrade("shipwright-slip", "harbor", 2, new Dictionary<string, int> { ["wood"] = 8, ["iron"] = 3 }, new[] { "storehouse" }, new string[0],
                        new StatModifier("speed", ModifierOperation.Percent, 0.15)),
                    Upgrade("reinforced-hull", "ship", 1, new Dictionary<string, int> { ["wood"] = 6, ["iron"] = 2 }, new string[0], new string[0],
                        new StatModifier("health", ModifierOperation.Percent, 0.5)),
                    Upgrade("iron-bound-guns", "ship", 2, new Dictionary<string, int> { ["wood"] = 8, ["iron"] = 4 }, new string[0], new string[0],
                        new StatModifier("damage-scale", ModifierOperation.Percent, 0.35)),
                    Upgrade("navigators-charts", "charts", 1, new Dictionary<string, int> { ["wood"] = 6, ["iron"] = 3 }, new string[0], new[] { "fast-travel" }) },
                new[] { "storehouse", "fast-travel" }, Array.Empty<AuthoredIdentity>(),
                new[] { "relic", "wreck", "raider", "gunner", "corsair" }, new[] { "gilded-shallows", "broken-causeway", "colossus-deeps" });
        }

        private static SessionSnapshot Campaign(DefinitionCatalog catalog)
        {
            var campaign = new CampaignState("cutter", "home-harbor", "home-harbor",
                new Dictionary<string, int> { ["wood"] = 12, ["iron"] = 5 }, new Dictionary<string, int> { ["harbor"] = 1, ["ship"] = 1, ["charts"] = 1 },
                new Dictionary<string, string> { ["cannon-1"] = "cannon", ["repeater-1"] = "repeater", ["brace-1"] = "brace" },
                new Dictionary<string, string> { ["weapon"] = "cannon-1", ["ability"] = "brace-1" },
                new Dictionary<string, HubState> { ["home-harbor"] = new HubState(true, true, new string[0]),
                    ["saltmarsh-harbor"] = new HubState(true, true, new string[0]), ["stormwatch-harbor"] = new HubState(false, false, new string[0]) },
                new[] { "storehouse", "fast-travel" }, new Dictionary<Guid, Outcome>());
            return new SessionSnapshot(3, campaign, null, Array.Empty<Guid>());
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
