using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class StuckRecoveryTests
    {
        private const float Step = 0.02f;
        private RecoverySettings _settings;
        private StuckDetector _detector;

        [SetUp]
        public void SetUp()
        {
            _settings = new RecoverySettings();
            _detector = new StuckDetector(_settings);
        }

        /// <summary>Feeds a session; returns how many times 07 was found stuck.</summary>
        private int Run(float seconds, float throttle, Vector3 start, Vector3 velocity)
        {
            int stuck = 0;
            Vector3 position = start;
            for (float t = 0f; t < seconds; t += Step)
            {
                position += velocity * Step;
                stuck += _detector.Step(throttle, position, Step) ? 1 : 0;
            }

            return stuck;
        }

        [Test]
        public void Parked_NeverTriggers()
        {
            Assert.AreEqual(0, Run(60f, 0f, Vector3.zero, Vector3.zero));
        }

        [Test]
        public void SlowClimb_NeverTriggers()
        {
            Assert.AreEqual(0, Run(60f, 1f, Vector3.zero, new Vector3(0f, 0.2f, 0.35f)));
        }

        [Test]
        public void PushingAgainstAWall_TriggersAfterStuckTime()
        {
            Assert.AreEqual(0, Run(_settings.StuckTime - 0.1f, 1f, Vector3.zero, Vector3.zero));
            Assert.AreEqual(1, Run(0.2f, 1f, Vector3.zero, Vector3.zero));
        }

        [Test]
        public void RockingInAHollow_StillCountsAsStuck()
        {
            int stuck = 0;
            for (float t = 0f; t < _settings.StuckTime + 0.5f; t += Step)
            {
                var position = new Vector3(0f, 0f, 0.5f * _settings.ProgressRadius * Mathf.Sin(t * 4f));
                stuck += _detector.Step(1f, position, Step) ? 1 : 0;
            }

            Assert.AreEqual(1, stuck);
        }

        [Test]
        public void LettingGo_ResetsTheClock()
        {
            Run(_settings.StuckTime - 0.5f, 1f, Vector3.zero, Vector3.zero);
            Run(0.1f, 0f, Vector3.zero, Vector3.zero);
            Assert.AreEqual(0, Run(_settings.StuckTime - 0.5f, 1f, Vector3.zero, Vector3.zero));
        }

        [Test]
        public void TriggersOnce_ThenReArms()
        {
            Assert.AreEqual(2, Run(2f * _settings.StuckTime + 0.1f, -1f, Vector3.zero, Vector3.zero));
        }

        [Test]
        public void Search_StartsBehindAtTheNearestRing_AndCoversEveryDirection()
        {
            Vector2 first = RecoveryPlanner.CandidateOffset(_settings, 0f, 0);
            Assert.AreEqual(-_settings.SearchMinRadius, first.y, 1e-4f, "First look straight behind.");
            Assert.AreEqual(0f, first.x, 1e-4f);

            var bearings = new HashSet<int>();
            for (int i = 0; i < _settings.SearchDirections; i++)
            {
                Vector2 offset = RecoveryPlanner.CandidateOffset(_settings, 90f, i);
                Assert.AreEqual(_settings.SearchMinRadius, offset.magnitude, 1e-3f);
                bearings.Add(Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg, 360f)));
            }

            Assert.AreEqual(_settings.SearchDirections, bearings.Count, "No direction tried twice per ring.");
            Vector2 last = RecoveryPlanner.CandidateOffset(_settings, 0f, _settings.SearchCandidates - 1);
            float farthest = _settings.SearchMinRadius + (_settings.SearchRings - 1) * _settings.SearchRingStep;
            Assert.AreEqual(farthest, last.magnitude, 1e-3f);
        }

        [Test]
        public void Search_SweepsAlternatelyLeftAndRight()
        {
            Vector2 second = RecoveryPlanner.CandidateOffset(_settings, 0f, 1);
            Vector2 third = RecoveryPlanner.CandidateOffset(_settings, 0f, 2);
            Assert.AreEqual(-Mathf.Sign(second.x), Mathf.Sign(third.x));
            Assert.Less(second.y, 0f, "Still behind.");
        }

        [Test]
        public void Lift_StartsAndEndsExactly_RisesAndNeverJolts()
        {
            var from = new Vector3(0f, 1f, 0f);
            var to = new Vector3(3f, 0.5f, -2f);
            Assert.AreEqual(from, RecoveryPlanner.LiftPosition(from, to, 1.5f, 0f));
            Assert.AreEqual(to, RecoveryPlanner.LiftPosition(from, to, 1.5f, 1f));
            Vector3 middle = RecoveryPlanner.LiftPosition(from, to, 1.5f, 0.5f);
            Assert.AreEqual(0.75f + 1.5f, middle.y, 1e-4f);

            Vector3 previous = from;
            float largestStep = 0f;
            for (int i = 1; i <= 100; i++)
            {
                Vector3 point = RecoveryPlanner.LiftPosition(from, to, 1.5f, i / 100f);
                largestStep = Mathf.Max(largestStep, Vector3.Distance(previous, point));
                previous = point;
            }

            Vector3 firstStep = RecoveryPlanner.LiftPosition(from, to, 1.5f, 0.01f) - from;
            Assert.Less(firstStep.magnitude, 0.2f * largestStep, "Leaves gently.");
        }
    }
}
