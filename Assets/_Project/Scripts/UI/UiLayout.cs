using System;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Every named element of GameUI.uxml, looked up once. A missing or mistyped element throws immediately with its
    /// name (the UXML and the code disagree: a wiring bug), never a silent null later.
    /// </summary>
    internal sealed class UiLayout
    {
        public UiLayout(VisualElement root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Hud = Require<VisualElement>("hud");

            Title = Require<VisualElement>("title");

            Reticle = Require<VisualElement>("reticle");
            ReticleRest = Require<VisualElement>("reticle-rest");
            ReticleHover = Require<VisualElement>("reticle-hover");

            PromptAnchor = Require<VisualElement>("prompt-anchor");
            Prompt = Require<VisualElement>("prompt");
            PromptShadow = Require<VisualElement>("prompt-shadow");
            PromptGlyph = Require<VisualElement>("prompt-glyph");
            PromptGlyphLabel = Require<Label>("prompt-glyph-label");
            PromptWord = Require<Label>("prompt-word");

            FriendAnchor = Require<VisualElement>("friend-anchor");
            FriendReadout = Require<VisualElement>("friend-readout");
            FriendReadoutShadow = Require<VisualElement>("friend-readout-shadow");
            FriendPips = Require<VisualElement>("friend-pips");
            FriendNameAnchor = Require<VisualElement>("friend-name-anchor");
            FriendName = Require<Label>("friend-name");

            ScrapChip = Require<VisualElement>("scrap-chip");
            ScrapChipShadow = Require<VisualElement>("scrap-chip-shadow");
            ScrapChipIcon = Require<VisualElement>("scrap-chip-icon");
            ScrapChipCount = Require<Label>("scrap-chip-count");

            MemoryCard = Require<VisualElement>("memory-card");
            MemoryCardShadow = Require<VisualElement>("memory-card-shadow");
            MemoryCardIcon = Require<VisualElement>("memory-card-icon");
            MemoryCardCaption = Require<Label>("memory-card-caption");
            MemoryCardCount = Require<Label>("memory-card-count");
            MemoryCardName = Require<Label>("memory-card-name");
            MemoryCardText = Require<Label>("memory-card-text");
            MemoryCardGlyph = Require<VisualElement>("memory-card-glyph");
            MemoryCardGlyphLabel = Require<Label>("memory-card-glyph-label");
            MemoryCardClose = Require<Label>("memory-card-close");

            TowerPanel = Require<VisualElement>("tower-panel");
            TowerPanelShadow = Require<VisualElement>("tower-panel-shadow");
            TowerName = Require<Label>("tower-name");
            TowerLevel = Require<Label>("tower-level");
            TowerTitle = Require<Label>("tower-title");
            TowerDescription = Require<Label>("tower-description");
            TowerConfirm = Require<VisualElement>("tower-confirm");
            TowerRing = Require<VisualElement>("tower-ring");
            TowerRingGlyph = Require<Label>("tower-ring-glyph");
            TowerHoldWord = Require<Label>("tower-hold-word");
            TowerNeed = Require<Label>("tower-need");
            TowerCostRow = Require<VisualElement>("tower-cost-row");
            TowerCostIcon = Require<VisualElement>("tower-cost-icon");
            TowerCost = Require<Label>("tower-cost");

            Ticker = Require<VisualElement>("ticker");
            TickerLamp = Require<VisualElement>("ticker-lamp");
            TickerText = Require<Label>("ticker-text");

            Pause = Require<VisualElement>("pause");
            PauseVeil = Require<VisualElement>("pause-veil");
            PauseStack = Require<VisualElement>("pause-stack");
            PauseMain = Require<VisualElement>("pause-main");
            PauseMainShadow = Require<VisualElement>("pause-main-shadow");
            PauseScrapIcon = Require<VisualElement>("pause-scrap-icon");
            PauseScrapCount = Require<Label>("pause-scrap-count");
            PageMain = Require<VisualElement>("page-main");
            PageQuit = Require<VisualElement>("page-quit");
            ResumeButton = Require<Button>("button-resume");
            SettingsButton = Require<Button>("button-settings");
            QuitButton = Require<Button>("button-quit");
            QuitStayButton = Require<Button>("button-quit-stay");
            QuitConfirmButton = Require<Button>("button-quit-confirm");
            PauseSettings = Require<VisualElement>("pause-settings");
            PauseSettingsShadow = Require<VisualElement>("pause-settings-shadow");
            MasterSlider = Require<SliderInt>("slider-master");
            MusicSlider = Require<SliderInt>("slider-music");
            SfxSlider = Require<SliderInt>("slider-sfx");
            AmbienceSlider = Require<SliderInt>("slider-ambience");
            LookSlider = Require<SliderInt>("slider-look");
            LookValue = Require<Label>("look-value");
            InvertToggle = Require<Toggle>("toggle-invert");
            LanguageButton = Require<Button>("button-language");
            SettingsBackButton = Require<Button>("button-settings-back");
        }

        public VisualElement Root { get; }

        public VisualElement Hud { get; }

        public VisualElement Title { get; }

        public VisualElement Reticle { get; }

        public VisualElement ReticleRest { get; }

        public VisualElement ReticleHover { get; }

        public VisualElement PromptAnchor { get; }

        public VisualElement Prompt { get; }

        public VisualElement PromptShadow { get; }

        public VisualElement PromptGlyph { get; }

        public Label PromptGlyphLabel { get; }

        public Label PromptWord { get; }

        public VisualElement FriendAnchor { get; }

        public VisualElement FriendReadout { get; }

        public VisualElement FriendReadoutShadow { get; }

        public VisualElement FriendPips { get; }

        public VisualElement FriendNameAnchor { get; }

        public Label FriendName { get; }

        public VisualElement ScrapChip { get; }

        public VisualElement ScrapChipShadow { get; }

        public VisualElement ScrapChipIcon { get; }

        public Label ScrapChipCount { get; }

        public VisualElement MemoryCard { get; }

        public VisualElement MemoryCardShadow { get; }

        public VisualElement MemoryCardIcon { get; }

        public Label MemoryCardCaption { get; }

        public Label MemoryCardCount { get; }

        public Label MemoryCardName { get; }

        public Label MemoryCardText { get; }

        public VisualElement MemoryCardGlyph { get; }

        public Label MemoryCardGlyphLabel { get; }

        public Label MemoryCardClose { get; }

        public VisualElement TowerPanel { get; }

        public VisualElement TowerPanelShadow { get; }

        public Label TowerName { get; }

        public Label TowerLevel { get; }

        public Label TowerTitle { get; }

        public Label TowerDescription { get; }

        public VisualElement TowerConfirm { get; }

        public VisualElement TowerRing { get; }

        public Label TowerRingGlyph { get; }

        public Label TowerHoldWord { get; }

        public Label TowerNeed { get; }

        public VisualElement TowerCostRow { get; }

        public VisualElement TowerCostIcon { get; }

        public Label TowerCost { get; }

        public VisualElement Ticker { get; }

        public VisualElement TickerLamp { get; }

        public Label TickerText { get; }

        public VisualElement Pause { get; }

        public VisualElement PauseVeil { get; }

        public VisualElement PauseStack { get; }

        public VisualElement PauseMain { get; }

        public VisualElement PauseMainShadow { get; }

        public VisualElement PauseScrapIcon { get; }

        public Label PauseScrapCount { get; }

        public VisualElement PageMain { get; }

        public VisualElement PageQuit { get; }

        public Button ResumeButton { get; }

        public Button SettingsButton { get; }

        public Button QuitButton { get; }

        public Button QuitStayButton { get; }

        public Button QuitConfirmButton { get; }

        public VisualElement PauseSettings { get; }

        public VisualElement PauseSettingsShadow { get; }

        public SliderInt MasterSlider { get; }

        public SliderInt MusicSlider { get; }

        public SliderInt SfxSlider { get; }

        public SliderInt AmbienceSlider { get; }

        public SliderInt LookSlider { get; }

        public Label LookValue { get; }

        public Toggle InvertToggle { get; }

        public Button LanguageButton { get; }

        public Button SettingsBackButton { get; }

        private T Require<T>(string name) where T : VisualElement
        {
            VisualElement element = Root.Q(name);
            if (element == null)
            {
                throw new InvalidOperationException($"GameUI.uxml has no element named '{name}'.");
            }

            if (!(element is T typed))
            {
                throw new InvalidOperationException(
                    $"GameUI.uxml element '{name}' is a {element.GetType().Name}, not a {typeof(T).Name}.");
            }

            return typed;
        }
    }
}
