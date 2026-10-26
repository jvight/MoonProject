using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Gameplay;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The build ghost's reason chip names what blocks it on a real overlap (owner playtest: a bare "Something's in the
    /// way" left the player stuck), and keeps the plain line when nothing is named.
    /// </summary>
    public sealed class PlacementReasonTextTests
    {
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
        public void ARealOverlap_NamesTheStructureInTheWay_InBothLanguages()
        {
            _localization.SetLanguage("en");
            Assert.AreEqual("Blocked by: " + _localization.Get("structure.charging_pad.name"),
                PlacementReasonText.Of(PlacementReason.Overlaps, "structure.charging_pad.name", _localization));
            _localization.SetLanguage("vi");
            Assert.AreEqual("Vướng: " + _localization.Get("structure.charging_pad.name"),
                PlacementReasonText.Of(PlacementReason.Overlaps, "structure.charging_pad.name", _localization));
        }

        [Test]
        public void AnOverlapWithNothingNamed_AndEveryOtherReason_KeepTheirOwnLine()
        {
            _localization.SetLanguage("en");
            Assert.AreEqual(_localization.Get(UiKeys.BuildReasonOverlaps),
                PlacementReasonText.Of(PlacementReason.Overlaps, string.Empty, _localization));
            Assert.AreEqual(_localization.Get(UiKeys.BuildReasonFrame),
                PlacementReasonText.Of(PlacementReason.NeedsFrame, "structure.charging_pad.name", _localization),
                "only an overlap names a blocker");
            Assert.AreEqual(string.Empty, PlacementReasonText.Of(PlacementReason.Valid, string.Empty, _localization));
        }
    }
}
