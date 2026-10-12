using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The tower coming online (feel-pass §8.2): a new stage waits for the beat's Follow (strength 0) so the cable race
    /// and the windows come after the flare; the top light flares on Impact and turns after; loads never wait or flare.
    /// </summary>
    public sealed class TowerBuildBeatTests
    {
        private const float Start = 10f;

        private HomeLightingSettings _settings;
        private TowerBuildBeat _beat;

        [SetUp]
        public void SetUp()
        {
            _settings = new HomeLightingSettings();
            _beat = new TowerBuildBeat(_settings);
        }

        [Test]
        public void NewStage_WaitsForTheBeatsFollow_ThenReleases()
        {
            _beat.Beat(FeelPhase.Anticipate, 0f, Start);
            Assert.IsFalse(_beat.Arrive(BasePowerStage.Home, false, Start + 0.1f), "held for the beat");
            Assert.IsFalse(_beat.TryRelease(Start + 2f, out _), "not before the Follow");
            _beat.Beat(FeelPhase.Impact, 1f, Start + 2.5f);
            Assert.IsFalse(_beat.TryRelease(Start + 2.6f, out _), "the flare comes first");
            _beat.Beat(FeelPhase.Follow, 0f, Start + 3f);
            Assert.IsTrue(_beat.TryRelease(Start + 3f, out BasePowerStage stage), "the cable race starts on Follow 0");
            Assert.AreEqual(BasePowerStage.Home, stage);
        }

        [Test]
        public void StageWithoutABeat_ReleasesAfterTheTimeout()
        {
            Assert.IsFalse(_beat.Arrive(BasePowerStage.Bay, false, Start));
            Assert.IsFalse(_beat.TryRelease(Start + _settings.BuildHoldTimeout * 0.5f, out _));
            Assert.IsTrue(_beat.TryRelease(Start + _settings.BuildHoldTimeout + 0.01f, out _), "never stuck dark");
        }

        [Test]
        public void Load_AppliesAtOnce_TurningWithoutAFlare()
        {
            Assert.IsTrue(_beat.Arrive(BasePowerStage.Home, true, Start), "a restored stage never waits");
            Assert.IsTrue(_beat.Turning, "a lit tower turns after a load");
            Assert.AreEqual(0f, _beat.Flare(Start), "and never flares");
        }

        [Test]
        public void Impact_FlaresAndSettles_ThenTheLightTurns()
        {
            Assert.IsFalse(_beat.Turning);
            Assert.AreEqual(1f, _beat.Sweep(Vector3.forward, Start), "steady before the tower is online");
            _beat.Beat(FeelPhase.Impact, 1f, Start);
            Assert.Greater(_beat.Flare(Start + _settings.BeaconFlareSeconds * 0.2f), 0.5f, "a bright flare");
            Assert.AreEqual(0f, _beat.Flare(Start + _settings.BeaconFlareSeconds), "settled");
            Assert.IsTrue(_beat.Turning);
            float facing = _beat.Sweep(Vector3.forward, 0f);
            float away = _beat.Sweep(Vector3.back, 0f);
            Assert.Greater(facing, away, "the turning beam sweeps past the viewer");
        }
    }
}
