using UnityEngine;

namespace PirateGame.Presentation.Combat
{
    // Attack marker on the sea plane: a ring at the aim point and a faint line
    // from the muzzle, clamped to weapon range. Visual only; aim is resolved by
    // the input adapter and motor.
    public sealed class AimMarker : MonoBehaviour
    {
        public Material ringMaterial, lineMaterial;
        public float radius = 1.3f;
        private LineRenderer ring, line;

        private void Awake()
        {
            ring = Create("Aim ring", ringMaterial, 0.16f, 40, true);
            for (int i = 0; i < 40; i++) { float a = i * Mathf.PI * 2 / 40; ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius)); }
            line = Create("Aim line", lineMaterial != null ? lineMaterial : ringMaterial, 0.06f, 2, false);
            Hide();
        }

        private LineRenderer Create(string name, Material material, float width, int count, bool loop)
        {
            var renderer = new GameObject(name).AddComponent<LineRenderer>();
            renderer.transform.SetParent(transform, false);
            renderer.sharedMaterial = material; renderer.widthMultiplier = width; renderer.positionCount = count;
            renderer.loop = loop; renderer.useWorldSpace = !loop;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return renderer;
        }

        public void Show(Vector3 origin, Vector3 point, float range)
        {
            origin.y = 0; point.y = 0;
            var offset = point - origin;
            if (offset.magnitude > range && range > 0) point = origin + offset.normalized * range;
            ring.gameObject.SetActive(true); line.gameObject.SetActive(true);
            ring.transform.position = point + Vector3.up * 0.9f;
            line.SetPosition(0, origin + Vector3.up * 0.9f + (point - origin).normalized * 2.6f);
            line.SetPosition(1, point + Vector3.up * 0.9f - (point - origin).normalized * radius);
        }

        public void Hide()
        {
            if (ring != null) ring.gameObject.SetActive(false);
            if (line != null) line.gameObject.SetActive(false);
        }
    }
}
