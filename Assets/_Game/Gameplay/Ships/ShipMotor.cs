using System;
using System.Collections.Generic;
using PirateGame.Rules.Application;
using UnityEngine;

namespace PirateGame.Gameplay.Ships
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class ShipMotor : MonoBehaviour
    {
        public Transform weaponOrigin;
        public Transform interactionOrigin;
        [Min(0.01f)] public float acceleration = 4;
        [Min(0.01f)] public float braking = 9;
        [Min(0.01f)] public float turnRate = 65;
        [Min(0.01f)] public float coastDrag = 0.5f;
        public Rigidbody Body { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public float Speed => suspended ? new Vector2(suspendedVelocity.x, suspendedVelocity.z).magnitude
            : new Vector2(Body.linearVelocity.x, Body.linearVelocity.z).magnitude;
        private float maximumSpeed = 8;
        private Vector3 suspendedVelocity;
        private Vector3 suspendedAngularVelocity;
        private bool suspended;
        private RigidbodyInterpolation suspendedInterpolation;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.useGravity = false;
            Body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.linearDamping = 0;
            Body.angularDamping = 0;
        }

        public void Configure(IReadOnlyDictionary<string, double> hullStats)
        {
            if (!hullStats.TryGetValue("speed", out var speed) || double.IsNaN(speed) || double.IsInfinity(speed) || speed <= 0 || speed > float.MaxValue)
                throw new ArgumentException("Ship requires a finite positive computed hull speed stat.");
            maximumSpeed = (float)speed;
        }

        public void Suspend(bool value)
        {
            if (suspended == value) return;
            suspended = value;
            if (value)
            {
                var position = Body.position;
                var rotation = Body.rotation;
                suspendedVelocity = Body.linearVelocity;
                suspendedAngularVelocity = Body.angularVelocity;
                suspendedInterpolation = Body.interpolation;
                // Inactive interpolated bodies can otherwise restore a stale render pose.
                Body.interpolation = RigidbodyInterpolation.None;
                Body.transform.SetPositionAndRotation(position, rotation);
                Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                Body.isKinematic = true;
            }
            else
            {
                Body.isKinematic = false;
                Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                Body.linearVelocity = suspendedVelocity;
                Body.angularVelocity = suspendedAngularVelocity;
                Body.interpolation = suspendedInterpolation;
            }
        }

        public void Step(InputIntent intent, bool brake, float delta)
        {
            if (suspended) return;
            var aim = new Vector3((float)intent.AimX, 0, (float)intent.AimZ);
            if (aim.sqrMagnitude > 0.000001f) AimDirection = aim.normalized;
            var forward = Body.rotation * Vector3.forward;
            var velocity = Body.linearVelocity;
            var target = brake ? Vector3.zero : forward * ((float)intent.Throttle * maximumSpeed);
            var rate = brake ? braking : Math.Abs(intent.Throttle) > 0 ? acceleration : coastDrag;
            Body.linearVelocity = Vector3.MoveTowards(velocity, target, rate * delta);
            Body.angularVelocity = Vector3.up * ((float)intent.Turn * turnRate * Mathf.Deg2Rad);
        }
    }
}
