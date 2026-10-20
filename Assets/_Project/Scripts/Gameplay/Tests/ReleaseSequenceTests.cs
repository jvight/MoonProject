using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The release after the answer (opening v3 R9, <see cref="ReleaseSequence"/>): the ring, then the view, then the
    /// rack at the next dock, in that order and once; and what a loaded game finds.
    /// </summary>
    public sealed class ReleaseSequenceTests
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

        private sealed class FakeRing : IRangeRing
        {
            public bool IsShown { get; set; }

            public RangeRingMode Mode { get; set; }

            public float Visibility01 => IsShown ? 1f : 0f;

            public int Count => 3;

            public Vector3 Point(int index) => Vector3.zero;
        }

        private static readonly Vector3 Relay = new Vector3(-300f, 3f, 0f);
        private static readonly Vector3 Dish = new Vector3(100f, 120f, 400f);
        private static readonly Vector3 Kestrel = new Vector3(20f, 0f, 60f);

        private EventBus _events;
        private FakeCall _call;
        private FakeRing _ring;
        private CallAnswerSettings _settings;
        private ReleaseSequence _release;
        private readonly List<string> _log = new List<string>();
        private readonly List<RangeRingMode> _rings = new List<RangeRingMode>();
        private ReleaseViewOpened _view;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _call = new FakeCall();
            _ring = new FakeRing();
            _settings = new CallAnswerSettings();
            _log.Clear();
            _rings.Clear();
            _release = new ReleaseSequence(_events, _call, _ring, mode =>
            {
                _rings.Add(mode);
                _ring.IsShown = true;
                _log.Add("ring");
            }, _settings, Relay, Dish, Kestrel);
            _events.Subscribe<ReleaseStageChanged>(changed => _log.Add("stage:" + changed.Stage +
                (changed.Restored ? "(restored)" : string.Empty)));
            _events.Subscribe<ReleaseViewOpened>(opened =>
            {
                _view = opened;
                _log.Add("view");
            });
            _events.Subscribe<SocketRackRevealed>(_ => _log.Add("rack"));
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                _release.Step(Step);
            }
        }

        private static RoverDockChanged Docked() => new RoverDockChanged(true, Vector3.zero, Quaternion.identity);

        [Test]
        public void TheRelease_WaitsForTheAnswerToFinish_AndIgnoresALoadedAnswer()
        {
            _events.Publish(new CallAnswerPhaseChanged(AnswerPhase.Answered, false));
            _events.Publish(new CallAnswerPhaseChanged(AnswerPhase.Done, true));
            Run(5f);
            Assert.AreEqual(ReleaseStage.Waiting, _release.Stage);
            Assert.AreEqual(0, _rings.Count);
        }

        [Test]
        public void TheStagger_IsTheFullRingFirst_ThenTheView_ThenTheRackAtTheNextDock()
        {
            _events.Publish(new CallAnswerPhaseChanged(AnswerPhase.Done, false));
            Assert.AreEqual(ReleaseStage.Ring, _release.Stage);
            CollectionAssert.AreEqual(new[] { "stage:Ring", "ring" }, _log, "the ring shows full, once");
            Assert.AreEqual(RangeRingMode.Full, _rings[0]);

            Run(3f);
            Assert.AreEqual(ReleaseStage.Ring, _release.Stage, "the view waits for the ring to go");
            _events.Publish(Docked());
            Assert.AreEqual(ReleaseStage.Ring, _release.Stage, "a dock now shows no rack");

            _ring.IsShown = false;
            Run(_settings.ReleaseGapSeconds * 0.5f);
            Assert.AreEqual(ReleaseStage.View, _release.Stage);
            Assert.AreEqual(0, _log.FindAll(entry => entry == "view").Count, "a breath between the ring and the view");
            Run(_settings.ReleaseGapSeconds);
            Assert.AreEqual(1, _log.FindAll(entry => entry == "view").Count, "then the view opens");
            Assert.AreEqual(Relay, _view.Relay);
            Assert.AreEqual(Dish, _view.Dish);
            Assert.AreEqual(Kestrel, _view.Kestrel);
            Assert.AreEqual(_settings.ViewSecondsEach, _view.SecondsEach);

            _events.Publish(Docked());
            Assert.AreEqual(ReleaseStage.View, _release.Stage, "a dock during the view shows no rack either");
            Run(_settings.ViewSecondsEach * 3f + 0.3f);
            Assert.AreEqual(ReleaseStage.RackPending, _release.Stage, "the view is over: the rack waits for a dock");
            Assert.AreEqual(0, _log.FindAll(entry => entry == "rack").Count);

            _events.Publish(Docked());
            Assert.AreEqual(ReleaseStage.Done, _release.Stage);
            _events.Publish(Docked());
            Run(10f);
            Assert.AreEqual(1, _log.FindAll(entry => entry == "rack").Count, "the rack shows once");
            Assert.AreEqual(1, _rings.Count, "and so does the ring");
            Assert.IsTrue(_release.RackShown);
        }

        [Test]
        public void ALoadedGame_AnsweredButWithNoRackYet_StartsAtTheRack_AndOneThatShowedItIsDone()
        {
            _call.State = CallState.Answered;
            _release.Restore(new CallSaveData { rackShown = false });
            _release.Announce();
            Assert.AreEqual(ReleaseStage.RackPending, _release.Stage);
            CollectionAssert.AreEqual(new[] { "stage:RackPending(restored)" }, _log, "no ring, no view");
            _events.Publish(Docked());
            Assert.AreEqual(ReleaseStage.Done, _release.Stage, "its next dock shows the rack");

            var fresh = new ReleaseSequence(_events, _call, _ring, mode => _rings.Add(mode), _settings, Relay, Dish, Kestrel);
            fresh.Restore(new CallSaveData { rackShown = true });
            fresh.Announce();
            Assert.AreEqual(ReleaseStage.Done, fresh.Stage);
            var saved = new CallSaveData();
            fresh.Capture(saved);
            Assert.IsTrue(saved.rackShown, "and it keeps the flag it was given");
        }

        [Test]
        public void ALoadedGame_BeforeTheAnswer_WaitsForIt()
        {
            _release.Restore(new CallSaveData());
            _release.Announce();
            Assert.AreEqual(ReleaseStage.Waiting, _release.Stage);
            Assert.AreEqual(0, _log.Count);
        }
    }
}
