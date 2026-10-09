using System;
using System.Collections.Generic;
using NUnit.Framework;
using MoonProject.Gameplay;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The panel's beat after a purchase (docs/features/M3-14): nothing is said while the station works, its own line
    /// comes only when its work shows on the purchase being waited for, glows its time, and a station that never shows
    /// its work is a fault; each station has a line of its own.
    /// </summary>
    public sealed class StationWorkTests
    {
        private const string Tower = "radio_tower";
        private const string Cradle = "rover.cargo_cradle";

        private TowerPanelSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new TowerPanelSettings();
        }

        [Test]
        public void TheLine_WaitsForTheStationsOwnMoment_OnThatPurchase()
        {
            var work = new StationWork(_settings);
            work.Begin(UpgradeStationKind.Workshop, Cradle);
            Assert.IsTrue(work.IsWorking);
            Assert.AreEqual(StationWork.Outcome.None, work.Step(_settings.CueWaitSeconds * 0.5f));

            Assert.IsFalse(work.Showed(UpgradeStationKind.Workshop, "rover.warm_headlamp"), "another piece");
            Assert.IsFalse(work.Showed(UpgradeStationKind.RadioTower, Cradle), "another station");
            Assert.IsTrue(work.IsWorking, "nothing said yet");

            Assert.IsTrue(work.Showed(UpgradeStationKind.Workshop, Cradle), "the bay set the piece on");
            Assert.IsTrue(work.IsShowingLine);
            Assert.IsFalse(work.Showed(UpgradeStationKind.Workshop, Cradle), "a line comes once");
            Assert.AreEqual(StationWork.Outcome.None, work.Step(_settings.CelebrateSeconds * 0.5f));
            Assert.AreEqual(StationWork.Outcome.Done, work.Step(_settings.CelebrateSeconds * 0.6f),
                "the line glows its time, then the panel moves on");
            Assert.IsFalse(work.IsBusy);
        }

        [Test]
        public void AStationThatNeverShowsItsWork_IsOverdue_AndAPauseDoesNotCount()
        {
            var work = new StationWork(_settings);
            work.Begin(UpgradeStationKind.RadioTower, Tower);
            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(StationWork.Outcome.None, work.Step(0f), "paused: the wait waits too");
            }

            Assert.AreEqual(StationWork.Outcome.None, work.Step(_settings.CueWaitSeconds * 0.9f));
            Assert.AreEqual(StationWork.Outcome.Overdue, work.Step(_settings.CueWaitSeconds * 0.2f));
            Assert.IsFalse(work.IsBusy);
            Assert.IsFalse(work.Showed(UpgradeStationKind.RadioTower, Tower), "a late cue changes nothing");
        }

        [Test]
        public void Clear_ForgetsTheWork()
        {
            var work = new StationWork(_settings);
            work.Begin(UpgradeStationKind.Workshop, Cradle);
            work.Clear();
            Assert.IsFalse(work.IsBusy);
            Assert.AreEqual(StationWork.Outcome.None, work.Step(_settings.CueWaitSeconds * 2f));
            Assert.Throws<ArgumentException>(() => work.Begin(UpgradeStationKind.Workshop, string.Empty));
        }

        [Test]
        public void EveryStation_HasALineOfItsOwn()
        {
            var lines = new HashSet<string>();
            foreach (UpgradeStationKind station in Enum.GetValues(typeof(UpgradeStationKind)))
            {
                Assert.IsTrue(lines.Add(UiKeys.StationLine(station)), $"{station} shares its line");
            }

            Assert.AreEqual(UiKeys.TowerPurchased, UiKeys.StationLine(UpgradeStationKind.RadioTower));
            Assert.AreEqual(UiKeys.BayFitted, UiKeys.StationLine(UpgradeStationKind.Workshop));
        }
    }
}
