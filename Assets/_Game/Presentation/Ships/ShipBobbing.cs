using UnityEngine;

namespace PirateGame.Presentation.Ships
{
    public sealed class ShipBobbing : MonoBehaviour
    {
        [Range(0, 0.5f)] public float amplitude = 0.12f;
        private Vector3 origin;
        private Quaternion rotation;
        private void Awake() { origin = transform.localPosition; rotation = transform.localRotation; }
        private void LateUpdate()
        {
            var phase = Time.time;
            transform.localPosition = origin + Vector3.up * (Mathf.Sin(phase * 1.8f) * amplitude);
            transform.localRotation = rotation * Quaternion.Euler(Mathf.Sin(phase * 1.3f) * amplitude * 9, 0, Mathf.Sin(phase) * amplitude * 12);
        }
        private void OnDisable() { transform.localPosition = origin; transform.localRotation = rotation; }
    }
}
