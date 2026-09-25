using UnityEngine;

namespace PirateGame.Presentation.Cameras
{
    public sealed class ShipFollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 49, -28.29f);
        [Min(0.01f)] public float followSharpness = 6;
        // Optional presentation-only focus (for example a sinking hull); the gameplay
        // target is untouched and resumes when the override is cleared.
        public Vector3? FocusOverride { get; set; }
        private Vector3 Goal => (FocusOverride ?? (target != null ? target.position : transform.position - offset)) + offset;
        public void Snap()
        {
            if (target == null && FocusOverride == null) return;
            transform.SetPositionAndRotation(Goal, Quaternion.Euler(60, 0, 0));
        }
        private void LateUpdate()
        {
            if (target == null && FocusOverride == null) return;
            transform.position = Vector3.Lerp(transform.position, Goal, 1 - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime));
            transform.rotation = Quaternion.Euler(60, 0, 0);
        }
    }
}
