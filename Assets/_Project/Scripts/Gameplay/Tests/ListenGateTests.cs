using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// When 07 listens (first-hour v2 §3): from tower level 1 on, after 2 s at rest (speed at most 0.3 m/s), and only
    /// after 30 m of driving since the last listen; the first listen of a session needs no distance.
    /// </summary>
    public sealed class ListenGateTests
    {
        private const float Step = 0.125f;

        private SonarTuning _tuning;
        private ListenGate _gate;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<SonarTuning>();
            _gate = new ListenGate();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void Defaults_MatchTheDesign()
        {
            Assert.AreEqual(2f, _tuning.RestSeconds);
            Assert.AreEqual(0.3f, _tuning.RestSpeed);
            Assert.AreEqual(30f, _tuning.ListenSpacing);
        }

        [Test]
        public void BeforeTheTowerWakes_07NeverListens()
        {
            Assert.AreEqual(0, Rest(false, Vector3.zero, 10f));
        }

        [Test]
        public void FirstListen_ComesAfterTheRest_WithNoDistance_AndSittingStillNeverRepeatsIt()
        {
            Assert.AreEqual(0, Rest(true, Vector3.zero, _tuning.RestSeconds - 2f * Step), "not before the rest");
            Assert.AreEqual(1, Rest(true, Vector3.zero, 3f * Step), "the first listen needs no driving");
            Assert.AreEqual(0, Rest(true, Vector3.zero, 30f), "sitting still never listens again");
        }

        [Test]
        public void Slow_CountsAsRest_ButDriving_ResetsIt()
        {
            Assert.IsFalse(_gate.Tick(true, Vector3.zero, _tuning.RestSpeed, _tuning.RestSeconds - Step, _tuning));
            Assert.IsFalse(_gate.Tick(true, Vector3.zero, _tuning.RestSpeed + 0.1f, Step, _tuning), "moving on");
            Assert.AreEqual(0f, _gate.Rested);
            Assert.IsFalse(_gate.Tick(true, Vector3.zero, 0f, _tuning.RestSeconds - Step, _tuning));
            Assert.IsTrue(_gate.Tick(true, Vector3.zero, _tuning.RestSpeed, Step, _tuning));
        }

        [Test]
        public void NextListen_NeedsTheSpacing_MeasuredOnTheGround()
        {
            Assert.AreEqual(1, Rest(true, Vector3.zero, _tuning.RestSeconds + Step));
            var near = new Vector3(_tuning.ListenSpacing - 1f, 50f, 0f);
            Drive(Vector3.zero, near);
            Assert.AreEqual(0, Rest(true, near, 10f), "not far enough: height does not count");

            var far = new Vector3(_tuning.ListenSpacing - 1f, 0f, 2f);
            Drive(near, far);
            Assert.GreaterOrEqual(_gate.Driven, _tuning.ListenSpacing);
            Assert.AreEqual(1, Rest(true, far, _tuning.RestSeconds + Step), "a new place listens once");
            Assert.AreEqual(0f, _gate.Driven, "and the count starts again");
        }

        [Test]
        public void DrivingBeforeTheTowerWakes_StillLetsTheFirstListenCome()
        {
            Drive(Vector3.zero, new Vector3(5f, 0f, 0f));
            Assert.AreEqual(0, Rest(false, new Vector3(5f, 0f, 0f), 5f));
            Assert.AreEqual(1, Rest(true, new Vector3(5f, 0f, 0f), _tuning.RestSeconds + Step));
        }

        private void Drive(Vector3 from, Vector3 to)
        {
            const int frames = 20;
            for (int i = 1; i <= frames; i++)
            {
                _gate.Tick(true, Vector3.Lerp(from, to, i / (float)frames), 5f, Step, _tuning);
            }
        }

        private int Rest(bool awake, Vector3 position, float seconds)
        {
            int listens = 0;
            for (float t = 0f; t < seconds - 1e-4f; t += Step)
            {
                listens += _gate.Tick(awake, position, 0f, Step, _tuning) ? 1 : 0;
            }

            return listens;
        }
    }
}
