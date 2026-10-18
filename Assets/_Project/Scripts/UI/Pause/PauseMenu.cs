using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The pause menu (Esc / gamepad Start). Opening it eases game time to a stop (the radio keeps playing on unscaled
    /// time), turns the rover controls off and frees the cursor; resuming reverses all three. Resume, Settings
    /// (volumes, look speed, invert look, language; persisted), New game and Quit (each asks once, resting on its
    /// safe answer; see <see cref="PausePages"/>). A confirmed new game restores game time and publishes
    /// <see cref="NewGameRequested"/>: the App puts the save away and reloads. Keyboard, mouse and gamepad all
    /// work: UI Toolkit moves focus between the buttons and sliders; Esc / B steps back one level. Every touch is
    /// published as a <see cref="UiCue"/> (open, close, focus move, confirm, back, slider step) for Audio.
    /// The same menu is the title's over the live world (M3-09, <see cref="OpenTitle"/>): Continue in place of Resume,
    /// New Game (asking once when a save exists), the same settings and Quit, under the wordmark; game time keeps
    /// running, and the title menu stays visible until dismissed. A first boot opens on a language step. The pause
    /// offers "Quit to title" instead of New Game. Quit to title, Quit, and New Game from pause draw a soft dark first,
    /// while New Game on the title eases the title away for the lens push-in, so the scene never cuts.
    /// </summary>
    internal sealed class PauseMenu
    {
        private const float MoveEpsilon = 0.1f;
        private const string TitleClass = "pause--title";
        private const string SettingsOpenClass = "pause--settings-open";
        private const string TabOnClass = "settings-tab--on";
        private const string TabSkinOnClass = "settings-tab-wrap--on";
        private const string IdleRowClass = "setting-row--idle";

        private readonly UiLayout _layout;
        private readonly PauseSettings _settings;
        private readonly TitleSettings _title;
        private readonly PlayerSettings _player;
        private readonly ILocalization _localization;
        private readonly InputReader _input;
        private readonly EventBus _events;
        private readonly SettingsStore _settingsStore;
        private readonly IGameFlow _flow;
        private readonly CursorPolicy _cursor;
        private readonly Action _quit;
        private readonly PauseClock _clock;
        private readonly Reveal _veil;
        private readonly Reveal _main;
        private readonly Reveal _settingsPanel;
        private readonly Reveal _mainPage;
        private readonly Reveal _quitPage;
        private readonly Reveal _newGamePage;
        private readonly Reveal _languagePage;
        private readonly Reveal _titleScreen;
        private readonly Reveal _titleCrt;
        private readonly Reveal _titleCrtOverlay;
        private readonly Reveal _titleKeyHint;
        private readonly Reveal _curtain;
        private readonly TitleScenePainter _titleScenePainter;
        private readonly TitleCrtOverlayPainter _titleCrtOverlayPainter;
        private readonly Label _continueNote;
        private readonly CreditsView _credits;
        private readonly VisualElement[] _settingsPages;
        private readonly Button[] _settingsTabs;
        private readonly MenuNavigator _navigator;
        private int _settingsTab;
        private readonly IntText _fpsNumbers;
        private readonly PausePages _pages = new PausePages();
        private readonly string[] _lookTexts;
        private float _resumeScale = 1f;
        private float _writtenShift = float.NaN;
        private float _writtenTop = float.NaN;
        private Leaving _leaving;
        private float _titleTime;
        private bool _lockedTitleTime;

        public PauseMenu(UiLayout layout, PauseSettings settings, TitleSettings title, PlayerSettings player,
            ILocalization localization, InputReader input, EventBus events, SettingsStore settingsStore,
            IGameFlow flow, IntText numbers, CursorPolicy cursor, Action quit,
            CreditsView credits)
        {
            _credits = credits ?? throw new ArgumentNullException(nameof(credits));
            _settingsPages = new[] { layout.PageSound, layout.PageCamera, layout.PageDisplay };
            _settingsTabs = new[] { layout.TabSound, layout.TabCamera, layout.TabDisplay };
            foreach (Button tab in _settingsTabs)
            {
                new TabPainter(tab.parent);
            }

            _fpsNumbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _cursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
            _quit = quit ?? throw new ArgumentNullException(nameof(quit));
            _clock = new PauseClock(settings);

            _veil = new Reveal(layout.PauseVeil, settings.Veil);
            _main = new Reveal(layout.PauseMain, settings.Menu);
            _settingsPanel = new Reveal(layout.PauseSettings, settings.SettingsPanel);
            _mainPage = new Reveal(layout.PageMain, settings.Page);
            _quitPage = new Reveal(layout.PageQuit, settings.Page);
            _newGamePage = new Reveal(layout.PageNewGame, settings.Page);
            _languagePage = new Reveal(layout.PageLanguage, settings.Page);
            _titleScreen = new Reveal(layout.TitleScreen, title.Reveal);
            _titleCrt = new Reveal(layout.TitleCrt, title.Reveal);
            _titleCrtOverlay = new Reveal(layout.TitleCrtOverlay, title.Reveal);
            _titleKeyHint = new Reveal(layout.TitleKeyHint, title.Reveal);
            _curtain = new Reveal(layout.Curtain, title.Curtain);
            _titleScenePainter = new TitleScenePainter(layout.TitleCrt, title, new VisualElement[]
            {
                layout.TitleScreen,
                layout.PauseMain,
                layout.PauseSettings,
                layout.TitleKeyHint,
            });
            _titleCrtOverlayPainter = new TitleCrtOverlayPainter(layout.TitleCrtOverlay, title);
            layout.TitleSubtitlePrefix.text = "// ";
            Button[] languageButtons = LanguageButtons(layout.PageLanguage, localization);
            _continueNote = CreateContinueNote(layout.ResumeButton);
            _navigator = new MenuNavigator(
                layout,
                settings,
                languageButtons,
                NavigationContext,
                Cue,
                SelectTab,
                StepSettingsControl,
                _credits.ScrollByStep);

            _veil.Snap(false);
            _main.Snap(false);
            _settingsPanel.Snap(false);
            _mainPage.Snap(true);
            _quitPage.Snap(false);
            _newGamePage.Snap(false);
            _languagePage.Snap(false);
            _titleScreen.Snap(false);
            _titleCrt.Snap(false);
            _titleCrtOverlay.Snap(false);
            _titleKeyHint.Snap(false);
            _curtain.Snap(false);
            new ShadowPainter(layout.PauseMainShadow);
            new ShadowPainter(layout.PauseSettingsShadow);

            _lookTexts = new string[player.LookStepCount + 1];
            for (int step = player.GentleLookStep; step <= player.LookStepCount; step++)
            {
                _lookTexts[step] = player.LookValue(step).ToString("0.##") + "×";
            }

            Bind();
            Relocalize();
        }

        public bool IsOpen { get; private set; }

        /// <summary>How much of the menu blur the world takes now (0..1): all of it paused, less on the title.</summary>
        public float Openness => _pages.IsTitle
            ? Mathf.Max(Mathf.Max(_main.Visibility, _settingsPanel.Visibility), _credits.Visibility) * _title.BackdropWeight
            : _veil.Visibility;

        /// <summary>True while the credits are open (from the title's Credits entry until Back).</summary>
        public bool IsShowingCredits => _credits.IsOpen;

        /// <summary>True while the menu is the title's (from <see cref="OpenTitle"/> until Continue or New Game).</summary>
        public bool IsTitle => IsOpen && _pages.IsTitle;

        /// <summary>Last measured CPU time (ms) of <see cref="TitleScenePainter.Paint"/>.</summary>
        public float TitleScenePaintMs => _titleScenePainter.LastPaintMilliseconds;

        /// <summary>True while the first boot's language step is up.</summary>
        public bool IsChoosingLanguage => _pages.Current == PausePage.Language;

        /// <summary>True while the settings panel is (easing) open.</summary>
        public bool IsSettingsOpen => _settingsPanel.Target;

        /// <summary>True while the quit question replaces the main buttons.</summary>
        public bool IsAskingToQuit => _pages.Current == PausePage.Quit;

        /// <summary>True while the new-game question replaces the main buttons.</summary>
        public bool IsAskingNewGame => _pages.Current == PausePage.NewGame;

        /// <summary>
        /// True once everything about the menu has finished easing (time fully stopped or fully running).
        /// </summary>
        public bool IsSettled => _clock.IsSettled &&
            (IsOpen
                ? (_main.IsShown || (_pages.IsTitle && (_settingsPanel.IsShown || _credits.IsOpen)))
                : _main.IsHidden);

        /// <summary>The page now shown (tests and captures).</summary>
        public int SettingsTab => _settingsTab;

        /// <summary>
        /// World-space bounding boxes of the painted title scene elements (Moon disc, rover, and visible rim landmarks).
        /// </summary>
        public void GetTitleArtBounds(out Rect moonDiscWorld, out Rect roverWorld, List<Rect> landmarkWorlds = null)
        {
            _titleScenePainter.SyncLeftColumnLayout();
            _titleScenePainter.GetDrawnArtBounds(out moonDiscWorld, out roverWorld, landmarkWorlds);
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            if (_clock.IsSettled)
            {
                _resumeScale = Time.timeScale;
            }

            IsOpen = true;
            _pages.Reset();
            SnapPages();
            WriteMode();
            _clock.Pause();
            _input.Disable();
            _cursor.Menu();
            _titleCrt.Snap(false);
            _titleCrtOverlay.Snap(false);
            _titleKeyHint.Snap(false);
            _veil.Show();
            _main.Show();
            _navigator.ResetOnOpen(_input.Menu.PointerPosition, _layout.ResumeButton);
            _events.Publish(new PauseChanged(true));
            Cue(UiCueKind.MenuOpen);
        }

        public void Resume()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            _navigator.ClearFocus();
            SaveIfChanged();
            _veil.Hide();
            _main.Hide();
            _settingsPanel.Hide();
            _clock.Resume();
            _input.Enable();
            _cursor.Drive();
            _events.Publish(new PauseChanged(false));
            Cue(UiCueKind.MenuClose);
        }

        /// <summary>Esc / B: one level back (a question or settings to the menu, the menu to the game).</summary>
        public void Back()
        {
            if (!IsOpen || _leaving != Leaving.None)
            {
                return;
            }

            if (_credits.IsOpen)
            {
                CloseCredits();
            }
            else if (_pages.Current == PausePage.Language)
            {
                ChooseLanguage(0);
            }
            else if (_pages.Back(out PauseFocus focus))
            {
                _navigator.RequestFocus(focus);
                Cue(UiCueKind.Back);
            }
            else if (_settingsPanel.Target)
            {
                CloseSettings();
                Cue(UiCueKind.Back);
            }
            else if (!_pages.IsTitle)
            {
                Resume();
            }
        }

        /// <summary>
        /// The title over the live world: the wordmark and the menu ease in as the dark of a boot lifts. Continue is
        /// offered when <paramref name="hasSave"/>; the first boot (<paramref name="chooseLanguage"/>) asks for a
        /// language first. The rover's controls are already held by App; game time keeps running.
        /// </summary>
        public void OpenTitle(bool hasSave, bool chooseLanguage)
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            _pages.ResetForTitle(hasSave, chooseLanguage);
            SnapPages();
            WriteMode();
            _cursor.Menu();
            _lockedTitleTime = false;
            _titleTime = 0f;
            Display(_layout.MenuFrame, true);
            StepTitleAnimation(_titleTime, false);
            _titleCrt.Snap(true);
            _titleCrtOverlay.Snap(true);
            _titleKeyHint.Show();
            _main.Show();
            _titleScreen.Show();
            _curtain.Snap(true);
            _curtain.Hide();
            _navigator.ResetOnOpen(_input.Menu.PointerPosition, _navigator.DefaultFocus(NavigationContext()));
        }

        /// <summary>Closes the title if it is open when an external caller starts or continues a game.</summary>
        public void CloseTitleIfOpen()
        {
            if (IsTitle)
            {
                _leaving = Leaving.Gone;
                CloseTitle();
            }
        }

        /// <summary>Evaluates the title screen's 2D CRT animation at an exact timestamp (for deterministic captures).</summary>
        public void SetTitleAnimationTime(float timeSeconds, bool standaloneFraming = false)
        {
            _lockedTitleTime = true;
            _titleTime = timeSeconds;
            Display(_layout.MenuFrame, !standaloneFraming);
            StepTitleAnimation(timeSeconds, standaloneFraming);
        }

        /// <param name="unscaledDeltaTime">Real seconds since the last tick.</param>
        public void Tick(float unscaledDeltaTime)
        {
            if (_clock.Step(unscaledDeltaTime))
            {
                Time.timeScale = _clock.IsSettled && !_clock.Paused ? _resumeScale : _resumeScale * _clock.Scale;
            }

            SwapPages();

            _veil.Tick(unscaledDeltaTime);
            _main.Tick(unscaledDeltaTime);
            _settingsPanel.Tick(unscaledDeltaTime);
            _mainPage.Tick(unscaledDeltaTime);
            _quitPage.Tick(unscaledDeltaTime);
            _newGamePage.Tick(unscaledDeltaTime);
            _languagePage.Tick(unscaledDeltaTime);
            _titleScreen.Tick(unscaledDeltaTime);
            _titleCrt.Tick(unscaledDeltaTime);
            _titleCrtOverlay.Tick(unscaledDeltaTime);
            _titleKeyHint.Tick(unscaledDeltaTime);
            _curtain.Tick(unscaledDeltaTime);
            _credits.Tick(unscaledDeltaTime);
            if (!_titleCrt.IsHidden)
            {
                if (!_lockedTitleTime)
                {
                    _titleTime += unscaledDeltaTime;
                }

                StepTitleAnimation(_titleTime, _titleScenePainter.StandaloneFraming);
            }

            ShiftForSettings();
            _layout.Pause.EnableInClassList(SettingsOpenClass, _settingsPanel.Target);
            CenterSettings();
            if (!_titleCrt.IsHidden)
            {
                _titleScenePainter.SyncLeftColumnLayout();
            }

            if (_leaving != Leaving.None && _curtain.IsShown)
            {
                Go();
            }

            _navigator.Tick(unscaledDeltaTime, _input.Menu.PointerPosition, _input.Menu.NavigateVector);
        }

        /// <summary>Re-reads the words set from code (the language selector).</summary>
        public void Relocalize()
        {
            _layout.LanguageButton.text = _localization.GetLanguageName(_localization.Language);
            WriteDisplay();
            WriteMode();
            _credits.Relocalize();
        }

        /// <summary>Restores normal game speed immediately (the UI is going away mid-pause).</summary>
        public void RestoreTime()
        {
            Time.timeScale = _resumeScale;
        }

        private MenuNavigationContext NavigationContext()
        {
            return new MenuNavigationContext(
                IsOpen && _leaving == Leaving.None,
                _pages.Current,
                _pages.DefaultFocus,
                _credits.IsOpen,
                _settingsPanel.Target,
                _settingsTab);
        }

        private void Bind()
        {
            _layout.ResumeButton.clicked += OnResumeClicked;
            _layout.SettingsButton.clicked += OnSettingsClicked;
            _layout.NewGameButton.clicked += OnNewGameClicked;
            _layout.NewGameKeepButton.clicked += OnKeepGoingClicked;
            _layout.NewGameConfirmButton.clicked += OnNewGameConfirmed;
            _layout.QuitToTitleButton.clicked += OnQuitToTitleClicked;
            _layout.CreditsButton.clicked += OnCreditsClicked;
            _layout.CreditsBack.clicked += OnCreditsBackClicked;
            _layout.QuitButton.clicked += OnQuitClicked;
            _layout.QuitStayButton.clicked += OnStayClicked;
            _layout.QuitConfirmButton.clicked += OnQuitConfirmed;
            _layout.SettingsBackButton.clicked += OnSettingsBackClicked;
            _layout.LanguageButton.clicked += OnLanguageClicked;
            _layout.TextSizeButton.clicked += OnTextSizeClicked;
            _layout.TabSound.clicked += () => OnTabClicked(0);
            _layout.TabCamera.clicked += () => OnTabClicked(1);
            _layout.TabDisplay.clicked += () => OnTabClicked(2);
            _layout.ResolutionPrev.clicked += () => StepDisplay(Step.Resolution, -1);
            _layout.ResolutionNext.clicked += () => StepDisplay(Step.Resolution, 1);
            _layout.ModePrev.clicked += () => StepDisplay(Step.Mode, -1);
            _layout.ModeNext.clicked += () => StepDisplay(Step.Mode, 1);
            _layout.FpsPrev.clicked += () => StepDisplay(Step.FpsCap, -1);
            _layout.FpsNext.clicked += () => StepDisplay(Step.FpsCap, 1);
            _layout.VSyncToggle.RegisterValueChangedCallback(OnVSyncChanged);

            BindVolume(_layout.MasterSlider, AudioBus.Master);
            BindVolume(_layout.MusicSlider, AudioBus.Music);
            BindVolume(_layout.SfxSlider, AudioBus.Sfx);
            BindVolume(_layout.AmbienceSlider, AudioBus.Ambience);

            SliderInt look = _layout.LookSlider;
            look.lowValue = _player.MinLookStep;
            look.highValue = _player.LookStepCount;
            look.fill = true;
            look.RegisterValueChangedCallback(OnLookChanged);
            _layout.InvertToggle.RegisterValueChangedCallback(OnInvertChanged);
            _layout.GentleCameraToggle.RegisterValueChangedCallback(OnGentleCameraChanged);
            _layout.RumbleToggle.RegisterValueChangedCallback(OnRumbleChanged);
            SliderInt rumble = _layout.RumbleSlider;
            rumble.lowValue = 0;
            rumble.highValue = _player.VolumeSteps;
            rumble.fill = true;
            rumble.RegisterValueChangedCallback(OnRumbleStrengthChanged);
            _layout.GuidanceToggle.RegisterValueChangedCallback(OnGuidanceChanged);
        }

        private void StepSettingsControl(VisualElement control, int delta)
        {
            if (control is SliderInt slider)
            {
                int next = Mathf.Clamp(slider.value + delta, slider.lowValue, slider.highValue);
                if (next != slider.value)
                {
                    slider.value = next;
                }

                return;
            }

            if (control is Toggle toggle)
            {
                toggle.value = !toggle.value;
                return;
            }

            if (control == _layout.ResolutionPrev)
            {
                StepDisplay(Step.Resolution, delta);
            }
            else if (control == _layout.ModePrev)
            {
                StepDisplay(Step.Mode, delta);
            }
            else if (control == _layout.FpsPrev)
            {
                StepDisplay(Step.FpsCap, delta);
            }
            else if (control == _layout.TextSizeButton)
            {
                OnTextSizeClicked();
            }
            else if (control == _layout.LanguageButton)
            {
                OnLanguageClicked();
            }
        }

        private void OnTabClicked(int tab)
        {
            if (!IsOpen || !_settingsPanel.Target)
            {
                return;
            }

            SelectTab(tab);
            _navigator.FocusControl(_settingsTabs[tab], byCode: true);
            Cue(UiCueKind.Confirm);
        }

        private void BindVolume(SliderInt slider, AudioBus bus)
        {
            slider.lowValue = 0;
            slider.highValue = _player.VolumeSteps;
            slider.fill = true;
            slider.RegisterValueChangedCallback(evt =>
            {
                _player.SetVolumeStep(bus, evt.newValue);
                Cue(UiCueKind.SliderStep);
            });
        }

        private void OpenSettings()
        {
            _layout.MasterSlider.SetValueWithoutNotify(_player.GetVolumeStep(AudioBus.Master));
            _layout.MusicSlider.SetValueWithoutNotify(_player.GetVolumeStep(AudioBus.Music));
            _layout.SfxSlider.SetValueWithoutNotify(_player.GetVolumeStep(AudioBus.Sfx));
            _layout.AmbienceSlider.SetValueWithoutNotify(_player.GetVolumeStep(AudioBus.Ambience));
            ShowLookStep();
            _layout.InvertToggle.SetValueWithoutNotify(_player.InvertY);
            _layout.GentleCameraToggle.SetValueWithoutNotify(_player.GentleCamera);
            _layout.RumbleToggle.SetValueWithoutNotify(_player.Rumble);
            _layout.RumbleSlider.SetValueWithoutNotify(_player.GetRumbleStep());
            _layout.GuidanceToggle.SetValueWithoutNotify(_player.Guidance);
            _layout.TextSizeButton.text = _player.TextSizeLabel;
            WriteDisplay();
            SelectTab(0);
            _settingsPanel.Show();
            if (_pages.IsTitle)
            {
                _main.Snap(false);
                ApplyTitleLayout();
            }

            _navigator.RequestFocus(_layout.MasterSlider);
        }

        /// <summary>Shows one of the settings' pages (0 Sound, 1 Camera and feel, 2 Display) and marks its tab.</summary>
        internal void SelectTab(int tab)
        {
            _settingsTab = tab;
            for (int i = 0; i < _settingsPages.Length; i++)
            {
                Display(_settingsPages[i], i == tab);
                _settingsTabs[i].EnableInClassList(TabOnClass, i == tab);
                _settingsTabs[i].parent.EnableInClassList(TabSkinOnClass, i == tab);
            }
        }

        private enum Step
        {
            Resolution,
            Mode,
            FpsCap,
        }

        private void StepDisplay(Step what, int direction)
        {
            if (!IsOpen || !_settingsPanel.Target)
            {
                return;
            }

            switch (what)
            {
                case Step.Resolution:
                    _player.StepResolution(direction);
                    break;
                case Step.Mode:
                    _player.StepWindowMode(direction);
                    break;
                default:
                    _player.StepFpsCap(direction);
                    break;
            }

            WriteDisplay();
            Cue(UiCueKind.Confirm);
        }

        private void OnVSyncChanged(ChangeEvent<bool> evt)
        {
            _player.VSync = evt.newValue;
            WriteDisplay();
            Cue(UiCueKind.Confirm);
        }

        /// <summary>Writes the Display page's values (allocation-free: cached labels and numbers).</summary>
        private void WriteDisplay()
        {
            _layout.ResolutionValue.text = _player.ResolutionLabels[_player.ResolutionIndex];
            _layout.ModeValue.text = _localization.Get(UiKeys.DisplayModeName(_player.WindowMode));
            _layout.VSyncToggle.SetValueWithoutNotify(_player.VSync);
            int cap = _player.FpsCap;
            _layout.FpsValue.text = cap > 0 ? _fpsNumbers.Get(cap) : _localization.Get(UiKeys.DisplayUnlimited);
            _layout.FpsRow.EnableInClassList(IdleRowClass, _player.VSync);
        }

        private void CloseSettings()
        {
            SaveIfChanged();
            _settingsPanel.Hide();
            if (_pages.IsTitle)
            {
                _settingsPanel.Snap(false);
                ApplyTitleLayout();
                if (!_credits.IsOpen)
                {
                    _main.Show();
                    _titleScreen.Show();
                    _titleKeyHint.Show();
                }
            }

            _navigator.RequestFocus(_layout.SettingsButton);
        }

        /// <summary>
        /// Eases the pages that are not current away; once they are gone, the current one eases in. Never two at once.
        /// </summary>
        private void SwapPages()
        {
            bool clear = Leave(_mainPage, PausePage.Main) & Leave(_quitPage, PausePage.Quit) &
                         Leave(_newGamePage, PausePage.NewGame) & Leave(_languagePage, PausePage.Language);
            if (clear)
            {
                Page(_pages.Current).Show();
            }
        }

        private bool Leave(Reveal page, PausePage which)
        {
            if (which == _pages.Current)
            {
                return true;
            }

            page.Hide();
            return page.IsHidden;
        }

        private Reveal Page(PausePage page)
        {
            switch (page)
            {
                case PausePage.Quit:
                    return _quitPage;
                case PausePage.NewGame:
                    return _newGamePage;
                case PausePage.Language:
                    return _languagePage;
                default:
                    return _mainPage;
            }
        }

        /// <summary>Asks <paramref name="question"/> in place of the main buttons, focused on its safe answer.</summary>
        private void Ask(PausePage question)
        {
            if (!IsOpen || _leaving != Leaving.None || !_pages.Ask(question))
            {
                return;
            }

            if (_settingsPanel.Target)
            {
                CloseSettings();
            }

            _navigator.RequestFocus(_pages.DefaultFocus);
            Cue(UiCueKind.Confirm);
        }

        /// <summary>Stay / Keep going: back to the button that asked.</summary>
        private void Answer(PausePage question)
        {
            if (IsOpen && _pages.Current == question && _pages.Back(out PauseFocus focus))
            {
                _navigator.RequestFocus(focus);
                Cue(UiCueKind.Back);
            }
        }

        private void SaveIfChanged()
        {
            _settingsStore.SaveIfChanged();
        }

        /// <summary>
        /// The pause menu slides left to keep menu and settings centred together. The title's menu is anchored in the
        /// left column and its settings open in place of the menu, so it never shifts.
        /// </summary>
        private void ShiftForSettings()
        {
            IResolvedStyle panel = _layout.PauseSettings.resolvedStyle;
            float width = panel.width + panel.marginLeft;
            float shift = _pages.IsTitle || float.IsNaN(width) || _settingsPanel.IsHidden
                ? 0f
                : -0.5f * width * _settingsPanel.Visibility;
            if (!float.IsNaN(_writtenShift) && Mathf.Abs(shift - _writtenShift) < MoveEpsilon)
            {
                return;
            }

            _layout.PauseStack.style.translate = new StyleTranslate(new Translate(shift, 0f));
            _writtenShift = shift;
        }

        /// <summary>
        /// The pause menu's settings open level with the menu's middle, so a tall panel (a big text size) grows up and
        /// down together and stays on the screen. The title's settings sit in the left column via the style sheet.
        /// </summary>
        private void CenterSettings()
        {
            float top = float.NaN;
            if (!_pages.IsTitle)
            {
                float menu = _layout.PauseMain.resolvedStyle.height;
                float panel = _layout.PauseSettings.resolvedStyle.height;
                if (!float.IsNaN(menu) && !float.IsNaN(panel))
                {
                    top = 0.5f * (menu - panel);
                }
            }

            if (float.IsNaN(top))
            {
                if (!float.IsNaN(_writtenTop))
                {
                    _layout.PauseSettings.style.top = StyleKeyword.Null;
                    _writtenTop = float.NaN;
                }

                return;
            }

            if (float.IsNaN(_writtenTop) || Mathf.Abs(top - _writtenTop) >= MoveEpsilon)
            {
                _layout.PauseSettings.style.top = top;
                _writtenTop = top;
            }
        }

        private void Cue(UiCueKind kind)
        {
            _events.Publish(new UiCue(kind));
        }

        private void OnResumeClicked()
        {
            if (!IsOpen || _leaving != Leaving.None || _pages.Current != PausePage.Main)
            {
                return;
            }

            if (!_pages.IsTitle)
            {
                Resume();
            }
            else if (_pages.HasSave)
            {
                CloseTitle();
                Cue(UiCueKind.Confirm);
                _events.Publish(new ContinueRequested());
            }
        }

        private void OnSettingsClicked()
        {
            if (IsOpen && _pages.Current == PausePage.Main)
            {
                OpenSettings();
                Cue(UiCueKind.Confirm);
            }
        }

        private void OnQuitClicked()
        {
            if (!_pages.IsTitle)
            {
                Ask(PausePage.Quit);
            }
            else if (IsOpen && _pages.Current == PausePage.Main && _leaving == Leaving.None)
            {
                SaveIfChanged();
                Cue(UiCueKind.Confirm);
                Draw(Leaving.Quit);
            }
        }

        /// <summary>The title's Credits entry: the credits come up over the title and its menu steps aside.</summary>
        private void OnCreditsClicked()
        {
            if (!IsOpen || !_pages.IsTitle || _pages.Current != PausePage.Main || _leaving != Leaving.None ||
                _credits.IsOpen)
            {
                return;
            }

            if (_settingsPanel.Target)
            {
                CloseSettings();
            }

            _credits.Open();
            _main.Snap(false);
            _titleScreen.Snap(false);
            _titleKeyHint.Snap(false);
            _navigator.RequestFocus(_layout.CreditsBack);
            Cue(UiCueKind.Confirm);
        }

        private void OnCreditsBackClicked()
        {
            if (_credits.IsOpen)
            {
                CloseCredits();
            }
        }

        private void CloseCredits()
        {
            _credits.Close();
            _navigator.Blur();
            _main.Show();
            _titleScreen.Show();
            _titleKeyHint.Show();
            _navigator.RequestFocus(_layout.CreditsButton);
            Cue(UiCueKind.Back);
        }

        /// <summary>Quit to title: saved, then the dark, then App reloads into the title.</summary>
        private void OnQuitToTitleClicked()
        {
            if (IsOpen && !_pages.IsTitle && _pages.Current == PausePage.Main && _leaving == Leaving.None)
            {
                SaveIfChanged();
                Cue(UiCueKind.Confirm);
                Draw(Leaving.ToTitle);
            }
        }

        private void OnStayClicked()
        {
            Answer(PausePage.Quit);
        }

        /// <summary>
        /// The title's New Game: over a save it asks once; with none, the world behind the title is already fresh, so
        /// play begins right here.
        /// </summary>
        private void OnNewGameClicked()
        {
            if (!_pages.IsTitle || _pages.HasSave)
            {
                Ask(PausePage.NewGame);
            }
            else if (IsOpen && _pages.Current == PausePage.Main && _leaving == Leaving.None)
            {
                CloseTitle();
                Cue(UiCueKind.Confirm);
                _events.Publish(new NewGameRequested());
            }
        }

        private void OnKeepGoingClicked()
        {
            Answer(PausePage.NewGame);
        }

        /// <summary>
        /// Start over: on the title over a save, the title eases away and the App is asked for a new game (the camera
        /// pushes into 07's closed lens, and App puts the save away and reloads inside O1's black); otherwise once the
        /// dark is drawn, the App is asked for a new game. Once only, however often the button is pressed.
        /// </summary>
        private void OnNewGameConfirmed()
        {
            if (IsOpen && _leaving == Leaving.None && _pages.ConfirmNewGame())
            {
                Cue(UiCueKind.Confirm);
                if (_pages.IsTitle)
                {
                    _leaving = Leaving.Gone;
                    CloseTitle();
                    RestoreTime();
                    _events.Publish(new NewGameRequested());
                }
                else
                {
                    Draw(Leaving.NewGame);
                }
            }
        }

        private void OnQuitConfirmed()
        {
            if (IsOpen && _pages.Current == PausePage.Quit)
            {
                SaveIfChanged();
                Cue(UiCueKind.Confirm);
                _quit();
            }
        }

        private void OnSettingsBackClicked()
        {
            if (IsOpen && _settingsPanel.Target)
            {
                CloseSettings();
                Cue(UiCueKind.Back);
            }
        }

        private void OnLanguageClicked()
        {
            if (IsOpen && _settingsPanel.Target)
            {
                _player.NextLanguage();
                Cue(UiCueKind.Confirm);
            }
        }

        private void OnTextSizeClicked()
        {
            if (IsOpen && _settingsPanel.Target)
            {
                _player.NextTextSize();
                _layout.TextSizeButton.text = _player.TextSizeLabel;
                Cue(UiCueKind.Confirm);
            }
        }

        /// <summary>Snaps every page to the current one (the menu opening).</summary>
        private void SnapPages()
        {
            _mainPage.Snap(_pages.Current == PausePage.Main);
            _quitPage.Snap(false);
            _newGamePage.Snap(false);
            _languagePage.Snap(_pages.Current == PausePage.Language);
        }

        /// <summary>The buttons the menu offers as the pause or as the title, and their words.</summary>
        private void WriteMode()
        {
            bool title = _pages.IsTitle;
            _layout.Pause.EnableInClassList(TitleClass, title);
            ApplyTitleLayout();
            _layout.ResumeButton.text = _localization.Get(title ? UiKeys.TitleContinue : UiKeys.PauseResume);
            _layout.SettingsButton.text = _localization.Get(title ? UiKeys.TitleSettings : UiKeys.PauseSettings);
            _layout.QuitButton.text = _localization.Get(title ? UiKeys.TitleQuit : UiKeys.PauseQuit);
            _continueNote.text = title && _pages.HasSave ? _localization.Get(UiKeys.TitleContinueNote) : string.Empty;
            Display(_layout.ResumeButton, !title || _pages.HasSave);
            Display(_layout.NewGameButton, title);
            Display(_layout.QuitToTitleButton, !title && _flow.ShowsTitle);
            Display(_layout.CreditsButton, title);
        }

        private void ApplyTitleLayout()
        {
            _layout.TitleScreen.style.left = StyleKeyword.Null;
            _layout.TitleScreen.style.top = StyleKeyword.Null;
            _layout.TitleScreen.style.width = StyleKeyword.Null;
            _layout.TitleSubtitleRow.style.marginTop = _title.SubtitleTopMargin;
            _layout.TitleRule.style.marginTop = _title.RuleTopMargin;
            _layout.TitleRule.style.width = StyleKeyword.Null;
            _layout.TitleRule.style.height = _title.RuleHeight;
            _layout.TitleRule.style.borderLeftWidth = _title.RuleAccentWidth;
            _layout.PauseFrame.style.paddingLeft = StyleKeyword.Null;
            _layout.PauseFrame.style.paddingTop = StyleKeyword.Null;
        }

        private static void Display(VisualElement element, bool shown)
        {
            element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Continue, or a New Game on the title: the title eases away and play begins under it.</summary>
        private void CloseTitle()
        {
            IsOpen = false;
            _navigator.ClearFocus();
            SaveIfChanged();
            _main.Hide();
            _titleScreen.Hide();
            _titleCrt.Hide();
            _titleCrtOverlay.Hide();
            _titleKeyHint.Hide();
            _settingsPanel.Hide();
            _cursor.Drive();
        }

        private void StepTitleAnimation(float timeSeconds, bool standaloneFraming)
        {
            _titleScenePainter.SetTime(timeSeconds, standaloneFraming);
            _titleScenePainter.SyncLeftColumnLayout();
            _titleCrtOverlayPainter.SetTime(timeSeconds, standaloneFraming);
            _navigator.SetCursorVisibility(_titleScenePainter.State.CursorVisible);
        }

        private static Label CreateContinueNote(Button resumeButton)
        {
            var note = new Label
            {
                pickingMode = PickingMode.Ignore,
            };
            note.AddToClassList("menu-button__note");
            resumeButton.Add(note);
            return note;
        }

        /// <summary>Draws the dark; once it is drawn, <see cref="Go"/> does what was chosen.</summary>
        private void Draw(Leaving leaving)
        {
            _leaving = leaving;
            _navigator.ClearFocus();
            _curtain.Show();
        }

        /// <summary>
        /// The dark is drawn. Game time comes back first (Time.timeScale is global and outlives a reload), then the
        /// App is asked to reload, or the game quits.
        /// </summary>
        private void Go()
        {
            Leaving leaving = _leaving;
            _leaving = Leaving.Gone;
            switch (leaving)
            {
                case Leaving.NewGame:
                    RestoreTime();
                    _events.Publish(new NewGameRequested());
                    break;
                case Leaving.ToTitle:
                    RestoreTime();
                    _events.Publish(new QuitToTitleRequested());
                    break;
                case Leaving.Quit:
                    _quit();
                    break;
            }
        }

        private Button[] LanguageButtons(VisualElement page, ILocalization localization)
        {
            var buttons = new Button[localization.Languages.Count];
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                var button = new Button(() => ChooseLanguage(index))
                {
                    text = localization.GetLanguageName(localization.Languages[i]),
                };
                button.AddToClassList("menu-button");
                page.Add(button);
                buttons[i] = button;
            }

            return buttons;
        }

        /// <summary>The first boot's language step: the language is set and the title's buttons come up.</summary>
        private void ChooseLanguage(int index)
        {
            if (!IsOpen || !_pages.LanguageChosen())
            {
                return;
            }

            _player.ChooseLanguage(_localization.Languages[index]);
            SaveIfChanged();
            _navigator.RequestFocus(_pages.DefaultFocus);
            Cue(UiCueKind.Confirm);
        }

        private void OnLookChanged(ChangeEvent<int> evt)
        {
            _player.SetLookStep(evt.newValue);
            int step = _player.GetLookStep();
            _layout.LookValue.text = _lookTexts[step];
            Cue(UiCueKind.SliderStep);
        }

        private void OnInvertChanged(ChangeEvent<bool> evt)
        {
            _player.InvertY = evt.newValue;
            Cue(UiCueKind.Confirm);
        }

        private void OnRumbleChanged(ChangeEvent<bool> evt)
        {
            _player.Rumble = evt.newValue;
            Cue(UiCueKind.Confirm);
        }

        private void OnRumbleStrengthChanged(ChangeEvent<int> evt)
        {
            _player.SetRumbleStep(evt.newValue);
            Cue(UiCueKind.SliderStep);
        }

        /// <summary>The Gentle camera also moves the look slider's floor, so the slider follows it.</summary>
        private void OnGentleCameraChanged(ChangeEvent<bool> evt)
        {
            _player.GentleCamera = evt.newValue;
            ShowLookStep();
            Cue(UiCueKind.Confirm);
        }

        private void ShowLookStep()
        {
            int look = _player.GetLookStep();
            _layout.LookSlider.lowValue = _player.MinLookStep;
            _layout.LookSlider.SetValueWithoutNotify(look);
            _layout.LookValue.text = _lookTexts[look];
        }

        private void OnGuidanceChanged(ChangeEvent<bool> evt)
        {
            _player.Guidance = evt.newValue;
            Cue(UiCueKind.Confirm);
        }

        /// <summary>What the dark is drawn for.</summary>
        private enum Leaving
        {
            None,
            NewGame,
            ToTitle,
            Quit,

            /// <summary>Done: the scene is going (or the game is quitting).</summary>
            Gone,
        }
    }
}
