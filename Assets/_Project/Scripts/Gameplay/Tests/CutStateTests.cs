using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The cut's state and beats (docs/design/feel-pass.md §3.1, §6): charge-up, ignition, steps, pause and resume,
    /// the battery from ignition only, and a new hold allowed the moment a cut ends.
    /// </summary>
    public sealed class CutStateTests
    {
        private const float Charge = 0.65f;
        private const float Resume = 0.3f;
        private const int Steps = 5;
        private const float CutSeconds = 2f;
        private const float Frame = 1f / 60f;

        private EventBus _events;
        private List<FeelBeat> _beats;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _beats = new List<FeelBeat>();
            _events.Subscribe<FeelBeat>(_beats.Add);
        }

        [Test]
        public void ChargeUp_ThenIgnition_ThenStepsAndTheBreak_PublishTheBeatsInOrder()
        {
            var cut = new CutState(_events, FeelKind.Cut, Charge, Resume, Steps);
            float progress = 0f;
            cut.Engage(progress, SalvageMaterial.Wiring, Vector3.one, Vector3.up);
            Assert.AreEqual(CutPhase.Charging, cut.Phase);
            Assert.IsFalse(cut.Draws, "a charge-up draws nothing");
            Assert.AreEqual(FeelPhase.Anticipate, _beats[0].Phase);
            Assert.AreEqual(FeelKind.Cut, _beats[0].Kind);
            Assert.AreEqual(Charge, _beats[0].Seconds, 1e-5f, "Anticipate carries the charge's length");
            Assert.AreEqual(SalvageMaterial.Wiring, _beats[0].Material);

            int frames = Run(cut, ref progress, Charge * 0.5f, out bool ignited);
            Assert.IsFalse(ignited);
            Assert.AreEqual(0f, progress, "nothing is cut while charging");
            Assert.That(cut.ChargeProgress, Is.InRange(0.4f, 0.6f));
            Run(cut, ref progress, Charge, out ignited);
            Assert.IsTrue(ignited);
            Assert.IsTrue(cut.Draws, "the battery draws from ignition on");
            Assert.AreEqual(FeelPhase.Act, _beats[1].Phase);
            Assert.AreEqual(0f, _beats[1].Strength, "ignition is an Act of strength 0");

            bool broke = false;
            for (int i = 0; i < 1000 && !broke; i++)
            {
                broke = cut.Step(Frame, CutSeconds, ref progress, Vector3.one, Vector3.up, out _);
            }

            Assert.IsTrue(broke);
            Assert.AreEqual(1f, progress);
            Assert.AreEqual(CutPhase.Idle, cut.Phase);
            Assert.AreEqual(FeelPhase.Impact, _beats[_beats.Count - 1].Phase, "the break is the Impact");
            int steps = 0;
            float last = 0f;
            for (int i = 2; i < _beats.Count - 1; i++)
            {
                Assert.AreEqual(FeelPhase.Act, _beats[i].Phase);
                Assert.Greater(_beats[i].Strength, last, "each step is stronger, its strength the progress");
                last = _beats[i].Strength;
                steps++;
            }

            Assert.AreEqual(Steps - 1, steps, "an Act per step crossed before the break (the last step is the Impact)");
            Assert.Greater(frames, 0);
        }

        [Test]
        public void LettingGoWhileCharging_Cancels_WithNothingLostOrDrawn()
        {
            var cut = new CutState(_events, FeelKind.Cut, Charge, Resume, Steps);
            float progress = 0f;
            cut.Engage(progress, SalvageMaterial.Metal, Vector3.zero, Vector3.up);
            Run(cut, ref progress, Charge * 0.5f, out _);
            cut.Release(progress, Vector3.zero, Vector3.up);
            Assert.AreEqual(CutPhase.Idle, cut.Phase);
            Assert.IsFalse(cut.Draws);
            Assert.AreEqual(0f, progress);
            Assert.AreEqual(FeelPhase.Cancel, _beats[_beats.Count - 1].Phase);
            Assert.AreEqual(2, _beats.Count, "Anticipate, then Cancel: no ignition");
            cut.Release(progress, Vector3.zero, Vector3.up);
            Assert.AreEqual(2, _beats.Count, "letting go twice cancels once");
        }

        [Test]
        public void LettingGoMidCut_Pauses_AndTheNextHoldResumesWithTheShortCharge()
        {
            var cut = new CutState(_events, FeelKind.Cut, Charge, Resume, Steps);
            float progress = 0f;
            cut.Engage(progress, SalvageMaterial.Optics, Vector3.zero, Vector3.up);
            Run(cut, ref progress, Charge + CutSeconds * 0.4f, out _);
            float kept = progress;
            Assert.That(kept, Is.InRange(0.3f, 0.5f));
            cut.Release(progress, Vector3.zero, Vector3.up);
            Assert.AreEqual(FeelPhase.Cancel, _beats[_beats.Count - 1].Phase);
            Assert.AreEqual(kept, _beats[_beats.Count - 1].Strength, 1e-5f, "Cancel mid-cut keeps the progress");

            cut.Engage(progress, SalvageMaterial.Optics, Vector3.zero, Vector3.up);
            Assert.AreEqual(Resume, cut.ChargeSeconds, 1e-5f, "the seam is still warm: the short charge");
            Assert.AreEqual(Resume, _beats[_beats.Count - 1].Seconds, 1e-5f);
            Assert.AreEqual(1f - Resume / Charge, _beats[_beats.Count - 1].Strength, 1e-5f,
                "M3-26: the run starts part-way, as far as the shorter charge saves");
            Run(cut, ref progress, Resume + Frame * 2f, out bool ignited);
            Assert.IsTrue(cut.Draws, "resumed after the short charge");
            Assert.GreaterOrEqual(progress, kept, "from where it was");
        }

        [Test]
        public void ABreak_LeavesTheStateFree_ForANewChargeAtOnce()
        {
            var cut = new CutState(_events, FeelKind.Cut, Charge, Resume, Steps);
            float progress = 0.99f;
            cut.Engage(progress, SalvageMaterial.Metal, Vector3.zero, Vector3.up);
            bool broke = false;
            for (int i = 0; i < 200 && !broke; i++)
            {
                broke = cut.Step(Frame, CutSeconds, ref progress, Vector3.zero, Vector3.up, out _);
            }

            Assert.IsTrue(broke);
            float next = 0f;
            cut.Engage(next, SalvageMaterial.Wiring, Vector3.zero, Vector3.up);
            Assert.AreEqual(CutPhase.Charging, cut.Phase, "never locked: the next piece charges while this one flies");
            Assert.AreEqual(Charge, cut.ChargeSeconds, 1e-5f, "a fresh piece takes the full charge");
            Assert.AreEqual(0f, _beats[_beats.Count - 1].Strength, "a fresh run starts at the battery");

            cut.Release(next, Vector3.zero, Vector3.up);
            cut.Tune(0.45f, 0.2f);
            cut.Engage(next, SalvageMaterial.Wiring, Vector3.zero, Vector3.up);
            Assert.AreEqual(0.45f, cut.ChargeSeconds, 1e-5f, "a better cutter charges sooner");
        }

        [Test]
        public void Dig_ChargesIgnitesStepsAndReleases_AsDigBeatsWithNoMaterial()
        {
            var dig = new CutState(_events, FeelKind.Dig, Charge, Resume, Steps);
            dig.Engage(0f, Vector3.zero, Vector3.up);
            Assert.AreEqual(FeelKind.Dig, _beats[0].Kind);
            Assert.AreEqual(FeelPhase.Anticipate, _beats[0].Phase);
            Assert.IsFalse(_beats[0].HasMaterial, "a dig works no material");
            bool ignited = false;
            for (float t = 0f; t < Charge + Frame && !ignited; t += Frame)
            {
                ignited = dig.Charge(Frame, Vector3.zero, Vector3.up);
            }

            Assert.IsTrue(ignited);
            Assert.IsTrue(dig.Draws, "the dig draws from ignition on");
            Assert.IsFalse(dig.Advance(0f, 0.3f, Vector3.zero, Vector3.up));
            Assert.AreEqual(FeelPhase.Act, _beats[_beats.Count - 1].Phase, "a step crossed (eased progress)");
            Assert.AreEqual(0.3f, _beats[_beats.Count - 1].Strength, 1e-5f);
            Assert.IsTrue(dig.Advance(0.9f, 1f, Vector3.zero, Vector3.up), "the relic's release");
            Assert.AreEqual(FeelPhase.Impact, _beats[_beats.Count - 1].Phase);
            Assert.AreEqual(CutPhase.Idle, dig.Phase);
            Assert.IsFalse(dig.Advance(0f, 0.5f, Vector3.zero, Vector3.up), "nothing advances while idle");
        }

        [Test]
        public void CutLine_HotPointCrawlsAlongItByLength()
        {
            var line = new[] { new Vector3(-1f, 0f, 0f), Vector3.zero, new Vector3(0f, 3f, 0f) };
            Assert.AreEqual(line[0], SalvageCutLines.PointAt(line, 0f));
            Assert.AreEqual(line[2], SalvageCutLines.PointAt(line, 1f));
            Assert.Less(Vector3.Distance(Vector3.zero, SalvageCutLines.PointAt(line, 0.25f)), 1e-5f,
                "a quarter of the 4 m line is the 1 m first segment");
            Assert.Less(Vector3.Distance(new Vector3(0f, 1f, 0f), SalvageCutLines.PointAt(line, 0.5f)), 1e-5f);
        }

        [Test]
        public void SiteEntry_FindsAValidCutLine_ByPieceNumber()
        {
            var entry = new SalvageSiteEntry("site.test", null, AbilityGate.Open, false, 1, new[]
            {
                new PieceCutLine(1, new[] { Vector3.zero, Vector3.right }),
                new PieceCutLine(2, new[] { Vector3.zero }),
            }, new[] { new PieceSizeEntry(1, PieceSize.Large) });
            Assert.IsTrue(entry.TryGetSize(1, out PieceSize size));
            Assert.AreEqual(PieceSize.Large, size);
            Assert.IsFalse(entry.TryGetSize(2, out _));
            Assert.IsTrue(entry.TryGetCutLine(1, out Vector3[] points));
            Assert.AreEqual(2, points.Length);
            Assert.IsFalse(entry.TryGetCutLine(2, out _), "a single point is no line");
            Assert.IsFalse(entry.TryGetCutLine(3, out _));
        }

        /// <summary>Steps the cut until <paramref name="seconds"/> have passed; true in ignited if it ignited.</summary>
        private static int Run(CutState cut, ref float progress, float seconds, out bool ignited)
        {
            ignited = false;
            int frames = 0;
            for (float t = 0f; t < seconds; t += Frame)
            {
                cut.Step(Frame, CutSeconds, ref progress, Vector3.zero, Vector3.up, out bool now);
                ignited |= now;
                frames++;
            }

            return frames;
        }
    }
}
