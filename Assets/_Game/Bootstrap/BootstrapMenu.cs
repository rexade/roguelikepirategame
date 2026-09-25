using System;
using System.IO;
using PirateGame.Composition;
using PirateGame.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PirateGame.Bootstrap
{
    // Title menu in the Bootstrap scene, and the Back button in the T02 ocean test.
    [RequireComponent(typeof(UIDocument))]
    public sealed class BootstrapMenu : MonoBehaviour
    {
        private static readonly Color Panel = new Color(0.055f, 0.1f, 0.11f, 0.9f), Edge = new Color(0.55f, 0.81f, 0.7f, 0.55f),
            Amber = new Color(0.92f, 0.79f, 0.44f), Pale = new Color(0.75f, 0.85f, 0.8f), Body = new Color(0.94f, 0.94f, 0.9f),
            ButtonFill = new Color(0.22f, 0.29f, 0.27f), Gold = new Color(0.76f, 0.64f, 0.31f), Focus = new Color(0.95f, 0.82f, 0.45f);
        private Button startOver;
        private bool confirming;

        private void OnEnable()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            bool ocean = SceneManager.GetActiveScene().name == "WaterTest";
            if (ocean) { BuildOceanTest(root); return; }

            root.style.flexGrow = 1;
            root.style.justifyContent = Justify.Center;
            root.style.alignItems = Align.Center;
            root.style.backgroundColor = new Color(0.02f, 0.06f, 0.08f, 0.35f);
            var card = new VisualElement();
            card.style.width = 520;
            card.style.paddingLeft = card.style.paddingRight = 38;
            card.style.paddingTop = 34; card.style.paddingBottom = 30;
            card.style.backgroundColor = Panel;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;
            SetBorder(card, Edge, 1);
            root.Add(card);

            card.Add(Text("PIRATE PROTOTYPE", 42, Amber, true, 3));
            card.Add(Text("Homeward Reach  ·  Galewater Reach", 17, Pale, false, 1));
            var tagline = Text("Salvage, fight and bank your plunder. Stronger ships reach more dangerous waters.", 15, Body, false, 0);
            tagline.style.whiteSpace = WhiteSpace.Normal;
            tagline.style.marginTop = 10; tagline.style.marginBottom = 18;
            card.Add(tagline);

            Button first = null;
            if (LaunchOptions.HasSaveFiles)
            {
                first = AddButton(card, "Continue voyage", () => Play(LaunchMode.Continue), true);
                var saved = LastSaved();
                if (saved != null)
                {
                    var note = Text(saved, 13, Pale, false, 0);
                    note.style.marginTop = 2;
                    card.Add(note);
                }
            }
            startOver = AddButton(card, "New campaign", StartOver, first == null);
            if (first == null) first = startOver;
            AddButton(card, "Ocean test", () => SceneManager.LoadScene("WaterTest"), false);
            AddButton(card, "Quit", Application.Quit, false);
            var footer = Text("WASD sail  ·  Mouse aim, LMB fire, RMB brace  ·  E salvage / dock  ·  M chart  ·  Esc pause", 12, Pale, false, 0);
            footer.style.marginTop = 18; footer.style.whiteSpace = WhiteSpace.Normal;
            card.Add(footer);
            root.schedule.Execute(() => first.Focus());
        }

        private void BuildOceanTest(VisualElement root)
        {
            root.style.paddingLeft = 24;
            root.style.paddingTop = 24;
            root.style.alignItems = Align.FlexStart;
            root.Add(Text("Ocean", 24, Color.white, false, 0));
            var back = AddButton(root, "Back", () => SceneManager.LoadScene("Bootstrap"), false);
            back.style.width = 180;
            var quit = AddButton(root, "Quit", Application.Quit, false);
            quit.style.width = 180;
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

        private static Label Text(string text, int size, Color color, bool bold, float spacing)
        {
            var label = new Label(text);
            label.style.fontSize = size;
            label.style.color = color;
            label.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            label.style.letterSpacing = spacing;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            return label;
        }

        private static Button AddButton(VisualElement parent, string text, Action action, bool primary)
        {
            var button = new Button(action) { text = text, name = text.ToLowerInvariant().Replace(' ', '-') };
            button.style.height = 46;
            button.style.marginTop = 10;
            button.style.fontSize = 18;
            button.style.backgroundColor = primary ? Gold : ButtonFill;
            button.style.color = primary ? new Color(0.1f, 0.12f, 0.09f) : Body;
            button.style.borderTopLeftRadius = button.style.borderTopRightRadius = button.style.borderBottomLeftRadius = button.style.borderBottomRightRadius = 4;
            SetBorder(button, new Color(0.41f, 0.53f, 0.47f), 1);
            // Visible keyboard focus: amber outline.
            button.RegisterCallback<FocusInEvent>(_ => SetBorder(button, Focus, 2));
            button.RegisterCallback<FocusOutEvent>(_ => SetBorder(button, new Color(0.41f, 0.53f, 0.47f), 1));
            parent.Add(button);
            return button;
        }

        private static void SetBorder(VisualElement element, Color color, float width)
        {
            element.style.borderTopColor = element.style.borderBottomColor = element.style.borderLeftColor = element.style.borderRightColor = color;
            element.style.borderTopWidth = element.style.borderBottomWidth = element.style.borderLeftWidth = element.style.borderRightWidth = width;
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
