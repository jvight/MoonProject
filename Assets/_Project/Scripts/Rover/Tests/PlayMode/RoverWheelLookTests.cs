using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>What the player sees of the wheels (owner playtest 00a309e9f: they look far too fast): the wheel's
    /// parts, its drawn size against the ground it stands on, and how far it turns a frame at 60 fps.</summary>
    public sealed class RoverWheelLookTests
    {
        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;

        [SetUp]
        public void SetUp()
        {
            Time.captureDeltaTime = 1f / 60f;
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -300f), 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _rover?.Dispose();
            _world.Dispose();
            _physics.Dispose();
        }

        private Transform Wheel(int index)
        {
            foreach (Transform node in _rover.Root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == RoverModelNodes.Wheel(index))
                {
                    return node;
                }
            }

            Assert.Fail("no wheel node");
            return null;
        }

        [UnityTest]
        public IEnumerator Report_TheWheelsPartsSizeAndTurnPerFrame()
        {
            yield return new WaitForSeconds(2.5f);
            Transform wheel = Wheel(0);
            var text = new StringBuilder("[rover-wheellook] wheel FL children:\n");
            foreach (MeshFilter filter in wheel.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                Bounds world = filter.GetComponent<Renderer>().bounds;
                text.AppendLine($"  {filter.name} verts {mesh.vertexCount} local {filter.transform.localPosition} " +
                    $"size world {world.size.x:0.000} x {world.size.y:0.000} x {world.size.z:0.000}");
            }

            Vector3 hub = wheel.position;
            var terrain = _rover.Context.Get<ITerrainQuery>();
            float ground = terrain.SampleHeight(hub.x, hub.z);
            text.AppendLine($"  hub height above the ground {hub.y - ground:0.000} m; radius measured " +
                $"{WheelKinematics.Radius(wheel):0.000} m; wheel lossy scale {wheel.lossyScale}");
            Debug.Log(text.ToString());

            foreach (float stick in new[] { 0.1f, 0.13f, 0.3f, 1f })
            {
                _rover.Drive.Drive = new Vector2(0f, stick);
                yield return new WaitForSeconds(3f);
                Quaternion last = wheel.localRotation;
                Vector3 start = _rover.Controller.Position;
                float spin = 0f;
                var steps = new System.Collections.Generic.List<float>();
                for (int i = 0; i < 120; i++)
                {
                    yield return null;
                    Quaternion delta = Quaternion.Inverse(last) * wheel.localRotation;
                    delta.ToAngleAxis(out float angle, out Vector3 axis);
                    if (angle > 180f)
                    {
                        angle -= 360f;
                    }

                    float step = axis.x >= 0f ? angle : -angle;
                    spin += step;
                    steps.Add(step);
                    last = wheel.localRotation;
                }

                float mean = spin / steps.Count;
                float variance = 0f;
                float worst = 0f;
                foreach (float step in steps)
                {
                    variance += (step - mean) * (step - mean);
                    worst = Mathf.Max(worst, Mathf.Abs(step - mean));
                }

                Debug.Log($"[rover-wheellook] per-frame turn at stick {stick:0.00}: mean {mean:0.00}, std "
                    + $"{Mathf.Sqrt(variance / steps.Count):0.00}, worst deviation {worst:0.00} deg");

                float travelled = Vector3.Distance(start, _rover.Controller.Position);
                Debug.Log($"[rover-wheellook] stick {stick:0.00}: {travelled / 2f:0.00} m/s, wheel turns {spin / 2f:0} deg/s " +
                    $"= {spin / steps.Count:0.0} deg/frame");

                if (stick <= 0.13f)
                {
                    float radius = WheelKinematics.Radius(wheel);
                    float expectedRate = travelled / 2f / radius * Mathf.Rad2Deg;
                    Assert.AreEqual(expectedRate, spin / 2f, expectedRate * 0.05f,
                        $"At a crawl (stick {stick}) the wheel turns the true amount for the ground it covers.");
                    Assert.Less(worst, 0.5f, "and turns evenly, frame after frame");
                }
            }

            _rover.Drive.Drive = Vector2.zero;
        }
    }
}
