using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The base's wake-up (docs/features/M3-16): sleeping stations answer the sonar "not yet" when the ring reaches
    /// them, the "next" line follows the first step not yet done, and each place greets 07 once, one at a time.
    /// </summary>
    public sealed class ProgressionTests
    {
        private readonly List<StationNotYet> _notYet = new List<StationNotYet>();
        private readonly List<string> _lines = new List<string>();
        private readonly List<string> _places = new List<string>();
        private EventBus _events;
        private SonarTuning _sonar;

        [SetUp]
        public void SetUp()
        {
            _notYet.Clear();
            _lines.Clear();
            _places.Clear();
            _events = new EventBus();
            _events.Subscribe<StationNotYet>(_notYet.Add);
            _events.Subscribe<NextStepChanged>(e => _lines.Add(e.LineKey));
            _events.Subscribe<PlaceFirstApproached>(e => _places.Add(e.PlaceId));
            _sonar = ScriptableObject.CreateInstance<SonarTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sonar);
        }

        [Test]
        public void ASleepingStation_AnswersNotYet_WhenTheRingReachesIt_AndAWokenOneStaysQuiet()
        {
            bool bayAsleep = true;
            using (var answers = new DormantAnswers(_events, _sonar))
            {
                answers.Add(ProgressionKeys.NotYetBay, new Vector3(10f, 0f, 0f), () => bayAsleep);
                answers.Add(ProgressionKeys.NotYetLift, new Vector3(0f, 0f, 20f), () => false);
                answers.Add(ProgressionKeys.NotYetShelf, new Vector3(_sonar.Range * 2f, 0f, 0f), () => true);

                _events.Publish(new SonarPinged(Vector3.zero, _sonar.Range));
                answers.Tick(0f);
                Assert.IsEmpty(_notYet, "the ring has not reached it yet");
                answers.Tick(_sonar.RingDuration + _sonar.AnswerLag + 0.01f);

                Assert.AreEqual(1, _notYet.Count, "only the sleeping bay in range answers");
                Assert.AreEqual(ProgressionKeys.NotYetBay, _notYet[0].ReasonKey);
                Assert.AreEqual(10f, _notYet[0].Distance, 1e-4f);

                bayAsleep = false;
                _events.Publish(new SonarPinged(Vector3.zero, _sonar.Range));
                answers.Tick(10f);
                answers.Tick(20f);
                Assert.AreEqual(1, _notYet.Count, "once awake it answers no more");
            }
        }

        [Test]
        public void TheNextLine_IsTheFirstStepNotYetDone_AndIsAnnouncedOnlyWhenItChanges()
        {
            bool woke = false;
            bool salvaged = false;
            var steps = new StepFlow(_events);
            steps.Add(ProgressionKeys.NextWake, () => woke);
            steps.Add(ProgressionKeys.NextSalvage, () => salvaged);

            steps.Tick();
            steps.Tick();
            salvaged = true;
            steps.Tick();
            woke = true;
            steps.Tick();

            CollectionAssert.AreEqual(new[] { ProgressionKeys.NextWake, string.Empty }, _lines,
                "announced at start, then empty once every step is done; a later step done early changes nothing");
            Assert.AreEqual(string.Empty, steps.Current);
        }

        [Test]
        public void EachPlace_Greets07Once_OneAtATime_OnlyWhenThere_AndRemembersAcrossALoad()
        {
            bool rackStands = false;
            PlaceApproaches places = NewPlaces(() => rackStands);
            const float gap = 2f;

            places.Tick(Vector3.zero, 0f);
            places.Tick(Vector3.zero, gap * 0.5f);
            Assert.AreEqual(new[] { ProgressionKeys.PlaceDock }, _places.ToArray(), "dock and ladder, one at a time");
            places.Tick(new Vector3(30f, 0f, 0f), gap);
            Assert.AreEqual(ProgressionKeys.PlaceLadder, _places[1], "the ladder's turn comes even once 07 left");
            places.Tick(new Vector3(60f, 0f, 0f), gap * 3f);
            Assert.AreEqual(2, _places.Count, "the rack is not there yet");
            rackStands = true;
            places.Tick(new Vector3(60f, 0f, 0f), gap * 4f);
            places.Tick(Vector3.zero, gap * 5f);
            places.Tick(Vector3.zero, gap * 6f);
            CollectionAssert.AreEqual(new[]
            {
                ProgressionKeys.PlaceDock, ProgressionKeys.PlaceLadder, ProgressionKeys.PlaceRack,
            }, _places, "each place once");
            Assert.IsTrue(places.HasApproached(ProgressionKeys.PlaceRack));

            string[] saved = places.Capture();
            _places.Clear();
            PlaceApproaches loaded = NewPlaces(() => true);
            loaded.Restore(saved);
            loaded.Tick(Vector3.zero, 0f);
            loaded.Tick(new Vector3(60f, 0f, 0f), gap * 2f);
            Assert.IsEmpty(_places, "a loaded game greets nothing twice");
        }

        private PlaceApproaches NewPlaces(System.Func<bool> rackStands)
        {
            var places = new PlaceApproaches(_events, 2f);
            places.Add(ProgressionKeys.PlaceDock, Vector3.zero, 3f);
            places.Add(ProgressionKeys.PlaceLadder, new Vector3(0.5f, 0f, 0f), 3f);
            places.Add(ProgressionKeys.PlaceRack, new Vector3(60f, 0f, 0f), 4f, rackStands);
            return places;
        }
    }
}
