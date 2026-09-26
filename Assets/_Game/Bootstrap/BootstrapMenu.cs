using System;
using System.IO;
using PirateGame.Composition;
using PirateGame.Persistence;
using PirateGame.UI.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PirateGame.Bootstrap
{
    // Title menu in the Bootstrap scene, and the Back button in the T02 ocean test.
    // The Drowned Sun look (fonts, parchment) comes from a Resources stylesheet so
    // the scene needs no extra wiring; without it the menu still works unstyled.
    [RequireComponent(typeof(UIDocument))]
    public sealed class BootstrapMenu : MonoBehaviour
    {
        public const string StyleResource = "DrownedSun/Title";
        private Button startOver;
        private bool confirming;

        private void OnEnable()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            var style = Resources.Load<StyleSheet>(StyleResource);
            if (style != null) root.styleSheets.Add(style);
            bool ocean = SceneManager.GetActiveScene().name == "WaterTest";
            if (ocean) { BuildOceanTest(root); return; }

            var page = Element("title-root", root);
            page.pickingMode = PickingMode.Ignore;
            var card = Element("title-card", page);
            card.Add(new SunLogo());
            card.Add(Text("THE DROWNED SUN", "game-title"));
            card.Add(new SunRule());
            card.Add(Text("Relight the beacons of drowned Aurelia.", "tagline"));

            Button first = null;
            if (LaunchOptions.HasSaveFiles)
            {
                first = AddButton(card, "Continue voyage", () => Play(LaunchMode.Continue), true);
                var saved = LastSaved();
                if (saved != null) card.Add(Text(saved, "title-note"));
            }
            startOver = AddButton(card, "New campaign", StartOver, first == null);
            if (first == null) first = startOver;
            AddButton(card, "Ocean test", () => SceneManager.LoadScene("WaterTest"), false);
            AddButton(card, "Quit", Application.Quit, false);
            card.Add(Text("WASD sail  ·  mouse aims, left fires, right braces  ·  E salvage and moor  ·  M chart  ·  Esc pause", "title-footer"));
            root.schedule.Execute(() => first.Focus());
        }

        private void BuildOceanTest(VisualElement root)
        {
            var page = Element("ocean-root", root);
            var card = Element("ocean-card", page);
            card.Add(Text("Ocean", "ocean-title"));
            var back = AddButton(card, "Back", () => SceneManager.LoadScene("Bootstrap"), false);
            AddButton(card, "Quit", Application.Quit, false);
            root.schedule.Execute(() => back.Focus());
        }

        private static string LastSaved()
        {
            var path = Path.Combine(LaunchOptions.SaveDirectory, JsonSaveStore.MainName);
            if (!File.Exists(path)) return null;
            var age = DateTime.Now - File.GetLastWriteTime(path);
            string when = age.TotalMinutes < 1 ? "just now" : age.TotalHours < 1 ? (int)age.TotalMinutes + " min ago"
                : age.TotalDays < 1 ? (int)age.TotalHours + " h ago" : File.GetLastWriteTime(path).ToString("d MMM HH:mm");
            return "Last saved " + when;
        }

        private static VisualElement Element(string className, VisualElement parent)
        {
            var element = new VisualElement();
            element.AddToClassList(className);
            parent.Add(element);
            return element;
        }

        private static Label Text(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static Button AddButton(VisualElement parent, string text, Action action, bool primary)
        {
            var button = new Button(action) { text = text, name = text.ToLowerInvariant().Replace(' ', '-') };
            button.AddToClassList("title-button");
            if (primary) button.AddToClassList("primary");
            parent.Add(button);
            return button;
        }

        // Replacing an existing campaign needs a second press; the old files are kept aside.
        private void StartOver()
        {
            if (LaunchOptions.HasSaveFiles && !confirming)
            {
                confirming = true;
                startOver.text = "Start over? Press again (old save is kept)";
                return;
            }
            Play(LaunchMode.NewCampaign);
        }

        private static void Play(LaunchMode mode)
        {
            LaunchOptions.Mode = mode;
            SceneManager.LoadScene(LaunchOptions.WorldScene);
        }
    }
}
