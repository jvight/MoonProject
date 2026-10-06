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
    /// The UI domain's game system (initialised last, after Gameplay). As little UI as possible, as calm as possible:
    /// a title while 07 wakes, context prompts only the first few times, a reticle only while aiming, a scrap chip
    /// only when the balance changes, a memory card per relic brought home, the tower upgrade panel on its pad, and
    /// the pause menu with settings. It registers <see cref="ILocalization"/> and owns the cursor and the UI's save
    /// sections. Everything animates on unscaled time so the menu stays alive while the game is paused.
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
        private TowerPanel _tower;
        private PauseMenu _pause;
        private bool _composed;
        private bool _bound;
        private bool _awake;
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

        internal TowerPanel Tower => _tower;

        internal TitleCard Title => _title;

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
            _glyphs = new GlyphLabels(services.Input);

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
            _tokens.Add(events.Subscribe<TetherAttached>(OnTetherAttached));
            _tokens.Add(events.Subscribe<LanguageChanged>(OnLanguageChanged));

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
            _prompt = new ContextPrompt(_layout, _tuning.Prompts, _director, services.View, _glyphs, _localization);
            _chip = new ScrapChip(_layout.ScrapChip, _layout.ScrapChipShadow, _layout.ScrapChipIcon,
                _layout.ScrapChipCount, _tuning.ScrapChip, _numbers);
            _chip.Snap(services.Wallet.Balance);
            _card = new MemoryCard(_layout, _tuning.MemoryCard, _localization, _relics);
            _tower = new TowerPanel(_layout, _tuning.TowerPanel, _localization, services.Shop, services.Wallet,
                services.Hints, _numbers);
            _pause = new PauseMenu(_layout, _tuning.Pause, _player, _localization, services.Input, services.Events,
                services.Save, services.Wallet, _numbers, _cursor, Quit);
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
            else if (cancelPressed && _card.CanDismiss)
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
            _title.Tick(hudTime);
            _reticle.Tick(deltaTime, _services.Tether.State, input.TetherHeld);
            _card.Tick(hudTime, _glyphs.Cancel(device), device);
            _tower.Tick(hudTime, !paused, input.ExcavateHeld, _glyphs.For(RoverAction.Excavate, device));
            _chip.SetPinned(_tower.IsVisible);
            _chip.Tick(deltaTime);

            if (_director.Displayed == InteractionKind.Reel && _director.WantsShown && input.Winch != 0f)
            {
                _director.NotifyUsed(InteractionKind.Reel);
            }

            bool promptsOpen = !paused && _awake && _sinceAwake >= _tuning.Prompts.StartDelay && !_title.IsPlaying &&
                               !_card.IsVisible && !_tower.IsVisible;
            Rect panel = _layout.Root.layout;
            _prompt.Tick(deltaTime, promptsOpen, _services.Hints.Primary, device,
                float.IsNaN(panel.width) ? Vector2.zero : panel.size);
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
            _director.NotifyUsed(InteractionKind.Excavate);
        }

        private void OnTetherAttached(TetherAttached attached)
        {
            _director.NotifyUsed(InteractionKind.Tether);
        }

        private void OnLanguageChanged(LanguageChanged changed)
        {
            if (!_bound)
            {
                return;
            }

            _staticText.Apply();
            _prompt.Relocalize();
            _card.Relocalize();
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
