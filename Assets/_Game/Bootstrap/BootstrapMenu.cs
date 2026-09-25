using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PirateGame.Bootstrap
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class BootstrapMenu : MonoBehaviour
    {
        private void OnEnable()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.style.paddingLeft = 24;
            root.style.paddingTop = 24;
            root.style.alignItems = Align.FlexStart;
            bool ocean = SceneManager.GetActiveScene().name == "WaterTest";
            var title = new Label(ocean ? "Ocean" : "Pirate Prototype");
            title.style.fontSize = 24;
            title.style.color = Color.white;
            root.Add(title);
            var open = new Button(() => SceneManager.LoadScene(ocean ? "Bootstrap" : "WaterTest"))
            {
                text = ocean ? "Back" : "Open ocean"
            };
            open.style.width = 180;
            open.style.height = 40;
            open.style.marginTop = 12;
            root.Add(open);
            var quit = new Button(() => Application.Quit()) { text = "Quit" };
            quit.style.width = 180;
            quit.style.height = 40;
            quit.style.marginTop = 8;
            root.Add(quit);
            root.schedule.Execute(() => open.Focus());
        }
    }
}
