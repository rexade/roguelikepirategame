using NUnit.Framework;
using PirateGame.Presentation.Cameras;
using PirateGame.Presentation.World;
using UnityEngine;

namespace PirateGame.Tests.T12
{
    public sealed class PresentationTests
    {
        private AtmosphereProfile shallows, causeway, deeps;

        [SetUp] public void Setup()
        {
            shallows = Profile("Glass-clear shallows", 0, 10, 20);
            causeway = Profile("Golden haze", 580, 30, 12);
            deeps = Profile("Sea mist", 1150, 50, 5);
        }

        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(shallows); Object.DestroyImmediate(causeway); Object.DestroyImmediate(deeps);
        }

        [Test] public void AtmosphereClampsOutsideAndBlendsBetweenAnchors()
        {
            var profiles = new[] { deeps, shallows, causeway };   // unsorted on purpose
            Assert.AreEqual(10, ZoneAtmosphere.Evaluate(profiles, -500, out var first).sunPitch);
            Assert.AreSame(shallows, first);
            Assert.AreEqual(50, ZoneAtmosphere.Evaluate(profiles, 5000, out var last).sunPitch);
            Assert.AreSame(deeps, last);
            Assert.AreEqual(30, ZoneAtmosphere.Evaluate(profiles, 580, out _).sunPitch, 1e-4f);
            // Halfway between anchors the smoothstep weight is exactly one half.
            var mid = ZoneAtmosphere.Evaluate(profiles, 290, out var nearest);
            Assert.AreEqual(20, mid.sunPitch, 1e-3f);
            Assert.AreEqual(16, mid.absorption, 1e-3f);
            // A quarter of the way the look is still mostly the shallows (eased blend).
            Assert.Less(ZoneAtmosphere.Evaluate(profiles, 145, out _).sunPitch, 15);
            Assert.IsNotNull(nearest);
        }

        [Test] public void VeilThickensOnlyNearTheLimits()
        {
            var limits = new Rect(-300, -200, 1000, 1650);
            Assert.AreEqual(0, ZoneAtmosphere.VeilFactor(limits, new Vector3(0, 0, 500), 110));
            Assert.AreEqual(1, ZoneAtmosphere.VeilFactor(limits, new Vector3(-300, 0, 500), 110), 1e-4f);
            Assert.AreEqual(0.5f, ZoneAtmosphere.VeilFactor(limits, new Vector3(0, 0, 1450 - 55), 110), 1e-4f);
        }

        [Test] public void TacticalFrameReproducesTheD05Camera()
        {
            var pose = ShipFollowCamera.Frame(new Vector3(10, 0, 20), 0, 60, 56.58f);
            var offset = pose.position - new Vector3(10, 0, 20);
            Assert.AreEqual(49, offset.y, 0.05f);
            Assert.AreEqual(-28.29f, offset.z, 0.05f);
            Assert.AreEqual(0, offset.x, 1e-4f);
            Assert.AreEqual(60, pose.rotation.eulerAngles.x, 1e-3f);
        }

        [Test] public void VoyageFrameSitsBehindTheShipAlongItsHeading()
        {
            // Heading east (yaw 90): the camera is west of the ship, low, looking east.
            var pose = ShipFollowCamera.Frame(Vector3.zero, 90, 28, 30, 12, 2);
            Assert.Less(pose.position.x, -10);
            Assert.AreEqual(0, pose.position.z, 1e-3f);
            Assert.Greater(pose.position.y, 5);
            Assert.Less(pose.position.y, 20);
            Assert.Greater(Vector3.Dot(pose.rotation * Vector3.forward, Vector3.right), 0.8f);
        }

        private static AtmosphereProfile Profile(string mood, float z, float pitch, float absorption)
        {
            var p = ScriptableObject.CreateInstance<AtmosphereProfile>();
            p.mood = mood; p.anchorZ = z;
            p.look.sunPitch = pitch; p.look.absorption = absorption; p.look.sunColor = Color.white;
            return p;
        }
    }
}
