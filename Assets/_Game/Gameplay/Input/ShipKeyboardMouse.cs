using PirateGame.Gameplay.Ships;
using PirateGame.Rules.Application;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PirateGame.Gameplay.Input
{
    public sealed class ShipKeyboardMouse : MonoBehaviour
    {
        public ShipSimulation simulation;
        public Camera aimCamera;
        private InputActionMap map;
        private InputAction throttle, turn, brake, fire, ability, interact, pointer;
        private bool focused = true;

        private void Awake()
        {
            map = new InputActionMap("T04 Ship");
            throttle = map.AddAction("Throttle", InputActionType.Value);
            throttle.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/s").With("Positive", "<Keyboard>/w");
            turn = map.AddAction("Turn", InputActionType.Value);
            turn.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            brake = map.AddAction("Brake", InputActionType.Button, "<Keyboard>/space");
            fire = map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            ability = map.AddAction("Ability", InputActionType.Button, "<Mouse>/rightButton");
            interact = map.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            pointer = map.AddAction("Aim", InputActionType.Value, "<Mouse>/position");
        }
        private void OnEnable() => map.Enable();
        private void OnDisable() { map.Disable(); if (simulation != null) simulation.ClearInput(); }
        private void OnDestroy() => map.Dispose();
        private void OnApplicationFocus(bool value) { focused = value; if (!value) simulation.ClearInput(); }
        private void Update()
        {
            if (!focused || simulation.Session == null || simulation.Session.IsPaused)
            { simulation.Submit(default, false); return; }
            Vector3 aim = Vector3.zero;
            if (aimCamera != null && Mouse.current != null)
            {
                var ray = aimCamera.ScreenPointToRay(pointer.ReadValue<Vector2>());
                var plane = new Plane(Vector3.up, simulation.motor.Body.position);
                if (plane.Raycast(ray, out var distance))
                    aim = Vector3.ProjectOnPlane(ray.GetPoint(distance) - simulation.motor.weaponOrigin.position, Vector3.up).normalized;
            }
            simulation.Submit(new InputIntent(throttle.ReadValue<float>(), turn.ReadValue<float>(), aim.x, aim.z,
                fire.IsPressed(), ability.WasPressedThisFrame(), interact.WasPressedThisFrame()), brake.IsPressed());
        }
    }
}
