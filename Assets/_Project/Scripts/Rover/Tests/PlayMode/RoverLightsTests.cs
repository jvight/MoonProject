using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// 07's lights key (the owner's playtest): a press eases the lamps off in about a quarter second and says so for
    /// the switch's click, a second press eases them back on, the SSR-3 headlamp pod lenses glow and switch with them
    /// once fitted, and the save remembers the switch.
    /// </summary>
    public sealed class RoverLightsTests : InputTestFixture
    {
        private const string LightsSaveKey = "rover.lights";

        // The ease takes the tuned switch time; this much more covers the frame it lands on.
        private const float SwitchSlack = 0.1f;

        /// <summary>Frames the lamps may take after the key to show the first step of their ease off.</summary>
        private const int EaseStartFrames = 10;

        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;
        private Gamepad _pad;

        public override void Setup()
        {
            base.Setup();
            _pad = InputSystem.AddDevice<Gamepad>();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -200f), 0f);
        }

        public override void TearDown()
        {
            _rover.Dispose();
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator TheLightsKey_EasesTheLampsOffAndOn_AndTheSaveKeepsIt()
        {
            yield return Wait(1f);
            _rover.Context.Events.Publish(new PartFitted(RoverSocket.Lamp, RoverKitPiece.LampBar, false, false, default, 0));
            yield return Wait(_rover.RigTuning.Growth.HeadlampFitSeconds + 0.1f);

            Renderer podLens = FindPodLens(_rover.Head, RoverModelNodes.HeadlampLensLeft);
            float lit = _rover.Headlamp.intensity;
            float podLit = EmissionOf(podLens);
            Assert.Greater(lit, 0f, "07's lamps start on");
            Assert.Greater(podLit, 0f, "the fitted SSR-3 pod lenses glow warm with 07's lamps");
            int switched = 0;
            bool lastOn = true;
            using (_rover.Context.Events.Subscribe<RoverLightsSwitched>(change =>
                   {
                       switched++;
                       lastOn = change.On;
                   }))
            {
                yield return Toggle();
                float seconds = _rover.RigTuning.LampSwitchSeconds;
                // The ease runs in the rig's own update, a frame or so after the key: sample once it has begun.
                int frames = 0;
                while (_rover.Headlamp.intensity >= lit && frames < EaseStartFrames)
                {
                    yield return null;
                    frames++;
                }

                float halfway = _rover.Headlamp.intensity;
                yield return Wait(seconds + SwitchSlack);
                Assert.AreEqual(1, switched, "the switch says so once, for its click");
                Assert.IsFalse(lastOn);
                Assert.Less(halfway, lit, $"the lamps begin to ease off within {EaseStartFrames} frames of the key");
                Assert.Greater(halfway, 0f, "an ease, not a cut");
                Assert.AreEqual(0f, _rover.Headlamp.intensity, 1e-3f, "and are dark within the switch time");
                Assert.AreEqual(0f, EmissionOf(podLens), 1e-3f, "the brow pod lenses go dark with the road light");
                StringAssert.Contains("\"lightsOff\":true", _rover.Save.Capture(LightsSaveKey), "the save keeps it");

                yield return Toggle();
                yield return Wait(seconds + SwitchSlack);
                Assert.AreEqual(2, switched);
                Assert.IsTrue(lastOn);
                Assert.AreEqual(lit, _rover.Headlamp.intensity, lit * 0.02f, "a second press brings them back");
                Assert.AreEqual(podLit, EmissionOf(podLens), podLit * 0.02f, "and the brow pod lenses glow warm again");
                StringAssert.Contains("\"lightsOff\":false", _rover.Save.Capture(LightsSaveKey));
            }
        }

        [UnityTest]
        public IEnumerator ARestoredSave_WithTheLightsOff_LoadsDark()
        {
            _rover.Dispose();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -200f), 0f,
                prepare: rover => rover.Save.Seed(LightsSaveKey, "{\"lightsOff\":true}"));
            yield return Wait(1f);
            Assert.AreEqual(0f, _rover.Headlamp.intensity, 1e-3f, "a save with the lights off loads dark");
        }

        private static Renderer FindPodLens(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node.GetComponent<Renderer>();
                }
            }

            Assert.Fail($"TestRover has no '{name}' renderer.");
            return null;
        }

        private static float EmissionOf(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetFloat(EmissionStrengthId);
        }

        private IEnumerator Toggle()
        {
            Press(_pad.leftStickButton);
            yield return null;
            Release(_pad.leftStickButton);
            yield return null;
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }
    }
}
