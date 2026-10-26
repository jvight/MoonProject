using NUnit.Framework;
using UnityEngine;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>A wheel's radius is the mesh's own.</summary>
    public sealed class WheelKinematicsTests
    {
        [Test]
        public void TheSoftCap_IsExactUpToItsKnee_EasesTowardItsCeiling_AndIsOddAboutZero()
        {
            const float knee = 450f;
            const float ceiling = 700f;
            foreach (float rate in new[] { 0f, 128f, 300f, 390f, 450f })
            {
                Assert.AreEqual(rate, WheelKinematics.SoftCap(rate, knee, ceiling), 1e-4f,
                    "ordinary driving turns the true amount");
            }

            Assert.Less(WheelKinematics.SoftCap(1300f, knee, ceiling), ceiling, "a fast wheel stays under the ceiling");
            Assert.Greater(WheelKinematics.SoftCap(1300f, knee, ceiling), 0.97f * ceiling, "and close to it");
            Assert.AreEqual(-WheelKinematics.SoftCap(800f, knee, ceiling), WheelKinematics.SoftCap(-800f, knee, ceiling),
                1e-4f);
            float last = 0f;
            for (float v = 25f; v < 3000f; v += 25f)
            {
                float capped = WheelKinematics.SoftCap(v, knee, ceiling);
                Assert.GreaterOrEqual(capped, last, "faster never turns slower");
                last = capped;
            }
        }

        [Test]
        public void TheRadius_IsTheMeshsOwn_WhateverTheTyresOrientation()
        {
            var wheel = new GameObject("Wheel").transform;
            try
            {
                wheel.position = new Vector3(2f, 0.35f, 3f);
                GameObject tyre = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(tyre.GetComponent<Collider>());
                tyre.transform.SetParent(wheel, false);
                tyre.transform.localScale = new Vector3(0.7f, 0.1f, 0.7f);
                tyre.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Assert.AreEqual(0.35f, WheelKinematics.Radius(wheel), 1e-3f, "a tyre whose axle is its mesh's Y");

                Object.DestroyImmediate(tyre);
                var rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(rim.GetComponent<Collider>());
                rim.transform.SetParent(wheel, false);
                rim.transform.localScale = new Vector3(0.2f, 0.5f, 0.5f);
                Assert.AreEqual(0.25f, WheelKinematics.Radius(wheel), 1e-3f, "a hub box 0.5 m across");
                Assert.AreEqual(0f, WheelKinematics.Radius(new GameObject("Bare").transform), "no mesh, no radius");
            }
            finally
            {
                Object.DestroyImmediate(wheel.gameObject);
                foreach (GameObject bare in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                {
                    if (bare.name == "Bare")
                    {
                        Object.DestroyImmediate(bare);
                    }
                }
            }
        }
    }
}
