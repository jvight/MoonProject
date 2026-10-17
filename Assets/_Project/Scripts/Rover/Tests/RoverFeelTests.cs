using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>The cut's rumble and lean from scripted beats (docs/design/feel-pass.md §3.1), on a fake gamepad.</summary>
    public sealed class RoverFeelTests
    {
        private const float Frame = 1f / 60f;
        private const float Tolerance = 1e-3f;

        private sealed class FakeMotors : IRumbleMotors
        {
            public bool IsConnected { get; set; } = true;

            public float Low { get; private set; }

            public float High { get; private set; }

            public void Set(float low, float high)
            {
                Low = low;
                High = high;
            }
        }

        private EventBus _events;
        private FakeMotors _motors;
        private FeelPreferences _preferences;
        private RoverFeel _feel;
        private float _time;
        private float _speed;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _motors = new FakeMotors();
            _preferences = new FeelPreferences();
            _feel = new RoverFeel(_events, new CutFeelSettings(), new DockFeelSettings(), new MotionFeelSettings(),
                _preferences, _motors, null);
            _time = 0f;
        }

        [TearDown]
        public void TearDown()
        {
            _feel.Dispose();
        }

        private void Beat(FeelPhase phase, float strength = 0f, float seconds = 0f, FeelKind kind = FeelKind.Cut)
        {
            _events.Publish(new FeelBeat(kind, phase, Vector3.forward, Vector3.up, strength, seconds));
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _time += Frame;
                _feel.Tick(Frame, _time, _speed, Vector3.zero);
            }
        }

        [Test]
        public void TheChargeStages_BraceTheBody_AndTheCrackLetsItRise()
        {
            Beat(FeelPhase.Anticipate, seconds: 1.4f);
            _events.Publish(new CutChargeStaged(0, 2, false, 0.45f));
            Run(0.5f);
            Assert.IsTrue(_feel.Brace.IsBraced, "07 plants itself for the charge.");
            Assert.Less(_feel.Brace.Heave, -0.02f, "Squatting on its suspension.");
            _events.Publish(new CutChargeStaged(1, 2, false, 0.45f));
            _events.Publish(new CutChargeStaged(2, 2, false, 0.45f));
            Assert.IsTrue(_feel.Brace.IsLocked, "The full lock holds it still.");

            Beat(FeelPhase.Act);
            Run(0.5f);
            Assert.IsTrue(_feel.Brace.IsBraced, "Braced through the beam.");
            Beat(FeelPhase.Impact);
            Run(2f);
            Assert.IsTrue(_feel.Brace.IsAtRest(Tolerance), "The crack lets it rise back to rest.");
        }

        [Test]
        public void TheCut_ChargesKicksSustainsAndCracks()
        {
            Beat(FeelPhase.Anticipate, seconds: 0.65f);
            Run(0.7f);
            Assert.AreEqual(0.08f, _motors.Low, Tolerance, "The charge has ramped to 0.08 and holds.");
            Assert.Greater(_feel.Nudge.Pitch, 0.5f, "07 leans into the line.");

            Beat(FeelPhase.Act);
            Run(Frame);
            Assert.AreEqual(0.25f, _motors.Low, Tolerance, "Ignition kicks at 0.25.");
            Run(0.12f);
            Assert.AreEqual(0.12f, _motors.Low, Tolerance, "Then settles to 0.12.");

            Beat(FeelPhase.Act, 1f);
            Run(Frame);
            Assert.AreEqual(0.18f, _motors.Low, Tolerance, "The beam's rumble climbs with progress.");

            Beat(FeelPhase.Impact);
            Run(Frame);
            Assert.AreEqual(0.35f, _motors.Low, Tolerance, "The crack is the strongest pulse.");
            Run(0.2f);
            Assert.AreEqual(0f, _motors.Low, Tolerance, "Then quiet: the beam let go.");
        }

        [Test]
        public void DriveStartAndStop_ArePublished_AndFelt()
        {
            var beats = new System.Collections.Generic.List<FeelBeat>();
            _events.Subscribe<FeelBeat>(beats.Add);
            _speed = 1f;
            Run(Frame);
            Assert.AreEqual(FeelKind.Drive, beats[0].Kind);
            Assert.AreEqual(FeelPhase.Anticipate, beats[0].Phase, "Start.");
            Assert.Greater(_motors.Low, 0f, "The start swell.");
            Run(0.5f);
            _speed = 0f;
            Run(Frame);
            Assert.AreEqual(FeelPhase.Impact, beats[1].Phase, "Stop.");
            Assert.AreEqual(0.06f, _motors.Low, Tolerance, "The stop rumble.");
        }

        [Test]
        public void TheLeap_CrouchesPerStep_KicksAtLaunch_AndLandsByImpact()
        {
            _events.Publish(new RoverJumpCharged(0.5f));
            Run(0.5f);
            Assert.AreEqual(0.04f + 0.12f * 0.5f, _motors.Low, Tolerance, "Charge rumble by step.");
            Assert.Less(_feel.Nudge.Heave, -0.03f, "07 crouches about 4 cm at half charge.");
            _events.Publish(new RoverJumped(1f));
            Run(Frame);
            Assert.AreEqual(0.25f, _motors.Low, Tolerance, "The launch kick.");
            Run(0.3f);
            _events.Publish(new RoverLanded(Vector3.zero, 6f, 1f));
            Run(Frame);
            Assert.AreEqual(0.3f, _motors.Low, Tolerance, "A hard landing rumbles hardest.");
            Run(0.05f);
            Assert.Less(_feel.Nudge.Heave, -0.02f, "Landing squashes 07's chassis down.");
        }

        [Test]
        public void TheSeat_AndTheBatteryEmptying_AreFelt()
        {
            Beat(FeelPhase.Impact, 1f, kind: FeelKind.Install);
            Run(Frame);
            Assert.AreEqual(0.2f, _motors.Low, Tolerance, "The seat's clunk.");
            Run(0.3f);
            _events.Publish(new LowPowerChanged(true, false));
            Run(Frame);
            Assert.Greater(_motors.Low, 0.05f, "The power-down rumble starts at 0.06 ...");
            Run(0.7f);
            Assert.AreEqual(0f, _motors.Low, Tolerance, "... and decays.");
        }

        [Test]
        public void TheDockLock_Dips07_AndRumbles015For80Ms()
        {
            Beat(FeelPhase.Impact, kind: FeelKind.Dock);
            Run(Frame);
            Assert.AreEqual(0.15f, _motors.Low, Tolerance);
            Run(0.1f);
            Assert.AreEqual(0f, _motors.Low, Tolerance, "Over after 80 ms.");
            Assert.Less(_feel.Nudge.Heave, -0.005f, "07 dips into the clamp.");
        }

        [Test]
        public void EachStockUnit_Ticks_ThinnedPastTheSixth()
        {
            var ticked = new System.Collections.Generic.List<int>();
            for (int unit = 0; unit < 12; unit++)
            {
                _events.Publish(new StockCountTicked(SalvageMaterial.Metal, unit, 12 - unit));
                Run(Frame);
                if (_motors.Low > 0f)
                {
                    ticked.Add(unit);
                }

                Run(0.1f);
            }

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 8, 10 }, ticked);
            Assert.LessOrEqual(_motors.Low, RumbleMixer.HardCap);
        }

        [Test]
        public void TheTowerComingOnline_Rumbles03()
        {
            Beat(FeelPhase.Anticipate, kind: FeelKind.Build);
            Run(Frame);
            Assert.AreEqual(0f, _motors.Low, "The approach is quiet.");
            Beat(FeelPhase.Impact, kind: FeelKind.Build);
            Run(Frame);
            Assert.AreEqual(0.3f, _motors.Low, Tolerance);
            Run(0.2f);
            Assert.AreEqual(0f, _motors.Low, Tolerance, "For ~120 ms.");
        }

        [Test]
        public void OnlyTheLastFollow_TheFold_Pulses()
        {
            Beat(FeelPhase.Follow, 0f);
            Run(Frame);
            Assert.AreEqual(0f, _motors.Low, "The tumble (first Follow, strength 0) is quiet.");
            Beat(FeelPhase.Follow, 1f);
            Run(Frame);
            Assert.AreEqual(0.05f, _motors.Low, Tolerance, "The fold (last Follow, strength 1) pulses.");
        }

        [Test]
        public void Releasing_FadesOffIn150Ms_AndTheLeanEasesBack()
        {
            Beat(FeelPhase.Anticipate, seconds: 0.65f);
            Run(0.7f);
            Beat(FeelPhase.Cancel);
            Run(0.08f);
            Assert.Greater(_motors.Low, 0f, "Fading, not cut.");
            Run(0.1f);
            Assert.AreEqual(0f, _motors.Low);
            Run(3f);
            Assert.IsTrue(_feel.Nudge.IsAtRest(1e-3f));
        }

        [Test]
        public void Off_Paused_OrNoGamepad_IsSilent_AndStrengthScales()
        {
            Beat(FeelPhase.Act);
            Run(0.2f);
            Assert.AreEqual(0.12f, _motors.Low, Tolerance);

            _preferences.RumbleStrength = 0.5f;
            Run(Frame);
            Assert.AreEqual(0.06f, _motors.Low, Tolerance, "Strength scales the mix.");

            _preferences.RumbleEnabled = false;
            Run(Frame);
            Assert.AreEqual(0f, _motors.Low, "Off stops at once.");

            _preferences.RumbleEnabled = true;
            Beat(FeelPhase.Act);
            _events.Publish(new PauseChanged(true));
            Run(Frame);
            Assert.AreEqual(0f, _motors.Low, "Paused stops at once.");
            _events.Publish(new PauseChanged(false));

            _motors.IsConnected = false;
            Beat(FeelPhase.Impact);
            Run(Frame);
            Assert.AreEqual(0f, _motors.Low, "Never rumbles without a pad.");
        }

        [Test]
        public void ObstacleCollision_JoltsChassis_AndPulsesRumble()
        {
            _events.Publish(new RoverCollided(Vector3.zero, Vector3.back, 6f));
            Run(Frame);
            Assert.Greater(_motors.Low, 0.2f, "Hard collision pulses rumble.");
            Run(0.04f);
            Assert.Less(_feel.Nudge.Heave, -0.01f, "Collision compresses heave.");
            Assert.Greater(_feel.Nudge.Pitch, 1f, "Collision dips the nose.");
        }
    }
}
