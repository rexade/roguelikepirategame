using UnityEngine;

namespace PirateGame.Presentation.World
{
    // The sparkle that marks relic gold: spins and pulses a small bright star.
    public sealed class Glint : MonoBehaviour
    {
        [Min(0)] public float spinDegrees = 70;
        [Min(0.01f)] public float period = 1.6f;
        [Range(0, 1)] public float pulse = 0.45f;
        private Vector3 baseScale;
        private float phase;

        private void Awake()
        {
            baseScale = transform.localScale;
            phase = Mathf.Abs(transform.position.x * 0.71f + transform.position.z * 0.29f) % period;
        }

        private void Update()
        {
            transform.Rotate(0, spinDegrees * Time.deltaTime, 0, Space.World);
            float wave = Mathf.Sin((Time.time + phase) / period * Mathf.PI * 2) * 0.5f + 0.5f;
            transform.localScale = baseScale * (1 - pulse + pulse * wave * wave);
        }
    }
}
