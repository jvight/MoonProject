using NUnit.Framework;
using UnityEditor;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The owner found the authored camera moves far too fast (2026-10-19): each must stay slow and gradual. The tuned
    /// asset is held to calm bounds so a later tweak cannot quietly bring the rush back.
    /// </summary>
    public sealed class CalmCameraTuningTests
    {
        private const float CalmestFaceMoveSeconds = 1.4f;
        private const float FastestFaceMoveSeconds = 1.0f;
        private const float FastestRecenterDegreesPerSecond = 20f;
        private const float SlowestRecenterButtonSeconds = 1.2f;
        private const float ShortestOpeningMoveSeconds = 2f;
        private const float ShortestPageEaseSeconds = 0.8f;

        private RoverCameraTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = AssetDatabase.LoadAssetAtPath<RoverCameraTuning>(RoverAssetPaths.CameraTuning);
            Assert.IsNotNull(_tuning, "the camera tuning asset exists");
        }

        [Test]
        public void TheFaceShot_OpensAndCloses_InAboutOneToOneAndAHalfSeconds()
        {
            Assert.That(_tuning.FaceInspectOpenSeconds, Is.InRange(FastestFaceMoveSeconds, CalmestFaceMoveSeconds));
            Assert.That(_tuning.FaceInspectCloseSeconds, Is.InRange(FastestFaceMoveSeconds, CalmestFaceMoveSeconds));
        }

        [Test]
        public void TheRecenter_TurnsNoFasterThanTwentyDegreesASecond_AndTheButtonTakesItsTime()
        {
            Assert.LessOrEqual(_tuning.RecenterMaxRate, FastestRecenterDegreesPerSecond);
            Assert.GreaterOrEqual(_tuning.RecenterButtonSeconds, SlowestRecenterButtonSeconds);
        }

        [Test]
        public void TheOpeningsPullBackAndMoveBehind_AreSlowEasedMoves()
        {
            Assert.GreaterOrEqual(_tuning.Opening.Post.PullBackSeconds, ShortestOpeningMoveSeconds);
            Assert.GreaterOrEqual(_tuning.Opening.Post.MoveBehindSeconds, ShortestOpeningMoveSeconds);
        }

        [Test]
        public void ThePageChange_EasesTheShotSlowerThanThePageFlips()
        {
            Assert.GreaterOrEqual(_tuning.FacePageEaseSeconds, ShortestPageEaseSeconds);
        }
    }
}
