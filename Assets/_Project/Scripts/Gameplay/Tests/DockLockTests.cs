using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The dock lock's sequence (docs/design/feel-pass.md §7.1): the walls rise and clamp when 07 docks to charge,
    /// each cell lights its share of the strips, the full sweep lowers them, an early drive-off drops them, and the
    /// rest 07 wakes into shows the state silently.
    /// </summary>
    public sealed class DockLockTests
    {
        private const float Frame = 1f / 60f;

        private EventBus _events;
        private List<FeelBeat> _beats;
        private BaseTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _beats = new List<FeelBeat>();
            _events.Subscribe<FeelBeat>(_beats.Add);
            _tuning = ScriptableObject.CreateInstance<BaseTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void RollOn_ThenDocking_RaisesTheWallsStaggered_AndClampsWithAnImpact()
        {
            DockLock dock = Rolled();
            Assert.IsEmpty(_beats, "rolling on chases the rim lamps without a beat");
            Assert.Greater(dock.RimLevel(0) + dock.RimLevel(1) + dock.RimLevel(2), 0f, "the rim lamps chase");

            dock.Step(true, true, 1f, 4, Vector3.zero, Frame);
            Assert.IsTrue(dock.Raised);
            Assert.AreEqual(FeelPhase.Anticipate, _beats[0].Phase, "the walls start rising");
            Assert.AreEqual(FeelKind.Dock, _beats[0].Kind);
            Assert.AreEqual(3 * _tuning.WallStagger + _tuning.WallRiseSeconds, _beats[0].Seconds, 1e-4f,
                "Seconds until the clamp");
            Run(dock, true, 1f, 4, _tuning.WallRiseSeconds * 0.5f);
            Assert.Greater(dock.WallHeight(0), dock.WallHeight(3), "the walls rise one after another");
            Run(dock, true, 1f, 4, _tuning.WallRiseSeconds + _tuning.WallStagger * 4f);
            Assert.IsTrue(dock.Locked);
            Assert.AreEqual(1f, dock.WallHeight(3), 1e-4f);
            FeelBeat impact = _beats.Find(beat => beat.Phase == FeelPhase.Impact);
            Assert.AreEqual(0.75f, impact.Strength, 1e-4f, "the clamp: 07 arrived a quarter full");
        }

        [Test]
        public void ARestWokenInto_StandsLockedAtOnce_Silently()
        {
            var dock = new DockLock(_events, _tuning);
            dock.Step(true, true, 2f, 4, Vector3.zero, Frame);
            Assert.IsTrue(dock.Locked, "07 woke on its charging pad: the lock stands");
            Assert.IsTrue(dock.Silent, "shown with no motion and no beat");
            Assert.AreEqual(1f, dock.WallHeight(0));
            Assert.IsEmpty(_beats);
        }

        [Test]
        public void AFirstRollOn_AfterWakingOffThePad_LocksWithItsBeats()
        {
            var dock = new DockLock(_events, _tuning);
            dock.Wake(false);
            dock.Step(true, false, 1f, 4, Vector3.zero, Frame);
            dock.Step(true, true, 1f, 4, Vector3.zero, Frame);
            Assert.IsTrue(dock.Raised);
            Assert.IsFalse(dock.Locked, "the walls rise, they do not stand at once");
            Assert.AreEqual(FeelPhase.Anticipate, _beats[0].Phase);
        }

        [Test]
        public void ALockClosedByDocking_IsNotSilent()
        {
            DockLock dock = Rolled();
            dock.Step(true, true, 1f, 4, Vector3.zero, Frame);
            Run(dock, true, 1f, 4, _tuning.WallRiseSeconds + _tuning.WallStagger * 4f);
            Assert.IsTrue(dock.Locked);
            Assert.IsFalse(dock.Silent, "the lock closed with its beats");
        }

        [Test]
        public void EachCell_LightsOneWall_WithFourCells()
        {
            DockLock dock = Locked(1f, 4);
            Assert.AreEqual(1f, dock.StripFill(0));
            Assert.AreEqual(0f, dock.StripFill(1));
            dock.Step(true, true, 2.5f, 4, Vector3.zero, Frame);
            Assert.AreEqual(1f, dock.StripFill(1), "the second cell lights the second wall");
            Assert.AreEqual(0f, dock.StripFill(2), "half a cell lights nothing yet");
            FeelBeat act = _beats.FindLast(beat => beat.Phase == FeelPhase.Act);
            Assert.AreEqual(0.5f, act.Strength, 1e-4f, "Act per cell, its strength the charge");
        }

        [Test]
        public void SixCells_SubdivideTheStrips()
        {
            DockLock dock = Locked(1f, 6);
            Assert.AreEqual(1f / 1.5f, dock.StripFill(0), 1e-4f, "a wall holds a cell and a half");
            dock.Step(true, true, 3f, 6, Vector3.zero, Frame);
            Assert.AreEqual(1f, dock.StripFill(1));
            Assert.AreEqual(0f, dock.StripFill(2));
            Assert.AreEqual(2, _beats.FindAll(beat => beat.Phase == FeelPhase.Act).Count,
                "an Act for each of the two cells gained, none for the one it arrived with");
        }

        [Test]
        public void Full_SweepsToGold_ThenLowersTheWalls_WithAReward()
        {
            DockLock dock = Locked(3f, 4);
            dock.Step(true, true, 4f, 4, Vector3.zero, Frame);
            Assert.AreEqual(FeelPhase.Reward, _beats[_beats.Count - 1].Phase);
            Run(dock, true, 4f, 4, _tuning.FullSweepSeconds);
            Assert.AreEqual(1f, dock.Sweep, 1e-4f, "cyan to warm gold");
            Assert.Greater(dock.WallHeight(3), 0.5f, "then the walls lower");
            Run(dock, true, 4f, 4, _tuning.WallLowerSeconds + _tuning.WallStagger * 4f);
            Assert.IsFalse(dock.Raised);
            Assert.AreEqual(0f, dock.WallHeight(0));
        }

        [Test]
        public void DrivingOffEarly_DropsTheWallsAtOnce_WithACancel()
        {
            DockLock dock = Locked(2f, 4);
            dock.Step(true, false, 2f, 4, Vector3.zero, Frame);
            Assert.AreEqual(FeelPhase.Cancel, _beats[_beats.Count - 1].Phase);
            Run(dock, false, 2f, 4, _tuning.WallDropSeconds + Frame);
            Assert.IsFalse(dock.Raised);
            Assert.AreEqual(0f, dock.WallHeight(2), "down in a quarter second, never blocking 07");
        }

        [Test]
        public void TheRest07WakesInto_ShowsTheStateSilently()
        {
            var dock = new DockLock(_events, _tuning);
            dock.Step(true, true, 2f, 4, Vector3.zero, Frame);
            Assert.IsTrue(dock.Locked, "already locked round 07");
            Assert.AreEqual(1f, dock.WallHeight(0));
            Assert.AreEqual(1f, dock.StripFill(1));
            Assert.IsEmpty(_beats, "without a beat");

            dock.Step(false, false, 2f, 4, Vector3.zero, Frame);
            Run(dock, false, 2f, 4, _tuning.WallDropSeconds + Frame);
            dock.Step(true, true, 2f, 4, Vector3.zero, Frame);
            Run(dock, true, 2f, 4, _tuning.WallRiseSeconds + _tuning.WallStagger * 4f);
            Assert.IsTrue(_beats.Exists(beat => beat.Phase == FeelPhase.Impact),
                "a later homecoming plays its beats, the clamp too");
        }

        private DockLock Rolled()
        {
            var dock = new DockLock(_events, _tuning);
            dock.Step(false, false, 1f, 4, Vector3.zero, Frame);
            dock.Step(true, false, 1f, 4, Vector3.zero, Frame);
            return dock;
        }

        private DockLock Locked(float charge, int cells)
        {
            var dock = new DockLock(_events, _tuning);
            dock.Step(false, false, charge, cells, Vector3.zero, Frame);
            dock.Step(true, false, charge, cells, Vector3.zero, Frame);
            dock.Step(true, true, charge, cells, Vector3.zero, Frame);
            Run(dock, true, charge, cells, _tuning.WallRiseSeconds + _tuning.WallStagger * 4f);
            Assert.IsTrue(dock.Locked);
            return dock;
        }

        private static void Run(DockLock dock, bool docked, float charge, int cells, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                dock.Step(true, docked, charge, cells, Vector3.zero, Frame);
            }
        }
    }
}
