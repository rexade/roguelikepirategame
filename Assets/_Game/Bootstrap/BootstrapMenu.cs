using PirateGame.Composition;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PirateGame.Bootstrap
{
    // Title menu in the Bootstrap scene, and the Back button in the T02 ocean test.
    [RequireComponent(typeof(UIDocument))]
    public sealed class BootstrapMenu : MonoBehaviour
    {
        private Button startOver;
        private bool confirming;

        private void OnEnable()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.style.paddingLeft = 48;
            root.style.paddingTop = 40;
            root.style.alignItems = Align.FlexStart;
            bool ocean = SceneManager.GetActiveScene().name == "WaterTest";
            var title = new Label(ocean ? "Ocean" : "Pirate Prototype");
            title.style.fontSize = ocean ? 24 : 42;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            root.Add(title);
            if (ocean)
            {
                var back = AddButton(root, "Back", () => SceneManager.LoadScene("Bootstrap"));
                AddButton(root, "Quit", Application.Quit);
                root.schedule.Execute(() => back.Focus());
                return;
            }
            var subtitle = new Label("Homeward Reach  ·  salvage, fight, bank your plunder");
            subtitle.style.fontSize = 18;
            subtitle.style.color = new Color(0.75f, 0.85f, 0.8f);
            subtitle.style.marginBottom = 18;
            root.Add(subtitle);
            Button first = null;
            if (LaunchOptions.HasSaveFiles)
                first = AddButton(root, "Continue voyage", () => Play(LaunchMode.Continue));
            startOver = AddButton(root, "New campaign", StartOver);
            if (first == null) first = startOver;
            AddButton(root, "Ocean test", () => SceneManager.LoadScene("WaterTest"));
            AddButton(root, "Quit", Application.Quit);
            root.schedule.Execute(() => first.Focus());
        }

        private static Button AddButton(VisualElement root, string text, System.Action action)
        {
            var button = new Button(action) { text = text, name = text.ToLowerInvariant().Replace(' ', '-') };
            button.style.width = 240;
            button.style.height = 42;
            button.style.marginTop = 10;
            button.style.fontSize = 17;
            root.Add(button);
            return button;
        }

        // Replacing an existing campaign needs a second press; the old files are kept aside.
        private void StartOver()
        {
            if (LaunchOptions.HasSaveFiles && !confirming)
            {
                confirming = true;
                startOver.text = "Start over? Press again";
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
