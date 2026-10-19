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

            NextLine = Require<VisualElement>("next-line");
            BatteryDial = Require<VisualElement>("battery-dial");
            BatteryDialFace = Require<VisualElement>("battery-dial-face");
            BatteryDialRing = Require<VisualElement>("battery-dial-ring");
            BatteryDialValue = Require<Label>("battery-dial-value");
            NextLineShadow = Require<VisualElement>("next-line-shadow");
            NextLineText = Require<Label>("next-line-text");
            NextLineProgress = Require<VisualElement>("next-line-progress");
            NextLineProgressFill = Require<VisualElement>("next-line-progress-fill");
            SunArc = Require<VisualElement>("sun-arc");
            SunArcFace = Require<VisualElement>("sun-arc-face");
            SunArcLabel = Require<Label>("sun-arc-label");
            SunArcTime = Require<Label>("sun-arc-time");
            Cargo = Require<VisualElement>("cargo");
            UnlockCard = Require<VisualElement>("unlock-card");

            PromptAnchor = Require<VisualElement>("prompt-anchor");
            Prompt = Require<VisualElement>("prompt");
            PromptShadow = Require<VisualElement>("prompt-shadow");
            PromptGlyph = Require<VisualElement>("prompt-glyph");
            PromptGlyphLabel = Require<Label>("prompt-glyph-label");
            PromptHoldRing = Require<VisualElement>("prompt-hold-ring");
            PromptWord = Require<Label>("prompt-word");

            PickupFeed = Require<VisualElement>("pickup-feed");

            SalvageRingAnchor = Require<VisualElement>("salvage-ring-anchor");
            SalvageRing = Require<VisualElement>("salvage-ring");


            LookHint = Require<VisualElement>("look-hint");
            LookHintShadow = Require<VisualElement>("look-hint-shadow");
            LookHintGlyph = Require<VisualElement>("look-hint-glyph");
            LookHintGlyphLabel = Require<Label>("look-hint-glyph-label");
            LookHintWord = Require<Label>("look-hint-word");
            ControlsCard = Require<VisualElement>("controls-card");
            ControlsCardShadow = Require<VisualElement>("controls-card-shadow");
            ControlsDriveGlyph = Require<VisualElement>("controls-drive-glyph");
            ControlsDriveGlyphLabel = Require<Label>("controls-drive-glyph-label");
            ControlsDriveWord = Require<Label>("controls-drive-word");
            ControlsLookGlyph = Require<VisualElement>("controls-look-glyph");
            ControlsLookGlyphLabel = Require<Label>("controls-look-glyph-label");
            ControlsLookWord = Require<Label>("controls-look-word");
            ControlsFaceGlyph = Require<VisualElement>("controls-face-glyph");
            ControlsFaceGlyphLabel = Require<Label>("controls-face-glyph-label");
            ControlsFaceWord = Require<Label>("controls-face-word");

            Pause = Require<VisualElement>("pause");
            PauseVeil = Require<VisualElement>("pause-veil");
            PauseFrame = Require<VisualElement>("pause-frame");
            PauseStack = Require<VisualElement>("pause-stack");
            PauseMain = Require<VisualElement>("pause-main");
            PauseMainShadow = Require<VisualElement>("pause-main-shadow");
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
            TitleCrt = Require<VisualElement>("title-crt");
            TitleScreen = Require<VisualElement>("title-screen");
            TitleWordmark = Require<VisualElement>("title-wordmark");
            TitleSubtitleRow = Require<VisualElement>("title-subtitle-row");
            TitleSubtitlePrefix = Require<Label>("title-subtitle-prefix");
            TitleRule = Require<VisualElement>("title-rule");
            TitleKeyHint = Require<Label>("title-key-hint");
            TitleCrtOverlay = Require<VisualElement>("title-crt-overlay");
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

            BuildReasonAnchor = Require<VisualElement>("build-reason-anchor");
            BuildReason = Require<VisualElement>("build-reason");
            BuildReasonShadow = Require<VisualElement>("build-reason-shadow");
            BuildReasonText = Require<Label>("build-reason-text");
            BuildStrip = Require<VisualElement>("build-strip");
            BuildStripShadow = Require<VisualElement>("build-strip-shadow");
            BuildStripTitle = Require<Label>("build-strip-title");
            BuildStripStatus = Require<Label>("build-strip-status");
            BuildStripCards = Require<VisualElement>("build-strip-cards");
            BuildStripName = Require<Label>("build-strip-name");
            BuildStripDesc = Require<Label>("build-strip-desc");
            BuildStripRecipe = Require<VisualElement>("build-strip-recipe");
            BuildStripGlyphPrimary = Require<VisualElement>("build-strip-glyph-primary");
            BuildStripGlyphPrimaryLabel = Require<Label>("build-strip-glyph-primary-label");
            BuildStripHintPrimary = Require<Label>("build-strip-hint-primary");
            BuildStripGlyphSecondary = Require<VisualElement>("build-strip-glyph-secondary");
            BuildStripGlyphSecondaryLabel = Require<Label>("build-strip-glyph-secondary-label");
            BuildStripHintSecondary = Require<Label>("build-strip-hint-secondary");
        }

        public VisualElement Root { get; }

        public VisualElement Hud { get; }

        /// <summary>The HUD's static pieces, inside the safe frame (world-anchored ones sit in <see cref="Hud"/>).</summary>
        public VisualElement HudFrame { get; }

        /// <summary>The title's and the pause menu's pages, inside their own safe frame.</summary>
        public VisualElement MenuFrame { get; }

        public VisualElement NextLine { get; }

        /// <summary>The battery dial at the bottom left: its glass face, the cells' ring and the percentage.</summary>
        public VisualElement BatteryDial { get; }

        public VisualElement BatteryDialFace { get; }

        /// <summary>07's abilities as round glass bubbles at the bottom centre (filled from code).</summary>
        public VisualElement BatteryDialRing { get; }

        public Label BatteryDialValue { get; }

        public VisualElement NextLineShadow { get; }

        public Label NextLineText { get; }

        /// <summary>The thin bar under the line that fills while 07 welds.</summary>
        public VisualElement NextLineProgress { get; }

        public VisualElement NextLineProgressFill { get; }

        /// <summary>The Sun's arc above the battery dial.</summary>
        public VisualElement SunArc { get; }

        public VisualElement SunArcFace { get; }

        public Label SunArcLabel { get; }

        public Label SunArcTime { get; }

        /// <summary>The cargo screen's root (Tab / View): its parts are found beneath it by name.</summary>
        public VisualElement Cargo { get; }

        /// <summary>The card that says what a newly fitted kit lets 07 do.</summary>
        public VisualElement UnlockCard { get; }

        public VisualElement PromptAnchor { get; }

        public VisualElement Prompt { get; }

        public VisualElement PromptShadow { get; }

        public VisualElement PromptGlyph { get; }

        public Label PromptGlyphLabel { get; }

        /// <summary>The ring round the prompt's keycap that fills while a hold verb runs.</summary>
        public VisualElement PromptHoldRing { get; }

        public Label PromptWord { get; }

        /// <summary>The column the pickup feed's rows are built into.</summary>
        public VisualElement PickupFeed { get; }

        public VisualElement SalvageRingAnchor { get; }

        public VisualElement SalvageRing { get; }

        public VisualElement LookHint { get; }

        public VisualElement LookHintShadow { get; }

        public VisualElement LookHintGlyph { get; }

        public Label LookHintGlyphLabel { get; }

        public Label LookHintWord { get; }

        /// <summary>The first controls card at the bottom left (<see cref="ControlsCardView"/>).</summary>
        public VisualElement ControlsCard { get; }

        public VisualElement ControlsCardShadow { get; }

        public VisualElement ControlsDriveGlyph { get; }

        public Label ControlsDriveGlyphLabel { get; }

        public Label ControlsDriveWord { get; }

        public VisualElement ControlsLookGlyph { get; }

        public Label ControlsLookGlyphLabel { get; }

        public Label ControlsLookWord { get; }

        public VisualElement ControlsFaceGlyph { get; }

        public Label ControlsFaceGlyphLabel { get; }

        public Label ControlsFaceWord { get; }

        public VisualElement Pause { get; }

        public VisualElement PauseVeil { get; }

        public VisualElement PauseFrame { get; }

        public VisualElement PauseStack { get; }

        public VisualElement PauseMain { get; }

        public VisualElement PauseMainShadow { get; }

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

        /// <summary>The full-monitor CRT background and right-pane 2D line-art scene for TITLE-SCREEN v1.</summary>
        public VisualElement TitleCrt { get; }

        /// <summary>The title's wordmark and subtitle, shown while the pause menu is the title's.</summary>
        public VisualElement TitleScreen { get; }

        /// <summary>The drawn Lunarion wordmark (WordmarkPainter) inside <see cref="TitleScreen"/>.</summary>
        public VisualElement TitleWordmark { get; }

        /// <summary>The subtitle row beneath the wordmark (`// một chiếc radio vẫn bật`).</summary>
        public VisualElement TitleSubtitleRow { get; }

        /// <summary>The cyan comment prefix ("// ") beside the title subtitle.</summary>
        public Label TitleSubtitlePrefix { get; }

        /// <summary>The warm amber divider rule beneath the title subtitle.</summary>
        public VisualElement TitleRule { get; }

        /// <summary>The bottom-left control key hint on the title screen.</summary>
        public Label TitleKeyHint { get; }

        /// <summary>The full-monitor CRT scanlines, refresh band, vignette, and curved bezel over the title screen.</summary>
        public VisualElement TitleCrtOverlay { get; }

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

        public VisualElement BuildReasonAnchor { get; }

        public VisualElement BuildReason { get; }

        public VisualElement BuildReasonShadow { get; }

        public Label BuildReasonText { get; }

        public VisualElement BuildStrip { get; }

        public VisualElement BuildStripShadow { get; }

        public Label BuildStripTitle { get; }

        public Label BuildStripStatus { get; }

        public VisualElement BuildStripCards { get; }

        public Label BuildStripName { get; }

        public Label BuildStripDesc { get; }

        public VisualElement BuildStripRecipe { get; }

        public VisualElement BuildStripGlyphPrimary { get; }

        public Label BuildStripGlyphPrimaryLabel { get; }

        public Label BuildStripHintPrimary { get; }

        public VisualElement BuildStripGlyphSecondary { get; }

        public Label BuildStripGlyphSecondaryLabel { get; }

        public Label BuildStripHintSecondary { get; }

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
