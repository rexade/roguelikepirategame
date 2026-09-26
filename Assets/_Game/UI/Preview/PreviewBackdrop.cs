using UnityEngine;
using UnityEngine.UIElements;

namespace PirateGame.UI.Preview
{
    // Full-screen stand-in vista behind the UI preview (the game shows the live world).
    [RequireComponent(typeof(UIDocument))]
    public sealed class PreviewBackdrop : MonoBehaviour
    {
        public Texture2D image;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;
            root.style.flexGrow = 1;
            root.pickingMode = PickingMode.Ignore;
            if (image != null) root.style.backgroundImage = image;
        }
    }
}
