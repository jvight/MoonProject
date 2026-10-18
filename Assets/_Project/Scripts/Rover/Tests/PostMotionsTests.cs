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
            Assert.AreEqual(_settings.LurchMetres, _motions.Lurch(0.675f), 1e-3f, "the lurch peaks midway through its window");
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
        public void TheWing_JerksHalfOpen_StallsThere_ThenFoldsBack()
        {
            Assert.AreEqual(0f, _motions.Wing(0f), 1e-5f);
            Assert.AreEqual(_settings.WingJerkOpen, _motions.Wing(0.2f), 1e-3f, "the jerk");
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
        public void TheReadyPerformance_BlinksTwice_ClicksTheLampsOffAndOn_ShiversThenGlances()
        {
            Assert.AreEqual(2, PostMotions.BlinkCount);
            Assert.AreEqual(0f, PostMotions.BlinkAt(0), 1e-5f, "the double blink opens the performance");
            Assert.That(PostMotions.BlinkAt(1), Is.InRange(0.05f, 0.25f), "and its second blink follows close");

            Assert.AreEqual(1f, PostMotions.Lamps(0f));
            Assert.AreEqual(0f, PostMotions.Lamps(0.3f), "the headlamps click off");
            Assert.AreEqual(1f, PostMotions.Lamps(0.5f), "and on again");
            Assert.AreEqual(1f, PostMotions.Lamps(1f));

            Assert.AreEqual(0f, PostMotions.Shiver(0.3f), 1e-5f, "no shiver while the lamps click");
            Assert.Greater(PostMotions.Shiver(0.5f) + PostMotions.Shiver(0.55f) + PostMotions.Shiver(0.6f), 0.1f);
            Assert.AreEqual(1f, PostMotions.ShiverProgress(0.75f), 1e-4f, "the last dust is off");

            Assert.AreEqual(0f, PostMotions.BeaconTurn(0.6f), 1e-5f, "the glance comes last");
            Assert.AreEqual(1f, PostMotions.BeaconTurn(1f), 1e-4f, "the head has turned to the beacon");
        }

        [Test]
        public void TheReadyPerformance_SoundsAreSorted_AndUseTheStartChecksFourPulses()
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

            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, seen);
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
