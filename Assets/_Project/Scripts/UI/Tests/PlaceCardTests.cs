using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
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
            "next.wake", "next.salvage", "next.tower.part", "next.tower.pad", "next.signal", "next.dig",
            "next.friend.repair", "next.garage", "next.garage.scrap", "next.port", "next.garage.crate",
            "next.garage.deck", "next.relic.dig", "next.relic", "next.lift.stitch", "next.lift.ride",
            "next.chasm", "next.bay.wake", "next.bay", "next.power.part", "next.power.carried",
            "next.power.build", "next.link.relay.0", "next.link.relay.1", "next.link", "notyet.bay",
            "notyet.chart", "ui.lab.turn", "ui.lab.look", "ui.lab.down", "glyph.lab.turn", "glyph.lab.turn.pad",
            "glyph.lab.look", "glyph.lab.look.pad", "glyph.lab.down", "glyph.lab.down.pad", UiKeys.ScanFragment,
            UiKeys.ScanKnown, UiKeys.ScanComplete, UiKeys.ScanSiteCount, "map.site.garage.name", "map.site.kestrel.name",
            "map.site.lander.name", "map.site.drill.name", "micro.06.name", "micro.mule.name", "micro.drone.name",
            "map.canyon.lip.name",
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
            var card = new PlaceCard(layout, settings, localization, new EventBus());
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
            Assert.AreEqual(DisplayStyle.None, layout.PlaceCardJob.style.display.value,
                "a place whose job nothing else is silent about is its name alone");
            Ticks(card, settings.Place.FadeIn + settings.PlaceHoldSeconds + settings.Place.FadeOut + 0.5f);
            Assert.IsNull(card.Current, "the dock was named once, not twice");
            Assert.IsFalse(card.IsVisible);
        }

        [Test]
        public void AScanSiteReached_ShowsThePlacesNameAndFragmentCount_OncePerPlace_ForSiteAndMicro()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            var layout = new UiLayout(uxml.Instantiate());
            TextAsset[] assets = StringTableAssets.Load();
            var tables = new StringTable[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                tables[i] = StringTable.Parse(assets[i].text, assets[i].name);
            }

            ILocalization localization = new LocalizationService(new EventBus(), tables);
            var settings = new GuidanceSettings();
            var card = new PlaceCard(layout, settings, localization, new EventBus());
            var slot = new GuidanceSlot();
            var wants = new bool[GuidanceSlot.KindCount];
            var shown = new bool[GuidanceSlot.KindCount];

            // Another card (tower/recipe card) holds the guidance slot first; ScanSiteReached waits its turn.
            wants[(int)GuidanceKind.Card] = true;
            shown[(int)GuidanceKind.Card] = true;
            card.Reached(new ScanSiteReached("site.garage", Vector3.zero, 2, 2));
            card.Reached(new ScanSiteReached("site.garage", Vector3.zero, 1, 2));
            card.Reached(new ScanSiteReached("micro.06", Vector3.zero, 1, 1));
            card.Reached(new ScanSiteReached("canyon.lip", Vector3.zero, 2, 2));
            wants[(int)GuidanceKind.Place] = card.IsWaiting || card.IsVisible;
            shown[(int)GuidanceKind.Place] = card.IsVisible;
            slot.Step(wants, shown);
            Assert.IsFalse(slot.Allows(GuidanceKind.Place), "waits while another card holds the guidance slot");
            card.Tick(Step, slot.Allows(GuidanceKind.Place));
            Assert.IsNull(card.Current);

            // The other card finishes; the scan site card takes the guidance slot alone.
            wants[(int)GuidanceKind.Card] = false;
            shown[(int)GuidanceKind.Card] = false;
            wants[(int)GuidanceKind.Next] = true;
            slot.Step(wants, shown);
            Assert.IsTrue(slot.Allows(GuidanceKind.Place));
            Assert.IsFalse(slot.Allows(GuidanceKind.Next), "takes the guidance slot alone");

            card.Tick(Step, slot.Allows(GuidanceKind.Place));
            Assert.AreEqual("map.site.garage.name", card.Current);
            Assert.AreEqual(localization.Get("map.site.garage.name"), layout.PlaceCardName.text);
            Assert.AreEqual(DisplayStyle.Flex, layout.PlaceCardJob.style.display.value);
            Assert.AreEqual("2 fragments to scan", layout.PlaceCardJob.text, "uses Left from the event");

            localization.SetLanguage("vi");
            card.Relocalize();
            Assert.AreEqual(localization.Get("map.site.garage.name"), layout.PlaceCardName.text);
            Assert.AreEqual(string.Format(localization.Get(UiKeys.ScanSiteCount), 2), layout.PlaceCardJob.text);
            StringAssert.DoesNotContain("[[", layout.PlaceCardName.text);
            StringAssert.DoesNotContain("[[", layout.PlaceCardJob.text);
            localization.SetLanguage("en");
            card.Relocalize();

            Ticks(card, settings.Place.FadeIn + settings.PlaceHoldSeconds + settings.Place.FadeOut + 0.5f);
            Assert.AreEqual("micro.06.name", card.Current, "micro.* uses <placeId>.name, and second site.garage was ignored");
            Assert.AreEqual(localization.Get("micro.06.name"), layout.PlaceCardName.text);
            Assert.AreEqual("1 fragments to scan", layout.PlaceCardJob.text);

            Ticks(card, settings.Place.FadeIn + settings.PlaceHoldSeconds + settings.Place.FadeOut + 0.5f);
            Assert.AreEqual("map.canyon.lip.name", card.Current, "canyon.lip uses map.<placeId>.name");
            Assert.AreEqual(localization.Get("map.canyon.lip.name"), layout.PlaceCardName.text);
            Assert.AreEqual("2 fragments to scan", layout.PlaceCardJob.text);

            Ticks(card, settings.Place.FadeIn + settings.PlaceHoldSeconds + settings.Place.FadeOut + 0.5f);
            Assert.IsNull(card.Current, "a second ScanSiteReached for the same place shows nothing");
            Assert.IsFalse(card.IsVisible);
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
                    if (key == "place.dock")
                    {
                        Assert.IsTrue(table.TryGet(key + ".job", out _), $"{table.Language}: {key}.job");
                    }
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
