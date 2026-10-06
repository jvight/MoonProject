using NUnit.Framework;

namespace MoonProject.Gameplay.Tests
{
    public sealed class SonarScheduleTests
    {
        private const float Range = 140f;
        private const float Duration = 5f;
        private const float Lag = 0.3f;

        [Test]
        public void RingArrival_MatchesTheRingRadius()
        {
            foreach (float distance in new[] { 0f, 10f, 70f, 139f })
            {
                float time = SonarSchedule.RingArrival(distance, Duration, Range);
                Assert.AreEqual(distance, SonarSchedule.RingRadius(time, Duration, Range), 1e-3f);
            }

            Assert.AreEqual(Range, SonarSchedule.RingRadius(Duration, Duration, Range), 1e-4f);
        }

        [Test]
        public void Answers_ComeNearestFirst_AfterTheRingTouchesThem()
        {
            var schedule = new SonarSchedule(4);
            Assert.IsTrue(schedule.Add(0, 90f, 10f, Range, Duration, Lag));
            Assert.IsTrue(schedule.Add(1, 20f, 10f, Range, Duration, Lag));
            Assert.IsTrue(schedule.Add(2, 50f, 10f, Range, Duration, Lag));

            float nearTime = 10f + SonarSchedule.RingArrival(20f, Duration, Range) + Lag;
            Assert.IsFalse(schedule.TryPop(nearTime - 0.01f, out _, out _), "nothing before the ring arrives");
            Assert.IsTrue(schedule.TryPop(nearTime, out int first, out float distance));
            Assert.AreEqual(1, first);
            Assert.AreEqual(20f, distance);
            Assert.IsTrue(schedule.TryPop(100f, out int second, out _));
            Assert.IsTrue(schedule.TryPop(100f, out int third, out _));
            Assert.AreEqual(2, second);
            Assert.AreEqual(0, third);
            Assert.AreEqual(0, schedule.Pending);
        }

        [Test]
        public void CloserRelics_AnswerSooner_EvenThoughTheRingSlows()
        {
            float near = SonarSchedule.RingArrival(15f, Duration, Range);
            float mid = SonarSchedule.RingArrival(70f, Duration, Range);
            float far = SonarSchedule.RingArrival(135f, Duration, Range);
            Assert.Less(near, mid);
            Assert.Less(mid, far);
            Assert.Less(near, 0.4f, "a relic next door answers almost at once");
            Assert.LessOrEqual(far, Duration);
        }

        [Test]
        public void OutOfRange_IsSilent()
        {
            var schedule = new SonarSchedule(2);
            Assert.IsFalse(schedule.Add(0, Range + 1f, 0f, Range, Duration, Lag));
            Assert.AreEqual(0, schedule.Pending);
        }

        [Test]
        public void ARelicAlreadyWaiting_KeepsItsEarlierAnswer()
        {
            var schedule = new SonarSchedule(2);
            schedule.Add(0, 40f, 0f, Range, Duration, Lag);
            schedule.Add(0, 40f, 0.5f, Range, Duration, Lag);
            Assert.AreEqual(1, schedule.Pending, "a second ping does not double the answer");
            Assert.IsFalse(schedule.TryPop(0.5f + SonarSchedule.RingArrival(40f, Duration, Range) + Lag - 0.6f,
                out _, out _));
            Assert.IsTrue(schedule.TryPop(SonarSchedule.RingArrival(40f, Duration, Range) + Lag, out _, out _),
                "the first ping's (earlier) answer is kept");

            schedule.Add(1, 120f, 0f, Range, Duration, Lag);
            schedule.Add(1, 5f, 0.1f, Range, Duration, Lag);
            Assert.AreEqual(1, schedule.Pending);
            Assert.IsTrue(schedule.TryPop(0.1f + SonarSchedule.RingArrival(5f, Duration, Range) + Lag, out _,
                out float distance));
            Assert.AreEqual(5f, distance, "an earlier answer replaces a later one");
        }

        [Test]
        public void Cancel_DropsAPendingAnswer()
        {
            var schedule = new SonarSchedule(2);
            schedule.Add(0, 40f, 0f, Range, Duration, Lag);
            schedule.Add(1, 60f, 0f, Range, Duration, Lag);
            schedule.Cancel(0);
            Assert.IsTrue(schedule.TryPop(100f, out int relic, out _));
            Assert.AreEqual(1, relic);
        }
    }
}
