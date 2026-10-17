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
    /// title while 07 wakes, context prompts only the first few times, the pickup chip when the stock changes, the
    /// hold ring at a salvage cut, the build menu with its recipe, the battery dial, the protocol readout, the one
    /// line that says what to do next, 07's hold on its CRT face, and the pause menu with settings. It registers
    /// <see cref="ILocalization"/> and owns the cursor and the UI's save sections. Everything animates on unscaled
    /// time so the menu stays alive while the game is paused.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UISystem : MonoBehaviour, IGameSystem
    {
        private const int CachedNumbers = 1000;

        // The hold's change lands a frame or two after the press; the chip stays "the cause" this long.
        private const float PickUpGraceSeconds = 1.5f;

        [Tooltip("The UIDocument showing GameUI.uxml with the Lunarion PanelSettings.")]
        [SerializeField] private UIDocument _document;

        [Tooltip("The UIDocument rendering 07's face UI into LofiLunarFaceTarget.asset via LofiLunarFacePanel.asset.")]
        [SerializeField] private UIDocument _faceDocument;

        [Tooltip("UI timings and motion (Assets/_Project/Data/Tuning/UI/UiTuning.asset).")]
        [SerializeField] private UiTuning _tuning;

        [Tooltip("String tables (Assets/_Project/Data/Localization), the primary language (en) first.")]
        [SerializeField] private TextAsset[] _stringTables = Array.Empty<TextAsset>();

        [Tooltip("The title's Lunarion wordmark (Generated/UI/Wordmark.asset, built by UI/Wordmark).")]
        [SerializeField] private WordmarkShape _wordmark;

        [Tooltip("07's socket diagram (Generated/UI/RoverDiagram.asset, built by UI/Rover Diagram).")]
        [SerializeField] private RoverDiagramData _roverDiagram;
        [SerializeField] private TextAsset _credits;
        [SerializeField] private Texture2D[] _structureIcons = Array.Empty<Texture2D>();

        private readonly List<IDisposable> _tokens = new List<IDisposable>();
        private UiServices _services;
        private LocalizationService _localization;
        private PromptLedger _ledger;
        private PromptDirector _director;
        private PlayerSettings _player;
        private SettingsStore _settings;
        private CursorPolicy _cursor;
        private GlyphLabels _glyphs;
        private IntText _numbers;
        private UiLayout _layout;
        private StaticTextLocalizer _staticText;
        private bool _facePanelVisible;
        private Reveal _hud;
        private StargazeVeil _stargaze;
        private HoldHint _holdHint;
        private FirstPrompt _firstPrompt;
        private LookUpChip _lookUpChip;
        private NextStepLine _nextStep;
        private NextLineView _nextLine;
        private LowBatteryHint _lowBattery;
        private BatteryDial _batteryDial;
        private bool _cellRowRevealed;
        private bool _carrying;
        private SaveTick _saveTick;
        private ProtocolReadout _protocol;
        private RecipeCard _recipeCard;
        private ContextPrompt _prompt;
        private PickupFeed _pickups;
        private SalvageRing _salvageRing;
        private readonly GuidanceSlot _slot = new GuidanceSlot();
        private readonly bool[] _guidanceWants = new bool[GuidanceSlot.KindCount];
        private readonly bool[] _guidanceShown = new bool[GuidanceSlot.KindCount];
        private CargoScreen _cargo;
        private UnlockCard _unlockCard;
        private EventBus _events;
        private BuildStripView _buildStrip;
        private PauseMenu _pause;
        private TextScale _textScale;
        private BackdropBlur _backdrop;
        private bool _composed;
        private bool _bound;
        private bool _awake;
        private bool _inOpening;
        private bool _titleWaiting;
        private bool _titleHasSave;
        private float _sinceAwake;
        private float _pickUpGrace;
        private bool _reopenCargo;

        /// <summary>The localization service this system registered (tests and editor tools read it).</summary>
        internal LocalizationService Localization => _localization;

        internal PauseMenu Pause => _pause;

        internal ContextPrompt Prompt => _prompt;

        internal PromptDirector Director => _director;

        internal PromptLedger Ledger => _ledger;

        internal PickupFeed Pickups => _pickups;

        internal SalvageRing SalvageRing => _salvageRing;

        internal SaveTick SaveTick => _saveTick;

        internal BuildStripView BuildStrip => _buildStrip;

        /// <summary>The guidance slot (tests): which element may show now.</summary>
        internal GuidanceSlot Slot => _slot;

        internal CargoScreen Cargo => _cargo;

        internal UnlockCard UnlockCard => _unlockCard;

        internal ProtocolReadout Protocol => _protocol;

        internal RecipeCard RecipeCard => _recipeCard;

        internal LookUpChip LookUp => _lookUpChip;

        internal BackdropBlur Backdrop => _backdrop;

        internal NextLineView NextLine => _nextLine;

        internal BatteryDial BatteryDial => _batteryDial;

        internal UiLayout Layout => _layout;

        internal CursorPolicy Cursor => _cursor;

        internal UIDocument FaceDocument => _faceDocument;

        internal bool IsFacePanelVisible => _facePanelVisible;

        internal bool IsBound => _bound;

        /// <summary>What confirming Quit does; tests replace it so the editor keeps running.</summary>
        internal Action QuitAction { get; set; } = Application.Quit;

        /// <summary>The screen the Display page drives (Unity's, unless a test hands in its own).</summary>
        internal IDisplayControl DisplayControl { get; set; } = new UnityDisplayControl();

        internal void Wire(UIDocument document, UiTuning tuning, TextAsset[] stringTables,
            WordmarkShape wordmark, RoverDiagramData roverDiagram, TextAsset credits, UIDocument faceDocument = null)
        {
            _credits = credits;
            _document = document;
            _faceDocument = faceDocument;
            _wordmark = wordmark;
            _roverDiagram = roverDiagram;
            _tuning = tuning;
            _stringTables = stringTables;
        }

        internal void WireStructureIcons(Texture2D[] icons)
        {
            _structureIcons = icons ?? Array.Empty<Texture2D>();
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
                : _faceDocument != null && _faceDocument.panelSettings == null ? "the FaceUI UIDocument has no PanelSettings."
                : _tuning == null ? "UiTuning is not assigned (run UI/Tuning)."
                : _stringTables == null || _stringTables.Length == 0 ? "no string tables are assigned."
                : Array.IndexOf(_stringTables, null) >= 0 ? "a string table slot is empty."
                : _tuning.Validate() != null ? "UiTuning " + _tuning.Validate() + "."
                : _wordmark == null ? "the WordmarkShape is not assigned (run UI/Wordmark)."
                : _roverDiagram == null ? "the RoverDiagramData is not assigned (run UI/Rover Diagram)."
                : _credits == null ? "the credits asset is not assigned."
                : _wordmark.Validate() != null ? "the wordmark " + _wordmark.Validate() + "."
                : _roverDiagram.Validate() != null ? "RoverDiagram " + _roverDiagram.Validate() + "."
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
            _player = new PlayerSettings(services.Audio, services.Look, services.Feel, DisplayControl, _localization,
                _tuning.Pause);
            _cursor = new CursorPolicy();
            _glyphs = new GlyphLabels(services.Input, _localization);

            _settings = new SettingsStore(_player, services.Settings);
            _settings.Load();
            ISaveService save = services.Save;
            _tokens.Add(save.Register(new SaveSection<SettingsSaveData>(UiSaveKeys.Settings,
                UiSaveKeys.SettingsVersion, _player.Capture, _settings.Migrate)));
            _tokens.Add(save.Register(new SaveSection<PromptsSaveData>(UiSaveKeys.Prompts,
                UiSaveKeys.PromptsVersion, _ledger.Capture, _ledger.Restore)));

            EventBus events = services.Events;
            _events = events;
            _tokens.Add(events.Subscribe<RoverAwoke>(OnRoverAwoke));
            _tokens.Add(events.Subscribe<TitleShown>(OnTitleShown));
            _tokens.Add(events.Subscribe<NewGameRequested>(_ => OnTitleDismissed()));
            _tokens.Add(events.Subscribe<ContinueRequested>(_ => OnTitleDismissed()));
            _tokens.Add(events.Subscribe<FeelBeat>(OnFeelBeat));
            _tokens.Add(events.Subscribe<CargoChanged>(OnCargoChanged));
            _tokens.Add(events.Subscribe<HomeStorageChanged>(_ => _cargo?.MarkDirty()));
            _tokens.Add(events.Subscribe<GameSaved>(OnGameSaved));
            _tokens.Add(events.Subscribe<StargazingChanged>(OnStargazingChanged));
            _tokens.Add(events.Subscribe<NextStepChanged>(OnNextStepChanged));
            _tokens.Add(events.Subscribe<ScanStarted>(_ => _director.Yield(InteractionKind.Scan)));
            _tokens.Add(events.Subscribe<FragmentScanned>(_ => _director.NotifyUsed(InteractionKind.Scan)));
            _tokens.Add(events.Subscribe<RoverWakeBeatReached>(OnWakeBeat));
            _tokens.Add(events.Subscribe<LanguageChanged>(OnLanguageChanged));
            _tokens.Add(events.Subscribe<TickerLine>(OnTickerLine));
            _tokens.Add(events.Subscribe<PartFitted>(OnPartFitted));
            _tokens.Add(events.Subscribe<BatteryArcRevealed>(OnBatteryArcRevealed));
            _tokens.Add(events.Subscribe<StructurePlaced>(_ => _director.NotifyUsed(InteractionKind.Build)));
            _tokens.Add(events.Subscribe<StructureRemoved>(_ => _director.NotifyUsed(InteractionKind.Deconstruct)));

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
            if (!_composed || _bound || _document.rootVisualElement == null ||
                (_faceDocument != null && _faceDocument.rootVisualElement == null))
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
            new WordmarkPainter(_layout.TitleWordmark, _wordmark);
            new SafeFrame(_layout, _tuning.Safe);
            _staticText = new StaticTextLocalizer(root, _localization);
            _hud = new Reveal(_layout.Hud, _tuning.Pause.Hud);
            _hud.Snap(true);
            _stargaze = new StargazeVeil(_tuning.Stargaze.Hud, _layout.PromptAnchor, _layout.SalvageRingAnchor,
                _layout.BuildReasonAnchor);
            _holdHint = new HoldHint(_tuning.Stargaze, _ledger);
            _firstPrompt = new FirstPrompt(_tuning.Stargaze, _ledger);
            _lookUpChip = new LookUpChip(_layout, _tuning.Stargaze, _localization);
            _nextStep = new NextStepLine(_tuning.Guidance);
            _nextLine = new NextLineView(_layout, _tuning.Guidance, _localization);
            _lowBattery = new LowBatteryHint(_tuning.Guidance, services.Battery, _localization);
            _batteryDial = new BatteryDial(_layout, _tuning.Battery, services.Battery, services.Beam);
            _saveTick = new SaveTick(_layout, _tuning.SaveTick);
            _protocol = new ProtocolReadout(_layout.Hud, _tuning.Guidance, _localization, services.Recovery);
            _recipeCard = new RecipeCard(_layout.Hud, _tuning.Guidance.Recipe, _localization, _numbers, services.Shop,
                services.Materials, services.RecipeLook);
            _prompt = new ContextPrompt(_layout, _tuning.Prompts, _director, services.View, _glyphs, _localization,
                services.Events, new[] { _layout.RecipeCardRoot, _layout.BuildStrip });
            _pickups = new PickupFeed(_layout, _tuning.PickupFeed, _numbers, _localization, services.Materials,
                services.Catalog, services.Events);
            _salvageRing = new SalvageRing(_layout, _tuning.Salvage, _tuning.Prompts, services.Salvage, services.View);
            _buildStrip = new BuildStripView(_layout, _tuning.BuildStripReveal, _tuning.Prompts, _localization,
                _numbers, services.Materials, services.View, services.BuildField, _structureIcons, _glyphs);
            _cargo = new CargoScreen(_layout, _tuning.Cargo, _localization, services.Hold,
                services.Home, services.Transfer, services.Dock, services.Equipment, services.Portrait, _roverDiagram,
                _glyphs, _numbers, services.Events, _ledger);
            BindFacePanel(root);
            _unlockCard = new UnlockCard(_layout, _tuning.UnlockCard, _localization, _glyphs);
            _pause = new PauseMenu(_layout, _tuning.Pause, _tuning.Title, _player, _localization, services.Input,
                services.Events, _settings, services.Flow,
                _numbers, _cursor, Quit,
                new CreditsView(_layout, _tuning.Credits, _localization, _credits.text));
            _textScale = new TextScale(_document.panelSettings);
            _backdrop = new BackdropBlur(transform, _tuning.Backdrop);
            _bound = true;
            if (_titleWaiting)
            {
                _titleWaiting = false;
                _pause.OpenTitle(_titleHasSave, !_titleHasSave);
            }
        }

        private void Update()
        {
            if (_bound)
            {
                Tick(Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime);
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
            else if (_cargo.IsOpen)
            {
                if (menu.CargoPressed || pausePressed || cancelPressed)
                {
                    CloseCargo();
                }
            }
            else if (menu.CargoPressed && CanOpenCargo())
            {
                OpenCargo();
            }
            else if (pausePressed)
            {
                _pause.Open();
            }

            if (!_pause.IsOpen && !_cargo.IsOpen && menu.ClickPressed)
            {
                _cursor.Recapture();
            }

            _pause.Tick(deltaTime);
            bool paused = _pause.IsOpen;
            SetFacePanelVisible(_cargo.IsOpen || _cargo.Openness > 0f);
            _cargo.Tick(deltaTime, CargoScreenSize(), input.ActiveDevice);
            if (_cargo.IsOpen)
            {
                StepCargoInput(menu, deltaTime);
            }

            if (_cargo.IsOpen && ((_cargo.IsTransferView && !_services.Dock.IsDocked) || _cargo.WantsClose))
            {
                _reopenCargo = _cargo.WantsClose;
                CloseCargo();
            }

            SetFacePanelVisible(_cargo.IsOpen || _cargo.Openness > 0f);
            bool cargoOpen = _cargo.IsOpen;
            _backdrop.Set(_pause.Openness, _cargo.Openness);
            float hudTime = paused ? 0f : deltaTime;
            _hud.Set(!paused && !cargoOpen);
            _hud.Tick(deltaTime);
            _saveTick.Tick(deltaTime);
            _stargaze.Tick(deltaTime);
            if (_awake)
            {
                _sinceAwake += hudTime;
            }

            InputDeviceKind device = input.ActiveDevice;
            _textScale.Apply(_player.TextScale);
            _nextStep.Tick(hudTime, _services.Rover.Speed, _player.Guidance);
            _lowBattery.Tick(paused ? 0f : deltaTime, _nextLine.ShowsBattery);
            DecideGuidance(paused);
            _unlockCard.Tick(hudTime, !cargoOpen && !_nextLine.IsVisible, device);
            _pickups.Tick(deltaTime);
            bool cutting = _services.Salvage.IsCutting;
            if (cutting)
            {
                _director.Yield(InteractionKind.Salvage);
            }

            Rect panel = _layout.Root.layout;
            Vector2 panelSize = float.IsNaN(panel.width) ? Vector2.zero : panel.size;
            _buildStrip?.Tick(hudTime, !paused && _awake && !cargoOpen, panelSize, device);
            _pickUpGrace = _services.Hints.Primary.Kind == InteractionKind.PickUp
                ? PickUpGraceSeconds
                : Mathf.Max(0f, _pickUpGrace - deltaTime);
            _prompt.Tick(deltaTime, PromptsOpen(paused) && _slot.Allows(GuidanceKind.Prompt),
                _services.Hints.Primary, device, panelSize);
            _salvageRing.Tick(deltaTime, !paused && _awake, panelSize);
            _protocol.Tick(hudTime);
            // Its own zone (right of centre): it stands beside the protocol readout, never over it.
            _recipeCard.Tick(hudTime, CardsOpen(paused) && _awake && _slot.Allows(GuidanceKind.Card));
            // The look-up chip is the gentlest of the prompts: a teaching prompt that wants the screen takes it first.
            bool hintOpen = LookHintsOpen() && _slot.Allows(GuidanceKind.Prompt) &&
                            !_director.Wants(_services.Hints.Primary);
            float stick = _tuning.Stargaze.FirstPromptStick * _tuning.Stargaze.FirstPromptStick;
            bool looking = !paused && (input.LookDelta.sqrMagnitude > 0f || input.LookRate.sqrMagnitude > stick);
            _firstPrompt.Tick(hudTime, hintOpen, looking, !paused && input.Drive.sqrMagnitude > stick);
            _holdHint.Tick(hudTime, hintOpen && !cargoOpen && _firstPrompt.Word == HintWord.None, _awake && _carrying);
            HintWord hint = _firstPrompt.Word != HintWord.None ? _firstPrompt.Word
                : _holdHint.WantsShown ? HintWord.Hold : HintWord.None;
            _lookUpChip.Tick(deltaTime, paused ? HintWord.None : hint, device);
            _nextLine.Tick(deltaTime, _nextStep, NextLineOpen(paused) && _slot.Allows(GuidanceKind.Next), _lowBattery,
                NextLineOpen(paused) && _slot.Allows(GuidanceKind.Battery));
            bool hudOpen = !paused && _awake && !_inOpening && !_stargaze.IsVeiled && !cargoOpen;
            _batteryDial.Tick(deltaTime, _cellRowRevealed, hudOpen);
        }

        /// <summary>The cell row exists from the first time the charge dropped below its reveal share.</summary>
        private void OnBatteryArcRevealed(BatteryArcRevealed revealed)
        {
            _cellRowRevealed = true;
        }

        /// <summary>Everything but the slot that lets the teaching prompt show.</summary>
        private bool PromptsOpen(bool paused)
        {
            return !paused && _awake && !_inOpening && _sinceAwake >= _tuning.Prompts.StartDelay &&
                   !_lookUpChip.IsVisible && !BuildMenuOpen();
        }

        private bool CardsOpen(bool paused)
        {
            return !paused && !_protocol.IsVisible && !_stargaze.IsVeiled;
        }

        private bool NextLineOpen(bool paused)
        {
            return !paused && _awake && !_inOpening && !_stargaze.IsVeiled && !_protocol.IsVisible &&
                   !_unlockCard.IsVisible && !BuildMenuOpen();
        }

        private bool LookHintsOpen()
        {
            return !_protocol.IsVisible && !BuildMenuOpen();
        }

        private bool BuildMenuOpen()
        {
            return _buildStrip != null && _buildStrip.IsOpen;
        }

        /// <summary>
        /// One guidance element at a time (docs/design/0.5.5-owner-feedback.md §4.1): the teaching prompt, then the
        /// recipe card, the low-battery hint and the next line. Each wants the slot when it could show; the slot goes
        /// to the highest and passes on once the screen is clear.
        /// </summary>
        private void DecideGuidance(bool paused)
        {
            bool looks = LookHintsOpen() && (_firstPrompt.Word != HintWord.None || _holdHint.Wants(_awake && _carrying));
            _guidanceWants[(int)GuidanceKind.Prompt] =
                (PromptsOpen(paused) && _director.Wants(_services.Hints.Primary)) || looks;
            _guidanceWants[(int)GuidanceKind.Card] = CardsOpen(paused) && _awake && _recipeCard.Wants;
            _guidanceWants[(int)GuidanceKind.Battery] = NextLineOpen(paused) && _lowBattery.WantsShown;
            _guidanceWants[(int)GuidanceKind.Next] = NextLineOpen(paused) && _nextStep.WantsShown;
            _guidanceShown[(int)GuidanceKind.Prompt] = _prompt.IsVisible || _lookUpChip.IsVisible;
            _guidanceShown[(int)GuidanceKind.Card] = _recipeCard.IsVisible;
            _guidanceShown[(int)GuidanceKind.Battery] = _nextLine.ShowsBattery;
            _guidanceShown[(int)GuidanceKind.Next] = _nextLine.ShowsNext;
            _slot.Step(_guidanceWants, _guidanceShown);
        }

        private const float CargoNavInitialRepeatDelay = 0.35f;
        private const float CargoNavRepeatInterval = 0.12f;

        private Vector2 _lastFaceMouseScreenPos = new Vector2(float.NaN, float.NaN);
        private NavigationMoveEvent.Direction _lastCargoNavDirection = NavigationMoveEvent.Direction.None;
        private float _cargoNavRepeatTimer;

        private void BindFacePanel(VisualElement root)
        {
            if (_faceDocument == null)
            {
                return;
            }

            VisualElement faceRoot = _faceDocument.rootVisualElement;
            faceRoot.pickingMode = PickingMode.Ignore;
            var moonRoot = new VisualElement { name = "moon-root", pickingMode = PickingMode.Ignore };
            moonRoot.AddToClassList("moon-root");
            for (VisualElement ancestor = _layout.Cargo.parent; ancestor != null; ancestor = ancestor.parent)
            {
                CopyStyleSheets(ancestor, moonRoot);
                CopyStyleSheets(ancestor, _layout.Cargo);
            }

            CopyStyleSheets(root, moonRoot);
            CopyStyleSheets(root, _layout.Cargo);
            moonRoot.Add(_layout.Cargo);
            faceRoot.Add(moonRoot);
            _cargo.FillPanelHeight = true;
            if (_faceDocument.panelSettings != null)
            {
                _faceDocument.panelSettings.sortingOrder = 1f;
                _faceDocument.panelSettings.SetScreenToPanelSpaceFunction(ScreenToFacePanel);
            }

            _facePanelVisible = true;
            SetFacePanelVisible(false);
        }

        private static void CopyStyleSheets(VisualElement source, VisualElement destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            int count = source.styleSheets.count;
            for (int i = 0; i < count; i++)
            {
                StyleSheet sheet = source.styleSheets[i];
                if (sheet != null && !destination.styleSheets.Contains(sheet))
                {
                    destination.styleSheets.Add(sheet);
                }
            }
        }

        private void SetFacePanelVisible(bool visible)
        {
            if (_faceDocument == null || _faceDocument.rootVisualElement == null || _facePanelVisible == visible)
            {
                return;
            }

            _facePanelVisible = visible;
            _faceDocument.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private Vector2 ScreenSize()
        {
            Rect panel = _layout.Root.layout;
            return float.IsNaN(panel.width) ? Vector2.zero : panel.size;
        }

        private Vector2 CargoScreenSize()
        {
            if (_faceDocument == null || _faceDocument.panelSettings == null)
            {
                return ScreenSize();
            }

            if (_faceDocument.rootVisualElement != null)
            {
                Rect rect = _faceDocument.rootVisualElement.layout;
                if (!float.IsNaN(rect.width) && rect.width > 0f && rect.height > 0f)
                {
                    return rect.size;
                }
            }

            Vector2Int reference = _faceDocument.panelSettings.referenceResolution;
            return reference.x > 0 && reference.y > 0
                ? new Vector2(reference.x, reference.y)
                : FaceScreenMapper.DefaultPanelSize;
        }

        private Vector2 ScreenToFacePanel(Vector2 screenPosition)
        {
            return FaceScreenMapper.ScreenToPanel(screenPosition, _services?.View?.Camera, _services?.FaceScreen,
                CargoScreenSize());
        }

        private static NavigationMoveEvent.Direction QuantizeNavigationDirection(Vector2 nav)
        {
            if (Mathf.Abs(nav.x) >= Mathf.Abs(nav.y))
            {
                return nav.x > 0.2f ? NavigationMoveEvent.Direction.Right
                    : nav.x < -0.2f ? NavigationMoveEvent.Direction.Left
                    : NavigationMoveEvent.Direction.None;
            }

            return nav.y > 0.2f ? NavigationMoveEvent.Direction.Up
                : nav.y < -0.2f ? NavigationMoveEvent.Direction.Down
                : NavigationMoveEvent.Direction.None;
        }

        private void StepCargoInput(MenuInput menu, float deltaTime)
        {
            FaceInventoryView view = _cargo.View;
            NavigationMoveEvent.Direction dir = QuantizeNavigationDirection(menu.NavigateVector);
            if (dir == NavigationMoveEvent.Direction.None)
            {
                _lastCargoNavDirection = NavigationMoveEvent.Direction.None;
                _cargoNavRepeatTimer = 0f;
                if (menu.NavigatePressed && view.FocusedHoldCell < 0 && view.FocusedSocketIndex < 0 &&
                    view.FocusedHomeCell < 0)
                {
                    _cargo.FocusFirst();
                }
            }
            else if (dir != _lastCargoNavDirection || menu.NavigatePressed)
            {
                _lastCargoNavDirection = dir;
                _cargoNavRepeatTimer = CargoNavInitialRepeatDelay;
                if (view.LastNavigationFrame != Time.frameCount)
                {
                    view.Navigate(dir);
                }
            }
            else
            {
                _cargoNavRepeatTimer -= deltaTime;
                if (_cargoNavRepeatTimer <= 0f)
                {
                    _cargoNavRepeatTimer = CargoNavRepeatInterval;
                    if (view.LastNavigationFrame != Time.frameCount)
                    {
                        view.Navigate(dir);
                    }
                }
            }

            if (menu.SubmitPressed)
            {
                view.SubmitFocused();
            }

            if (_faceDocument == null || _services.FaceScreen == null || !_services.FaceScreen.IsOpen)
            {
                return;
            }

            Vector2 rawMouse = menu.PointerPosition;
            if (float.IsNaN(rawMouse.x) || float.IsNaN(rawMouse.y))
            {
                return;
            }

            bool moved = float.IsNaN(_lastFaceMouseScreenPos.x)
                ? rawMouse.sqrMagnitude > 0.01f
                : (rawMouse - _lastFaceMouseScreenPos).sqrMagnitude > 0.01f;
            bool down = menu.ClickPressed;
            bool up = menu.ClickReleased;
            if (!moved && !down && !up)
            {
                return;
            }

            _lastFaceMouseScreenPos = rawMouse;
            Camera cam = _services.View?.Camera;
            float height = cam != null && cam.pixelHeight > 0 ? cam.pixelHeight : Screen.height;
            var uiScreenPos = new Vector2(rawMouse.x, height - rawMouse.y);
            Vector2 panelPos = FaceScreenMapper.ScreenToPanel(uiScreenPos, cam, _services.FaceScreen, CargoScreenSize(),
                height);
            if (!float.IsNaN(panelPos.x) && !float.IsNaN(panelPos.y))
            {
                if (down)
                {
                    view.PointerDownAtPanel(panelPos);
                }
                else if (moved)
                {
                    view.PointerMoveAtPanel(panelPos);
                }

                if (up)
                {
                    view.PointerUpAtPanel(panelPos);
                }
            }
            else if (up && view.IsDraggingKit)
            {
                view.CompleteKitDrag(null);
            }
        }

        private Texture FaceTargetTexture()
        {
            return _faceDocument != null && _faceDocument.panelSettings != null
                ? _faceDocument.panelSettings.targetTexture
                : null;
        }

        /// <summary>The cargo screen opens once 07 is awake, over a quiet screen (not paused, not on the title).</summary>
        private bool CanOpenCargo()
        {
            return _awake && !_pause.IsOpen;
        }

        private void OpenCargo()
        {
            _reopenCargo = false;
            _lastFaceMouseScreenPos = _services.Input.Menu.PointerPosition;
            _lastCargoNavDirection = NavigationMoveEvent.Direction.None;
            _cargoNavRepeatTimer = 0f;
            SetFacePanelVisible(true);
            _cargo.Open();
            _services.FaceScreen?.SetScreenOpen(true, FaceTargetTexture());
            _services.Input.Disable();
            _cursor.OpenInventory();
            _cargo.FocusFirst();
            _events.Publish(new UiCue(UiCueKind.Confirm));
        }

        private void CloseCargo()
        {
            _lastCargoNavDirection = NavigationMoveEvent.Direction.None;
            _cargoNavRepeatTimer = 0f;
            _cargo.Close();
            _services.FaceScreen?.SetScreenOpen(false, FaceTargetTexture());
            _services.Input.Enable();
            _cursor.CloseInventory();
            _events.Publish(new UiCue(UiCueKind.Back));
        }

        private void Quit()
        {
            QuitAction();
        }

        /// <summary>A prompt retires when its verb completes: the hold gaining a piece just after the pick-up chip.</summary>
        private void OnCargoChanged(CargoChanged changed)
        {
            _carrying = changed.UsedSlots > 0;
            _cargo?.MarkDirty();
            if (_pickUpGrace > 0f)
            {
                _director.NotifyUsed(InteractionKind.PickUp);
            }
        }

        private void OnRoverAwoke(RoverAwoke awoke)
        {
            _awake = true;
        }

        /// <summary>
        /// App put the title up: the menu opens as the title's, on the language step when there is no save yet (a first
        /// boot: the player's language lives in the save).
        /// </summary>
        private void OnTitleShown(TitleShown shown)
        {
            if (_bound)
            {
                _pause.OpenTitle(shown.HasSave, !shown.HasSave);
                return;
            }

            _titleWaiting = true;
            _titleHasSave = shown.HasSave;
        }

        private void OnTitleDismissed()
        {
            _titleWaiting = false;
            _pause?.CloseTitleIfOpen();
        }

        private void OnGameSaved(GameSaved saved)
        {
            if (_bound && _awake && !_inOpening && !_pause.IsTitle)
            {
                _saveTick.Saved();
            }
        }

        private void OnFeelBeat(FeelBeat beat)
        {
            if (_bound)
            {
                _salvageRing.OnBeat(beat);
                if (beat.Kind == FeelKind.Collect && beat.Phase == FeelPhase.Reward)
                {
                    _director.NotifyUsed(InteractionKind.Salvage);
                }

                // The install moment ends with its Proud chirp: the cargo screen the player fitted from comes back.
                if (beat.Kind == FeelKind.Install && beat.Phase == FeelPhase.Reward && _reopenCargo)
                {
                    _reopenCargo = false;
                    if (CanOpenCargo() && !_cargo.IsOpen)
                    {
                        OpenCargo();
                    }
                }
            }
        }

        private void OnStargazingChanged(StargazingChanged changed)
        {
            if (_bound)
            {
                _stargaze.SetStargazing(changed.IsStargazing);
            }
        }

        private void OnNextStepChanged(NextStepChanged changed)
        {
            _nextStep.SetKey(changed.LineKey);
        }

        /// <summary>The full opening's crackle and its chase frame time the first prompt (cutscenes O14).</summary>
        private void OnWakeBeat(RoverWakeBeatReached reached)
        {
            if (!_bound)
            {
                return;
            }

            if (reached.Beat == WakeBeat.Crackle)
            {
                _inOpening = true;
                _firstPrompt.OpeningBegan();
                _protocol.OpeningBegan();
            }
            else if (reached.Beat == WakeBeat.GazeEarth)
            {
                _protocol.Cue();
            }
            else if (reached.Beat == WakeBeat.ChaseFrame)
            {
                _inOpening = false;
                _protocol.Cue();
                _firstPrompt.ChaseFrameReached();
            }
        }

        private void OnPartFitted(PartFitted fitted)
        {
            if (_bound && AbilityTable.TryAbilityOf(fitted.Part, out RoverAbility gained))
            {
                _unlockCard.Gained(gained);
                _cargo.MarkDirty();
            }
        }

        /// <summary>Gameplay's low-charge line is guidance (priority of the slot), offered to the low-battery hint.</summary>
        private void OnTickerLine(TickerLine line)
        {
            if (LowBatteryHint.IsLowLine(line))
            {
                _lowBattery.Offer(line);
            }
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
            _lookUpChip.Relocalize();
            _lowBattery.Relocalize();
            _nextLine.Relocalize(_nextStep, _lowBattery);
            _protocol.Relocalize();
            _recipeCard.Relocalize();
            _recipeCard.Relocalize();
            _buildStrip?.Relocalize();
            _pickups.Relocalize();
            _cargo.Relocalize();
            _unlockCard.Relocalize();
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
                if (_pause.IsOpen || _cargo.IsOpen)
                {
                    _services.Input.Enable();
                }

                if (_faceDocument != null && _faceDocument.panelSettings != null)
                {
                    _faceDocument.panelSettings.SetScreenToPanelSpaceFunction(null);
                }

                _services.FaceScreen?.SetScreenOpen(false, null);
                _pause.RestoreTime();
                _textScale.Restore();
                _backdrop.Dispose();
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
