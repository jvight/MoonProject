using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// A place's card names it and its one job, once per place, one card at a time, on the names' soft shadow; every
    /// place and every step line has its words in both languages.
    /// </summary>
    public sealed class PlaceCardTests
    {
        private const float Step = 0.1f;

        private static readonly string[] Places =
        {
            "dock", "lander", "tower", "bay", "ladder", "lift", "lab", "rack",
        };

        private static readonly string[] Lines =
        {
            "next.wake", "next.salvage", "next.tower", "next.signal", "next.relic", "next.lift.stitch",
            "next.lift.ride", "next.bay", "next.leap", "next.relay", "next.chart", "next.chart.look", "notyet.bay",
            "notyet.chart", "ui.lab.turn", "ui.lab.look", "ui.lab.down", "glyph.lab.turn", "glyph.lab.turn.pad",
            "glyph.lab.look", "glyph.lab.look.pad", "glyph.lab.down", "glyph.lab.down.pad", UiKeys.BlueprintFound,
            UiKeys.BlueprintDelivered,
        };

        [Test]
        public void APlaceCard_ShowsNameAndJob_OncePerPlace_OneAtATime()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            var layout = new UiLayout(uxml.Instantiate());
            ILocalization localization = TestTables.Localization(new EventBus(),
                "place.dock.name", "Charging Dock", "Đế sạc",
                "place.dock.job", "Where 07 rests.", "Nơi 07 nghỉ.",
                "place.ladder.name", "Crew's Ladder", "Thang",
                "place.ladder.job", "Not made for wheels.", "Không dành cho bánh xe.");
            var settings = new GuidanceSettings();
            var card = new PlaceCard(layout, settings, localization);
            Assert.AreSame(layout.PlaceCard, layout.PlaceCardShadow.parent, "the shadow fades with the card");

            card.Approached("place.dock");
            card.Approached("ladder");
            card.Approached("place.dock");
            card.Tick(Step, false);
            Assert.IsNull(card.Current, "waits while the screen is busy");

            card.Tick(Step, true);
            Assert.AreEqual("place.dock.name", card.Current);
            Assert.AreEqual("Charging Dock", layout.PlaceCardName.text);
            Assert.AreEqual("Where 07 rests.", layout.PlaceCardJob.text);
            Ticks(card, settings.Place.FadeIn + settings.PlaceHoldSeconds + settings.Place.FadeOut + 0.5f);
            Assert.AreEqual("place.ladder.name", card.Current, "the next place waited its turn");
            Assert.AreEqual("Not made for wheels.", layout.PlaceCardJob.text);
            Ticks(card, settings.Place.FadeIn + settings.PlaceHoldSeconds + settings.Place.FadeOut + 0.5f);
            Assert.IsNull(card.Current, "the dock was named once, not twice");
            Assert.IsFalse(card.IsVisible);
        }

        [Test]
        public void AFoundBlueprint_GetsTheSameQuietCard_OncePerKit()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            var layout = new UiLayout(uxml.Instantiate());
            ILocalization localization = TestTables.Localization(new EventBus(),
                UiKeys.UpgradeName("rover.cargo_cradle"), "Cargo Cradle", "Giá chở hàng",
                UiKeys.BlueprintFound, "Blueprint found", "Tìm thấy bản vẽ");
            var settings = new GuidanceSettings();
            var card = new PlaceCard(layout, settings, localization);
            card.BlueprintFound("rover.cargo_cradle");
            card.BlueprintFound("rover.cargo_cradle");
            card.Tick(Step, true);
            Assert.AreEqual("Cargo Cradle", layout.PlaceCardName.text);
            Assert.AreEqual("Blueprint found", layout.PlaceCardJob.text);
            Ticks(card, settings.Place.FadeIn + settings.PlaceHoldSeconds + settings.Place.FadeOut + 0.5f);
            Assert.IsNull(card.Current, "once per kit");
        }

        [Test]
        public void EveryPlaceAndStepLine_HasItsWords_InEveryLanguage()
        {
            foreach (TextAsset asset in StringTableAssets.Load())
            {
                StringTable table = StringTable.Parse(asset.text, asset.name);
                foreach (string place in Places)
                {
                    string key = PlaceCard.KeyBase(place);
                    Assert.IsTrue(table.TryGet(key + ".name", out _), $"{table.Language}: {key}.name");
                    Assert.IsTrue(table.TryGet(key + ".job", out _), $"{table.Language}: {key}.job");
                }

                foreach (string line in Lines)
                {
                    Assert.IsTrue(table.TryGet(line, out _), $"{table.Language}: {line}");
                }
            }
        }

        private static void Ticks(PlaceCard card, float seconds)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                card.Tick(Step, true);
            }
        }
    }
}
