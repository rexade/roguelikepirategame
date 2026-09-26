using UnityEngine;

namespace PirateGame.Presentation.World
{
    // A sun beacon's fire and light pillar. Composition sets it from the campaign's
    // hub state (lit = activated); relighting animates the pillar rising.
    public sealed class BeaconView : MonoBehaviour
    {
        public string hubId = "";
        public GameObject litParts, darkParts;
        public Transform pillar;
        public Light fireLight;
        [Min(0)] public float fireIntensity = 60000;
        [Min(0.1f)] public float relightSeconds = 2.8f;
        public bool Lit { get; private set; }
        private float progress = 1;

        public void SetLit(bool lit, bool animate)
        {
            Lit = lit;
            if (litParts != null) litParts.SetActive(lit);
            if (darkParts != null) darkParts.SetActive(!lit);
            progress = lit && animate ? 0 : 1;
            Show();
        }

        private void Update()
        {
            if (progress >= 1) return;
            progress = Mathf.Min(1, progress + Time.deltaTime / relightSeconds);
            Show();
        }

        private void Show()
        {
            float rise = Mathf.SmoothStep(0, 1, progress);
            if (pillar != null) pillar.localScale = new Vector3(Mathf.Lerp(2.2f, 1, rise), Mathf.Max(0.001f, rise), Mathf.Lerp(2.2f, 1, rise));
            if (fireLight != null) fireLight.intensity = Lit ? fireIntensity * Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress * 3)) : 0;
        }
    }
}
