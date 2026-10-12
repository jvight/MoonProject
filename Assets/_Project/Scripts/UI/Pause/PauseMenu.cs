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
        private const string TitleClass = "pause--title";

        private readonly UiLayout _layout;
        private readonly PauseSettings _settings;
        private readonly TitleSettings _title;
        private readonly PlayerSettings _player;
        private readonly ILocalization _localization;
        private readonly InputReader _input;
        private readonly EventBus _events;
        private readonly SettingsStore _settingsStore;
        private readonly IMaterialStock _materials;
        private readonly IRadioProgram _radio;
        private readonly IRelayStatus _relays;
        private readonly IGameFlow _flow;
        private readonly MaterialSlots _stock;
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
        private readonly Reveal _curtain;
        private readonly Button[] _languageButtons;
        private readonly PausePages _pages = new PausePages();
        private readonly string[] _lookTexts;
        private Focusable _pendingFocus;
        private bool _focusingByCode;
        private bool _pointerPressed;
        private float _resumeScale = 1f;
        private float _writtenShift = float.NaN;
        private Leaving _leaving;
        private float _untouched;
        private bool _resting;

        public PauseMenu(UiLayout layout, PauseSettings settings, TitleSettings title, PlayerSettings player,
            ILocalization localization, InputReader input, EventBus events, SettingsStore settingsStore,
            IMaterialStock materials,
            IRadioProgram radio, IRelayStatus relays, IGameFlow flow, IntText numbers, CursorPolicy cursor, Action quit)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            _materials = materials ?? throw new ArgumentNullException(nameof(materials));
            _radio = radio ?? throw new ArgumentNullException(nameof(radio));
            _relays = relays ?? throw new ArgumentNullException(nameof(relays));
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _stock = new MaterialSlots(layout.PauseMaterials, numbers, false);
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
            _curtain = new Reveal(layout.Curtain, title.Curtain);
            _languageButtons = LanguageButtons(layout.PageLanguage, localization);
            _veil.Snap(false);
            _main.Snap(false);
            _settingsPanel.Snap(false);
            _mainPage.Snap(true);
            _quitPage.Snap(false);
            _newGamePage.Snap(false);
            _languagePage.Snap(false);
            _titleScreen.Snap(false);
            _curtain.Snap(false);
            new ShadowPainter(layout.PauseMainShadow);
            new ShadowPainter(layout.PauseSettingsShadow);
            new CassetteIconPainter(layout.PauseCassetteIcon);
            new RelayIconPainter(layout.PauseRelayIcon);

            _lookTexts = new string[player.LookStepCount + 1];
            for (int step = player.MinLookStep; step <= player.LookStepCount; step++)
            {
                _lookTexts[step] = player.LookValue(step).ToString("0.##") + "×";
            }

            Bind();
            Relocalize();
        }

        public bool IsOpen { get; private set; }

        /// <summary>How much of the menu blur the world takes now (0..1): all of it paused, less on the title.</summary>
        public float Openness => _pages.IsTitle ? _main.Visibility * _title.BackdropWeight : _veil.Visibility;

        /// <summary>True while the menu is the title's (from <see cref="OpenTitle"/> until Continue or New Game).</summary>
        public bool IsTitle => IsOpen && _pages.IsTitle;

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
        public bool IsSettled => _clock.IsSettled && (IsOpen ? _main.IsShown : _main.IsHidden);

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
            WriteMaterials();
            WriteCassettes();
            WriteRelays();
            _veil.Show();
            _main.Show();
            _pendingFocus = _layout.ResumeButton;
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
            Blur();
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

            if (_resting)
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
            _main.Show();
            _titleScreen.Show();
            _curtain.Snap(true);
            _curtain.Hide();
            _untouched = 0f;
            _resting = false;
            _pendingFocus = DefaultFocus();
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
            _curtain.Tick(unscaledDeltaTime);
            ShiftForSettings();
            if (_leaving != Leaving.None && _curtain.IsShown)
            {
                Go();
            }


            if (IsOpen && !_resting)
            {
                if (_pendingFocus == null && _input.Menu.NavigatePressed && FocusedElement() == null)
                {
                    _pendingFocus = DefaultFocus();
                }

                if (_pendingFocus is VisualElement target && IsDisplayed(target) && target.canGrabFocus)
                {
                    _focusingByCode = true;
                    target.Focus();
                    _focusingByCode = false;
                    _pendingFocus = null;
                }
            }

            _pointerPressed = false;
        }

        /// <summary>Re-reads the words set from code (the language selector, the cassette and relay lines).</summary>
        public void Relocalize()
        {
            _layout.LanguageButton.text = _localization.GetLanguageName(_localization.Language);
            WriteMode();
            WriteCassettes();
            WriteRelays();
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
            _layout.QuitButton.clicked += OnQuitClicked;
            _layout.QuitStayButton.clicked += OnStayClicked;
            _layout.QuitConfirmButton.clicked += OnQuitConfirmed;
            _layout.SettingsBackButton.clicked += OnSettingsBackClicked;
            _layout.LanguageButton.clicked += OnLanguageClicked;
            _layout.Pause.RegisterCallback<FocusInEvent>(OnFocusIn);
            _layout.Pause.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);

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
            _layout.GuidanceToggle.RegisterValueChangedCallback(OnGuidanceChanged);
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

        /// <summary>The stock summary in the header: every material's icon and count.</summary>
        private void WriteMaterials()
        {
            for (int i = 0; i < Materials.Count; i++)
            {
                SalvageMaterial material = Materials.At(i);
                _stock.SetCount(material, Materials.Of(_materials, material));
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
            int look = _player.GetLookStep();
            _layout.LookSlider.SetValueWithoutNotify(look);
            _layout.LookValue.text = _lookTexts[look];
            _layout.InvertToggle.SetValueWithoutNotify(_player.InvertY);
            _layout.GuidanceToggle.SetValueWithoutNotify(_player.Guidance);
            _settingsPanel.Show();
            _pendingFocus = _layout.MasterSlider;
        }

        private void CloseSettings()
        {
            SaveIfChanged();
            _settingsPanel.Hide();
            _pendingFocus = _layout.SettingsButton;
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
            Cue(UiCueKind.Confirm);
        }

        /// <summary>Stay / Keep going: back to the button that asked.</summary>
        private void Answer(PausePage question)
        {
            if (IsOpen && _pages.Current == question && _pages.Back(out PauseFocus focus))
            {
                _pendingFocus = Button(focus);
                Cue(UiCueKind.Back);
            }
        }

        private void SaveIfChanged()
        {
            _settingsStore.SaveIfChanged();
        }

        /// <summary>
        /// The pause menu slides left to keep menu and settings centred together. The title's menu is anchored low and
        /// left over the world and its settings open beside it, so it never moves (it would leave the screen).
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
            if (IsOpen && !_focusingByCode && !_pointerPressed)
            {
                Cue(UiCueKind.FocusMove);
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
            _layout.ResumeButton.text = _localization.Get(title ? UiKeys.TitleContinue : UiKeys.PauseResume);
            _layout.SettingsButton.text = _localization.Get(title ? UiKeys.TitleSettings : UiKeys.PauseSettings);
            _layout.QuitButton.text = _localization.Get(title ? UiKeys.TitleQuit : UiKeys.PauseQuit);
            Display(_layout.ResumeButton, !title || _pages.HasSave);
            Display(_layout.NewGameButton, title);
            Display(_layout.QuitToTitleButton, !title && _flow.ShowsTitle);
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
            _resting = false;
            Blur();
            SaveIfChanged();
            _main.Hide();
            _titleScreen.Hide();
            _settingsPanel.Hide();
            _cursor.Drive();
        }

        /// <summary>
        /// Counts the title's untouched time: after a while the menu and the wordmark rest out of sight (stillness is a
        /// reward); a step, a click or Esc brings them back.
        /// </summary>
        private void TickTitle(float unscaledDeltaTime)
        {
            if (!IsTitle || _leaving != Leaving.None)
            {
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
                _titleScreen.Hide();
                return;
            }

            _main.Show();
            _titleScreen.Show();
            _pendingFocus = DefaultFocus();
        }

        /// <summary>Draws the dark; once it is drawn, <see cref="Go"/> does what was chosen.</summary>
        private void Draw(Leaving leaving)
        {
            _leaving = leaving;
            _pendingFocus = null;
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
