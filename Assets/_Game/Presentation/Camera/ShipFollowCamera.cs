using UnityEngine;

namespace PirateGame.Presentation.Cameras
{
    public enum CameraFraming { Voyage, Approach, Tactical, Harbor }

    // Follows the player's ship. With `framings` off it is the fixed D05 camera
    // (60 degrees, north up). With framings on it blends three views:
    //   Voyage   - low, behind the ship, yaw following the heading (the horizon shows);
    //   Approach - 45 degrees and closer near a berth or salvage (Near);
    //   Tactical - the D05 60-degree view while enemies are close (Threat), with the
    //              yaw frozen so the view never spins mid-fight;
    // plus a Harbor shot of the beacon while docked. Presentation only.
    public sealed class ShipFollowCamera : MonoBehaviour
    {
        public Transform target;
        // D05 tactical offset (pitch 60, about 56.6 m); used as-is in fixed mode.
        public Vector3 offset = new Vector3(0, 49, -28.29f);
        [Min(0.01f)] public float followSharpness = 6;
        public bool framings;
        [Header("Voyage")] public float voyagePitch = 28, voyageDistance = 30, voyageLookAhead = 12, voyageLookHeight = 2;
        [Header("Approach")] public float approachPitch = 45, approachDistance = 34, approachLookAhead = 3;
        [Header("Harbor")] public float harborPitch = 16, harborDistance = 46, harborLookHeight = 9, harborOrbitDegrees = 9, harborOrbitSeconds = 50;
        [Min(0.05f)] public float blendSeconds = 1.1f;
        [Min(1)] public float yawDegreesPerSecond = 75;

        // Inputs from composition, each 0..1.
        public float Threat { get; set; }
        public float Near { get; set; }
        // Docked: the beacon to look at and the compass direction to look from.
        public Vector3? HarborFocus { get; set; }
        public float HarborYaw { get; set; }
        // Optional presentation-only focus (for example a sinking hull); the gameplay
        // target is untouched and resumes when the override is cleared.
        public Vector3? FocusOverride { get; set; }

        public float TacticalPitch => 60;
        public float TacticalDistance => offset.magnitude;
        public float Yaw => yaw;
        public CameraFraming Framing =>
            !framings ? CameraFraming.Tactical : harbor > 0.5f ? CameraFraming.Harbor : threat > 0.5f ? CameraFraming.Tactical : near > 0.5f ? CameraFraming.Approach : CameraFraming.Voyage;

        private float threat, near, harbor, yaw, clock;
        private Vector3 focus;
        private bool placed;

        // Camera pose looking at `focus` (plus a look-ahead along the yaw) from `distance`.
        public static Pose Frame(Vector3 focus, float yaw, float pitch, float distance, float lookAhead = 0, float lookHeight = 0)
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var ahead = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var lookAt = focus + ahead * lookAhead + Vector3.up * lookHeight;
            return new Pose(lookAt - rotation * Vector3.forward * distance, rotation);
        }

        public void Snap()
        {
            if (!framings)
            {
                if (target == null && FocusOverride == null) return;
                transform.SetPositionAndRotation(FixedGoal, Quaternion.Euler(60, 0, 0));
                return;
            }
            if (target == null && FocusOverride == null && HarborFocus == null) return;
            threat = FocusOverride != null ? 1 : Mathf.Clamp01(Threat);
            near = Mathf.Clamp01(Near);
            harbor = HarborFocus != null && FocusOverride == null ? 1 : 0;
            if (target != null && threat < 1) yaw = target.eulerAngles.y;
            focus = FocusOverride ?? (target != null ? target.position : focus);
            placed = true;
            Place(0);
        }

        private Vector3 FixedGoal => (FocusOverride ?? (target != null ? target.position : transform.position - offset)) + offset;

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (!framings)
            {
                if (target == null && FocusOverride == null) return;
                transform.position = Vector3.Lerp(transform.position, FixedGoal, 1 - Mathf.Exp(-followSharpness * dt));
                transform.rotation = Quaternion.Euler(60, 0, 0);
                return;
            }
            if (target == null && FocusOverride == null && HarborFocus == null) return;
            if (!placed) { Snap(); return; }
            float step = dt / blendSeconds;
            threat = Mathf.MoveTowards(threat, FocusOverride != null ? 1 : Mathf.Clamp01(Threat), step);
            near = Mathf.MoveTowards(near, Mathf.Clamp01(Near), step);
            harbor = Mathf.MoveTowards(harbor, HarborFocus != null && FocusOverride == null ? 1 : 0, step * 0.8f);
            var goal = FocusOverride ?? (target != null ? target.position : focus);
            focus = Vector3.Lerp(focus, goal, 1 - Mathf.Exp(-followSharpness * dt));
            if (target != null)
            {
                // The yaw eases toward the heading, ever slower as the threat rises.
                float heading = target.eulerAngles.y;
                float follow = 1 - Ease(threat);
                float eased = Mathf.LerpAngle(yaw, heading, 1 - Mathf.Exp(-1.6f * dt * follow));
                yaw = Mathf.MoveTowardsAngle(yaw, eased, yawDegreesPerSecond * follow * dt);
            }
            clock += dt;
            Place(clock);
        }

        private void Place(float time)
        {
            float t = Ease(threat), n = Ease(near), h = Ease(harbor);
            float pitch = Mathf.Lerp(Mathf.Lerp(voyagePitch, approachPitch, n), TacticalPitch, t);
            float distance = Mathf.Lerp(Mathf.Lerp(voyageDistance, approachDistance, n), TacticalDistance, t);
            float lookAhead = Mathf.Lerp(Mathf.Lerp(voyageLookAhead, approachLookAhead, n), 0, t);
            float lookHeight = Mathf.Lerp(voyageLookHeight, 0, Mathf.Max(n, t));
            var sea = Frame(focus, yaw, pitch, distance, lookAhead, lookHeight);
            if (h > 0 && HarborFocus != null)
            {
                float orbit = HarborYaw + Mathf.Sin(time / harborOrbitSeconds * Mathf.PI * 2) * harborOrbitDegrees;
                var shore = Frame(HarborFocus.Value, orbit, harborPitch, harborDistance, 0, harborLookHeight);
                transform.SetPositionAndRotation(Vector3.Lerp(sea.position, shore.position, h), Quaternion.Slerp(sea.rotation, shore.rotation, h));
                return;
            }
            transform.SetPositionAndRotation(sea.position, sea.rotation);
        }

        private static float Ease(float x) => x * x * (3 - 2 * x);
    }
}
