using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>The shape of each self-test motion: where it starts, where it peaks, and that it comes back to rest.</summary>
    public sealed class PostMotionsTests
    {
        private PostSettings _settings;
        private PostMotions _motions;

        [SetUp]
        public void SetUp()
        {
            _settings = new PostSettings();
            _motions = new PostMotions(_settings);
        }

        [Test]
        public void TheHead_SweepsBothWays_ThenNods_AndEndsAtRest()
        {
            float left = 0f;
            float right = 0f;
            float nod = 0f;
            for (float p = 0f; p <= 1f; p += 0.005f)
            {
                Vector2 head = _motions.Head(p);
                left = Mathf.Min(left, head.x);
                right = Mathf.Max(right, head.x);
                nod = Mathf.Min(nod, head.y);
            }

            Assert.Less(left, -_settings.HeadSweepDegrees * 0.4f, "it sweeps one way");
            Assert.Greater(right, _settings.HeadSweepDegrees * 0.4f, "and the other");
            Assert.Less(nod, -_settings.HeadNodDegrees * 0.8f, "then nods down");
            Assert.AreEqual(Vector2.zero, _motions.Head(0f));
            Assert.AreEqual(0f, _motions.Head(1f).y, 1e-3f);
        }

        [Test]
        public void TheWheels_TwitchOneAtATime_InOrder()
        {
            for (int wheel = 0; wheel < 4; wheel++)
            {
                float center = PostMotions.Pulse(BootCheckStep.Wheels, wheel).Share + 0.11f;
                Assert.Greater(Mathf.Abs(_motions.Wheel(wheel, center - 0.04f)), 1f, $"wheel {wheel} twitches in its window");
                for (int other = 0; other < 4; other++)
                {
                    if (other != wheel)
                    {
                        Assert.AreEqual(0f, _motions.Wheel(other, center), 1e-5f, $"wheel {other} is still while {wheel} twitches");
                    }
                }
            }
        }

        [Test]
        public void TheMotor_ShudderSettlesAndTheLurchGoesOutAndBack()
        {
            Assert.AreEqual(0f, _motions.Shudder(0f), 1e-5f);
            Assert.AreEqual(0f, _motions.Shudder(0.9f), 1e-5f, "the shudder has settled");
            float peak = 0f;
            for (float p = 0f; p < 0.7f; p += 0.01f)
            {
                peak = Mathf.Max(peak, Mathf.Abs(_motions.Shudder(p)));
            }

            Assert.Greater(peak, _settings.ShudderMetres * 0.4f);
            Assert.AreEqual(0f, _motions.Lurch(0.2f), 1e-5f);
            float creep = 0f;
            for (float p = 0f; p <= 1f; p += 0.005f)
            {
                creep = Mathf.Max(creep, _motions.Lurch(p));
            }

            Assert.AreEqual(_settings.LurchMetres, creep, 1e-3f, "the lurch reaches its full creep");
            Assert.AreEqual(0f, _motions.Lurch(1f), 1e-5f, "and 07 is back where it was");
        }

        [Test]
        public void TheCutter_TriesToIgniteThreeTimes_EachWeakerThanTheLast_ThenIsDark()
        {
            float[] peaks = new float[3];
            for (int i = 0; i < 3; i++)
            {
                float start = PostMotions.Pulse(BootCheckStep.Cutter, 2 * i).Share;
                peaks[i] = _motions.CutterFlash(start + 0.09f);
            }

            Assert.Greater(peaks[0], peaks[1]);
            Assert.Greater(peaks[1], peaks[2]);
            Assert.Greater(peaks[2], 0.3f, "even the last attempt is seen");
            Assert.AreEqual(0f, _motions.CutterFlash(0.98f), 1e-5f, "then the nozzle is dark");
            Assert.AreEqual(0f, _motions.CutterFlash(0.0f), 1e-5f);
            Assert.AreNotEqual(0f, _motions.Turret(0.19f), "the turret jerks with the first attempt");
        }

        [Test]
        public void TheServo_Steps_NeverGoesBack_HoldsBetweenSteps_AndFinishesWhereItShould()
        {
            const int steps = 3;
            Assert.AreEqual(0f, PostMotions.ServoCurve(0f, steps, 0.2f, 0.4f, 0.1f), 1e-6f);
            Assert.AreEqual(1f, PostMotions.ServoCurve(1f, steps, 0.2f, 0.4f, 0.1f), 1e-6f);
            float last = 0f;
            int flat = 0;
            for (float u = 0.001f; u <= 1f; u += 0.001f)
            {
                float value = PostMotions.ServoCurve(u, steps, 0.2f, 0.4f, 0.1f);
                Assert.GreaterOrEqual(value, last - 1e-6f, "a servo never runs backwards");
                flat += value - last < 1e-6f ? 1 : 0;
                last = value;
            }

            Assert.Greater(flat, 150, "it holds still between steps, a fifth of each");
            for (int i = 1; i < steps; i++)
            {
                Assert.AreEqual((float)i / steps, PostMotions.ServoCurve((float)i / steps - 1e-4f, steps, 0.2f, 0.4f, 0.1f),
                    2e-3f, "each step lands exactly where the next one starts");
            }
        }

        [Test]
        public void AStiffServo_CreepsThenCatchesThenFrees_NotOneSmoothMove()
        {
            const float catchLevel = 0.4f;
            float early = PostMotions.ServoCurve(0.2f, 1, 0.2f, catchLevel, 0.1f);
            float caught = PostMotions.ServoCurve(0.4f, 1, 0.2f, catchLevel, 0.1f);
            float caughtLater = PostMotions.ServoCurve(0.44f, 1, 0.2f, catchLevel, 0.1f);
            Assert.Less(early, catchLevel, "it creeps toward the catch");
            Assert.AreEqual(catchLevel, caught, 1e-3f, "it catches there");
            Assert.AreEqual(caught, caughtLater, 1e-3f, "and stays caught a moment");
            Assert.Greater(PostMotions.ServoCurve(0.7f, 1, 0.2f, catchLevel, 0.1f), catchLevel + 0.2f, "then frees");
        }

        [Test]
        public void TheDefaultBoot_IsSlow_NothingSnapsAndTheShudderIsNotABuzz()
        {
            var lines = new PostSettings();
            float headSeconds = 0f;
            float shudderSeconds = 0f;
            for (int i = 0; i < lines.LineCount; i++)
            {
                BootCheckStep check = lines.LineStep(i);
                float seconds = lines.LineSeconds(i);
                if (check == BootCheckStep.Head)
                {
                    headSeconds = seconds;
                }
                else if (check == BootCheckStep.Motor)
                {
                    shudderSeconds = seconds;
                }
                else if (check == BootCheckStep.Wheels || check == BootCheckStep.SolarWing || check == BootCheckStep.Antenna)
                {
                    Assert.GreaterOrEqual(seconds, 1f, $"{check}: a part of an old machine takes its time");
                }
            }

            Assert.GreaterOrEqual(headSeconds, 1.2f, "the head check has time to catch and free");
            Assert.GreaterOrEqual(shudderSeconds, 1f, "the motor check has time for its bursts");

            float fastest = 0f;
            float previous = _motions.Head(0f).x;
            const float step = 0.0005f;
            for (float p = step; p <= 1f; p += step)
            {
                float yaw = _motions.Head(p).x;
                fastest = Mathf.Max(fastest, Mathf.Abs(yaw - previous) / (step * headSeconds));
                previous = yaw;
            }

            Assert.Less(fastest, 70f, "the neck never swings faster than 70 deg/s (the old sweep reached 200)");

            int crossings = 0;
            float before = 0f;
            for (float p = step; p <= 1f; p += step)
            {
                float shake = _motions.Shudder(p);
                crossings += before * shake < 0f ? 1 : 0;
                before = Mathf.Abs(shake) > 1e-7f ? shake : before;
            }

            Assert.Less(crossings / shudderSeconds, 20f, "the shudder is a slow shake, not a buzz (the old one crossed zero 60 times a second)");
        }

        [Test]
        public void TheWing_JerksHalfOpen_StallsThere_ThenFoldsBack()
        {
            Assert.AreEqual(0f, _motions.Wing(0f), 1e-5f);
            Assert.AreEqual(_settings.WingJerkOpen, _motions.Wing(0.3f), 1e-3f, "the jerk");
            Assert.AreEqual(_settings.WingJerkOpen, _motions.Wing(0.4f), 0.05f, "stuck half open");
            Assert.Less(_motions.Wing(0.85f), _settings.WingJerkOpen * 0.5f, "folding back");
            Assert.AreEqual(0f, _motions.Wing(1f), 1e-5f, "folded");
        }

        [Test]
        public void TheAntennaRises_TheCellsFillToHalf()
        {
            Assert.AreEqual(0f, _motions.Antenna(0f), 1e-5f);
            Assert.AreEqual(_settings.AntennaRiseMetres, _motions.Antenna(1f), 1e-4f);
            Assert.AreEqual(0f, _motions.Battery(0f), 1e-5f);
            Assert.AreEqual(0.5f, _motions.Battery(1f), 1e-4f, "the battery reads 50%");
        }

        [Test]
        public void TheReadyPerformance_BlinksTwice_ClicksTheLampsOffAndOn_ThenGlances_WithoutAShake()
        {
            Assert.AreEqual(2, PostMotions.BlinkCount);
            Assert.AreEqual(0f, PostMotions.BlinkAt(0), 1e-5f, "the double blink opens the performance");
            Assert.That(PostMotions.BlinkAt(1), Is.InRange(0.05f, 0.25f), "and its second blink follows close");

            Assert.AreEqual(1f, PostMotions.Lamps(0f));
            Assert.AreEqual(0f, PostMotions.Lamps(0.3f), "the headlamps click off");
            Assert.AreEqual(1f, PostMotions.Lamps(0.5f), "and on again");
            Assert.AreEqual(1f, PostMotions.Lamps(1f));

            Assert.AreEqual(0f, PostMotions.DustOffProgress(0.3f), 1e-5f, "the dust waits for the lamps to come back");
            Assert.AreEqual(1f, PostMotions.DustOffProgress(0.75f), 1e-4f, "the last dust is off");

            Assert.AreEqual(0f, PostMotions.BeaconTurn(0.6f), 1e-5f, "the glance comes last");
            Assert.AreEqual(1f, PostMotions.BeaconTurn(1f), 1e-4f, "the head has turned to the beacon");
        }

        [Test]
        public void TheReadyPerformance_SoundsAreSorted_AndUseTheStartChecksPulses()
        {
            float last = -1f;
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int n = 0; n < PostMotions.PerformPulseCount; n++)
            {
                PostMotions.PulseMark mark = PostMotions.PerformPulse(n);
                Assert.GreaterOrEqual(mark.Share, last, "sounds fire in order");
                Assert.IsTrue(seen.Add(mark.Index), $"pulse index {mark.Index} is used once");
                last = mark.Share;
            }

            CollectionAssert.AreEquivalent(new[] { 0, 2, 3 }, seen, "glance, lamps on, lamps off: no shiver");
            Assert.AreEqual(0f, PostMotions.Lamps(PostMotions.PerformPulse(0).Share), "the first click turns them off");
            Assert.AreEqual(1f, PostMotions.Lamps(PostMotions.PerformPulse(1).Share), "the second turns them on");
        }

        [Test]
        public void ThePulses_AreSortedByMoment_AndHaveDistinctIndicesPerStep()
        {
            foreach (BootCheckStep step in new[]
            {
                BootCheckStep.Head, BootCheckStep.Wheels, BootCheckStep.Motor, BootCheckStep.Cutter,
                BootCheckStep.SolarWing, BootCheckStep.Antenna,
            })
            {
                float last = -1f;
                var seen = new System.Collections.Generic.HashSet<int>();
                for (int n = 0; n < PostMotions.PulseCount(step); n++)
                {
                    PostMotions.PulseMark mark = PostMotions.Pulse(step, n);
                    Assert.GreaterOrEqual(mark.Share, last, $"{step}: pulses fire in order");
                    Assert.IsTrue(seen.Add(mark.Index), $"{step}: pulse index {mark.Index} is used once");
                    last = mark.Share;
                }
            }
        }
    }
}
