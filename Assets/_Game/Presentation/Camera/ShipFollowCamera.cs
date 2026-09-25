using UnityEngine;

namespace PirateGame.Presentation.Cameras
{
    public sealed class ShipFollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 49, -28.29f);
        [Min(0.01f)] public float followSharpness = 6;
        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.Lerp(transform.position, target.position + offset, 1 - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime));
            transform.rotation = Quaternion.Euler(60, 0, 0);
        }
    }
}
