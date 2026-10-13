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
            HudFrame = Require<VisualElement>("hud-frame");
            MenuFrame = Require<VisualElement>("menu-frame");
            LabHud = Require<VisualElement>("lab-hud");

            NextLine = Require<VisualElement>("next-line");
            BatteryDial = Require<VisualElement>("battery-dial");
            HomeMeter = Require<VisualElement>("home-meter");
            HomeMeterArc = Require<VisualElement>("home-meter-arc");
            HomeMeterValue = Require<Label>("home-meter-value");
            HomeMeterKey = Require<Label>("home-meter-key");
            BatteryDialFace = Require<VisualElement>("battery-dial-face");
            BatteryDialRing = Require<VisualElement>("battery-dial-ring");
            BatteryDialValue = Require<Label>("battery-dial-value");
            NextLineShadow = Require<VisualElement>("next-line-shadow");
            NextLineText = Require<Label>("next-line-text");
            PlaceCard = Require<VisualElement>("place-card");
            PlaceCardShadow = Require<VisualElement>("place-card-shadow");
            PlaceCardName = Require<Label>("place-card-name");
            PlaceCardJob = Require<Label>("place-card-job");
            KitTitle = Require<VisualElement>("kit-title");
            KitTitleName = Require<Label>("kit-title-name");
            KitTitleShadow = Require<VisualElement>("kit-title-shadow");
            RewardCard = Require<VisualElement>("reward-card");
            Cargo = Require<VisualElement>("cargo");
            UnlockCard = Require<VisualElement>("unlock-card");
            CrateNote = Require<VisualElement>("crate-note");
            RewardTitle = Require<Label>("reward-card-title");
            RewardLines = Require<VisualElement>("reward-card-lines");
            TowerState = Require<Label>("tower-state");

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
            FriendPartsLabel = Require<Label>("friend-parts-label");
            FriendNameAnchor = Require<VisualElement>("friend-name-anchor");
            FriendName = Require<VisualElement>("friend-name");
            FriendNameShadow = Require<VisualElement>("friend-name-shadow");
            FriendNameText = Require<Label>("friend-name-text");

            PickupFeed = Require<VisualElement>("pickup-feed");

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
            RecipeCardRoot = Require<VisualElement>("recipe-card");
            TowerPanelShadow = Require<VisualElement>("tower-panel-shadow");
            TowerName = Require<Label>("tower-name");
            TowerLevel = Require<Label>("tower-level");
            TowerTitle = Require<Label>("tower-title");
            TowerDescription = Require<Label>("tower-description");
            TowerConfirm = Require<VisualElement>("tower-confirm");
            TowerRing = Require<VisualElement>("tower-ring");
            TowerRingGlyph = Require<Label>("tower-ring-glyph");
            TowerHoldWord = Require<Label>("tower-hold-word");
            TowerRecipe = Require<VisualElement>("tower-recipe");
            TowerChoices = Require<VisualElement>("tower-choices");
            TowerPick = Require<VisualElement>("tower-pick");
            TowerPickGlyph = Require<VisualElement>("tower-pick-glyph");
            TowerPickGlyphLabel = Require<Label>("tower-pick-glyph-label");

            Ticker = Require<VisualElement>("ticker");
            TickerLamp = Require<VisualElement>("ticker-lamp");
            TickerText = Require<Label>("ticker-text");
            TickerIcon = Require<VisualElement>("ticker-icon");
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

            HopVeil = Require<VisualElement>("hop-veil");
            HopVeilDark = Require<VisualElement>("hop-veil-dark");
            HopVeilGrain = Require<VisualElement>("hop-veil-grain");

            Pause = Require<VisualElement>("pause");
            PauseVeil = Require<VisualElement>("pause-veil");
            PauseStack = Require<VisualElement>("pause-stack");
            PauseMain = Require<VisualElement>("pause-main");
            PauseMainShadow = Require<VisualElement>("pause-main-shadow");
            PauseCan = Require<VisualElement>("pause-can");
            PauseCanRows = Require<VisualElement>("pause-can-rows");
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
            QuitToTitleButton = Require<Button>("button-quit-to-title");
            QuitButton = Require<Button>("button-quit");
            QuitStayButton = Require<Button>("button-quit-stay");
            QuitConfirmButton = Require<Button>("button-quit-confirm");
            PageNewGame = Require<VisualElement>("page-new-game");
            PageLanguage = Require<VisualElement>("page-language");
            TitleScreen = Require<VisualElement>("title-screen");
            TitleWordmark = Require<VisualElement>("title-wordmark");
            Curtain = Require<VisualElement>("curtain");
            SaveTick = Require<VisualElement>("save-tick");
            CreditsButton = Require<Button>("button-credits");
            TabSound = Require<Button>("button-tab-sound");
            TabCamera = Require<Button>("button-tab-camera");
            TabDisplay = Require<Button>("button-tab-display");
            PageSound = Require<VisualElement>("settings-page-sound");
            PageCamera = Require<VisualElement>("settings-page-camera");
            PageDisplay = Require<VisualElement>("settings-page-display");
            ResolutionPrev = Require<Button>("button-resolution-prev");
            ResolutionNext = Require<Button>("button-resolution-next");
            ResolutionValue = Require<Label>("resolution-value");
            ModePrev = Require<Button>("button-mode-prev");
            ModeNext = Require<Button>("button-mode-next");
            ModeValue = Require<Label>("mode-value");
            VSyncToggle = Require<Toggle>("toggle-vsync");
            FpsRow = Require<VisualElement>("row-fps");
            FpsPrev = Require<Button>("button-fps-prev");
            FpsNext = Require<Button>("button-fps-next");
            FpsValue = Require<Label>("fps-value");
            Credits = Require<VisualElement>("credits");
            CreditsTitle = Require<Label>("credits-title");
            CreditsScroll = Require<ScrollView>("credits-scroll");
            CreditsBack = Require<Button>("button-credits-back");
            SaveTickCheck = Require<VisualElement>("save-tick-check");
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
            GentleCameraToggle = Require<Toggle>("toggle-gentle-camera");
            RumbleToggle = Require<Toggle>("toggle-rumble");
            RumbleSlider = Require<SliderInt>("slider-rumble");
            GuidanceToggle = Require<Toggle>("toggle-guidance");
            LanguageButton = Require<Button>("button-language");
            TextSizeButton = Require<Button>("button-text-size");
            SettingsBackButton = Require<Button>("button-settings-back");
        }

        public VisualElement Root { get; }

        public VisualElement Hud { get; }

        /// <summary>The HUD's static pieces, inside the safe frame (world-anchored ones sit in <see cref="Hud"/>).</summary>
        public VisualElement HudFrame { get; }

        /// <summary>The title's and the pause menu's pages, inside their own safe frame.</summary>
        public VisualElement MenuFrame { get; }

        /// <summary>The crew's lab's own quiet screen, shown instead of the HUD while 07 is up in the lab.</summary>
        public VisualElement LabHud { get; }

        public VisualElement NextLine { get; }

        /// <summary>The battery dial at the bottom left: its glass face, the cells' ring and the percentage.</summary>
        public VisualElement BatteryDial { get; }

        public VisualElement BatteryDialFace { get; }

        /// <summary>The distance home at the top centre: its arc, the metres and the bearing line.</summary>
        public VisualElement HomeMeter { get; }

        public VisualElement HomeMeterArc { get; }

        public Label HomeMeterValue { get; }

        public Label HomeMeterKey { get; }

        /// <summary>07's abilities as round glass bubbles at the bottom centre (filled from code).</summary>
        public VisualElement BatteryDialRing { get; }

        public Label BatteryDialValue { get; }

        public VisualElement NextLineShadow { get; }

        public Label NextLineText { get; }

        public VisualElement PlaceCard { get; }

        public VisualElement PlaceCardShadow { get; }

        public Label PlaceCardName { get; }

        public Label PlaceCardJob { get; }

        public VisualElement KitTitle { get; }

        public Label KitTitleName { get; }

        public VisualElement KitTitleShadow { get; }

        /// <summary>The card that says what a tower level gave: its box, its title and the column of its lines.</summary>
        public VisualElement RewardCard { get; }

        /// <summary>The cargo screen's root (Tab / View): its parts are found beneath it by name.</summary>
        public VisualElement Cargo { get; }

        /// <summary>The card that says what a newly fitted kit lets 07 do.</summary>
        public VisualElement UnlockCard { get; }

        /// <summary>The card with a crew crate's packing note.</summary>
        public VisualElement CrateNote { get; }

        public Label RewardTitle { get; }

        public VisualElement RewardLines { get; }

        /// <summary>The tower card's state word ("offline" until the tower is built).</summary>
        public Label TowerState { get; }

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

        /// <summary>The words on the parts chip: whose parts, how many of how many.</summary>
        public Label FriendPartsLabel { get; }

        public VisualElement FriendNameAnchor { get; }

        public VisualElement FriendName { get; }

        public VisualElement FriendNameShadow { get; }

        public Label FriendNameText { get; }

        /// <summary>The column the pickup feed's rows are built into.</summary>
        public VisualElement PickupFeed { get; }

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

        /// <summary>The recipe card's box (the prompt keeps clear of it).</summary>
        public VisualElement RecipeCardRoot { get; }

        public VisualElement TowerPanelShadow { get; }

        public Label TowerName { get; }

        public Label TowerLevel { get; }

        public Label TowerTitle { get; }

        public Label TowerDescription { get; }

        public VisualElement TowerConfirm { get; }

        public VisualElement TowerRing { get; }

        public Label TowerRingGlyph { get; }

        public Label TowerHoldWord { get; }

        public VisualElement TowerRecipe { get; }

        public VisualElement TowerChoices { get; }

        public VisualElement TowerPick { get; }

        public VisualElement TowerPickGlyph { get; }

        public Label TowerPickGlyphLabel { get; }

        public VisualElement Ticker { get; }

        public VisualElement TickerLamp { get; }

        public Label TickerText { get; }

        /// <summary>The radio icon in the ticker toast's round glass bubble.</summary>
        public VisualElement TickerIcon { get; }

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

        public VisualElement HopVeil { get; }

        public VisualElement HopVeilDark { get; }

        public VisualElement HopVeilGrain { get; }

        public VisualElement Pause { get; }

        public VisualElement PauseVeil { get; }

        public VisualElement PauseStack { get; }

        public VisualElement PauseMain { get; }

        public VisualElement PauseMainShadow { get; }

        /// <summary>"07 can": the verbs learned so far, on the pause page (docs/design/first-hour-v2 6.5).</summary>
        public VisualElement PauseCan { get; }

        public VisualElement PauseCanRows { get; }

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

        public Button QuitToTitleButton { get; }

        public Button QuitButton { get; }

        public Button QuitStayButton { get; }

        public Button QuitConfirmButton { get; }

        public VisualElement PageNewGame { get; }

        /// <summary>The first boot's language step: one button per language, made from code.</summary>
        public VisualElement PageLanguage { get; }

        /// <summary>The title's wordmark and subtitle, shown while the pause menu is the title's.</summary>
        public VisualElement TitleScreen { get; }

        /// <summary>The drawn Lunarion wordmark (WordmarkPainter) inside <see cref="TitleScreen"/>.</summary>
        public VisualElement TitleWordmark { get; }

        /// <summary>A soft dark over everything, drawn before the scene reloads (New Game, Quit to title, Quit).</summary>
        public VisualElement Curtain { get; }

        /// <summary>The save tick: a tiny "07" and a check mark in the corner.</summary>
        public VisualElement SaveTick { get; }

        public VisualElement SaveTickCheck { get; }

        /// <summary>The settings' three tabs (Sound, Camera and feel, Display) and their pages.</summary>
        public Button TabSound { get; }

        public Button TabCamera { get; }

        public Button TabDisplay { get; }

        public VisualElement PageSound { get; }

        public VisualElement PageCamera { get; }

        public VisualElement PageDisplay { get; }

        /// <summary>The Display page's resolution, window mode and FPS cap, each stepped by a pair of arrows.</summary>
        public Button ResolutionPrev { get; }

        public Button ResolutionNext { get; }

        public Label ResolutionValue { get; }

        public Button ModePrev { get; }

        public Button ModeNext { get; }

        public Label ModeValue { get; }

        public Toggle VSyncToggle { get; }

        /// <summary>The FPS cap's row, dimmed while V-Sync (which sets the pace) is on.</summary>
        public VisualElement FpsRow { get; }

        public Button FpsPrev { get; }

        public Button FpsNext { get; }

        public Label FpsValue { get; }

        /// <summary>The title's Credits entry.</summary>
        public Button CreditsButton { get; }

        /// <summary>The credits panel's overlay, its title, its scrolling text and its Back button.</summary>
        public VisualElement Credits { get; }

        public Label CreditsTitle { get; }

        public ScrollView CreditsScroll { get; }

        public Button CreditsBack { get; }

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

        public Toggle GentleCameraToggle { get; }

        public Toggle RumbleToggle { get; }

        public SliderInt RumbleSlider { get; }

        public Toggle GuidanceToggle { get; }

        public Button LanguageButton { get; }

        public Button SettingsBackButton { get; }

        /// <summary>Cycles the text size: 100, 125, 150%.</summary>
        public Button TextSizeButton { get; }

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
