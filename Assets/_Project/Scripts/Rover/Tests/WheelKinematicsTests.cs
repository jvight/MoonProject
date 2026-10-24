using NUnit.Framework;
using UnityEngine;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>A wheel's radius is the mesh's own.</summary>
    public sealed class WheelKinematicsTests
    {
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
