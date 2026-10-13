using NUnit.Framework;
using UnityEngine;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>The Cells charge-up (0.5.5 §5.4): cells fill per stage after the brace, the full lock goes toward
    /// white, a release drains back last stage first, and the conduits keep a warm trace after firing.</summary>
    public sealed class ChargeCellsTests
    {
        private const float Frame = 1f / 120f;
        private const float Brace = 0.5f;
        private const float StageTime = 0.45f;
        private const float Warm = 6f;

        private readonly ChargeCellsSettings _settings = new ChargeCellsSettings();

        private static void Run(ChargeCells cells, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                cells.Step(Frame);
            }
        }

        [Test]
        public void NothingFills_DuringTheBrace_ThenStageOneEasesOut()
        {
            var cells = new ChargeCells(_settings);
            cells.Begin(false, 3, StageTime, Brace);
            Run(cells, Brace - 0.02f);
            Assert.AreEqual(0f, cells.Fill(1), "07 braces first.");
            Assert.AreEqual(0f, cells.ConduitGlow);
            Run(cells, 0.02f + StageTime * 0.5f);
            Assert.Greater(cells.Fill(1), 0.6f, "Fast at first...");
            Assert.Less(cells.Fill(1), 1f, "...slowing into the lock.");
            Assert.AreEqual(0f, cells.Fill(2), "One stage at a time.");
            Assert.Greater(cells.ConduitGlow, 0f, "The conduits carry it from stage 1.");
        }

        [Test]
        public void AWarmCharge_SkipsTheBrace()
        {
            var cells = new ChargeCells(_settings);
            cells.Begin(true, 1, StageTime, Brace);
            Run(cells, 0.1f);
            Assert.Greater(cells.Fill(1), 0f);
        }

        [Test]
        public void EachLock_FillsItsCell_AndTheFullLockGoesTowardWhite()
        {
            var cells = new ChargeCells(_settings);
            cells.Begin(true, 2, StageTime, Brace);
            Run(cells, 0.1f);
            cells.Stage(1);
            Assert.AreEqual(1f, cells.Fill(1), "The lock is authoritative.");
            Assert.AreEqual(0f, cells.LockGlow);
            Run(cells, StageTime);
            cells.Stage(2);
            Run(cells, 0.2f);
            Assert.AreEqual(1f, cells.Fill(2));
            Assert.AreEqual(0f, cells.Fill(3), "A medium piece needs two cells.");
            Assert.AreEqual(1f, cells.Heat, 1e-4f);
            Assert.AreEqual(_settings.LockGlow, cells.LockGlow, 1e-4f, "The ring lights at the full lock.");
            Assert.AreEqual(1.5f * _settings.LockGlow, cells.RingGlow(1.5f), 1e-4f, "A better tier locks brighter.");
            Assert.AreEqual(_settings.LockGlow, cells.StageGlow(1), 1e-4f, "Every lit part runs hot.");
            Assert.Greater(cells.Colour.r, _settings.Colour.r, "Toward white.");
        }

        [Test]
        public void AReleaseBeforeFiring_DrainsTheLastStageFirst_WithinItsTime()
        {
            var cells = new ChargeCells(_settings);
            cells.Begin(true, 3, StageTime, Brace);
            cells.Stage(1);
            cells.Stage(2);
            Run(cells, StageTime * 0.5f);
            cells.Release(Warm);
            Run(cells, _settings.DrainSeconds * 0.4f);
            Assert.AreEqual(0f, cells.Fill(3), 1e-4f);
            Assert.Less(cells.Fill(2), 1f, "Stage 2 drains before stage 1.");
            Assert.AreEqual(1f, cells.Fill(1), 1e-4f);
            Run(cells, _settings.DrainSeconds * 0.7f);
            Assert.IsFalse(cells.IsShowing, "Back at the battery in 200 ms; no firing, so no warm trace.");
        }

        [Test]
        public void AfterFiring_TheCellsFade_AndTheConduitsHoldTheWarmTrace()
        {
            var cells = new ChargeCells(_settings);
            cells.Begin(true, 1, StageTime, Brace);
            cells.Stage(1);
            cells.Fire();
            Run(cells, 1f);
            Assert.AreEqual(1f, cells.Fill(1), "Held through the beam.");
            cells.End(Warm);
            Run(cells, _settings.FadeSeconds + 0.1f);
            Assert.AreEqual(0f, cells.Fill(1));
            Assert.AreEqual(0f, cells.LockGlow);
            Assert.AreEqual(_settings.WarmGlow, cells.ConduitGlow, 1e-4f, "The warm emitter shows why the next is quick.");
            Assert.AreEqual(_settings.WarmGlow, cells.RingGlow(1.5f), 1e-4f, "The ring keeps the trace, unboosted.");
            Run(cells, Warm);
            Assert.AreEqual(0f, cells.ConduitGlow);
            Assert.AreEqual(0f, cells.RingGlow(1.5f));
            Assert.IsFalse(cells.IsShowing);
        }

        [Test]
        public void AReleaseAfterFiring_EndsTheCut_NotADrain()
        {
            var cells = new ChargeCells(_settings);
            cells.Begin(true, 1, StageTime, Brace);
            cells.Stage(1);
            cells.Fire();
            cells.Release(Warm);
            Run(cells, _settings.FadeSeconds + 0.1f);
            Assert.AreEqual(_settings.WarmGlow, cells.ConduitGlow, 1e-4f);
        }
    }
}
