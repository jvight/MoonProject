using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The Display page (docs: settings Display): the monitor's sizes listed once and largest first, steps that wrap,
    /// every change applied at once, older settings files reading borderless at the desktop's resolution with V-Sync
    /// on, and the choices surviving a round trip.
    /// </summary>
    public sealed class DisplaySettingsTests
    {
        private FakeSettingsServices _services;
        private FakeDisplayControl _display;
        private PlayerSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _services = new FakeSettingsServices();
            _display = new FakeDisplayControl();
            _settings = Make(_services, _display);
        }

        [Test]
        public void TheMonitorsSizes_AreListedOnce_LargestFirst_WithoutTheTooSmall()
        {
            var offered = new[]
            {
                new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(1920, 1080),
                new Vector2Int(800, 600), new Vector2Int(2560, 1440), new Vector2Int(1920, 1080),
            };
            List<Vector2Int> sizes = DisplayChoices.Distinct(offered, 1024);
            CollectionAssert.AreEqual(
                new[] { new Vector2Int(2560, 1440), new Vector2Int(1920, 1080), new Vector2Int(1280, 720) }, sizes);
        }

        [Test]
        public void AnOldSettingsFile_ReadsBorderlessAtTheDesktopResolution_VSyncOn()
        {
            _settings.Restore(new SettingsSaveData());
            Assert.AreEqual(new Vector2Int(1920, 1080), _display.Resolutions[_settings.ResolutionIndex]);
            Assert.AreEqual(DisplayMode.Borderless, _settings.WindowMode);
            Assert.IsTrue(_settings.VSync);
            Assert.AreEqual(60, _settings.FpsCap);
            Assert.AreEqual(1920, _display.Applied.Width);
            Assert.AreEqual(DisplayMode.Borderless, _display.Applied.Mode);
            Assert.IsTrue(_display.Applied.VSync);
            Assert.IsFalse(_settings.IsDirty, "reading a file is not a change");
        }

        [Test]
        public void EveryChange_IsAppliedAtOnce_AndMarksTheSettingsChanged()
        {
            _settings.Restore(new SettingsSaveData());
            int before = _display.Applications;
            _settings.StepResolution(1);
            Assert.AreEqual(before + 1, _display.Applications);
            Assert.AreEqual(new Vector2Int(1600, 900), new Vector2Int(_display.Applied.Width, _display.Applied.Height));
            Assert.IsTrue(_settings.IsDirty);

            _settings.StepWindowMode(1);
            Assert.AreEqual(DisplayMode.Exclusive, _display.Applied.Mode);
            _settings.StepWindowMode(1);
            Assert.AreEqual(DisplayMode.Windowed, _display.Applied.Mode);
            _settings.StepWindowMode(1);
            Assert.AreEqual(DisplayMode.Borderless, _display.Applied.Mode, "the modes wrap round");

            _settings.VSync = false;
            Assert.IsFalse(_display.Applied.VSync);
            _settings.StepFpsCap(1);
            Assert.AreEqual(120, _display.Applied.FpsCap);
        }

        [Test]
        public void TheSteps_WrapAtBothEnds()
        {
            Assert.AreEqual(0, DisplayChoices.Step(4, 1, 5));
            Assert.AreEqual(4, DisplayChoices.Step(0, -1, 5));
            _settings.Restore(new SettingsSaveData { resolutionWidth = 2560, resolutionHeight = 1440 });
            Assert.AreEqual(0, _settings.ResolutionIndex);
            _settings.StepResolution(-1);
            Assert.AreEqual(_settings.ResolutionLabels.Length - 1, _settings.ResolutionIndex);
            Assert.AreEqual("2560 × 1440", _settings.ResolutionLabels[0]);
        }

        [Test]
        public void TheUnlimitedCap_IsTheLastChoice_AndHoldsNothing()
        {
            _settings.Restore(new SettingsSaveData { vsyncOff = true, fpsCap = 0 });
            Assert.AreEqual(0, _settings.FpsCap);
            Assert.AreEqual(0, _display.Applied.FpsCap);
            Assert.IsFalse(_display.Applied.VSync);
        }

        [Test]
        public void TheChoices_SurviveARoundTrip()
        {
            _settings.Restore(new SettingsSaveData());
            _settings.StepResolution(2);
            _settings.StepWindowMode(2);
            _settings.VSync = false;
            _settings.StepFpsCap(2);
            SettingsSaveData saved = _settings.Capture();
            Assert.AreEqual(1280, saved.resolutionWidth);
            Assert.AreEqual(720, saved.resolutionHeight);
            Assert.AreEqual((int)DisplayMode.Windowed, saved.windowMode);
            Assert.IsTrue(saved.vsyncOff);
            Assert.AreEqual(144, saved.fpsCap);

            var other = new FakeDisplayControl();
            PlayerSettings restored = Make(new FakeSettingsServices(), other);
            restored.Restore(saved);
            Assert.AreEqual(1280, other.Applied.Width);
            Assert.AreEqual(DisplayMode.Windowed, other.Applied.Mode);
            Assert.IsFalse(other.Applied.VSync);
            Assert.AreEqual(144, other.Applied.FpsCap);
        }

        [Test]
        public void ASizeTheMonitorNoLongerOffers_FallsBackToTheClosestOne()
        {
            _settings.Restore(new SettingsSaveData { resolutionWidth = 3840, resolutionHeight = 2160 });
            Assert.AreEqual(new Vector2Int(2560, 1440), _display.Resolutions[_settings.ResolutionIndex],
                "the nearest in area, never an index out of range");
            _settings.Restore(new SettingsSaveData { windowMode = 9, fpsCap = 77 });
            Assert.AreEqual(DisplayMode.Borderless, _settings.WindowMode, "an unknown mode is the default one");
            Assert.AreEqual(60, _settings.FpsCap, "an unknown limit is the default one");
        }

        private static PlayerSettings Make(FakeSettingsServices services, FakeDisplayControl display)
        {
            ILocalization localization = TestTables.Localization(new EventBus(), "next.tower", "x", "y");
            return new PlayerSettings(services, services, services, display, localization, new PauseSettings());
        }
    }
}
