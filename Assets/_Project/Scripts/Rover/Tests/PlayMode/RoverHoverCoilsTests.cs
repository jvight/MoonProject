using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The Hover-Jump coils through the real wiring: hidden (and dark) without the ability, popping in when it is
    /// bought, squashing and glowing with the charge (rings and the cyan light under the belly), springing out on the
    /// leap and going dark after it. Metrics go to Logs/rover-metrics/hovercoils-metrics.md.
    /// </summary>
    public sealed class RoverHoverCoilsTests : InputTestFixture
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private static readonly string OutputFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "rover-metrics"));

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;
        private MaterialPropertyBlock _block;

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -250f), 0f);
            _block = new MaterialPropertyBlock();
        }

        public override void TearDown()
        {
            _rover.Dispose();
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        private static IEnumerator Frames(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private float RingGlow()
        {
            _rover.CoilGlows[0].GetPropertyBlock(_block);
            return _block.GetVector(EmissionColorId).x;
        }

        [UnityTest]
        public IEnumerator Coils_StayHiddenWithoutTheAbility_AndPopInWhenBought()
        {
            yield return Frames(0.5f);
            _rover.Drive.JumpHeld = true;
            yield return Frames(1f);
            Assert.IsFalse(_rover.CoilMount.gameObject.activeSelf, "No coils before the Hover-Jump is bought.");
            Assert.IsFalse(_rover.CoilLight.enabled, "No charge light either.");
            _rover.Drive.JumpHeld = false;
            yield return Frames(0.5f);

            var report = new FeelReport();
            float chassisRest = _rover.Chassis.localPosition.y;
            _rover.Context.Get<IRoverAbilities>().Grant(RoverAbility.HoverJump);
            yield return null;
            Assert.IsTrue(_rover.CoilMount.gameObject.activeSelf, "Bought: the coils appear.");
            float boughtAt = Time.time;
            float largest = 0f;
            float bob = 0f;
            float settled = 0f;
            while (Time.time - boughtAt < 2f)
            {
                yield return null;
                float scale = _rover.CoilMount.localScale.y;
                largest = Mathf.Max(largest, scale);
                bob = Mathf.Max(bob, _rover.Chassis.localPosition.y - chassisRest);
                settled = Mathf.Abs(scale - 1f) > 0.02f ? Time.time - boughtAt : settled;
            }

            report.Add("Pop-in: mount overshoot", largest, "x", 1.05f, 1.4f, "one soft overshoot");
            report.Add("Pop-in: settled after", settled, "s", 0f, 1.2f, "< 1.2 s");
            report.Add("Pop-in: chassis bob", bob, "m", 0.01f, 0.1f, "a visible little hop");
            report.Write(Path.Combine(OutputFolder, "hovercoils-popin-metrics.md"));
            Assert.IsEmpty(report.Failures, report.Table);
        }

        [UnityTest]
        public IEnumerator Coils_SquashAndGlowWithTheCharge_SpringOutOnTheLeap_AndGoDark()
        {
            HoverCoilSettings settings = _rover.RigTuning.HoverCoils;
            _rover.Context.Get<IRoverAbilities>().Grant(RoverAbility.HoverJump);
            yield return Frames(1f);
            Assert.IsTrue(_rover.CoilMount.gameObject.activeSelf, "Owned from the start: the coils are there.");
            Assert.AreEqual(1f, _rover.CoilMount.localScale.y, 1e-3f, "No pop-in for an ability owned at start-up.");
            Assert.IsFalse(_rover.CoilLight.enabled, "Dark until the jump charges.");

            var report = new FeelReport();
            _rover.Drive.JumpHeld = true;
            yield return Frames(_rover.Tuning.HoverJump.ChargeTime + 0.15f);
            report.Add("Full charge: coil length", _rover.Coils[0].localScale.y, "x",
                1f - settings.ChargeSquash - 0.02f, 1f - settings.ChargeSquash + 0.02f, "squashed");
            report.Add("Full charge: ring glow", RingGlow(), "", settings.GlowPeak * 0.95f, settings.GlowPeak * 1.01f,
                "white x peak");
            report.Add("Full charge: light on", _rover.CoilLight.enabled ? 1f : 0f, "", 1f, 1f, "lit");
            report.Add("Full charge: light intensity", _rover.CoilLight.intensity, "",
                settings.LightIntensity * 0.95f, settings.LightIntensity * 1.01f, "tuned");

            _rover.Drive.JumpHeld = false;
            float releasedAt = Time.time;
            float longest = 0f;
            float darkAfter = -1f;
            while (Time.time - releasedAt < 3f)
            {
                yield return null;
                longest = Mathf.Max(longest, _rover.Coils[0].localScale.y);
                if (darkAfter < 0f && !_rover.CoilLight.enabled)
                {
                    darkAfter = Time.time - releasedAt;
                }
            }

            report.Add("Leap: coil spring-out", longest, "x", 1.15f, settings.MaxStretch, "a quick boing");
            report.Add("After the leap: light off after", darkAfter, "s", 0.3f, 2.5f, "fades, then off");
            report.Add("After the leap: ring glow", RingGlow(), "", 0f, 0f, "dark");
            report.Add("After the leap: coil length", _rover.Coils[0].localScale.y, "x", 0.99f, 1.01f, "at rest");
            report.Write(Path.Combine(OutputFolder, "hovercoils-metrics.md"));
            Assert.IsEmpty(report.Failures, report.Table);
        }
    }
}
