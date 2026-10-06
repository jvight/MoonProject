using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The localization service and the player settings that persist the language with volumes and look.
    /// </summary>
    public sealed class LocalizationTests
    {
        private const string English =
            "{\"language\":\"en\",\"name\":\"English\",\"entries\":[" +
            "{\"key\":\"ui.pause.resume\",\"text\":\"Resume\"},{\"key\":\"ui.only.english\",\"text\":\"Hello\"}]}";

        private const string Vietnamese =
            "{\"language\":\"vi\",\"name\":\"Tiếng Việt\",\"entries\":[" +
            "{\"key\":\"ui.pause.resume\",\"text\":\"Tiếp tục\"}]}";

        private EventBus _events;
        private LocalizationService _localization;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _localization = new LocalizationService(_events,
                new[] { StringTable.Parse(English, "en"), StringTable.Parse(Vietnamese, "vi") });
        }

        [Test]
        public void StartsInThePrimaryLanguage_AndSwitches_PublishingTheChange()
        {
            var changes = new List<string>();
            using (_events.Subscribe<LanguageChanged>(changed => changes.Add(changed.Language)))
            {
                Assert.AreEqual("en", _localization.Language);
                Assert.AreEqual("Resume", _localization.Get("ui.pause.resume"));
                CollectionAssert.AreEqual(new[] { "en", "vi" }, _localization.Languages);
                Assert.AreEqual("Tiếng Việt", _localization.GetLanguageName("vi"));

                _localization.SetLanguage("vi");
                _localization.SetLanguage("vi");
                Assert.AreEqual("Tiếp tục", _localization.Get("ui.pause.resume"));
                CollectionAssert.AreEqual(new[] { "vi" }, changes, "one change, no repeat for the same language");
            }
        }

        [Test]
        public void AKeyMissingFromTheChosenLanguage_ShowsThePrimaryText_AndIsReportedOnce()
        {
            _localization.SetLanguage("vi");
            LogAssert.Expect(LogType.Error, new Regex("ui.only.english"));
            Assert.AreEqual("Hello", _localization.Get("ui.only.english"));
            Assert.AreEqual("Hello", _localization.Get("ui.only.english"), "reported once, not every frame");
        }

        [Test]
        public void AnUnknownKey_ShowsItself_AndIsReported()
        {
            LogAssert.Expect(LogType.Error, new Regex("ui.not.there"));
            Assert.AreEqual("ui.not.there", _localization.Get("ui.not.there"));
        }

        [Test]
        public void Get_ReturnsTheSameString_WithoutBuildingOne()
        {
            Assert.AreSame(_localization.Get("ui.pause.resume"), _localization.Get("ui.pause.resume"));
        }

        [Test]
        public void BadTablesAndLanguages_FailLoudly()
        {
            Assert.Throws<ArgumentException>(() => _localization.SetLanguage("fr"));
            Assert.Throws<FormatException>(() => StringTable.Parse("{\"language\":\"en\"}", "nameless"));
            Assert.Throws<FormatException>(() => StringTable.Parse(
                "{\"language\":\"en\",\"name\":\"E\",\"entries\":[{\"key\":\"a.b\",\"text\":\"1\"}," +
                "{\"key\":\"a.b\",\"text\":\"2\"}]}", "duplicate"));
            Assert.Throws<ArgumentException>(() => new LocalizationService(_events,
                new[] { StringTable.Parse(English, "en"), StringTable.Parse(English, "en again") }));
        }

        [Test]
        public void PlayerSettings_RoundTripVolumesLookAndLanguage()
        {
            var services = new FakeSettingsServices();
            var pause = new PauseSettings();
            var settings = new PlayerSettings(services, services, _localization, pause);
            settings.SetVolumeStep(AudioBus.Music, 3);
            settings.SetLookStep(6);
            settings.InvertY = true;
            settings.NextLanguage();
            Assert.IsTrue(settings.IsDirty);
            Assert.AreEqual(0.3f, services.GetVolume(AudioBus.Music), 1e-5f, "steps are tenths of full volume");
            Assert.AreEqual(1.5f, services.Sensitivity, 1e-5f, "look steps are quarter multipliers");
            Assert.AreEqual("vi", _localization.Language);
            string json = JsonUtility.ToJson(settings.Capture());

            var otherServices = new FakeSettingsServices();
            var otherLocalization = new LocalizationService(new EventBus(),
                new[] { StringTable.Parse(English, "en"), StringTable.Parse(Vietnamese, "vi") });
            var restored = new PlayerSettings(otherServices, otherServices, otherLocalization, pause);
            restored.Restore(JsonUtility.FromJson<SettingsSaveData>(json));
            Assert.AreEqual(0.3f, otherServices.GetVolume(AudioBus.Music), 1e-5f);
            Assert.AreEqual(1f, otherServices.GetVolume(AudioBus.Master), 1e-5f);
            Assert.AreEqual(1.5f, otherServices.Sensitivity, 1e-5f);
            Assert.IsTrue(otherServices.InvertY);
            Assert.AreEqual("vi", otherLocalization.Language);
            Assert.IsFalse(restored.IsDirty, "a restore is not a change to save");
            Assert.AreEqual(3, restored.GetVolumeStep(AudioBus.Music));
            Assert.AreEqual(6, restored.GetLookStep());
        }

        [Test]
        public void PlayerSettings_KeepsTheLanguage_WhenTheSavedOneIsGone()
        {
            var services = new FakeSettingsServices();
            var settings = new PlayerSettings(services, services, _localization, new PauseSettings());
            LogAssert.Expect(LogType.Warning, new Regex("'fr' is not available"));
            settings.Restore(new SettingsSaveData { language = "fr" });
            Assert.AreEqual("en", _localization.Language);
        }

        [Test]
        public void PlayerSettings_CyclesLanguages_BackToThePrimary()
        {
            var services = new FakeSettingsServices();
            var settings = new PlayerSettings(services, services, _localization, new PauseSettings());
            settings.NextLanguage();
            settings.NextLanguage();
            Assert.AreEqual("en", _localization.Language);
        }
    }
}
