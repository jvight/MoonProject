using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI.Tests
{
    /// <summary>The text size setting: 100, 125, 150%, saved, and applied by shrinking the panel's reference height.</summary>
    public sealed class TextSizeTests
    {
        [Test]
        public void TheTextSize_StepsThroughTheTunedScales_AndWrapsRound()
        {
            PlayerSettings settings = NewSettings();

            Assert.AreEqual(1f, settings.TextScale);
            Assert.AreEqual("100%", settings.TextSizeLabel);
            settings.NextTextSize();
            Assert.AreEqual(1.25f, settings.TextScale);
            Assert.AreEqual("125%", settings.TextSizeLabel);
            settings.NextTextSize();
            Assert.AreEqual("150%", settings.TextSizeLabel);
            settings.NextTextSize();
            Assert.AreEqual(1f, settings.TextScale, "back to the normal size");
            Assert.IsTrue(settings.IsDirty);
        }

        [Test]
        public void TheTextSize_IsSaved_AndASaveFromBeforeItReadsNormal()
        {
            PlayerSettings settings = NewSettings();
            settings.NextTextSize();
            settings.NextTextSize();

            PlayerSettings restored = NewSettings();
            restored.Restore(settings.Capture());
            Assert.AreEqual(1.5f, restored.TextScale);

            PlayerSettings older = NewSettings();
            older.Restore(new SettingsSaveData());
            Assert.AreEqual(1f, older.TextScale, "an older save has no text size");
        }

        [Test]
        public void ACorruptSavedTextSize_FallsToTheNearestOffered()
        {
            PlayerSettings settings = NewSettings();

            settings.Restore(new SettingsSaveData { textScale = 1.4f });
            Assert.AreEqual(1.5f, settings.TextScale);
            settings.Restore(new SettingsSaveData { textScale = float.NaN });
            Assert.AreEqual(1f, settings.TextScale);
            settings.Restore(new SettingsSaveData { textScale = -3f });
            Assert.AreEqual(1f, settings.TextScale);
        }

        [Test]
        public void TheScale_ShrinksThePanelsReferenceResolution_AndPutsItBack()
        {
            var panel = ScriptableObject.CreateInstance<PanelSettings>();
            try
            {
                panel.referenceResolution = new Vector2Int(1920, 1080);
                var scale = new TextScale(panel);

                scale.Apply(1.5f);
                Assert.AreEqual(new Vector2Int(1280, 720), panel.referenceResolution);
                scale.Apply(1.25f);
                Assert.AreEqual(new Vector2Int(1536, 864), panel.referenceResolution);
                scale.Restore();
                Assert.AreEqual(new Vector2Int(1920, 1080), panel.referenceResolution);
                Assert.Throws<System.ArgumentOutOfRangeException>(() => scale.Apply(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(panel);
            }
        }

        private static PlayerSettings NewSettings()
        {
            var services = new FakeSettingsServices();
            ILocalization localization = TestTables.Localization(new EventBus(), "next.tower", "x", "y");
            return new PlayerSettings(services, services, services, localization, new PauseSettings());
        }
    }
}
