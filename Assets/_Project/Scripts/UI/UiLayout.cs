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
            KitTitle = Require<VisualElement>("kit-title");
            KitTitleName = Require<Label>("kit-title-name");
            KitTitleShadow = Require<VisualElement>("kit-title-shadow");

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
            FriendName = Require<VisualElement>("friend-name");
            FriendNameShadow = Require<VisualElement>("friend-name-shadow");
            FriendNameText = Require<Label>("friend-name-text");

            MaterialsChip = Require<VisualElement>("materials-chip");
            MaterialsChipShadow = Require<VisualElement>("materials-chip-shadow");
            MaterialsChipItems = Require<VisualElement>("materials-chip-items");

            SalvageRingAnchor = Require<VisualElement>("salvage-ring-anchor");
            SalvageRing = Require<VisualElement>("salvage-ring");
            SiteNameAnchor = Require<VisualElement>("site-name-anchor");
            SiteName = Require<VisualElement>("site-name");
            SiteNameShadow = Require<VisualElement>("site-name-shadow");
            SiteNameText = Require<Label>("site-name-text");

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
            TowerRecipe = Require<VisualElement>("tower-recipe");
            TowerChoices = Require<VisualElement>("tower-choices");
            TowerPick = Require<VisualElement>("tower-pick");
            TowerPickGlyph = Require<VisualElement>("tower-pick-glyph");
            TowerPickGlyphLabel = Require<Label>("tower-pick-glyph-label");
            TowerPickWord = Require<Label>("tower-pick-word");

            Ticker = Require<VisualElement>("ticker");
            TickerLamp = Require<VisualElement>("ticker-lamp");
            TickerText = Require<Label>("ticker-text");
            TickerLine = Require<VisualElement>("ticker-line");

            LookHint = Require<VisualElement>("look-hint");
            LookHintShadow = Require<VisualElement>("look-hint-shadow");
            LookHintGlyph = Require<VisualElement>("look-hint-glyph");
            LookHintGlyphLabel = Require<Label>("look-hint-glyph-label");
            LookHintWord = Require<Label>("look-hint-word");

            DialReadout = Require<VisualElement>("dial-readout");
            DialReadoutShadow = Require<VisualElement>("dial-readout-shadow");
            DialDetents = Require<VisualElement>("dial-detents");
            DialStation = Require<Label>("dial-station");
            DialTape = Require<Label>("dial-tape");

            HopList = Require<VisualElement>("hop-list");
            HopListShadow = Require<VisualElement>("hop-list-shadow");
            HopListRows = Require<VisualElement>("hop-list-rows");

            RelayTagAnchor = Require<VisualElement>("relay-tag-anchor");
            RelayTag = Require<VisualElement>("relay-tag");
            RelayTagShadow = Require<VisualElement>("relay-tag-shadow");
            RelayTagRing = Require<VisualElement>("relay-tag-ring");
            RelayTagIcon = Require<VisualElement>("relay-tag-icon");
            RelayTagRecipe = Require<VisualElement>("relay-tag-recipe");
            RelayTagNeed = Require<Label>("relay-tag-need");

            HopVeil = Require<VisualElement>("hop-veil");
            HopVeilDark = Require<VisualElement>("hop-veil-dark");
            HopVeilGrain = Require<VisualElement>("hop-veil-grain");

            Pause = Require<VisualElement>("pause");
            PauseVeil = Require<VisualElement>("pause-veil");
            PauseStack = Require<VisualElement>("pause-stack");
            PauseMain = Require<VisualElement>("pause-main");
            PauseMainShadow = Require<VisualElement>("pause-main-shadow");
            PauseMaterials = Require<VisualElement>("pause-materials");
            PauseCassettes = Require<VisualElement>("pause-cassettes");
            PauseCassetteIcon = Require<VisualElement>("pause-cassette-icon");
            PauseCassettesCount = Require<Label>("pause-cassettes-count");
            PauseRelays = Require<VisualElement>("pause-relays");
            PauseRelayIcon = Require<VisualElement>("pause-relay-icon");
            PauseRelaysCount = Require<Label>("pause-relays-count");
            PageMain = Require<VisualElement>("page-main");
            PageQuit = Require<VisualElement>("page-quit");
            ResumeButton = Require<Button>("button-resume");
            SettingsButton = Require<Button>("button-settings");
            NewGameButton = Require<Button>("button-new-game");
            QuitButton = Require<Button>("button-quit");
            QuitStayButton = Require<Button>("button-quit-stay");
            QuitConfirmButton = Require<Button>("button-quit-confirm");
            PageNewGame = Require<VisualElement>("page-new-game");
            NewGameKeepButton = Require<Button>("button-new-game-keep");
            NewGameConfirmButton = Require<Button>("button-new-game-confirm");
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

        public VisualElement KitTitle { get; }

        public Label KitTitleName { get; }

        public VisualElement KitTitleShadow { get; }

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

        public VisualElement FriendName { get; }

        public VisualElement FriendNameShadow { get; }

        public Label FriendNameText { get; }

        public VisualElement MaterialsChip { get; }

        public VisualElement MaterialsChipShadow { get; }

        public VisualElement MaterialsChipItems { get; }

        public VisualElement SalvageRingAnchor { get; }

        public VisualElement SalvageRing { get; }

        public VisualElement SiteNameAnchor { get; }

        public VisualElement SiteName { get; }

        public VisualElement SiteNameShadow { get; }

        public Label SiteNameText { get; }

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

        public VisualElement TowerRecipe { get; }

        public VisualElement TowerChoices { get; }

        public VisualElement TowerPick { get; }

        public VisualElement TowerPickGlyph { get; }

        public Label TowerPickGlyphLabel { get; }

        public Label TowerPickWord { get; }

        public VisualElement Ticker { get; }

        public VisualElement TickerLamp { get; }

        public Label TickerText { get; }

        public VisualElement TickerLine { get; }

        public VisualElement LookHint { get; }

        public VisualElement LookHintShadow { get; }

        public VisualElement LookHintGlyph { get; }

        public Label LookHintGlyphLabel { get; }

        public Label LookHintWord { get; }

        public VisualElement DialReadout { get; }

        public VisualElement DialReadoutShadow { get; }

        public VisualElement DialDetents { get; }

        public Label DialStation { get; }

        public Label DialTape { get; }

        public VisualElement HopList { get; }

        public VisualElement HopListShadow { get; }

        public VisualElement HopListRows { get; }

        public VisualElement RelayTagAnchor { get; }

        public VisualElement RelayTag { get; }

        public VisualElement RelayTagShadow { get; }

        public VisualElement RelayTagRing { get; }

        public VisualElement RelayTagIcon { get; }

        public VisualElement RelayTagRecipe { get; }

        public Label RelayTagNeed { get; }

        public VisualElement HopVeil { get; }

        public VisualElement HopVeilDark { get; }

        public VisualElement HopVeilGrain { get; }

        public VisualElement Pause { get; }

        public VisualElement PauseVeil { get; }

        public VisualElement PauseStack { get; }

        public VisualElement PauseMain { get; }

        public VisualElement PauseMainShadow { get; }

        public VisualElement PauseMaterials { get; }

        public VisualElement PauseCassettes { get; }

        public VisualElement PauseCassetteIcon { get; }

        public Label PauseCassettesCount { get; }

        public VisualElement PauseRelays { get; }

        public VisualElement PauseRelayIcon { get; }

        public Label PauseRelaysCount { get; }

        public VisualElement PageMain { get; }

        public VisualElement PageQuit { get; }

        public Button ResumeButton { get; }

        public Button SettingsButton { get; }

        public Button NewGameButton { get; }

        public Button QuitButton { get; }

        public Button QuitStayButton { get; }

        public Button QuitConfirmButton { get; }

        public VisualElement PageNewGame { get; }

        public Button NewGameKeepButton { get; }

        public Button NewGameConfirmButton { get; }

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
