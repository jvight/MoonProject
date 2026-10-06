using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Hover-Jump (M3-03) through the real wiring: nothing without the ability; with it, feel metrics per charge level
    /// (apex, hang time, distance at top speed, landing impact and settle), written to
    /// Logs/rover-metrics/hoverjump-metrics.md.
    /// </summary>
    public sealed class RoverHoverJumpTests : InputTestFixture
    {
        private const float SettleAngle = 0.5f;

        private static readonly string OutputFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "rover-metrics"));

        private readonly List<RoverJumpCharged> _charges = new List<RoverJumpCharged>();
        private readonly List<RoverJumped> _jumps = new List<RoverJumped>();
        private readonly List<RoverLanded> _landings = new List<RoverLanded>();
        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;
        private float _peakPerk;
        private float _peakOof;

        public override void Setup()
        {
            base.Setup();
            _charges.Clear();
            _jumps.Clear();
            _landings.Clear();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -350f), 0f);
            EventBus events = _rover.Context.Events;
            events.Subscribe<RoverJumpCharged>(_charges.Add);
            events.Subscribe<RoverJumped>(_jumps.Add);
            events.Subscribe<RoverLanded>(_landings.Add);
        }

        public override void TearDown()
        {
            _rover.Dispose();
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        private float HeightAboveGround
        {
            get
            {
                Vector3 sphere = _rover.Controller.PhysicsBody.position;
                return sphere.y - _rover.Controller.SphereRadius - TestWorld.Height(sphere.x, sphere.z);
            }
        }

        private static IEnumerator Physics(float seconds)
        {
            float until = Time.fixedTime + seconds;
            while (Time.fixedTime < until)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator WithoutTheAbility_HoldingJumpDoesNothing()
        {
            yield return Physics(1f);
            _rover.Drive.JumpHeld = true;
            yield return Physics(1.5f);
            _rover.Drive.JumpHeld = false;
            float highest = 0f;
            float until = Time.fixedTime + 2f;
            while (Time.fixedTime < until)
            {
                yield return new WaitForFixedUpdate();
                highest = Mathf.Max(highest, HeightAboveGround);
            }

            Assert.IsEmpty(_charges);
            Assert.IsEmpty(_jumps);
            Assert.AreEqual(0f, _rover.Controller.JumpCharge);
            Assert.Less(highest, 0.05f, "07 stays on the ground.");
        }

        [UnityTest]
        public IEnumerator HoverJump_MeetsItsFeelTargets()
        {
            _rover.Context.Get<IRoverAbilities>().Grant(RoverAbility.HoverJump);
            var report = new FeelReport();
            yield return Physics(1f);

            yield return Leap(report, "Tap", 0f, 0.4f, 1.5f, "small hop (0.4-1.5 m)");
            yield return Leap(report, "Half charge", 0.4f, 2f, 5.5f, "between");
            int chargesBefore = _charges.Count;
            yield return Leap(report, "Full charge", 0.9f, 6f, 8f, "6-8 m");
            report.Add("Charge events for a full charge", _charges.Count - chargesBefore, "", 5f, 5f,
                "start + 4 steps");
            report.Add("Full leap strength", _jumps[_jumps.Count - 1].Strength, "", 0.99f, 1f, "1");
            report.Add("Joy after the full leap (perk)", _peakPerk, "", 0.5f, 2f, "a happy perk");
            report.Add("Oof after the full leap", _peakOof, "", 0f, 0f, "never");

            yield return LeapAtTopSpeed(report);

            report.Write(Path.Combine(OutputFolder, "hoverjump-metrics.md"));
            Assert.IsEmpty(report.Failures, report.Table);
        }

        private IEnumerator Leap(FeelReport report, string name, float hold, float minApex, float maxApex,
            string apexTarget)
        {
            yield return Physics(_rover.Tuning.HoverJump.Cooldown + 0.3f);
            int landings = _landings.Count;
            _rover.Drive.JumpHeld = true;
            yield return new WaitForFixedUpdate();
            yield return Physics(hold);
            _rover.Drive.JumpHeld = false;
            float takeOff = Time.fixedTime;
            float apex = 0f;
            _peakPerk = 0f;
            _peakOof = 0f;
            while (_landings.Count == landings && Time.fixedTime - takeOff < 12f)
            {
                yield return new WaitForFixedUpdate();
                apex = Mathf.Max(apex, HeightAboveGround);
            }

            Assert.Greater(_landings.Count, landings, $"{name}: no landing announced.");
            RoverLanded landed = _landings[_landings.Count - 1];
            report.Add($"{name}: apex", apex, "m", minApex, maxApex, apexTarget);
            report.Add($"{name}: landing impact", landed.ImpactSpeed, "m/s", 0f, 2.5f, "cushioned (< oof 3.2)");
            report.Note($"{name}: hang time", landed.AirTime, "s");

            float landedAt = Time.time;
            float lastUnsettled = landedAt;
            float previousSign = 0f;
            int overshoots = 0;
            while (Time.time - landedAt < 3f)
            {
                yield return null;
                _peakPerk = Mathf.Max(_peakPerk, _rover.BodyLanguage.Mood.Perk);
                _peakOof = Mathf.Max(_peakOof, _rover.BodyLanguage.Mood.Oof);
                float pitch = Mathf.Asin(Mathf.Clamp(_rover.Chassis.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                if (Mathf.Abs(pitch) >= SettleAngle)
                {
                    lastUnsettled = Time.time;
                    float sign = Mathf.Sign(pitch);
                    overshoots += previousSign != 0f && sign != previousSign ? 1 : 0;
                    previousSign = sign;
                }
            }

            report.Add($"{name}: landing settle", lastUnsettled - landedAt, "s", 0f, 1.5f, "< 1.5 s");
            report.Add($"{name}: settle overshoots", overshoots, "", 0f, 2f, "<= 2");
        }

        private IEnumerator LeapAtTopSpeed(FeelReport report)
        {
            RoverController controller = _rover.Controller;
            _rover.Drive.Drive = new Vector2(0f, 1f);
            yield return Physics(7f);
            int landings = _landings.Count;
            _rover.Drive.JumpHeld = true;
            yield return Physics(0.9f);
            _rover.Drive.JumpHeld = false;
            yield return new WaitForFixedUpdate();
            Vector3 from = controller.PhysicsBody.position;
            float speed = controller.Speed;
            float apex = 0f;
            float takeOff = Time.fixedTime;
            int outOfFrame = 0;
            float lowestCamera = float.MaxValue;
            while (_landings.Count == landings && Time.fixedTime - takeOff < 12f)
            {
                yield return new WaitForFixedUpdate();
                apex = Mathf.Max(apex, HeightAboveGround);
                Vector3 onScreen = _rover.Camera.WorldToViewportPoint(controller.Position);
                outOfFrame += onScreen.z > 0f && onScreen.x is > 0f and < 1f && onScreen.y is > 0f and < 1f ? 0 : 1;
                Vector3 camera = _rover.Camera.transform.position;
                lowestCamera = Mathf.Min(lowestCamera, camera.y - TestWorld.Height(camera.x, camera.z));
            }

            report.Add("Leap camera: steps with 07 out of frame", outOfFrame, "", 0f, 0f, "0");
            report.Add("Leap camera: lowest clearance above ground", lowestCamera, "m", 0.3f, 100f, "never in terrain");
            Vector3 to = controller.PhysicsBody.position;
            float distance = Vector2.Distance(new Vector2(from.x, from.z), new Vector2(to.x, to.z));
            report.Note("Top speed at take-off", speed, "m/s");
            report.Add("Full leap at top speed: distance", distance, "m", 12f, 60f, "clears >= 12 m");
            report.Add("Full leap at top speed: apex", apex, "m", 6f, 8.5f, "6-8 m");
            report.Add("Full leap at top speed: landing impact", _landings[_landings.Count - 1].ImpactSpeed, "m/s",
                0f, 2.5f, "cushioned");
            _rover.Drive.Drive = Vector2.zero;
        }
    }
}
