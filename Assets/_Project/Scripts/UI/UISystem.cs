using System;
using System.Collections.Generic;
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
    /// The UI domain's game system (initialised last, after Gameplay). As little UI as possible, as calm as possible: a
    /// title while 07 wakes, context prompts only the first few times, a reticle only while aiming, a scrap chip only
    /// when the balance changes, a story card per relic brought home, crew log found and cassette collected, the tower
    /// upgrade panel on its pad, a few warm pips over a broken friend while 07 is near, its name and its crew log when
    /// it wakes, the radio's ticker line along the bottom, the station's name when Bell's dial is turned, the relay
    /// network's price tag, hop list and soft hop fade, and the pause menu with settings. It registers
    /// <see cref="ILocalization"/> and owns the cursor and the UI's save sections. Everything animates on unscaled time
    /// so the menu stays alive while the game is paused.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UISystem : MonoBehaviour, IGameSystem
    {
        private const int CachedNumbers = 1000;

        [Tooltip("The UIDocument showing GameUI.uxml with the Lofi Lunar PanelSettings.")]
        [SerializeField] private UIDocument _document;

        [Tooltip("UI timings and motion (Assets/_Project/Data/Tuning/UI/UiTuning.asset).")]
        [SerializeField] private UiTuning _tuning;

        [Tooltip("Every relic (Assets/_Project/Data/Content/RelicCatalog.asset): memory cards check ids against it.")]
        [SerializeField] private RelicCatalog _relics;

        [Tooltip("String tables (Assets/_Project/Data/Localization), the primary language (en) first.")]
        [SerializeField] private TextAsset[] _stringTables = Array.Empty<TextAsset>();

        private readonly List<IDisposable> _tokens = new List<IDisposable>();
        private UiServices _services;
        private LocalizationService _localization;
        private PromptLedger _ledger;
        private PromptDirector _director;
        private PlayerSettings _player;
        private CursorPolicy _cursor;
        private GlyphLabels _glyphs;
        private IntText _numbers;
        private UiLayout _layout;
        private StaticTextLocalizer _staticText;
        private Reveal _hud;
        private TitleCard _title;
        private TetherReticle _reticle;
        private ContextPrompt _prompt;
        private ScrapChip _chip;
        private MemoryCard _card;
        private TickerQueue _tickerLines;
        private RadioTicker _ticker;
        private DialReadout _dial;
        private HopList _hopList;
        private HopFade _hopFade;
        private RelayTag _relayTag;
        private TowerPanel _tower;
        private FriendReadout _friendReadout;
        private FriendNameTag _friendName;
        private PauseMenu _pause;
        private bool _composed;
        private bool _bound;
        private bool _awake;
        private bool _digging;
        private float _sinceAwake;

        /// <summary>The localization service this system registered (tests and editor tools read it).</summary>
        internal LocalizationService Localization => _localization;

        internal PauseMenu Pause => _pause;

        internal ContextPrompt Prompt => _prompt;

        internal PromptDirector Director => _director;

        internal PromptLedger Ledger => _ledger;

        internal TetherReticle Reticle => _reticle;

        internal ScrapChip Chip => _chip;

        internal MemoryCard Card => _card;

        internal RadioTicker Ticker => _ticker;

        internal TickerQueue TickerLines => _tickerLines;

        internal DialReadout Dial => _dial;

        internal HopList HopList => _hopList;

        internal HopFade HopFade => _hopFade;

        internal RelayTag RelayTag => _relayTag;

        internal TowerPanel Tower => _tower;

        internal TitleCard Title => _title;

        internal FriendReadout FriendReadout => _friendReadout;

        internal FriendNameTag FriendName => _friendName;

        internal UiLayout Layout => _layout;

        internal CursorPolicy Cursor => _cursor;

        internal bool IsBound => _bound;

        /// <summary>What confirming Quit does; tests replace it so the editor keeps running.</summary>
        internal Action QuitAction { get; set; } = Application.Quit;

        internal void Wire(UIDocument document, UiTuning tuning, RelicCatalog relics, TextAsset[] stringTables)
        {
            _document = document;
            _tuning = tuning;
            _relics = relics;
            _stringTables = stringTables;
        }

        public void Initialize(GameContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!ValidateWiring())
            {
                return;
            }

            Compose(UiServices.From(context));
            context.Register<ILocalization>(_localization);
        }

        /// <summary>Composes the UI over explicit services (tests, captures) instead of a GameContext.</summary>
        internal void Initialize(UiServices services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (ValidateWiring())
            {
                Compose(services);
            }
        }

        private bool ValidateWiring()
        {
            string problem = _document == null ? "the UIDocument is not assigned."
                : _document.visualTreeAsset == null ? "the UIDocument has no GameUI.uxml."
                : _document.panelSettings == null ? "the UIDocument has no PanelSettings."
                : _tuning == null ? "UiTuning is not assigned (run UI/Tuning)."
                : _relics == null ? "the RelicCatalog is not assigned."
                : _stringTables == null || _stringTables.Length == 0 ? "no string tables are assigned."
                : Array.IndexOf(_stringTables, null) >= 0 ? "a string table slot is empty."
                : _tuning.Validate() != null ? "UiTuning " + _tuning.Validate() + "."
                : null;
            if (problem == null)
            {
                return true;
            }

            Debug.LogError($"{nameof(UISystem)}: {problem}", this);
            enabled = false;
            return false;
        }

        private void Compose(UiServices services)
        {
            _services = services;
            var tables = new StringTable[_stringTables.Length];
            for (int i = 0; i < tables.Length; i++)
            {
                tables[i] = StringTable.Parse(_stringTables[i].text, _stringTables[i].name);
            }

            _localization = new LocalizationService(services.Events, tables);
            _numbers = new IntText(CachedNumbers);
            _ledger = new PromptLedger(_tuning.Prompts);
            _director = new PromptDirector(_tuning.Prompts, _ledger);
            _player = new PlayerSettings(services.Audio, services.Look, _localization, _tuning.Pause);
            _cursor = new CursorPolicy();
            _glyphs = new GlyphLabels(services.Input, _localization);
            _tickerLines = new TickerQueue(_tuning.Ticker, new TickerText(_localization));

            ISaveService save = services.Save;
            _tokens.Add(save.Register(new SaveSection<SettingsSaveData>(UiSaveKeys.Settings,
                UiSaveKeys.SettingsVersion, _player.Capture, _player.Restore)));
            _tokens.Add(save.Register(new SaveSection<PromptsSaveData>(UiSaveKeys.Prompts,
                UiSaveKeys.PromptsVersion, _ledger.Capture, _ledger.Restore)));

            EventBus events = services.Events;
            _tokens.Add(events.Subscribe<CurrencyChanged>(OnCurrencyChanged));
            _tokens.Add(events.Subscribe<RelicDeposited>(OnRelicDeposited));
            _tokens.Add(events.Subscribe<RoverAwoke>(OnRoverAwoke));
            _tokens.Add(events.Subscribe<SonarPinged>(OnSonarPinged));
            _tokens.Add(events.Subscribe<ExcavationStarted>(OnExcavationStarted));
            _tokens.Add(events.Subscribe<ExcavationStopped>(OnExcavationStopped));
            _tokens.Add(events.Subscribe<TetherAttached>(OnTetherAttached));
            _tokens.Add(events.Subscribe<LanguageChanged>(OnLanguageChanged));
            _tokens.Add(events.Subscribe<FriendRepairStarted>(OnFriendRepairStarted));
            _tokens.Add(events.Subscribe<FriendRepaired>(OnFriendRepaired));
            _tokens.Add(events.Subscribe<TickerLine>(OnTickerLine));
            _tokens.Add(events.Subscribe<CrewLogFound>(OnCrewLogFound));
            _tokens.Add(events.Subscribe<CassetteCollected>(OnCassetteCollected));
            _tokens.Add(events.Subscribe<RadioProgramChanged>(OnRadioProgramChanged));
            _tokens.Add(events.Subscribe<RelayRestored>(OnRelayRestored));
            _tokens.Add(events.Subscribe<RadioHopListChanged>(OnRadioHopListChanged));

            services.Input.Menu.Enable();
            _cursor.Drive();
            _composed = true;
            TryBind();
        }

        private void Start()
        {
            TryBind();
        }

        private void TryBind()
        {
            if (!_composed || _bound || _document.rootVisualElement == null)
            {
                return;
            }

            try
            {
                Bind(_document.rootVisualElement);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"{nameof(UISystem)}: {exception.Message}", this);
                enabled = false;
            }
        }

        private void Bind(VisualElement root)
        {
            UiServices services = _services;
            _layout = new UiLayout(root);
            _staticText = new StaticTextLocalizer(root, _localization);
            _hud = new Reveal(_layout.Hud, _tuning.Pause.Hud);
            _hud.Snap(true);
            _title = new TitleCard(_layout.Title, _tuning.Title);
            _reticle = new TetherReticle(_layout.Reticle, _layout.ReticleRest, _layout.ReticleHover, _tuning.Reticle);
            _prompt = new ContextPrompt(_layout, _tuning.Prompts, _director, services.View, _glyphs, _localization,
                services.Events);
            _chip = new ScrapChip(_layout.ScrapChip, _layout.ScrapChipShadow, _layout.ScrapChipIcon,
                _layout.ScrapChipCount, _tuning.ScrapChip, _numbers);
            _chip.Snap(services.Wallet.Balance);
            _card = new MemoryCard(_layout, _tuning.MemoryCard, _localization, services.Events, _relics,
                services.Friends);
            _friendReadout = new FriendReadout(_layout, _tuning.Friends, _tuning.Prompts, services.Friends,
                services.Rover, services.View);
            _friendName = new FriendNameTag(_layout, _tuning.Friends, _tuning.Prompts, services.Friends,
                _localization, services.View);
            _ticker = new RadioTicker(_layout, _tuning.Ticker, _tickerLines);
            _dial = new DialReadout(_layout, _tuning.DialReadout, _localization, services.Radio);
            _hopList = new HopList(_layout, _tuning.Relays, _localization, services.Hop);
            _hopFade = new HopFade(_layout, _tuning.Relays, services.Hop);
            _relayTag = new RelayTag(_layout, _tuning.Relays, _tuning.Prompts, services.Hints, services.Relays,
                services.View, _numbers);
            _tower = new TowerPanel(_layout, _tuning.TowerPanel, _localization, services.Events, services.Shop,
                services.Wallet, services.Hints, _numbers);
            _pause = new PauseMenu(_layout, _tuning.Pause, _player, _localization, services.Input, services.Events,
                services.Save, services.Wallet, services.Radio, services.Relays, _numbers, _cursor, Quit);
            _bound = true;
            if (_awake)
            {
                _title.Play();
            }
        }

        private void Update()
        {
            if (_bound)
            {
                Tick(Time.unscaledDeltaTime);
            }
        }

        private void Tick(float deltaTime)
        {
            InputReader input = _services.Input;
            MenuInput menu = input.Menu;
            bool pausePressed = menu.PausePressed;
            bool cancelPressed = menu.CancelPressed;
            if (_pause.IsOpen)
            {
                if (pausePressed || cancelPressed)
                {
                    _pause.Back();
                }
            }
            else if ((pausePressed || cancelPressed) && _card.CanDismiss)
            {
                _card.Dismiss();
            }
            else if (pausePressed)
            {
                _pause.Open();
            }

            if (!_pause.IsOpen && menu.ClickPressed)
            {
                _cursor.Recapture();
            }

            _pause.Tick(deltaTime);
            bool paused = _pause.IsOpen;
            float hudTime = paused ? 0f : deltaTime;
            _hud.Set(!paused);
            _hud.Tick(deltaTime);
            if (_awake)
            {
                _sinceAwake += hudTime;
            }

            InputDeviceKind device = input.ActiveDevice;
            bool hopping = _services.Hop.Phase >= RadioHopPhase.Leaving || _hopFade.IsVisible;
            _title.Tick(hudTime);
            _reticle.Tick(deltaTime, _services.Tether.State, input.TetherHeld);
            _card.Tick(hudTime, _glyphs.Cancel(device), device, !_ticker.IsVisible && !hopping && !_hopList.IsBusy);
            _tower.Tick(hudTime, !paused, input.ExcavateHeld, _glyphs.For(RoverAction.Excavate, device));
            _chip.SetPinned(_tower.IsVisible);
            _chip.Tick(deltaTime);

            if (_director.Displayed == InteractionKind.Reel && _director.WantsShown && input.Winch != 0f)
            {
                _director.NotifyUsed(InteractionKind.Reel);
            }

            bool promptsOpen = !paused && _awake && _sinceAwake >= _tuning.Prompts.StartDelay && !_title.IsPlaying &&
                               !_card.IsVisible && !_tower.IsVisible && !_dial.IsBusy && !_hopList.IsBusy && !hopping;
            Rect panel = _layout.Root.layout;
            Vector2 panelSize = float.IsNaN(panel.width) ? Vector2.zero : panel.size;
            _friendReadout.Tick(deltaTime, !paused, panelSize,
                _prompt.StackHeight(InteractionKind.Repair, _tuning.Friends.StackGap));
            _friendName.Tick(hudTime, panelSize);
            _prompt.Tick(deltaTime, promptsOpen, _services.Hints.Primary, device, panelSize);
            bool tagOpen = !paused && _awake && !_title.IsPlaying && !_card.IsVisible && !_tower.IsVisible &&
                           !_hopList.IsBusy && !hopping;
            _relayTag.Tick(deltaTime, tagOpen, panelSize,
                _prompt.StackHeight(InteractionKind.Restore, _tuning.Relays.TagStackGap));

            if (_services.Hop.Phase == RadioHopPhase.Choosing && _dial.IsVisible)
            {
                _dial.MakeWay();
            }

            _hopList.Tick(deltaTime, !_dial.IsVisible && !_prompt.IsVisible, _glyphs.For(RoverAction.Excavate, device),
                device);
            _dial.Tick(hudTime, !_prompt.IsVisible && !_hopList.IsBusy);
            bool tickerOpen = _awake && !_title.IsPlaying && !_card.IsBusy && !_digging && !_prompt.IsVisible &&
                              !_reticle.IsVisible && !_dial.IsBusy && !_hopList.IsBusy && !hopping;
            _ticker.Tick(hudTime, tickerOpen);
            _hopFade.Tick(deltaTime);
        }

        private void Quit()
        {
            QuitAction();
        }

        private void OnCurrencyChanged(CurrencyChanged changed)
        {
            if (!_bound)
            {
                return;
            }

            if (changed.Delta == 0)
            {
                _chip.Snap(changed.Total);
            }
            else
            {
                _chip.Change(changed.Total);
            }
        }

        private void OnRelicDeposited(RelicDeposited deposited)
        {
            _director.NotifyUsed(InteractionKind.Deposit);
            if (_bound)
            {
                _card.Enqueue(deposited.RelicId, deposited.DisplayedCount);
            }
        }

        private void OnRoverAwoke(RoverAwoke awoke)
        {
            _awake = true;
            if (_bound)
            {
                _title.Play();
            }
        }

        private void OnSonarPinged(SonarPinged pinged)
        {
            _director.NotifyUsed(InteractionKind.Ping);
        }

        private void OnExcavationStarted(ExcavationStarted started)
        {
            _digging = true;
            _director.NotifyUsed(InteractionKind.Excavate);
        }

        private void OnExcavationStopped(ExcavationStopped stopped)
        {
            _digging = false;
        }

        private void OnCrewLogFound(CrewLogFound found)
        {
            if (_bound)
            {
                _card.EnqueueCrewLog(found.LogId);
            }
        }

        private void OnCassetteCollected(CassetteCollected collected)
        {
            if (_bound)
            {
                _card.EnqueueCassette(collected.CassetteId, collected.Collected, collected.Total);
            }
        }

        private void OnRadioProgramChanged(RadioProgramChanged changed)
        {
            if (_bound && _dial.OnProgramChanged())
            {
                _director.NotifyUsed(InteractionKind.Tune);
            }
        }

        private void OnRelayRestored(RelayRestored restored)
        {
            _director.NotifyUsed(InteractionKind.Restore);
        }

        private void OnRadioHopListChanged(RadioHopListChanged changed)
        {
            if (changed.Open)
            {
                _director.NotifyUsed(InteractionKind.Hop);
            }
        }

        private void OnTickerLine(TickerLine line)
        {
            _tickerLines.Enqueue(line);
        }

        private void OnTetherAttached(TetherAttached attached)
        {
            _director.NotifyUsed(InteractionKind.Tether);
        }

        private void OnFriendRepairStarted(FriendRepairStarted started)
        {
            _director.NotifyUsed(InteractionKind.Repair);
        }

        private void OnFriendRepaired(FriendRepaired repaired)
        {
            if (!_bound)
            {
                return;
            }

            IFriendStatuses friends = _services.Friends;
            for (int i = 0; i < friends.Count; i++)
            {
                if (string.Equals(friends.Definition(i).Id, repaired.FriendId, StringComparison.Ordinal))
                {
                    _friendName.Show(i);
                    break;
                }
            }

            _card.EnqueueLog(repaired.FriendId);
        }

        private void OnLanguageChanged(LanguageChanged changed)
        {
            if (!_bound)
            {
                return;
            }

            _glyphs.Clear();
            _staticText.Apply();
            _prompt.Relocalize();
            _card.Relocalize();
            _ticker.Relocalize();
            _dial.Relocalize();
            _hopList.Relocalize();
            _friendName.Relocalize();
            _tower.Relocalize();
            _pause.Relocalize();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _bound && !_pause.IsOpen)
            {
                _cursor.Recapture();
            }
        }

        private void OnDestroy()
        {
            foreach (IDisposable token in _tokens)
            {
                token.Dispose();
            }

            _tokens.Clear();
            if (!_composed)
            {
                return;
            }

            if (_bound)
            {
                if (_pause.IsOpen)
                {
                    _services.Input.Enable();
                }

                _pause.RestoreTime();
                _hopFade.Dispose();
            }

            _services.Input.Menu.Disable();
            _cursor.Release();
        }

        private void OnValidate()
        {
            if (_tuning != null && _tuning.Validate() != null)
            {
                Debug.LogError($"{nameof(UISystem)}: UiTuning {_tuning.Validate()}.", this);
            }
        }
    }
}
