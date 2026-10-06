using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Scripted play sessions with a virtual gamepad through the real InputReader, on test ground built in code.
    /// Measures the feel targets of the rover brief, prints a metrics table (Logs/rover-metrics/feel-metrics.md) and
    /// captures a few frames. Lunar gravity is set for the session (ProjectSettings may not have it yet) and restored.
    /// </summary>
    public sealed class RoverFeelMetricsTests : InputTestFixture
    {
        private const float LunarGravity = -1.62f;
        private const float StopSpeed = 0.05f;
        private const float SettleAngle = 0.5f;
        private const int CaptureWidth = 960;
        private const int CaptureHeight = 540;
        private const int MaxInputAttempts = 5;

        private static readonly string OutputFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "rover-metrics"));

        private Gamepad _pad;
        private InputActionAsset _actions;
        private TestWorld _world;
        private Vector3 _savedGravity;
        private int _cameraViolations;
        private float _cameraLowestClearance;

        public override void Setup()
        {
            base.Setup();
            _pad = InputSystem.AddDevice<Gamepad>();
            _actions = TestControls.Create();
            _savedGravity = Physics.gravity;
            Physics.gravity = new Vector3(0f, LunarGravity, 0f);
            _cameraViolations = 0;
            _cameraLowestClearance = float.MaxValue;
        }

        public override void TearDown()
        {
            Physics.gravity = _savedGravity;
            _world?.Dispose();
            Object.Destroy(_actions);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator FeelMetrics_MeetTheirTargets()
        {
            _world = new TestWorld();
            var report = new FeelReport();

            yield return FlatSession(report);
            yield return BumpSession(report);
            yield return SlopeSession(report);

            report.Add("Camera frames inside terrain", _cameraViolations, "frames", 0f, 0f, "0");
            report.Note("Camera lowest clearance above terrain", _cameraLowestClearance, "m");
            report.Write(Path.Combine(OutputFolder, "feel-metrics.md"));
            Assert.IsEmpty(report.Failures, report.Table);
        }

        private IEnumerator FlatSession(FeelReport report)
        {
            TestRover rover = TestRover.Spawn(_actions, _world, new Vector3(0f, 0f, -200f), 0f);
            RoverController controller = rover.Controller;
            DriveSettings drive = rover.Tuning.Drive;
            yield return Hold(rover, Vector2.zero, 0.5f);

            yield return Drive(new Vector2(0f, 1f));
            float start = -1f;
            float t90 = float.NaN;
            float t0 = Time.fixedTime;
            while (Time.fixedTime - t0 < 8f)
            {
                yield return new WaitForFixedUpdate();
                if (start < 0f && controller.DriveInput.y > 0.01f)
                {
                    start = Time.fixedTime;
                }

                if (float.IsNaN(t90) && controller.Speed >= 0.9f * drive.TopSpeed)
                {
                    t90 = Time.fixedTime - start;
                }
            }

            report.Add("0 to 90% top speed", t90, "s", 2.2f, 2.8f, "~2.5 s");
            report.Add("Top speed (flat)", controller.Speed, "m/s", 7.6f, 8.4f, "~8 m/s");
            CaptureFrame(rover, "01-cruising");

            yield return Drive(Vector2.zero);
            float coastStart = Time.fixedTime;
            float coastTop = controller.Speed;
            var speeds = new List<float>();
            while (controller.Speed > StopSpeed && Time.fixedTime - coastStart < 8f)
            {
                yield return new WaitForFixedUpdate();
                speeds.Add(controller.Speed);
            }

            float coastTime = Time.fixedTime - coastStart;
            int threeQuarters = Mathf.Clamp(speeds.Count * 3 / 4, 0, speeds.Count - 1);
            float tail = speeds.Count > 0 ? speeds[threeQuarters] / coastTop : 1f;
            report.Add("Coast to stop from top speed", coastTime, "s", 1.6f, 2.6f, "~2 s");
            report.Add("Speed left at 3/4 of the coast", tail, "x top", 0f, 0.2f, "<= 0.2 (eased roll-out)");

            yield return Hold(rover, new Vector2(0f, -1f), 6f);
            report.Add("Reverse top speed", controller.Speed, "m/s", 3f, 4f, "slower than forward (~3.5)");

            yield return Hold(rover, new Vector2(0f, 1f), 7f);
            yield return Drive(new Vector2(0f, -1f));
            float brakeStart = Time.fixedTime;
            while (controller.ForwardSpeed > drive.ReverseEngageSpeed && Time.fixedTime - brakeStart < 5f)
            {
                yield return new WaitForFixedUpdate();
            }

            report.Add("Brake (full reverse input) from top speed", Time.fixedTime - brakeStart, "s", 0.8f, 1.5f,
                "soft, ~1.1 s");

            yield return Hold(rover, new Vector2(0f, 1f), 7f);
            yield return Hold(rover, new Vector2(1f, 1f), 2f);
            float headingStart = controller.Heading;
            float speedSum = 0f;
            int samples = 0;
            float turnStart = Time.fixedTime;
            while (Time.fixedTime - turnStart < 2f)
            {
                yield return new WaitForFixedUpdate();
                speedSum += controller.Speed;
                samples++;
            }

            float yawRate = Mathf.Abs(Mathf.DeltaAngle(headingStart, controller.Heading)) / 2f * Mathf.Deg2Rad;
            float radius = speedSum / samples / Mathf.Max(yawRate, 1e-3f);
            report.Add("Turn radius at top speed", radius, "m", 6f, 10f, "wide, calm arc (6-10 m)");
            CaptureFrame(rover, "02-turning");

            yield return Hold(rover, Vector2.zero, 4f);
            Vector3 pivotFrom = controller.Position;
            float pivotHeading = controller.Heading;
            yield return Hold(rover, new Vector2(1f, 0f), 2f);
            float pivotTurned = Mathf.Abs(Mathf.DeltaAngle(pivotHeading, controller.Heading));
            report.Add("Pivot turn at standstill (2 s)", pivotTurned, "deg", 90f, 180f, "turns on the spot");
            report.Add("Pivot turn drift", Vector3.Distance(pivotFrom, controller.Position), "m", 0f, 0.4f,
                "stays in place");

            yield return Drive(Vector2.zero);
            rover.Dispose();
            yield return null;
        }

        private IEnumerator BumpSession(FeelReport report)
        {
            TestRover rover = TestRover.Spawn(_actions, _world, new Vector3(TestWorld.BumpX, 0f, -50f), 0f);
            RoverController controller = rover.Controller;
            var landings = new List<RoverLanded>();
            var landingTimes = new List<float>();
            using (rover.Context.Events.Subscribe<RoverLanded>(landed =>
                   {
                       landings.Add(landed);
                       landingTimes.Add(Time.time);
                   }))
            {
                yield return Hold(rover, Vector2.zero, 0.5f);
                yield return Drive(new Vector2(0f, 1f));

                float approachSpeed = 0f;
                float apex = 0f;
                bool captured = false;
                float takeoff = -1f;
                float timeout = Time.time + 20f;
                while (landings.Count == 0 && Time.time < timeout)
                {
                    yield return null;
                    CheckCamera(rover);
                    Vector3 p = controller.Position;
                    if (p.z < TestWorld.BumpStartZ)
                    {
                        approachSpeed = controller.Speed;
                    }

                    if (p.z > TestWorld.BumpStartZ && !controller.IsGrounded)
                    {
                        takeoff = takeoff < 0f ? Time.time : takeoff;
                        apex = Mathf.Max(apex, p.y - TestWorld.Height(p.x, p.z));
                        if (!captured && Time.time - takeoff > 0.35f)
                        {
                            CaptureFrame(rover, "03-hop");
                            captured = true;
                        }
                    }
                }

                Assert.IsNotEmpty(landings, "07 never left the ground over the 1 m crest at top speed.");
                report.Add("Speed arriving at the 1 m crest", approachSpeed, "m/s", 7.6f, 8.4f, "top speed");
                report.Add("Air time over 1 m crest at top speed", landings[0].AirTime, "s", 0.8f, 1.2f,
                    "0.8-1.2 s, floaty");
                report.Note("Hop apex above ground", apex, "m");
                report.Note("Landing impact speed", landings[0].ImpactSpeed, "m/s");

                float landedAt = landingTimes[0];
                float lastUnsettled = landedAt;
                int overshoots = 0;
                float previousSign = 0f;
                float peak = 0f;
                while (Time.time - landedAt < 3f)
                {
                    yield return null;
                    CheckCamera(rover);
                    float pitch = Mathf.Asin(Mathf.Clamp(rover.Chassis.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                    peak = Mathf.Max(peak, Mathf.Abs(pitch));
                    if (Mathf.Abs(pitch) >= SettleAngle)
                    {
                        lastUnsettled = Time.time;
                        float sign = Mathf.Sign(pitch);
                        if (previousSign != 0f && sign != previousSign)
                        {
                            overshoots++;
                        }

                        previousSign = sign;
                    }

                    if (Mathf.Abs(Time.time - landedAt - 0.25f) < Time.deltaTime * 0.5f)
                    {
                        CaptureFrame(rover, "04-landing");
                    }
                }

                report.Add("Landing settle (body pitch < 0.5 deg)", lastUnsettled - landedAt, "s", 0f, 1.5f, "< 1.5 s");
                report.Add("Landing settle overshoots", overshoots, "", 0f, 2f, "<= 2");
                report.Note("Peak body pitch after landing", peak, "deg");
            }

            yield return Drive(Vector2.zero);
            rover.Dispose();
            yield return null;
        }

        private IEnumerator SlopeSession(FeelReport report)
        {
            Vector3 spawn = new Vector3(TestWorld.SlopeX - 15f, 0f, 0f);
            TestRover rover = TestRover.Spawn(_actions, _world, spawn, 0f);
            RoverController controller = rover.Controller;
            yield return Hold(rover, Vector2.zero, 1f);

            Vector3 parked = controller.Position;
            yield return Hold(rover, Vector2.zero, 3f);
            report.Add("Creep while parked on a 30 deg slope (3 s)", Vector3.Distance(parked, controller.Position), "m",
                0f, 0.05f, "~0");

            float worstTilt = 0f;
            float worstChassis = 0f;
            float airborne = 0f;
            var plan = new[]
            {
                (new Vector2(0f, 1f), 2f),
                (new Vector2(1f, 1f), 2.5f),
                (new Vector2(-1f, 1f), 4f),
                (Vector2.zero, 2.5f),
            };
            foreach ((Vector2 stick, float seconds) in plan)
            {
                yield return Drive(stick);
                float until = Time.time + seconds;
                while (Time.time < until)
                {
                    yield return null;
                    CheckCamera(rover);
                    worstTilt = Mathf.Max(worstTilt, Vector3.Angle(controller.Rotation * Vector3.up, Vector3.up));
                    worstChassis = Mathf.Max(worstChassis, Vector3.Angle(rover.Chassis.up, Vector3.up));
                    airborne += controller.IsGrounded ? 0f : Time.deltaTime;
                }

                if (stick.x > 0f)
                {
                    CaptureFrame(rover, "05-slope");
                }
            }

            report.Add("Worst body tilt driving a 30 deg slope", worstTilt, "deg", 0f, 40f, "no flip (<= 40)");
            report.Add("Worst chassis tilt incl. jelly lean", worstChassis, "deg", 0f, 50f, "no flip (<= 50)");
            report.Note("Airborne time on the slope", airborne, "s");

            yield return Drive(Vector2.zero);
            rover.Dispose();
            yield return null;
        }

        private IEnumerator Hold(TestRover rover, Vector2 stick, float seconds)
        {
            yield return Drive(stick);
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
                CheckCamera(rover);
            }
        }

        /// <summary>
        /// Queues stick input from the Update phase (device state is not addressable in FixedUpdate) and waits until
        /// the device reports it (the stick's deadzone processor clamps it to the unit circle), re-queuing if the
        /// input runtime dropped the event.
        /// </summary>
        private IEnumerator Drive(Vector2 stick)
        {
            yield return null;
            for (int attempt = 0; attempt < MaxInputAttempts; attempt++)
            {
                Set(_pad.leftStick, stick);
                yield return null;
                if ((_pad.leftStick.ReadValue() - Vector2.ClampMagnitude(stick, 1f)).sqrMagnitude < 1e-4f)
                {
                    yield break;
                }
            }

            Assert.Fail($"The virtual gamepad never reported stick {stick}.");
        }

        private void CheckCamera(TestRover rover)
        {
            Vector3 camera = rover.Camera.transform.position;
            float clearance = camera.y - TestWorld.Height(camera.x, camera.z);
            _cameraLowestClearance = Mathf.Min(_cameraLowestClearance, clearance);
            bool inside = clearance < 0.05f
                || Physics.CheckSphere(camera, 0.05f, Layers.DriveableMask, QueryTriggerInteraction.Ignore);
            _cameraViolations += inside ? 1 : 0;
        }

        private static void CaptureFrame(TestRover rover, string name)
        {
            FrameCapture.SavePng(rover.Camera, CaptureWidth, CaptureHeight, Path.Combine(OutputFolder, name + ".png"));
        }
    }
}
