using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;
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
    /// </summary>
    internal sealed class PauseMenu
    {
        private const float MoveEpsilon = 0.1f;

        private readonly UiLayout _layout;
        private readonly PauseSettings _settings;
        private readonly PlayerSettings _player;
        private readonly ILocalization _localization;
        private readonly InputReader _input;
        private readonly EventBus _events;
        private readonly ISaveService _save;
        private readonly IMaterialStock _materials;
        private readonly IRadioProgram _radio;
        private readonly IRelayStatus _relays;
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
        private readonly PausePages _pages = new PausePages();
        private readonly string[] _lookTexts;
        private Focusable _pendingFocus;
        private bool _focusingByCode;
        private bool _pointerPressed;
        private float _resumeScale = 1f;
        private float _writtenShift = float.NaN;

        public PauseMenu(UiLayout layout, PauseSettings settings, PlayerSettings player, ILocalization localization,
            InputReader input, EventBus events, ISaveService save, IMaterialStock materials, IRadioProgram radio,
            IRelayStatus relays, IntText numbers, CursorPolicy cursor, Action quit)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _materials = materials ?? throw new ArgumentNullException(nameof(materials));
            _radio = radio ?? throw new ArgumentNullException(nameof(radio));
            _relays = relays ?? throw new ArgumentNullException(nameof(relays));
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
            _veil.Snap(false);
            _main.Snap(false);
            _settingsPanel.Snap(false);
            _mainPage.Snap(true);
            _quitPage.Snap(false);
            _newGamePage.Snap(false);
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
            _mainPage.Snap(true);
            _quitPage.Snap(false);
            _newGamePage.Snap(false);
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
            if (!IsOpen)
            {
                return;
            }

            if (_pages.Back(out PauseFocus focus))
            {
                _pendingFocus = Button(focus);
                Cue(UiCueKind.Back);
            }
            else if (_settingsPanel.Target)
            {
                CloseSettings();
                Cue(UiCueKind.Back);
            }
            else
            {
                Resume();
            }
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
            ShiftForSettings();

            if (IsOpen)
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
                         Leave(_newGamePage, PausePage.NewGame);
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
                default:
                    return _layout.ResumeButton;
            }
        }

        /// <summary>Asks <paramref name="question"/> in place of the main buttons, focused on its safe answer.</summary>
        private void Ask(PausePage question)
        {
            if (!IsOpen || !_pages.Ask(question))
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
            if (_player.IsDirty && _save.SaveNow())
            {
                _player.MarkSaved();
            }
        }

        private void ShiftForSettings()
        {
            IResolvedStyle panel = _layout.PauseSettings.resolvedStyle;
            float width = panel.width + panel.marginLeft;
            float shift = float.IsNaN(width) || _settingsPanel.IsHidden
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
            if (IsOpen && _pages.Current == PausePage.Main)
            {
                Resume();
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
            Ask(PausePage.Quit);
        }

        private void OnStayClicked()
        {
            Answer(PausePage.Quit);
        }

        private void OnNewGameClicked()
        {
            Ask(PausePage.NewGame);
        }

        private void OnKeepGoingClicked()
        {
            Answer(PausePage.NewGame);
        }

        /// <summary>
        /// Start over: game time comes back first (Time.timeScale is global and outlives the reload), then the App is
        /// asked for a new game. Once only, however often the button is pressed before the scene goes.
        /// </summary>
        private void OnNewGameConfirmed()
        {
            if (IsOpen && _pages.ConfirmNewGame())
            {
                Cue(UiCueKind.Confirm);
                RestoreTime();
                _events.Publish(new NewGameRequested());
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
    }
}
