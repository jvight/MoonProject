using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The answer to the call (opening v3 R8, <see cref="CallAnswer"/>): when 07 may transmit, the hold, the pulse out
    /// to relay.1 and on to the Peak, the milestone, that it happens once, and that a loaded game is silent.
    /// </summary>
    public sealed class CallAnswerTests
    {
        private const float Step = 1f / 30f;

        private sealed class FakeCall : ICallSource
        {
            public Vector3 Direction => Vector3.forward;

            public CallState State { get; set; } = CallState.Clear;

            public int Decoded => 0;

            public int Total => 4;

            public string Sender => "ARK";
        }

        private static readonly Vector3 Mast = new Vector3(10f, 0f, 5f);
        private static readonly Vector3 Relay = new Vector3(-300f, 0f, 10f);
        private static readonly Vector3 Peak = new Vector3(100f, 120f, 400f);

        private EventBus _events;
        private FakeCall _call;
        private CallReplySettings _replySettings;
        private CallAnswerSettings _settings;
        private CallReply _reply;
        private MastRaise _raise;
        private CallAnswer _answer;
        private readonly List<string> _log = new List<string>();
        private readonly List<NetworkPulseLaunched> _pulses = new List<NetworkPulseLaunched>();
        private int _prompts;
        private int _milestones;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _call = new FakeCall();
            _replySettings = new CallReplySettings();
            _settings = new CallAnswerSettings();
            _reply = new CallReply(_events, _call, _replySettings);
            _raise = new MastRaise(_events, _replySettings);
            _answer = new CallAnswer(_events, _call, _reply, _raise, _settings, Relay, Peak);
            _log.Clear();
            _pulses.Clear();
            _prompts = 0;
            _milestones = 0;
            _events.Subscribe<CallAnswerPhaseChanged>(changed => _log.Add("phase:" + changed.Phase +
                (changed.Restored ? "(restored)" : string.Empty)));
            _events.Subscribe<NetworkPulseLaunched>(pulse =>
            {
                _pulses.Add(pulse);
                _log.Add("pulse" + pulse.Leg);
            });
            _events.Subscribe<RelayAcked>(_ => _log.Add("ack"));
            _events.Subscribe<PeakDishGlinted>(_ => _log.Add("glint"));
            _events.Subscribe<TransmitPrompted>(_ => _prompts++);
            _events.Subscribe<OpeningMilestoneReached>(reached =>
            {
                if (reached.Milestone == OpeningMilestone.CallAnswered)
                {
                    _milestones++;
                    _log.Add("milestone");
                }
            });
        }

        private void Stand()
        {
            _events.Publish(new StructureBuilt(1, StructureIds.AntennaMast, Mast, 0f, true));
            _reply.Restore(new CallSaveData { replyTried = true });
        }

        private void Run(float seconds, bool night, bool held, Vector3 rover, float speed = 0f)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                _answer.Step(Step, night, held, rover, speed);
            }
        }

        [Test]
        public void NothingIsReady_UntilTheMastStands_TheNightFalls_TheCallIsClear_AndTheReplyFailed()
        {
            Run(1f, true, false, Mast);
            Assert.AreEqual(AnswerPhase.NotReady, _answer.Phase, "no mast yet");
            _events.Publish(new StructureBuilt(1, StructureIds.AntennaMast, Mast, 0f));
            Run(1f, true, false, Mast);
            Assert.AreEqual(AnswerPhase.NotReady, _answer.Phase, "a mast still being raised is not standing");
            _raise.Step(MastRaise.TotalSeconds(_replySettings) + 1f);
            Run(1f, true, false, Mast);
            Assert.AreEqual(AnswerPhase.NotReady, _answer.Phase, "the reply has not failed yet");
            _reply.Restore(new CallSaveData { replyTried = true });
            Run(1f, false, false, Mast);
            Assert.AreEqual(AnswerPhase.NotReady, _answer.Phase, "not before dark");
            _call.State = CallState.Locked;
            Run(1f, true, false, Mast);
            Assert.AreEqual(AnswerPhase.NotReady, _answer.Phase, "nor while the call is only locked");
            _call.State = CallState.Clear;
            Run(1f, true, false, Mast);
            Assert.AreEqual(AnswerPhase.Ready, _answer.Phase, "all of it: 07 may answer");
        }

        [Test]
        public void HoldingAtTheMast_FillsTheRing_LettingGoDrainsIt_AndTheFarOrMovingCannotTransmit()
        {
            Stand();
            Run(0.5f, true, false, Mast);
            Assert.AreEqual(AnswerPhase.Ready, _answer.Phase);
            Run(_settings.HoldSeconds * 0.5f, true, true, Mast);
            Assert.AreEqual(0.5f, _answer.HoldProgress01, 0.05f, "half the hold, half the ring");
            Assert.IsTrue(_answer.AtTheMast);
            Run(_settings.HoldSeconds * 0.3f, true, false, Mast);
            Assert.Less(_answer.HoldProgress01, 0.05f, "let go, it drains (faster than it filled)");
            Run(5f, true, true, Mast + new Vector3(_settings.TransmitRadius + 20f, 0f, 0f));
            Assert.AreEqual(0f, _answer.HoldProgress01, "too far from the mast");
            Run(5f, true, true, Mast, _settings.StillSpeed + 1f);
            Assert.AreEqual(0f, _answer.HoldProgress01, "and not while driving");
            Assert.AreEqual(AnswerPhase.Ready, _answer.Phase);
        }

        [Test]
        public void SteppingUpToTheMast_PromptsOnce_UntilAWayOffAndBack()
        {
            Stand();
            Run(0.5f, true, false, Mast + new Vector3(50f, 0f, 0f));
            Assert.AreEqual(0, _prompts);
            Run(0.5f, true, false, Mast);
            Assert.AreEqual(1, _prompts, "07 arrives: TX · READY again");
            Run(2f, true, false, Mast);
            Assert.AreEqual(1, _prompts, "not every frame");
            Run(0.5f, true, false, Mast + new Vector3(50f, 0f, 0f));
            Run(0.5f, true, false, Mast);
            Assert.AreEqual(2, _prompts, "but again after a way off");
        }

        [Test]
        public void TheTransmission_RunsPulseToRelay_AcksWithTheMilestone_ThenOnToThePeak_OnceOnly()
        {
            Stand();
            Run(0.5f, true, false, Mast);
            Run(_settings.HoldSeconds + 0.2f, true, true, Mast);
            Assert.AreEqual(AnswerPhase.Transmitting, _answer.Phase);
            Assert.AreEqual(0, _milestones, "the call is not answered before relay.1 answers");
            Assert.AreEqual(1, _pulses.Count);
            Assert.AreEqual(Mast, _pulses[0].From);
            Assert.AreEqual(Relay, _pulses[0].To);
            Assert.That(_pulses[0].Seconds, Is.InRange(_settings.MinLegSeconds, _settings.MaxLegSeconds));

            Run(_pulses[0].Seconds + 0.3f, true, false, Mast);
            Assert.AreEqual(AnswerPhase.Answered, _answer.Phase, "relay.1 has answered");
            Assert.AreEqual(1, _milestones, "CallAnswered is published once");
            Run(_settings.RouteDelaySeconds + 0.3f, true, false, Mast);
            Assert.AreEqual(2, _pulses.Count, "the pulse runs on from relay.1");
            Assert.AreEqual(Relay, _pulses[1].From);
            Assert.AreEqual(Peak.x, _pulses[1].To.x, 1e-3f);
            Assert.Greater(_pulses[1].To.y, Peak.y, "to the dish, a little above the summit's ground");
            Run(_pulses[1].Seconds + 0.5f, true, false, Mast);
            Assert.AreEqual(AnswerPhase.Done, _answer.Phase);

            CollectionAssert.AreEqual(new[]
            {
                "phase:Ready", "phase:Transmitting", "pulse0", "phase:Answered", "ack", "milestone", "pulse1",
                "glint", "phase:Done",
            }, _log, "in this order, each once");
            Run(60f, true, true, Mast);
            Assert.AreEqual(1, _milestones, "and never again");
        }

        [Test]
        public void ACallAlreadyAnswered_WhenALoadedGameStarts_IsDoneAndSilent()
        {
            Stand();
            _call.State = CallState.Answered;
            Run(2f, true, true, Mast);
            Assert.AreEqual(AnswerPhase.Done, _answer.Phase);
            Assert.AreEqual(0, _pulses.Count);
            Assert.AreEqual(0, _milestones);
            CollectionAssert.AreEqual(new[] { "phase:Done(restored)" }, _log);
        }

        [Test]
        public void ARelayAcksPositionAndTheLegSeconds_FollowTheDistance()
        {
            Assert.AreEqual(_settings.MinLegSeconds, _settings.LegSeconds(1f), "short ways take the least");
            Assert.AreEqual(_settings.MaxLegSeconds, _settings.LegSeconds(100000f), "long ways take the most");
            float mid = _settings.PulseSpeed * (_settings.MinLegSeconds + _settings.MaxLegSeconds) * 0.5f;
            Assert.AreEqual((_settings.MinLegSeconds + _settings.MaxLegSeconds) * 0.5f, _settings.LegSeconds(mid), 1e-3f);
        }
    }
}
