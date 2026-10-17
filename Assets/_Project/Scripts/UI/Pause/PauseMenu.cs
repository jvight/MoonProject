using System;
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
    /// published as a <see cref="UiCue"/> (open, close, focus move, confirm, back, slider step) for Audio. Beside the
    /// heading, a small summary shows 07's metal, wiring and optics (<see cref="IMaterialStock"/>). Once 07 owns
    /// a cassette, a quiet line under the heading counts them against every tape in the game (both numbers from
    /// <see cref="IRadioProgram"/>, so a loaded save is counted right); once a relay mast is lit, another counts the
    /// lit masts against them all (<see cref="IRelayStatus"/>). Neither shows before the world has shown the thing.
    /// The same menu is the title's over the live world (M3-09, <see cref="OpenTitle"/>): Continue in place of Resume,
    /// New Game (asking once when a save exists), the same settings and Quit, under the wordmark; game time keeps
    /// running, and the menu rests out of sight when left untouched. A first boot opens on a language step. The pause
    /// offers "Quit to title" instead of New Game. New Game, Quit to title and Quit draw a soft dark first, so the
    /// scene never cuts.
    /// </summary>
    internal sealed class PauseMenu
    {
        private const float MoveEpsilon = 0.1f;
        private const float RepeatInitialDelay = 0.35f;
        private const float RepeatInterval = 0.1f;
        private const float AxisDeadzone = 0.5f;
        private const string TitleClass = "pause--title";
        private const string SettingsOpenClass = "pause--settings-open";
        private const string TabOnClass = "settings-tab--on";
        private const string TabSkinOnClass = "settings-tab-wrap--on";
        private const string TabFocusClass = "settings-tab-wrap--focus";
        private const string IdleRowClass = "setting-row--idle";
        private const string FocusedRowClass = "setting-row--focused";

        private readonly UiLayout _layout;
        private readonly PauseSettings _settings;
        private readonly TitleSettings _title;
        private readonly PlayerSettings _player;
        private readonly ILocalization _localization;
        private readonly InputReader _input;
        private readonly EventBus _events;
        private readonly SettingsStore _settingsStore;
        private readonly IRadioProgram _radio;
        private readonly IRelayStatus _relays;
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
        private readonly Label[] _titleCursors;
        private readonly VisualElement[] _cursorHosts;
        private readonly Button[] _languageButtons;
        private readonly Button[] _mainPageButtons;
        private readonly Button[] _newGamePageButtons;
        private readonly Button[] _quitPageButtons;
        private readonly VisualElement[][] _settingsTabControls;
        private readonly VisualElement[] _settingRowControls;
        private readonly VisualElement[] _settingRows;
        private readonly CreditsView _credits;
        private readonly VisualElement[] _settingsPages;
        private readonly Button[] _settingsTabs;
        private int _settingsTab;
        private readonly IntText _fpsNumbers;
        private readonly PausePages _pages = new PausePages();
        private readonly string[] _lookTexts;
        private Focusable _pendingFocus;
        private Focusable _lastValidFocus;
        private VisualElement _lastSyncedFocus;
        private bool _focusingByCode;
        private bool _pointerPressed;
        private Vector2 _lastPointerPos = new Vector2(float.NaN, float.NaN);
        private int _lastNavigateFrame = -1;
        private Vector2Int _heldDir;
        private float _repeatTimer;
        private float _resumeScale = 1f;
        private float _writtenShift = float.NaN;
        private float _writtenTop = float.NaN;
        private Leaving _leaving;
        private float _untouched;
        private bool _resting;
        private float _titleTime;
        private bool _lockedTitleTime;
        private bool _lastCursorVisible = true;

        public PauseMenu(UiLayout layout, PauseSettings settings, TitleSettings title, PlayerSettings player,
            ILocalization localization, InputReader input, EventBus events, SettingsStore settingsStore,
            IRadioProgram radio, IRelayStatus relays, IGameFlow flow, IntText numbers, CursorPolicy cursor, Action quit,
            CreditsView credits)
        {
            _credits = credits ?? throw new ArgumentNullException(nameof(credits));
            _settingsPages = new[] { layout.PageSound, layout.PageCamera, layout.PageDisplay };
            _settingsTabs = new[] { layout.TabSound, layout.TabCamera, layout.TabDisplay };
            foreach (Button tab in _settingsTabs)
            {
                VisualElement skin = tab.parent;
                new TabPainter(skin);
                tab.RegisterCallback<FocusInEvent>(_ => skin.AddToClassList(TabFocusClass));
                tab.RegisterCallback<FocusOutEvent>(_ => skin.RemoveFromClassList(TabFocusClass));
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
            _radio = radio ?? throw new ArgumentNullException(nameof(radio));
            _relays = relays ?? throw new ArgumentNullException(nameof(relays));
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
            _titleScenePainter = new TitleScenePainter(layout.TitleCrt, title);
            _titleCrtOverlayPainter = new TitleCrtOverlayPainter(layout.TitleCrtOverlay, title);
            layout.TitleSubtitlePrefix.text = "// ";
            _languageButtons = LanguageButtons(layout.PageLanguage, localization);
            _mainPageButtons = new[]
            {
                layout.ResumeButton,
                layout.NewGameButton,
                layout.SettingsButton,
                layout.CreditsButton,
                layout.QuitToTitleButton,
                layout.QuitButton,
            };
            _newGamePageButtons = new[]
            {
                layout.NewGameKeepButton,
                layout.NewGameConfirmButton,
            };
            _quitPageButtons = new[]
            {
                layout.QuitStayButton,
                layout.QuitConfirmButton,
            };
            _settingsTabControls = new[]
            {
                new VisualElement[]
                {
                    layout.MasterSlider,
                    layout.MusicSlider,
                    layout.SfxSlider,
                    layout.AmbienceSlider,
                },
                new VisualElement[]
                {
                    layout.LookSlider,
                    layout.InvertToggle,
                    layout.GentleCameraToggle,
                    layout.RumbleToggle,
                    layout.RumbleSlider,
                    layout.GuidanceToggle,
                },
                new VisualElement[]
                {
                    layout.ResolutionPrev,
                    layout.ModePrev,
                    layout.VSyncToggle,
                    layout.FpsPrev,
                    layout.TextSizeButton,
                    layout.LanguageButton,
                },
            };
            _settingRowControls = new VisualElement[]
            {
                layout.MasterSlider,
                layout.MusicSlider,
                layout.SfxSlider,
                layout.AmbienceSlider,
                layout.LookSlider,
                layout.InvertToggle,
                layout.GentleCameraToggle,
                layout.RumbleToggle,
                layout.RumbleSlider,
                layout.GuidanceToggle,
                layout.ResolutionPrev,
                layout.ModePrev,
                layout.VSyncToggle,
                layout.FpsPrev,
                layout.TextSizeButton,
                layout.LanguageButton,
            };
            _settingRows = new VisualElement[_settingRowControls.Length];
            for (int i = 0; i < _settingRowControls.Length; i++)
            {
                _settingRows[i] = FindSettingRow(_settingRowControls[i]);
            }

            _continueNote = CreateContinueNote(layout.ResumeButton);
            _titleCursors = CreateTitleCursors(layout, _languageButtons, _settingRows, out _cursorHosts);
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
            new CassetteIconPainter(layout.PauseCassetteIcon);
            new RelayIconPainter(layout.PauseRelayIcon);

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

        /// <summary>True while the title's menu rests out of sight, left untouched.</summary>
        public bool IsResting => _resting;

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
            WriteCassettes();
            WriteRelays();
            _titleCrt.Snap(false);
            _titleCrtOverlay.Snap(false);
            _titleKeyHint.Snap(false);
            _veil.Show();
            _main.Show();
            _lastPointerPos = _input.Menu.PointerPosition;
            _heldDir = Vector2Int.zero;
            _repeatTimer = 0f;
            _pendingFocus = _layout.ResumeButton;
            _lastValidFocus = _layout.ResumeButton;
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
            _pendingFocus = null;
            _lastValidFocus = null;
            Blur();
            SyncSettingRowFocus(null);
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
            else if (_resting)
            {
                Rest(false);
            }
            else if (_pages.Current == PausePage.Language)
            {
                ChooseLanguage(0);
            }
            else if (_pages.Back(out PauseFocus focus))
            {
                _pendingFocus = Button(focus);
                _lastValidFocus = _pendingFocus;
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
            _untouched = 0f;
            _resting = false;
            _lastPointerPos = _input.Menu.PointerPosition;
            _heldDir = Vector2Int.zero;
            _repeatTimer = 0f;
            _pendingFocus = DefaultFocus();
            _lastValidFocus = _pendingFocus;
        }

        /// <summary>Evaluates the title screen's 2D CRT animation at an exact timestamp (for deterministic captures).</summary>
        public void SetTitleAnimationTime(float timeSeconds, bool standaloneFraming = false)
        {
            _lockedTitleTime = true;
            _titleTime = timeSeconds;
            _untouched = 0f;
            if (_resting)
            {
                Rest(false);
                if (_settingsPanel.Target)
                {
                    _settingsPanel.Snap(true);
                }
                else
                {
                    _main.Snap(true);
                }

                _titleScreen.Snap(true);
                _titleKeyHint.Snap(true);
            }

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
            TickTitle(unscaledDeltaTime);

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
            if (_leaving != Leaving.None && _curtain.IsShown)
            {
                Go();
            }

            if (IsOpen && !_resting && _leaving == Leaving.None)
            {
                TickPointerHover();
                TickNavigation(unscaledDeltaTime);

                if (_pendingFocus == null)
                {
                    Focusable current = FocusedElement();
                    if (!(current is VisualElement currentEl) || !IsValidFocusForCurrentState(currentEl))
                    {
                        _pendingFocus = (_lastValidFocus is VisualElement lastEl && IsValidFocusForCurrentState(lastEl))
                            ? _lastValidFocus
                            : DefaultFocus();
                    }
                }

                if (_pendingFocus is VisualElement target && IsDisplayed(target) && target.canGrabFocus)
                {
                    FocusControl(target, byCode: true);
                    _pendingFocus = null;
                }

                SyncSettingRowFocus(FocusedElement() as VisualElement);
            }

            _pointerPressed = false;
        }

        /// <summary>Re-reads the words set from code (the language selector, the cassette and relay lines).</summary>
        public void Relocalize()
        {
            _layout.LanguageButton.text = _localization.GetLanguageName(_localization.Language);
            WriteDisplay();
            WriteMode();
            WriteCassettes();
            WriteRelays();
            _credits.Relocalize();
        }

        /// <summary>Restores normal game speed immediately (the UI is going away mid-pause).</summary>
        public void RestoreTime()
        {
            Time.timeScale = _resumeScale;
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
            _layout.Pause.RegisterCallback<FocusInEvent>(OnFocusIn);
            _layout.Pause.RegisterCallback<FocusOutEvent>(OnFocusOut);
            _layout.Pause.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _layout.Pause.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);

            BindHoverFocus(_layout.ResumeButton);
            BindHoverFocus(_layout.NewGameButton);
            BindHoverFocus(_layout.SettingsButton);
            BindHoverFocus(_layout.CreditsButton);
            BindHoverFocus(_layout.QuitToTitleButton);
            BindHoverFocus(_layout.QuitButton);
            BindHoverFocus(_layout.NewGameKeepButton);
            BindHoverFocus(_layout.NewGameConfirmButton);
            BindHoverFocus(_layout.QuitStayButton);
            BindHoverFocus(_layout.QuitConfirmButton);
            BindHoverFocus(_layout.SettingsBackButton);
            BindHoverFocus(_layout.CreditsBack);
            for (int i = 0; i < _languageButtons.Length; i++)
            {
                BindHoverFocus(_languageButtons[i]);
            }

            for (int i = 0; i < _settingsTabs.Length; i++)
            {
                BindHoverFocus(_settingsTabs[i].parent, _settingsTabs[i]);
            }

            for (int i = 0; i < _settingRowControls.Length; i++)
            {
                VisualElement control = _settingRowControls[i];
                VisualElement row = _settingRows[i] ?? control;
                BindHoverFocus(row, control);
            }

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

        private void BindHoverFocus(VisualElement hitArea, VisualElement focusTarget = null)
        {
            VisualElement target = focusTarget ?? hitArea;
            hitArea.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (IsOpen && !_resting && _leaving == Leaving.None && _pendingFocus == null &&
                    IsValidFocusForCurrentState(target))
                {
                    FocusControl(target, byCode: true);
                }
            });
            hitArea.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (IsOpen && !_resting && _leaving == Leaving.None && _pendingFocus == null &&
                    evt.deltaPosition.sqrMagnitude > 0f && IsValidFocusForCurrentState(target))
                {
                    FocusControl(target, byCode: true);
                }
            });
        }

        private void FocusControl(VisualElement target, bool byCode)
        {
            if (target == null || !IsDisplayed(target) || !target.canGrabFocus)
            {
                return;
            }

            _lastValidFocus = target;
            if (FocusedElement() == target)
            {
                SyncSettingRowFocus(target);
                return;
            }

            bool prev = _focusingByCode;
            _focusingByCode = byCode;
            target.Focus();
            _focusingByCode = prev;
            SyncSettingRowFocus(target);
        }

        private void SyncSettingRowFocus(VisualElement focused)
        {
            if (focused == _lastSyncedFocus)
            {
                return;
            }

            _lastSyncedFocus = focused;
            VisualElement focusedRow = focused != null ? FindSettingRow(focused) : null;
            for (int i = 0; i < _settingRows.Length; i++)
            {
                VisualElement row = _settingRows[i];
                if (row != null)
                {
                    row.EnableInClassList(FocusedRowClass, row == focusedRow);
                }
            }

            for (int i = 0; i < _settingsTabs.Length; i++)
            {
                Button tab = _settingsTabs[i];
                tab.parent.EnableInClassList(TabFocusClass, focused == tab);
            }

            VisualElement activeCursorHost = focusedRow ??
                (focused != null && (focused == _layout.TabSound || focused == _layout.TabCamera || focused == _layout.TabDisplay)
                    ? focused.parent
                    : focused);
            for (int i = 0; i < _titleCursors.Length; i++)
            {
                Label cursor = _titleCursors[i];
                if (cursor != null)
                {
                    cursor.style.display = (_cursorHosts[i] != null && _cursorHosts[i] == activeCursorHost)
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                }
            }
        }

        private static VisualElement FindSettingRow(VisualElement control)
        {
            for (VisualElement current = control; current != null; current = current.parent)
            {
                if (current.ClassListContains("setting-row"))
                {
                    return current;
                }
            }

            return null;
        }

        private bool IsValidFocusForCurrentState(VisualElement element)
        {
            if (element == null || !IsDisplayed(element) || !element.canGrabFocus)
            {
                return false;
            }

            if (_credits.IsOpen)
            {
                return element == _layout.CreditsBack;
            }

            if (_settingsPanel.Target)
            {
                return _layout.PauseSettings.Contains(element);
            }

            return _layout.PauseMain.Contains(element);
        }

        private void TickPointerHover()
        {
            Vector2 pointerPos = _input.Menu.PointerPosition;
            if (float.IsNaN(_lastPointerPos.x))
            {
                _lastPointerPos = pointerPos;
                return;
            }

            if ((pointerPos - _lastPointerPos).sqrMagnitude <= 0.25f)
            {
                return;
            }

            _lastPointerPos = pointerPos;
            IPanel panel = _layout.Root.panel;
            if (panel == null)
            {
                return;
            }

            Vector2 screenTopLeft = new Vector2(pointerPos.x, Screen.height - pointerPos.y);
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenTopLeft);
            VisualElement hovered = HitTestInteractive(panelPos);
            if (hovered != null)
            {
                FocusControl(hovered, byCode: true);
            }
        }

        private VisualElement HitTestInteractive(Vector2 panelPos)
        {
            if (_credits.IsOpen)
            {
                return ContainsPoint(_layout.CreditsBack, panelPos) ? _layout.CreditsBack : null;
            }

            if (_settingsPanel.Target)
            {
                for (int i = 0; i < _settingsTabs.Length; i++)
                {
                    if (ContainsPoint(_settingsTabs[i].parent, panelPos))
                    {
                        return _settingsTabs[i];
                    }
                }

                VisualElement[] tabControls = _settingsTabControls[_settingsTab];
                for (int i = 0; i < tabControls.Length; i++)
                {
                    VisualElement control = tabControls[i];
                    VisualElement row = FindSettingRow(control) ?? control;
                    if (ContainsPoint(row, panelPos))
                    {
                        return control;
                    }
                }

                if (ContainsPoint(_layout.SettingsBackButton, panelPos))
                {
                    return _layout.SettingsBackButton;
                }

                return null;
            }

            Button[] buttons = CurrentPageButtons();
            for (int i = 0; i < buttons.Length; i++)
            {
                if (ContainsPoint(buttons[i], panelPos))
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private static bool ContainsPoint(VisualElement element, Vector2 panelPos)
        {
            return element != null && IsDisplayed(element) && element.worldBound.Contains(panelPos);
        }

        private Button[] CurrentPageButtons()
        {
            switch (_pages.Current)
            {
                case PausePage.NewGame:
                    return _newGamePageButtons;
                case PausePage.Quit:
                    return _quitPageButtons;
                case PausePage.Language:
                    return _languageButtons;
                default:
                    return _mainPageButtons;
            }
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            if (!IsOpen || _resting || _leaving != Leaving.None)
            {
                return;
            }

            evt.StopPropagation();
            _layout.Root.panel?.focusController?.IgnoreEvent(evt);
            if (evt.direction == NavigationMoveEvent.Direction.None || _lastNavigateFrame == Time.frameCount)
            {
                return;
            }

            _lastNavigateFrame = Time.frameCount;
            HandleNavigation(evt.direction);
        }

        private void TickNavigation(float unscaledDeltaTime)
        {
            Vector2 nav = _input.Menu.NavigateVector;
            Vector2Int dir = Vector2Int.zero;
            if (Mathf.Abs(nav.x) >= AxisDeadzone || Mathf.Abs(nav.y) >= AxisDeadzone)
            {
                if (Mathf.Abs(nav.y) >= Mathf.Abs(nav.x))
                {
                    dir = new Vector2Int(0, nav.y > 0f ? 1 : -1);
                }
                else
                {
                    dir = new Vector2Int(nav.x > 0f ? 1 : -1, 0);
                }
            }

            if (dir == Vector2Int.zero)
            {
                _heldDir = Vector2Int.zero;
                _repeatTimer = 0f;
                return;
            }

            bool trigger = false;
            if (dir != _heldDir)
            {
                _heldDir = dir;
                _repeatTimer = RepeatInitialDelay;
                trigger = true;
            }
            else
            {
                _repeatTimer -= unscaledDeltaTime;
                if (_repeatTimer <= 0f)
                {
                    _repeatTimer += RepeatInterval;
                    trigger = true;
                }
            }

            if (!trigger || _lastNavigateFrame == Time.frameCount)
            {
                return;
            }

            _lastNavigateFrame = Time.frameCount;
            NavigationMoveEvent.Direction moveDir;
            if (dir.y > 0)
            {
                moveDir = NavigationMoveEvent.Direction.Up;
            }
            else if (dir.y < 0)
            {
                moveDir = NavigationMoveEvent.Direction.Down;
            }
            else if (dir.x < 0)
            {
                moveDir = NavigationMoveEvent.Direction.Left;
            }
            else
            {
                moveDir = NavigationMoveEvent.Direction.Right;
            }

            HandleNavigation(moveDir);
        }

        private void HandleNavigation(NavigationMoveEvent.Direction direction)
        {
            VisualElement focused = FocusedElement() as VisualElement;
            if (!IsValidFocusForCurrentState(focused))
            {
                VisualElement fallback = (_lastValidFocus is VisualElement lastEl && IsValidFocusForCurrentState(lastEl))
                    ? lastEl
                    : DefaultFocus() as VisualElement;
                FocusControl(fallback, byCode: false);
                return;
            }

            if (_credits.IsOpen)
            {
                if (direction == NavigationMoveEvent.Direction.Up)
                {
                    _credits.ScrollByStep(-1);
                }
                else if (direction == NavigationMoveEvent.Direction.Down)
                {
                    _credits.ScrollByStep(1);
                }

                return;
            }

            if (_settingsPanel.Target)
            {
                HandleSettingsNavigation(focused, direction);
                return;
            }

            if (direction == NavigationMoveEvent.Direction.Up || direction == NavigationMoveEvent.Direction.Down)
            {
                StepButtonList(CurrentPageButtons(), focused, direction == NavigationMoveEvent.Direction.Down ? 1 : -1);
            }
        }

        private void StepButtonList(Button[] buttons, VisualElement focused, int delta)
        {
            int currentIdx = -1;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == focused)
                {
                    currentIdx = i;
                    break;
                }
            }

            if (currentIdx < 0)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (IsDisplayed(buttons[i]) && buttons[i].canGrabFocus)
                    {
                        FocusControl(buttons[i], byCode: false);
                        return;
                    }
                }

                return;
            }

            for (int idx = currentIdx + delta; idx >= 0 && idx < buttons.Length; idx += delta)
            {
                Button candidate = buttons[idx];
                if (IsDisplayed(candidate) && candidate.canGrabFocus)
                {
                    FocusControl(candidate, byCode: false);
                    return;
                }
            }
        }

        private void HandleSettingsNavigation(VisualElement focused, NavigationMoveEvent.Direction direction)
        {
            int tabIdx = -1;
            for (int i = 0; i < _settingsTabs.Length; i++)
            {
                if (_settingsTabs[i] == focused)
                {
                    tabIdx = i;
                    break;
                }
            }

            VisualElement[] rows = _settingsTabControls[_settingsTab];
            int rowIdx = -1;
            VisualElement focusedRow = FindSettingRow(focused);
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == focused || (focusedRow != null && FindSettingRow(rows[i]) == focusedRow))
                {
                    rowIdx = i;
                    break;
                }
            }

            bool isBack = focused == _layout.SettingsBackButton;

            if (direction == NavigationMoveEvent.Direction.Up || direction == NavigationMoveEvent.Direction.Down)
            {
                int delta = direction == NavigationMoveEvent.Direction.Down ? 1 : -1;
                if (tabIdx >= 0)
                {
                    if (delta > 0 && rows.Length > 0)
                    {
                        FocusControl(rows[0], byCode: false);
                    }
                }
                else if (rowIdx >= 0)
                {
                    int nextRow = rowIdx + delta;
                    if (nextRow < 0)
                    {
                        FocusControl(_settingsTabs[_settingsTab], byCode: false);
                    }
                    else if (nextRow < rows.Length)
                    {
                        FocusControl(rows[nextRow], byCode: false);
                    }
                    else
                    {
                        FocusControl(_layout.SettingsBackButton, byCode: false);
                    }
                }
                else if (isBack)
                {
                    if (delta < 0 && rows.Length > 0)
                    {
                        FocusControl(rows[rows.Length - 1], byCode: false);
                    }
                }
                else if (rows.Length > 0)
                {
                    FocusControl(rows[0], byCode: false);
                }

                return;
            }

            if (direction == NavigationMoveEvent.Direction.Left || direction == NavigationMoveEvent.Direction.Right)
            {
                int delta = direction == NavigationMoveEvent.Direction.Right ? 1 : -1;
                if (tabIdx >= 0)
                {
                    int nextTab = Mathf.Clamp(tabIdx + delta, 0, _settingsTabs.Length - 1);
                    if (nextTab != tabIdx)
                    {
                        SelectTab(nextTab);
                        FocusControl(_settingsTabs[nextTab], byCode: false);
                        Cue(UiCueKind.Confirm);
                    }

                    return;
                }

                if (rowIdx >= 0)
                {
                    StepSettingsControl(rows[rowIdx], delta);
                }
            }
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
            FocusControl(_settingsTabs[tab], byCode: true);
            Cue(UiCueKind.Confirm);
        }

        /// <summary>Allocates (it formats): only when the menu opens or the language changes.</summary>
        private void WriteCassettes()
        {
            int owned = _radio.OwnedTapeCount;
            _layout.PauseCassettes.style.display = owned > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (owned > 0)
            {
                _layout.PauseCassettesCount.text = string.Format(_localization.Get(UiKeys.PauseCassettes), owned,
                    _radio.TotalTapeCount);
            }
        }

        /// <summary>Allocates (it formats): only when the menu opens or the language changes.</summary>
        private void WriteRelays()
        {
            int lit = _relays.LitMasts;
            _layout.PauseRelays.style.display = lit > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (lit > 0)
            {
                _layout.PauseRelaysCount.text = string.Format(_localization.Get(UiKeys.PauseRelays), lit,
                    _relays.MastCount);
            }
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

            _pendingFocus = _layout.MasterSlider;
            _lastValidFocus = _layout.MasterSlider;
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

        /// <summary>The page now shown (tests and captures).</summary>
        public int SettingsTab => _settingsTab;

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
                if (!_resting && !_credits.IsOpen)
                {
                    _main.Show();
                    _titleScreen.Show();
                    _titleKeyHint.Show();
                }
            }

            _pendingFocus = _layout.SettingsButton;
            _lastValidFocus = _layout.SettingsButton;
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

        private Focusable Button(PauseFocus focus)
        {
            switch (focus)
            {
                case PauseFocus.NewGame:
                    return _layout.NewGameButton;
                case PauseFocus.Quit:
                    return _layout.QuitButton;
                case PauseFocus.QuitStay:
                    return _layout.QuitStayButton;
                case PauseFocus.NewGameKeep:
                    return _layout.NewGameKeepButton;
                case PauseFocus.Language:
                    return _languageButtons[0];
                default:
                    return _layout.ResumeButton;
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

            _pendingFocus = Button(_pages.DefaultFocus);
            _lastValidFocus = _pendingFocus;
            Cue(UiCueKind.Confirm);
        }

        /// <summary>Stay / Keep going: back to the button that asked.</summary>
        private void Answer(PausePage question)
        {
            if (IsOpen && _pages.Current == question && _pages.Back(out PauseFocus focus))
            {
                _pendingFocus = Button(focus);
                _lastValidFocus = _pendingFocus;
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

        /// <summary>
        /// True when neither the element nor any ancestor is display: none (a fading page is not yet shown).
        /// </summary>
        private static bool IsDisplayed(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current.resolvedStyle.display == DisplayStyle.None)
                {
                    return false;
                }
            }

            return true;
        }

        private Focusable DefaultFocus()
        {
            if (_credits.IsOpen)
            {
                return _layout.CreditsBack;
            }

            if (_settingsPanel.Target)
            {
                return _layout.MasterSlider;
            }

            return Button(_pages.DefaultFocus);
        }

        private Focusable FocusedElement()
        {
            return _layout.Root.panel?.focusController?.focusedElement;
        }

        private void Blur()
        {
            FocusedElement()?.Blur();
        }

        private void Cue(UiCueKind kind)
        {
            _events.Publish(new UiCue(kind));
        }

        /// <summary>
        /// Focus moved by the player's keyboard or gamepad (not the menu placing it, not a mouse click): a soft tick.
        /// </summary>
        private void OnFocusIn(FocusInEvent evt)
        {
            if (evt.target is VisualElement el && IsValidFocusForCurrentState(el))
            {
                _lastValidFocus = el;
                SyncSettingRowFocus(el);
            }

            if (IsOpen && !_focusingByCode && !_pointerPressed)
            {
                Cue(UiCueKind.FocusMove);
            }
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            if (FocusedElement() == null)
            {
                SyncSettingRowFocus(null);
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            _pointerPressed = true;
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
            _pendingFocus = _layout.CreditsBack;
            _lastValidFocus = _layout.CreditsBack;
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
            Blur();
            _main.Show();
            _titleScreen.Show();
            _titleKeyHint.Show();
            _pendingFocus = _layout.CreditsButton;
            _lastValidFocus = _layout.CreditsButton;
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
        /// Start over: once the dark is drawn, the App is asked for a new game (it puts the save away and reloads).
        /// Once only, however often the button is pressed before the scene goes.
        /// </summary>
        private void OnNewGameConfirmed()
        {
            if (IsOpen && _leaving == Leaving.None && _pages.ConfirmNewGame())
            {
                Cue(UiCueKind.Confirm);
                Draw(Leaving.NewGame);
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

        /// <summary>Continue, or a New Game with no save: the title eases away and play begins under it.</summary>
        private void CloseTitle()
        {
            IsOpen = false;
            _pendingFocus = null;
            _lastValidFocus = null;
            _resting = false;
            Blur();
            SyncSettingRowFocus(null);
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
            _titleCrtOverlayPainter.SetTime(timeSeconds, standaloneFraming);
            bool cursorVisible = _titleScenePainter.State.CursorVisible;
            if (cursorVisible != _lastCursorVisible)
            {
                _lastCursorVisible = cursorVisible;
                Visibility visibility = cursorVisible ? Visibility.Visible : Visibility.Hidden;
                for (int i = 0; i < _titleCursors.Length; i++)
                {
                    _titleCursors[i].style.visibility = visibility;
                }
            }
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

        private static Label[] CreateTitleCursors(UiLayout layout, Button[] languageButtons, VisualElement[] settingRows,
            out VisualElement[] hosts)
        {
            VisualElement[] targets =
            {
                layout.ResumeButton,
                layout.NewGameButton,
                layout.SettingsButton,
                layout.CreditsButton,
                layout.QuitToTitleButton,
                layout.QuitButton,
                layout.NewGameKeepButton,
                layout.NewGameConfirmButton,
                layout.QuitStayButton,
                layout.QuitConfirmButton,
                layout.SettingsBackButton,
                layout.CreditsBack,
                layout.TabSound.parent,
                layout.TabCamera.parent,
                layout.TabDisplay.parent,
            };

            var cursors = new Label[targets.Length + languageButtons.Length + settingRows.Length];
            hosts = new VisualElement[cursors.Length];
            int idx = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                hosts[idx] = targets[i];
                cursors[idx++] = AttachCursor(targets[i]);
            }

            for (int i = 0; i < languageButtons.Length; i++)
            {
                hosts[idx] = languageButtons[i];
                cursors[idx++] = AttachCursor(languageButtons[i]);
            }

            for (int i = 0; i < settingRows.Length; i++)
            {
                hosts[idx] = settingRows[i];
                cursors[idx++] = AttachCursor(settingRows[i]);
            }

            return cursors;
        }

        private static Label AttachCursor(VisualElement host)
        {
            var cursor = new Label(">")
            {
                pickingMode = PickingMode.Ignore,
            };
            cursor.style.display = DisplayStyle.None;
            cursor.AddToClassList("menu-button__cursor");
            host?.Add(cursor);
            return cursor;
        }

        /// <summary>
        /// Counts the title's untouched time: after a while the menu and the wordmark rest out of sight (stillness is a
        /// reward); a step, a click or Esc brings them back.
        /// </summary>
        private void TickTitle(float unscaledDeltaTime)
        {
            if (!IsTitle || _leaving != Leaving.None || _lockedTitleTime)
            {
                return;
            }

            if (_credits.IsOpen)
            {
                _untouched = 0f;
                return;
            }

            MenuInput menu = _input.Menu;
            bool touched = menu.NavigatePressed || menu.ClickPressed;
            if (_resting)
            {
                if (touched)
                {
                    Rest(false);
                }

                return;
            }

            _untouched = touched || _settingsPanel.Target ? 0f : _untouched + unscaledDeltaTime;
            if (_untouched >= _title.IdleSeconds)
            {
                Rest(true);
            }
        }

        private void Rest(bool resting)
        {
            _resting = resting;
            _untouched = 0f;
            if (resting)
            {
                Blur();
                _main.Hide();
                _settingsPanel.Hide();
                _titleScreen.Hide();
                _titleKeyHint.Hide();
                return;
            }

            _main.Show();
            _titleScreen.Show();
            _titleKeyHint.Show();
            _pendingFocus = DefaultFocus();
            _lastValidFocus = _pendingFocus;
        }

        /// <summary>Draws the dark; once it is drawn, <see cref="Go"/> does what was chosen.</summary>
        private void Draw(Leaving leaving)
        {
            _leaving = leaving;
            _pendingFocus = null;
            _lastValidFocus = null;
            Blur();
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
            _pendingFocus = Button(_pages.DefaultFocus);
            _lastValidFocus = _pendingFocus;
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
