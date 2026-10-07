using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Gameplay;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The ticker and the story card share the stage one at a time: the real card and ticker, built over GameUI.uxml
    /// and stepped the way the UI system steps them, are never on screen together, and the ticker's line comes back
    /// after the card has gone.
    /// </summary>
    public sealed class TickerStageTests
    {
        private const float Frame = 1f / 60f;
        private const string Home = "test.ticker.home";
        private const string CloseGlyph = "Esc";

        private UiTuning _tuning;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void ACard_NeverSharesTheScreenWithATickerLine()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            var catalog = AssetDatabase.LoadAssetAtPath<RelicCatalog>(UiAssetPaths.RelicCatalog);
            Assert.IsNotNull(uxml, UiAssetPaths.Uxml);
            Assert.IsNotNull(catalog, UiAssetPaths.RelicCatalog);
            string relic = catalog.Relics[0].Id;
            var events = new EventBus();
            LocalizationService localization = TestTables.Localization(events,
                UiKeys.CardClose, "Close", "Đóng",
                UiKeys.CardCaption, "Memory {0} of {1}", "Ký ức thứ {0} trong {1}",
                UiKeys.RelicName(relic), "A relic", "Một kỷ vật",
                UiKeys.RelicMemory(relic), "It remembers Earth.", "Nó nhớ Trái Đất.",
                Home, "Bell got home before you.", "Bell về nhà trước bạn rồi.");
            _tuning = ScriptableObject.CreateInstance<UiTuning>();
            var layout = new UiLayout(uxml.Instantiate());
            var card = new MemoryCard(layout, _tuning.MemoryCard, localization, events, catalog, new NoFriends());
            var lines = new TickerQueue(_tuning.Ticker, new TickerText(localization));
            var ticker = new RadioTicker(layout, _tuning.Ticker, lines);

            lines.Enqueue(new TickerLine(Home));
            for (float t = 0f; t < 10f && !ticker.IsShown; t += Frame)
            {
                Step(card, ticker);
            }

            Assert.IsTrue(ticker.IsShown, "the line is up");
            card.Enqueue(relic, 1);
            bool cardShown = false;
            bool lineBack = false;
            for (float t = 0f; t < 60f; t += Frame)
            {
                Step(card, ticker);
                Assert.IsFalse(card.IsVisible && ticker.IsVisible, $"card and ticker overlap at {t:0.00} s");
                cardShown |= card.IsVisible;
                lineBack |= cardShown && !card.IsBusy && ticker.IsShown;
            }

            Assert.IsTrue(cardShown, "the card got its turn");
            Assert.IsTrue(lineBack, "and the ticker's line came back after it");
        }

        /// <summary>The order and the gates of the UI system's tick.</summary>
        private static void Step(MemoryCard card, RadioTicker ticker)
        {
            card.Tick(Frame, CloseGlyph, InputDeviceKind.KeyboardMouse, !ticker.IsVisible);
            ticker.Tick(Frame, !card.IsBusy);
        }
    }
}
