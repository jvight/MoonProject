using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>Which beam framing runs: the cut's push-in only on a fresh big charge, the dig's on its own
    /// Anticipate, each ended only by its own kind's beats.</summary>
    public sealed class BeamFramingTests
    {
        private static readonly Vector3 Ahead = new Vector3(1f, 0f, 3f);

        private RoverCameraTuning _tuning;
        private BeamFraming _beam;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            _beam = new BeamFraming(_tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private void Beat(FeelKind kind, FeelPhase phase, float strength = 0f)
        {
            _beam.OnBeat(new FeelBeat(kind, phase, Ahead, Vector3.up, strength, 0f), Vector3.zero, Vector3.forward,
                false);
        }

        private void Staged(int needed, bool warm, bool gentle = false)
        {
            _beam.OnStaged(new CutChargeStaged(0, needed, warm, 0.45f), Vector3.zero, Vector3.forward, gentle);
        }

        [Test]
        public void ACutsAnticipate_AloneNeverMovesTheCamera()
        {
            Beat(FeelKind.Cut, FeelPhase.Anticipate);
            Assert.IsFalse(_beam.Framing.IsActive, "The push-in waits for the charge's stages.");
        }

        [Test]
        public void AFreshHullCharge_PushesIn_TowardTheBeamsSide()
        {
            Beat(FeelKind.Cut, FeelPhase.Anticipate);
            Staged(3, false);
            Assert.IsTrue(_beam.Framing.IsActive);
            Assert.IsFalse(_beam.IsDig);
            Assert.AreEqual(Ahead, _beam.Framing.CutPoint);
            Assert.AreEqual(1f, _beam.Framing.Side, "The beam runs to 07's right.");
        }

        [Test]
        public void WarmSmallOrGentleCharges_StayStill_AndOnlyStageZeroBegins()
        {
            Beat(FeelKind.Cut, FeelPhase.Anticipate);
            Staged(3, true);
            Staged(2, false);
            Staged(3, false, true);
            _beam.OnStaged(new CutChargeStaged(1, 3, false, 0.45f), Vector3.zero, Vector3.forward, false);
            Assert.IsFalse(_beam.Framing.IsActive);
        }

        [Test]
        public void TheDig_FramesOnItsOwnAnticipate_AndOnlyItsOwnBeatsEndIt()
        {
            Beat(FeelKind.Dig, FeelPhase.Anticipate);
            Assert.IsTrue(_beam.Framing.IsActive);
            Assert.IsTrue(_beam.IsDig);
            Beat(FeelKind.Cut, FeelPhase.Cancel);
            Assert.IsTrue(_beam.Framing.IsActive, "A cut's release is not the dig's.");
            Beat(FeelKind.Dig, FeelPhase.Follow, 1f);
            Assert.IsFalse(_beam.Framing.IsActive, "The relic is up: it eases back.");
        }

        [Test]
        public void TheCutsPushIn_EndsOnItsFold_OrARelease()
        {
            Beat(FeelKind.Cut, FeelPhase.Anticipate);
            Staged(3, false);
            Beat(FeelKind.Dig, FeelPhase.Cancel);
            Assert.IsTrue(_beam.Framing.IsActive, "A dig's beat is not the cut's.");
            Beat(FeelKind.Cut, FeelPhase.Follow, 0.5f);
            Assert.IsTrue(_beam.Framing.IsActive, "A segment change keeps it.");
            Beat(FeelKind.Cut, FeelPhase.Follow, 1f);
            Assert.IsFalse(_beam.Framing.IsActive);

            Staged(3, false);
            Beat(FeelKind.Cut, FeelPhase.Cancel);
            Assert.IsFalse(_beam.Framing.IsActive, "A release ends it too.");
        }
    }
}
