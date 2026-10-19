using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using MoonProject.Core;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The self-test's clock: every tuned line in order, announced twice, never skipped, ended by Finished. The motion
    /// tests run every check (<see cref="Full"/>), since the opening's own lines leave some out for pace.
    /// </summary>
    public sealed class PostSequencerTests
    {
        private const float Frame = 1f / 60f;

        private PostSettings _settings;
        private PostSequencer _post;
        private readonly List<PostEvent> _events = new List<PostEvent>();
        private readonly List<PostEvent> _all = new List<PostEvent>();

        [SetUp]
        public void SetUp()
        {
            Use(Full());
        }

        /// <summary>Every check of the self-test, at the seconds they had before the opening was cut for pace.</summary>
        internal static PostSettings Full()
        {
            return new PostSettings(new[]
            {
                new PostLine(BootCheckStep.Carrier, 1.4f),
                new PostLine(BootCheckStep.Resume, 1.4f),
                new PostLine(BootCheckStep.Agenda, 1.6f),
                new PostLine(BootCheckStep.Lens, 1f),
                new PostLine(BootCheckStep.Lamps, 2.4f),
                new PostLine(BootCheckStep.Head, 1.8f),
                new PostLine(BootCheckStep.Wheels, 1.8f),
                new PostLine(BootCheckStep.Motor, 1.6f),
                new PostLine(BootCheckStep.Cutter, 2.4f),
                new PostLine(BootCheckStep.SolarWing, 2f),
                new PostLine(BootCheckStep.Antenna, 1.8f),
                new PostLine(BootCheckStep.Battery, 1.2f),
                new PostLine(BootCheckStep.Assembly, 0.7f),
                new PostLine(BootCheckStep.Begin, 1.4f),
            });
        }

        /// <summary>Every check but the wheels', as tuning may leave one out.</summary>
        internal static PostSettings WithoutWheels()
        {
            return new PostSettings(new[]
            {
                new PostLine(BootCheckStep.Carrier, 0.7f),
                new PostLine(BootCheckStep.Lamps, 1.2f),
                new PostLine(BootCheckStep.Cutter, 1.2f),
                new PostLine(BootCheckStep.Begin, 0.4f),
            });
        }

        private static PostSettings Shipped()
        {
            return AssetDatabase.LoadAssetAtPath<RoverCameraTuning>(RoverAssetPaths.CameraTuning).Opening.Post;
        }

        private void Use(PostSettings settings)
        {
            _settings = settings;
            _post = new PostSequencer(_settings, new LampPowerUp(new OpeningSettings()));
            _events.Clear();
            _all.Clear();
        }

        private void Drain()
        {
            while (_post.TryTake(out PostEvent postEvent))
            {
                _all.Add(postEvent);
                if (postEvent.Kind != PostEventKind.Pulse)
                {
                    _events.Add(postEvent);
                }
            }
        }

        private void RunToEnd()
        {
            float time = 0f;
            while (!_post.IsFinished && time < 120f)
            {
                _post.Step(Frame);
                time += Frame;
                Drain();
            }
        }

        [Test]
        public void Begin_AnnouncesTheFirstLine_AtOnce()
        {
            _post.Begin();
            Drain();
            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual(PostEventKind.Started, _events[0].Kind);
            Assert.AreEqual(BootCheckStep.Carrier, _events[0].Step, "why 07 woke comes first");
        }

        [Test]
        public void EveryCheck_StartsThenCompletes_InOrder_ThenFinishes()
        {
            _post.Begin();
            Drain();
            RunToEnd();
            Assert.AreEqual(_post.StepCount * 2 + 1, _events.Count);
            BootCheckStep expected = BootCheckStep.Carrier;
            for (int i = 0; i < _post.StepCount; i++)
            {
                PostEvent started = _events[2 * i];
                PostEvent completed = _events[2 * i + 1];
                Assert.AreEqual(PostEventKind.Started, started.Kind);
                Assert.AreEqual(PostEventKind.Completed, completed.Kind);
                Assert.AreEqual(expected, started.Step, "The checks run in their order.");
                Assert.AreEqual(expected, completed.Step);
                expected++;
            }

            Assert.AreEqual(PostEventKind.Finished, _events[_events.Count - 1].Kind);
        }

        [Test]
        public void TheResults_AreTheStory()
        {
            _post.Begin();
            Drain();
            RunToEnd();
            var results = new Dictionary<BootCheckStep, BootCheckResult>();
            foreach (PostEvent postEvent in _events)
            {
                if (postEvent.Kind == PostEventKind.Completed)
                {
                    results[postEvent.Step] = postEvent.Result;
                }
            }

            Assert.AreEqual(BootCheckResult.Ok, results[BootCheckStep.Lamps]);
            Assert.AreEqual(BootCheckResult.Ok, results[BootCheckStep.Wheels]);
            Assert.AreEqual(BootCheckResult.Fault, results[BootCheckStep.Cutter], "The igniter is missing.");
            Assert.AreEqual(BootCheckResult.Jammed, results[BootCheckStep.SolarWing]);
            Assert.AreEqual(BootCheckResult.ReceiveOnly, results[BootCheckStep.Antenna]);
            Assert.AreEqual(BootCheckResult.Ok, results[BootCheckStep.Battery]);
            Assert.AreEqual(BootCheckResult.Info, results[BootCheckStep.Begin]);
        }

        [Test]
        public void TheRun_TakesItsTunedTime_AndNeverEndsEarly()
        {
            _post.Begin();
            float time = 0f;
            while (!_post.IsFinished && time < 120f)
            {
                _post.Step(Frame);
                time += Frame;
            }

            Assert.AreEqual(_post.TotalSeconds, time, 2f * Frame);
        }

        [Test]
        public void ALongHitchyFrame_StillAnnouncesEveryLine()
        {
            const float Hitch = 0.7f;
            _post.Begin();
            Drain();
            float guard = 0f;
            while (!_post.IsFinished && guard < 120f)
            {
                _post.Step(Hitch);
                Drain();
                guard += Hitch;
            }

            Assert.IsTrue(_post.IsFinished);
            Assert.AreEqual(_post.StepCount * 2 + 1, _events.Count);
            Assert.AreEqual(PostEventKind.Finished, _events[_events.Count - 1].Kind);
        }

        [Test]
        public void EachCheck_PulsesItsTicks_BetweenItsStartAndItsCompletion()
        {
            _post.Begin();
            Drain();
            RunToEnd();
            BootCheckStep current = BootCheckStep.Carrier;
            bool inside = false;
            var pulses = new Dictionary<BootCheckStep, List<int>>();
            foreach (PostEvent postEvent in _all)
            {
                if (postEvent.Kind == PostEventKind.Started)
                {
                    current = postEvent.Step;
                    inside = true;
                }
                else if (postEvent.Kind == PostEventKind.Completed)
                {
                    inside = false;
                }
                else if (postEvent.Kind == PostEventKind.Pulse)
                {
                    Assert.IsTrue(inside, "a pulse comes inside its check");
                    Assert.AreEqual(current, postEvent.Step);
                    if (!pulses.TryGetValue(postEvent.Step, out List<int> list))
                    {
                        list = new List<int>();
                        pulses[postEvent.Step] = list;
                    }

                    list.Add(postEvent.Index);
                }
            }

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, Distinct(pulses[BootCheckStep.Wheels]), "four wheel twitches");
            CollectionAssert.AreEqual(new[] { 0, 3, 1, 4, 2, 5 }, pulses[BootCheckStep.Cutter], "three sparks, each with the beacon's blink");
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, pulses[BootCheckStep.SolarWing], "jerk, stall, fold");
            CollectionAssert.AreEqual(new[] { 0, 1 }, pulses[BootCheckStep.Head], "sweep, nod");
            Assert.IsFalse(pulses.ContainsKey(BootCheckStep.Begin), "the start's motions are the ready performance");
            Assert.IsFalse(pulses.ContainsKey(BootCheckStep.Assembly), "the assembly line has nothing to tick");
        }

        [Test]
        public void TheLamps_PulseTheirThreeFlickers_ThenTheRampTenTimesASecond_UpToOne()
        {
            _post.Begin();
            Drain();
            RunToEnd();
            var flickers = new List<int>();
            var ramp = new List<float>();
            foreach (PostEvent postEvent in _all)
            {
                if (postEvent.Kind == PostEventKind.Pulse && postEvent.Step == BootCheckStep.Lamps)
                {
                    if (postEvent.Index < 3)
                    {
                        flickers.Add(postEvent.Index);
                    }
                    else
                    {
                        Assert.AreEqual(3, postEvent.Index, "the ramp is index 3");
                        ramp.Add(postEvent.Progress01);
                    }
                }
            }

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, flickers);
            Assert.AreEqual(new OpeningSettings().LampRampSeconds * 10f, ramp.Count, 2f,
                "the ramp is ticked about ten times a second");
            Assert.AreEqual(0f, ramp[0], 1e-3f);
            Assert.AreEqual(1f, ramp[ramp.Count - 1], 0.12f, "and ends at full");
            for (int i = 1; i < ramp.Count; i++)
            {
                Assert.GreaterOrEqual(ramp[i], ramp[i - 1]);
            }
        }

        [Test]
        public void TheContinuousChecks_ReportProgress_FromZeroToOne()
        {
            _post.Begin();
            Drain();
            RunToEnd();
            foreach (BootCheckStep step in new[] { BootCheckStep.Battery, BootCheckStep.Antenna })
            {
                float first = -1f;
                float last = -1f;
                int count = 0;
                foreach (PostEvent postEvent in _all)
                {
                    if (postEvent.Kind == PostEventKind.Pulse && postEvent.Step == step && postEvent.Progress01 > 0f || postEvent.Kind == PostEventKind.Pulse && postEvent.Step == step && count == 0)
                    {
                        if (step == BootCheckStep.Antenna && postEvent.Index == 0)
                        {
                            continue;
                        }

                        first = first < 0f ? postEvent.Progress01 : first;
                        last = postEvent.Progress01;
                        count++;
                    }
                }

                Assert.GreaterOrEqual(count, 5, $"{step}: the progress is published repeatedly");
                Assert.AreEqual(0f, first, 1e-3f);
                Assert.AreEqual(1f, last, 0.15f);
            }
        }

        private static List<int> Distinct(List<int> values)
        {
            var seen = new List<int>();
            foreach (int value in values)
            {
                if (!seen.Contains(value))
                {
                    seen.Add(value);
                }
            }

            return seen;
        }

        [Test]
        public void TheOpeningsLines_RunEveryCheck_FastAndDense_InUnderTenSeconds()
        {
            foreach (PostSettings settings in new[] { new PostSettings(), Shipped() })
            {
                Use(settings);
                Assert.AreEqual(Full().LineCount, settings.LineCount, "every check prints its line");
                Assert.That(_post.TotalSeconds, Is.InRange(8f, 10f), "a dense, fast run of lines");
            }
        }

        [Test]
        public void TheOpeningsLines_KeepWhoWokeAndTheMissingIgniter_AndRunInTheirOrder()
        {
            foreach (PostSettings settings in new[] { new PostSettings(), Shipped() })
            {
                Assert.IsNull(settings.Validate());
                Assert.IsTrue(settings.Runs(BootCheckStep.Carrier), "07's name and why it woke");
                Assert.IsTrue(settings.Runs(BootCheckStep.Cutter), "the missing igniter the opening asks for");
                Use(settings);
                _post.Begin();
                Drain();
                RunToEnd();
                Assert.AreEqual(settings.LineCount * 2 + 1, _events.Count);
                for (int i = 0; i < settings.LineCount; i++)
                {
                    Assert.AreEqual(settings.LineStep(i), _events[2 * i].Step);
                    Assert.AreEqual(settings.LineStep(i), _events[2 * i + 1].Step);
                }
            }
        }

        [Test]
        public void ACheckLeftOut_IsNeverAnnounced_NorPulsed()
        {
            Use(WithoutWheels());
            Assert.IsFalse(_settings.Runs(BootCheckStep.Wheels));
            _post.Begin();
            Drain();
            RunToEnd();
            foreach (PostEvent postEvent in _all)
            {
                Assert.AreNotEqual(BootCheckStep.Wheels, postEvent.Step);
            }

            Assert.AreEqual(0f, _settings.SecondsOf(BootCheckStep.Wheels));
        }

        [Test]
        public void Lines_WithoutTheCarrierFirst_OutOfOrder_OrWithoutTheLamps_AreRefused()
        {
            Assert.IsNotNull(new PostSettings(new PostLine[0]).Validate(), "no lines");
            Assert.IsNotNull(new PostSettings(new[] { new PostLine(BootCheckStep.Lamps, 1.2f) }).Validate(),
                "the carrier comes first: it clears the face's log");
            Assert.IsNotNull(new PostSettings(new[]
            {
                new PostLine(BootCheckStep.Carrier, 1f),
                new PostLine(BootCheckStep.Cutter, 1f),
                new PostLine(BootCheckStep.Lamps, 1f),
            }).Validate(), "out of step order");
            Assert.IsNotNull(new PostSettings(new[]
            {
                new PostLine(BootCheckStep.Carrier, 1f),
                new PostLine(BootCheckStep.Cutter, 1f),
            }).Validate(), "07's lamps come on in the lamps check");
            Assert.IsNull(Full().Validate());
        }

        [Test]
        public void TheLampsCheck_IsAsLongAsTheLampPowerUp()
        {
            var lamps = new LampPowerUp(new OpeningSettings());
            Assert.GreaterOrEqual(new PostSettings().SecondsOf(BootCheckStep.Lamps), lamps.TotalSeconds);
            Assert.GreaterOrEqual(_settings.SecondsOf(BootCheckStep.Lamps), lamps.TotalSeconds);
        }
    }
}
