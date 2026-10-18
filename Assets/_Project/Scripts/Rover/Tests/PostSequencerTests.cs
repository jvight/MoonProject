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

        [SetUp]
        public void SetUp()
        {
            _settings = new PostSettings();
            _post = new PostSequencer(_settings);
            _events.Clear();
        }

        private void Drain()
        {
            while (_post.TryTake(out PostEvent postEvent))
            {
                _events.Add(postEvent);
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
        public void Begin_AnnouncesTheFirstCheck_AtOnce()
        {
            _post.Begin();
            Drain();
            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual(PostEventKind.Started, _events[0].Kind);
            Assert.AreEqual(BootCheckStep.Lens, _events[0].Step);
        }

        [Test]
        public void EveryCheck_StartsThenCompletes_InOrder_ThenFinishes()
        {
            _post.Begin();
            Drain();
            RunToEnd();
            Assert.AreEqual(PostSequencer.StepCount * 2 + 1, _events.Count);
            BootCheckStep expected = BootCheckStep.Lens;
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
        public void ALongFrame_StillAnnouncesEveryStep()
        {
            _post.Begin();
            Drain();
            _post.Step(_post.TotalSeconds + 5f);
            Drain();
            Assert.IsTrue(_post.IsFinished);
            Assert.AreEqual(PostSequencer.StepCount * 2 + 1, _events.Count);
            Assert.AreEqual(PostEventKind.Finished, _events[_events.Count - 1].Kind);
        }

        [Test]
        public void TheChecks_FitTheBriefAfterTheLensStages()
        {
            Assert.That(_post.TotalSeconds, Is.InRange(14f, 24f), "The checks themselves, after the lens stages.");
        }

        [Test]
        public void TheLampsCheck_IsAsLongAsTheLampPowerUp()
        {
            var lamps = new LampPowerUp(new OpeningSettings());
            Assert.GreaterOrEqual(_settings.SecondsOf(BootCheckStep.Lamps), lamps.TotalSeconds);
        }
    }
}
