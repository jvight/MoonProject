using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// 07's one failed reply (opening v3 R7, <see cref="CallReply"/>) and the antenna mast's raise
    /// (<see cref="MastRaise"/>): when they fire, that they fire once, and that a loaded game never repeats them.
    /// </summary>
    public sealed class FailedReplyTests
    {
        private const float Step = 1f / 30f;

        private sealed class FakeCall : ICallSource
        {
            public Vector3 Direction => Vector3.forward;

            public CallState State { get; set; }

            public int Decoded => 0;

            public int Total => 4;
        }

        private EventBus _events;
        private FakeCall _call;
        private CallReplySettings _settings;
        private CallReply _reply;
        private int _failed;
        private readonly List<MastSegmentRaised> _raised = new List<MastSegmentRaised>();
        private readonly List<float> _raisedAt = new List<float>();

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _call = new FakeCall();
            _settings = new CallReplySettings();
            _reply = new CallReply(_events, _call, _settings);
            _failed = 0;
            _raised.Clear();
            _raisedAt.Clear();
            _events.Subscribe<CallReplyFailed>(_ => _failed++);
            _events.Subscribe<MastSegmentRaised>(_raised.Add);
        }

        private void Run(float seconds, bool night, float speed)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                _reply.Step(Step, night, speed);
            }
        }

        [Test]
        public void TheReply_FailsOnce_WhenTheCallIsClear_AfterDark_And07StandsStill()
        {
            _call.State = CallState.Clear;
            Run(_settings.StillSeconds * 0.5f, true, 0f);
            Assert.AreEqual(0, _failed, "not before 07 has stood still a while");
            Run(_settings.StillSeconds, true, 0f);
            Assert.AreEqual(1, _failed, "then it tries to answer");
            Assert.IsTrue(_reply.Tried);
            Run(60f, true, 0f);
            Assert.AreEqual(1, _failed, "and never again");
        }

        [Test]
        public void TheReply_WaitsForDark_TheClearCall_AndAStill07()
        {
            _call.State = CallState.Locked;
            Run(20f, true, 0f);
            Assert.AreEqual(0, _failed, "a call that is only locked is not answered");
            _call.State = CallState.Clear;
            Run(20f, false, 0f);
            Assert.AreEqual(0, _failed, "nor before the night has fallen");
            Run(20f, true, 3f);
            Assert.AreEqual(0, _failed, "nor while 07 is driving");
            Run(_settings.StillSeconds * 0.8f, true, 0f);
            Run(0.5f, true, 3f);
            Run(_settings.StillSeconds * 0.8f, true, 0f);
            Assert.AreEqual(0, _failed, "moving again starts the wait over");
            Run(_settings.StillSeconds, true, 0f);
            Assert.AreEqual(1, _failed);
        }

        [Test]
        public void ALoadedGame_ThatAlreadyTried_SaysNothing_AndOneThatDidNotStillTriesOnce()
        {
            _call.State = CallState.Clear;
            _reply.Restore(new CallSaveData { replyTried = true });
            Run(30f, true, 0f);
            Assert.AreEqual(0, _failed, "the saved reply is not repeated");

            var fresh = new CallReply(_events, _call, _settings);
            fresh.Restore(new CallSaveData { replyTried = false });
            for (float t = 0f; t < 30f; t += Step)
            {
                fresh.Step(Step, true, 0f);
            }

            Assert.AreEqual(1, _failed, "a game that reaches the moment loaded tries once");
            var saved = new CallSaveData();
            fresh.Capture(saved);
            Assert.IsTrue(saved.replyTried, "and saves it");
        }

        [Test]
        public void AnAnsweredCall_NeverFailsItsReply()
        {
            _call.State = CallState.Answered;
            Run(30f, true, 0f);
            Assert.AreEqual(0, _failed);
            Assert.IsTrue(_reply.Tried, "it is moot");
        }

        [Test]
        public void TheMast_RaisesThreeSegmentsInOrder_EachLockingOnce_ThenIsDone()
        {
            var raise = new MastRaise(_events, _settings);
            Assert.AreEqual(3, raise.Segments);
            _events.Publish(new StructureBuilt(1, StructureIds.AntennaMast, new Vector3(4f, 0f, 5f), 0f));
            Assert.IsTrue(raise.Active);
            Assert.AreEqual(0f, raise.Lift01(0), 1e-4f, "every segment starts lowered");
            float seconds = 0f;
            float total = MastRaise.TotalSeconds(_settings) + 1f;
            for (; seconds < total; seconds += Step)
            {
                raise.Step(Step);
                while (_raisedAt.Count < _raised.Count)
                {
                    _raisedAt.Add(seconds);
                }
            }

            Assert.AreEqual(3, _raised.Count, "one event per segment");
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(i, _raised[i].Index, "base, middle, top in order");
                Assert.AreEqual(3, _raised[i].Count);
                Assert.AreEqual(new Vector3(4f, 0f, 5f), _raised[i].Position);
                Assert.AreEqual(MastRaise.LockTime(_settings, i), _raisedAt[i], Step * 2f, "each at its lock time");
            }

            Assert.IsTrue(_raised[2].IsLast);
            Assert.IsTrue(raise.Done);
            Assert.IsFalse(raise.Active);
            Assert.AreEqual(1f, raise.Lift01(2), 1e-4f);
        }

        [Test]
        public void TheMastSegments_LiftOneAfterAnother_EasedAndNeverBackwards()
        {
            var raise = new MastRaise(_events, _settings);
            _events.Publish(new StructureBuilt(1, StructureIds.AntennaMast, Vector3.zero, 0f));
            var last = new float[3];
            for (float t = 0f; t < MastRaise.TotalSeconds(_settings); t += Step)
            {
                raise.Step(Step);
                for (int i = 0; i < 3; i++)
                {
                    float lift = raise.Lift01(i);
                    Assert.GreaterOrEqual(lift, last[i] - 1e-5f, "a segment only goes up");
                    Assert.LessOrEqual(lift - last[i], 0.06f, "in small eased steps");
                    last[i] = lift;
                    if (i > 0)
                    {
                        Assert.LessOrEqual(lift, raise.Lift01(i - 1) + 1e-4f, "the base is up before the next starts");
                    }
                }
            }
        }

        [Test]
        public void AMastBuiltInAnEarlierSession_StandsRaised_AndPublishesNothing()
        {
            var raise = new MastRaise(_events, _settings);
            _events.Publish(new StructureBuilt(1, StructureIds.AntennaMast, Vector3.zero, 0f, true));
            raise.Step(10f);
            Assert.IsTrue(raise.Done);
            Assert.AreEqual(1f, raise.Lift01(0));
            Assert.AreEqual(0, _raised.Count, "a loaded game's mast is up silently");
        }

        [Test]
        public void OnlyTheMast_IsRaised()
        {
            var raise = new MastRaise(_events, _settings);
            _events.Publish(new StructureBuilt(1, StructureIds.ChargingPad, Vector3.zero, 0f));
            Assert.IsFalse(raise.Active);
            Assert.IsFalse(raise.Done);
        }
    }
}
