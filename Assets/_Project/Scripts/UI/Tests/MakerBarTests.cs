using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The per-maker bar on a maker scan's card (opening-v3-payoff-audit §3): filled cells for the entries understood,
    /// empty ones up to the next unlock, which it names; no bar at all for a maker whose next unlock has no name yet.
    /// </summary>
    public sealed class MakerBarTests
    {
        private const string Headlamp = "adapted.ssr3_lamp";

        private LocalizationService _localization;

        [SetUp]
        public void SetUp()
        {
            var en = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssetPaths.StringTable("en"));
            var vi = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssetPaths.StringTable("vi"));
            _localization = new LocalizationService(new EventBus(),
                new[] { StringTable.Parse(en.text, "en"), StringTable.Parse(vi.text, "vi") });
        }

        [Test]
        public void TheTricklePost_ShowsKesslerAraiHalfway_AndNamesTheHeadlamp_InEnglishAndVietnamese()
        {
            _localization.SetLanguage("en");
            Assert.IsTrue(MakerBar.TryLine(DatabankMaker.KesslerArai, 1, 2, Headlamp, _localization, out string en));
            Assert.AreEqual("KESSLER–ARAI █░ · next: SSR-3 Headlamp", en);

            _localization.SetLanguage("vi");
            Assert.IsTrue(MakerBar.TryLine(DatabankMaker.KesslerArai, 1, 2, Headlamp, _localization, out string vi));
            Assert.AreEqual("KESSLER–ARAI █░ · tiếp theo: Đèn pha SSR-3", vi);
        }

        [Test]
        public void TheBarCountsEveryStepTowardTheNextUnlock()
        {
            _localization.SetLanguage("en");
            Assert.IsTrue(MakerBar.TryLine(DatabankMaker.KesslerArai, 2, 5, Headlamp, _localization, out string line));
            StringAssert.Contains("██░░░", line);
        }

        [Test]
        public void AMakerWithNoNamedNextUnlock_ShowsNoBar_NeverAFakeOne()
        {
            Assert.IsFalse(MakerBar.TryLine(DatabankMaker.ILC, 1, 3, string.Empty, _localization, out _),
                "no next unlock at all");
            Assert.IsFalse(MakerBar.TryLine(DatabankMaker.ILC, 1, 3, "adapted.reflector_beacon", _localization, out _),
                "a next unlock with no name yet (its model is still coming)");
            Assert.IsFalse(MakerBar.TryLine(DatabankMaker.None, 1, 2, Headlamp, _localization, out _), "no maker");
            Assert.IsFalse(MakerBar.TryLine(DatabankMaker.KesslerArai, 2, 2, Headlamp, _localization, out _),
                "the threshold is met: the card is the unlock's, not a full bar");
            Assert.IsFalse(MakerBar.TryLine(DatabankMaker.KesslerArai, 1, MakerBar.MaxCells + 1, Headlamp,
                _localization, out _), "a track too long to draw");
        }

        [Test]
        public void AnAdaptedBlueprint_IsNamedOnlyWhenItsNameExists()
        {
            Assert.IsTrue(MakerBar.TryName(Headlamp, _localization, out string key));
            Assert.AreEqual("adapted.ssr3_lamp.name", key);
            Assert.IsFalse(MakerBar.TryName("adapted.reflector_beacon", _localization, out _));
            Assert.IsFalse(MakerBar.TryName(string.Empty, _localization, out _));
        }
    }
}
