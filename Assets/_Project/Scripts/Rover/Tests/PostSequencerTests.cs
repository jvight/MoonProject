using System.Collections.Generic;
using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>The self-test's clock: every check in order, announced twice, never skipped, ended by Finished.</summary>
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
            _settings = new PostSettings();
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
            Assert.AreEqual(PostSequencer.StepCount * 2 + 1, _events.Count);
            BootCheckStep expected = BootCheckStep.Carrier;
            for (int i = 0; i < PostSequencer.StepCount; i++)
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
            Assert.AreEqual(PostSequencer.StepCount * 2 + 1, _events.Count);
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
            Assert.AreEqual(1.1f * 10f, ramp.Count, 2f, "the ramp is ticked about ten times a second");
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
        public void TheLines_FitTheBriefOnceTheEyesAreAwake()
        {
            Assert.That(_post.TotalSeconds, Is.InRange(20f, 28f), "The lines on the face, after the eyes wake.");
        }

        [Test]
        public void TheLampsCheck_IsAsLongAsTheLampPowerUp()
        {
            var lamps = new LampPowerUp(new OpeningSettings());
            Assert.GreaterOrEqual(_settings.SecondsOf(BootCheckStep.Lamps), lamps.TotalSeconds);
        }
    }
}
