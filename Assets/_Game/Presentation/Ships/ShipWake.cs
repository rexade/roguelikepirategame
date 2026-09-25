using PirateGame.Gameplay.Ships;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace PirateGame.Presentation.Ships
{
    // Visual-only stern foam: follows the motor's planar speed, never feeds back.
    public sealed class ShipWake : MonoBehaviour
    {
        public ShipMotor motor;
        public WaterDecal wake;
        [Range(0, 1)] public float strength = 1;
        [Min(0.01f)] public float response = 3;
        private float level;
        private void LateUpdate()
        {
            if (motor == null || wake == null || motor.Body == null) return;
            float target = motor.MaximumSpeed > 0 ? Mathf.Clamp01(motor.Speed / motor.MaximumSpeed) : 0;
            level = Mathf.MoveTowards(level, target, response * Time.deltaTime);
            wake.surfaceFoamDimmer = level * strength;
        }
    }
}
