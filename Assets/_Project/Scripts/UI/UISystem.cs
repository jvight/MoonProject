using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Map;
using MoonProject.Core.Save;
using MoonProject.Gameplay;
using MoonProject.Gameplay.Map;
using MoonProject.UI.Map;

namespace MoonProject.UI
{
    /// <summary>
    /// The UI domain's game system (initialised last, after Gameplay). As little UI as possible, as calm as possible: a
    /// title while 07 wakes, context prompts only the first few times, a reticle only while aiming, the materials chip
    /// only when the stock changes, the hold ring at a salvage cut and a site's name the first time it answers the
    /// sonar, a story card per relic brought home, crew log found and cassette collected, the station upgrade panel
    /// with its recipe (a list to pick from at Kenji's Rover Bay) and each station's line once its work shows, the name
    /// of each piece of kit as it settles onto 07, a few warm pips over a broken friend while 07 is near, its name and
    /// its crew log when it wakes, the radio's ticker line along the bottom, the station's name when Bell's dial is
    /// turned, the relay network's price tag, hop list and soft hop fade, and the pause menu with settings. It
    /// registers <see cref="ILocalization"/> and owns the cursor and the UI's save sections. Everything animates on
    /// unscaled time so the menu stays alive while the game is paused.
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

        [Tooltip("Every relic (Assets/_Project/Data/Content/RelicCatalog.asset): memory cards check ids against it.")]
        [SerializeField] private RelicCatalog _relics;

        [Tooltip("String tables (Assets/_Project/Data/Localization), the primary language (en) first.")]
        [SerializeField] private TextAsset[] _stringTables = Array.Empty<TextAsset>();

        [Tooltip("The crew's map geometry (Generated/World/Map/MapSheet.asset, docs/features/M3-07).")]
        [SerializeField] private MapSheet _mapSheet;

        [Tooltip("The crew's map view (Assets/_Project/Data/Tuning/UI/MapViewTuning.asset).")]
        [SerializeField] private MapViewTuning _mapTuning;

        [Tooltip("The title's Lunarion wordmark (Generated/UI/Wordmark.asset, built by UI/Wordmark).")]
        [SerializeField] private WordmarkShape _wordmark;

        [Tooltip("07's socket diagram (Generated/UI/RoverDiagram.asset, built by UI/Rover Diagram).")]
        [SerializeField] private RoverDiagramData _roverDiagram;
        [SerializeField] private TextAsset _credits;
        [SerializeField] private Texture2D[] _structureIcons = Array.Empty<Texture2D>();

        private readonly List<IDisposable> _tokens = new List<IDisposable>();
        private readonly JourneyNotice _journey = new JourneyNotice();
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
        private LabHud _labHud;
        private RecoveryBoard _board;
        private StargazeVeil _stargaze;
        private LookUpHint _lookUpHint;
        private HoldHint _holdHint;
        private FitHint _fitHint;
        private FirstPrompt _firstPrompt;
        private LookUpChip _lookUpChip;
        private NextStepLine _nextStep;
        private NextLineView _nextLine;
        private LowBatteryHint _lowBattery;
        private BatteryDial _batteryDial;
        private HomeMeter _homeMeter;
        private bool _cellRowRevealed;
        private PlaceCard _placeCard;
        private SaveTick _saveTick;
        private readonly NotYetLines _notYet = new NotYetLines();
        private ProtocolReadout _protocol;
        private RecipeCard _recipeCard;
        private TetherReticle _reticle;
        private ContextPrompt _prompt;
        private PickupFeed _pickups;
        private SalvageRing _salvageRing;
        private SiteNameTag _siteName;
        private MemoryCard _card;
        private RewardCard _rewardCard;
        private readonly GuidanceSlot _slot = new GuidanceSlot();
        private readonly bool[] _guidanceWants = new bool[GuidanceSlot.KindCount];
        private readonly bool[] _guidanceShown = new bool[GuidanceSlot.KindCount];
        private CargoScreen _cargo;
        private UnlockCard _unlockCard;
        private CrateNote _crateNote;
        private TickerQueue _tickerLines;
        private int _tickerStartsAnnounced;
        private EventBus _events;
        private RadioTicker _ticker;
        private DialReadout _dial;
        private HopList _hopList;
        private HopFade _hopFade;
        private RelayTag _relayTag;
        private TowerPanel _tower;
        private BuildStripView _buildStrip;
        private KitTitle _kitTitle;
        private FriendNameTag _friendName;
        private PauseMenu _pause;
        private TextScale _textScale;
        private BackdropBlur _backdrop;
        private IMapKnowledge _mapKnowledge;
        private IStationReach _mapReach;
        private IChartOverlay _mapOverlay;
        private MapView _map;
        private bool _composed;
        private bool _bound;
        private bool _awake;
        private bool _titleWaiting;
        private bool _titleHasSave;
        private bool _digging;
        private float _sinceAwake;
        private float _pickUpGrace;
        private bool _reopenCargo;

        /// <summary>The localization service this system registered (tests and editor tools read it).</summary>
        internal LocalizationService Localization => _localization;

        internal PauseMenu Pause => _pause;

        internal ContextPrompt Prompt => _prompt;

        internal PromptDirector Director => _director;

        internal PromptLedger Ledger => _ledger;

        internal TetherReticle Reticle => _reticle;

        internal PickupFeed Pickups => _pickups;

        internal SalvageRing SalvageRing => _salvageRing;

        internal SaveTick SaveTick => _saveTick;

        internal SiteNameTag SiteName => _siteName;

        internal MemoryCard Card => _card;

        internal RadioTicker Ticker => _ticker;

        internal TickerQueue TickerLines => _tickerLines;

        internal DialReadout Dial => _dial;

        internal HopList HopList => _hopList;

        internal HopFade HopFade => _hopFade;

        internal RelayTag RelayTag => _relayTag;

        internal TowerPanel Tower => _tower;

        internal BuildStripView BuildStrip => _buildStrip;

        internal KitTitle KitTitle => _kitTitle;

        internal RewardCard RewardCard => _rewardCard;

        /// <summary>The guidance slot (tests): which element may show now.</summary>
        internal GuidanceSlot Slot => _slot;

        internal CargoScreen Cargo => _cargo;

        internal UnlockCard UnlockCard => _unlockCard;

        internal CrateNote CrateNote => _crateNote;



        internal FriendNameTag FriendName => _friendName;

        internal PlaceCard PlaceCard => _placeCard;

        internal RecoveryBoard Board => _board;

        internal ProtocolReadout Protocol => _protocol;

        internal RecipeCard RecipeCard => _recipeCard;

        internal LookUpChip LookUp => _lookUpChip;

        internal BackdropBlur Backdrop => _backdrop;

        /// <summary>The crew's map view, or null when composed without the map's services (tests).</summary>
        internal MapView Map => _map;

        internal NextLineView NextLine => _nextLine;

        internal BatteryDial BatteryDial => _batteryDial;

        internal HomeMeter HomeMeter => _homeMeter;


        internal UiLayout Layout => _layout;

        internal CursorPolicy Cursor => _cursor;

        internal UIDocument FaceDocument => _faceDocument;

        internal bool IsFacePanelVisible => _facePanelVisible;

        internal bool IsBound => _bound;

        /// <summary>What confirming Quit does; tests replace it so the editor keeps running.</summary>
        internal Action QuitAction { get; set; } = Application.Quit;

        /// <summary>The screen the Display page drives (Unity's, unless a test hands in its own).</summary>
        internal IDisplayControl DisplayControl { get; set; } = new UnityDisplayControl();

        internal void Wire(UIDocument document, UiTuning tuning, RelicCatalog relics, TextAsset[] stringTables,
            WordmarkShape wordmark, RoverDiagramData roverDiagram, TextAsset credits, UIDocument faceDocument = null)
        {
            _credits = credits;
            _document = document;
            _faceDocument = faceDocument;
            _wordmark = wordmark;
            _roverDiagram = roverDiagram;
            _tuning = tuning;
            _relics = relics;
            _stringTables = stringTables;
        }

        internal void WireStructureIcons(Texture2D[] icons)
        {
            _structureIcons = icons ?? Array.Empty<Texture2D>();
        }

        /// <summary>The crew's map (docs/features/M3-07): its sheet and its view's tuning.</summary>
        internal void WireMap(MapSheet sheet, MapViewTuning tuning)
        {
            _mapSheet = sheet;
            _mapTuning = tuning;
        }

        public void Initialize(GameContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!ValidateWiring() || !ResolveMap(context))
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

        /// <summary>
        /// The crew's map's services from Gameplay. A rig with neither the map's assets nor its knowledge composes
        /// without the map (UI's own test rigs); half of it is a wiring error.
        /// </summary>
        private bool ResolveMap(GameContext context)
        {
            bool known = context.TryGet(out _mapKnowledge);
            if (!known && _mapSheet == null && _mapTuning == null)
            {
                return true;
            }

            string problem = _mapSheet == null ? "the MapSheet is not assigned (run World/Map Sheet)."
                : _mapSheet.Validate() != null ? "MapSheet " + _mapSheet.Validate()
                : _mapTuning == null ? "MapViewTuning is not assigned (run UI/Map Tuning)."
                : !known ? "no IMapKnowledge is registered (Gameplay's MapSystem)."
                : !context.TryGet(out _mapReach) ? "no IStationReach is registered."
                : !context.TryGet(out _mapOverlay) ? "no IChartOverlay is registered (Gameplay's MapSystem)."
                : null;
            if (problem == null)
            {
                return true;
            }

            Debug.LogError($"{nameof(UISystem)}: {problem}", this);
            enabled = false;
            return false;
        }

        private bool ValidateWiring()
        {
            string problem = _document == null ? "the UIDocument is not assigned."
                : _document.visualTreeAsset == null ? "the UIDocument has no GameUI.uxml."
                : _document.panelSettings == null ? "the UIDocument has no PanelSettings."
                : _faceDocument != null && _faceDocument.panelSettings == null ? "the FaceUI UIDocument has no PanelSettings."
                : _tuning == null ? "UiTuning is not assigned (run UI/Tuning)."
                : _relics == null ? "the RelicCatalog is not assigned."
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
            _tickerLines = new TickerQueue(_tuning.Ticker, new TickerText(_localization));

            _settings = new SettingsStore(_player, services.Settings);
            _settings.Load();
            ISaveService save = services.Save;
            _tokens.Add(save.Register(new SaveSection<SettingsSaveData>(UiSaveKeys.Settings,
                UiSaveKeys.SettingsVersion, _player.Capture, _settings.Migrate)));
            _tokens.Add(save.Register(new SaveSection<PromptsSaveData>(UiSaveKeys.Prompts,
                UiSaveKeys.PromptsVersion, _ledger.Capture, _ledger.Restore)));

            EventBus events = services.Events;
            _events = events;
            _tokens.Add(events.Subscribe<MaterialsChanged>(OnMaterialsChanged));
            _tokens.Add(events.Subscribe<RelicDeposited>(OnRelicDeposited));
            _tokens.Add(events.Subscribe<RelicStowed>(OnRelicStowed));
            _tokens.Add(events.Subscribe<RoverAwoke>(OnRoverAwoke));
            _tokens.Add(events.Subscribe<TitleShown>(OnTitleShown));
            _tokens.Add(events.Subscribe<FeelBeat>(OnFeelBeat));
            _tokens.Add(events.Subscribe<UpgradePurchased>(OnUpgradePurchased));
            _tokens.Add(events.Subscribe<CargoChanged>(OnCargoChanged));
            _tokens.Add(events.Subscribe<HomeStorageChanged>(_ => _cargo?.MarkDirty()));
            _tokens.Add(events.Subscribe<FriendPartCollected>(_ => _director.NotifyUsed(InteractionKind.PickUp)));
            _tokens.Add(events.Subscribe<CrewCrateOpened>(OnCrewCrateOpened));
            _tokens.Add(events.Subscribe<GameSaved>(OnGameSaved));
            _tokens.Add(events.Subscribe<StationNotYet>(OnStationNotYet));
            _tokens.Add(events.Subscribe<StargazingChanged>(OnStargazingChanged));
            _tokens.Add(events.Subscribe<NextStepChanged>(OnNextStepChanged));
            _tokens.Add(events.Subscribe<PlaceFirstApproached>(OnPlaceFirstApproached));
            _tokens.Add(events.Subscribe<ScanSiteReached>(OnScanSiteReached));
            _tokens.Add(events.Subscribe<FragmentScanned>(OnBoardFragmentScanned));
            _tokens.Add(events.Subscribe<LabEntered>(_ => _labHud.Enter()));
            _tokens.Add(events.Subscribe<LabLeft>(OnLabLeft));
            _tokens.Add(events.Subscribe<LabStationLooked>(OnLabStationLooked));
            _tokens.Add(events.Subscribe<TechNodeChanged>(_ =>
            {
                _board?.MarkDirty();
                _cargo?.MarkDirty();
            }));
            _tokens.Add(events.Subscribe<RecoveryProgressChanged>(OnRecoveryProgressChanged));
            _tokens.Add(events.Subscribe<ScanStarted>(_ => _director.Yield(InteractionKind.Scan)));
            _tokens.Add(events.Subscribe<FragmentScanned>(OnFragmentScanned));
            _tokens.Add(events.Subscribe<BlueprintCompleted>(OnBlueprintCompleted));
            _tokens.Add(events.Subscribe<RoverWideShotChanged>(OnWideShotChanged));
            _tokens.Add(events.Subscribe<RoverWakeBeatReached>(OnWakeBeat));
            _tokens.Add(events.Subscribe<SiteAnswered>(OnSiteAnswered));
            _tokens.Add(events.Subscribe<ExcavationStarted>(OnExcavationStarted));
            _tokens.Add(events.Subscribe<ExcavationStopped>(OnExcavationStopped));
            _tokens.Add(events.Subscribe<TetherAttached>(OnTetherAttached));
            _tokens.Add(events.Subscribe<LanguageChanged>(OnLanguageChanged));
            _tokens.Add(events.Subscribe<FriendRepairStarted>(OnFriendRepairStarted));
            _tokens.Add(events.Subscribe<FriendRepaired>(OnFriendRepaired));
            _tokens.Add(events.Subscribe<TickerLine>(OnTickerLine));
            _tokens.Add(events.Subscribe<VoiceLineStarted>(OnVoiceLineStarted));
            _tokens.Add(events.Subscribe<CrewLogFound>(OnCrewLogFound));
            _tokens.Add(events.Subscribe<CassetteCollected>(OnCassetteCollected));
            _tokens.Add(events.Subscribe<RadioProgramChanged>(OnRadioProgramChanged));
            _tokens.Add(events.Subscribe<RelayRestored>(OnRelayRestored));
            _tokens.Add(events.Subscribe<RadioHopListChanged>(OnRadioHopListChanged));
            _tokens.Add(events.Subscribe<StationCued>(OnStationCued));
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
            _labHud = new LabHud(_layout.LabHud, _tuning.Lab, _localization, _ledger);
            _board = new RecoveryBoard(_layout.LabHud, _tuning.Lab.Board, _localization, services.Tech,
                services.Recovery, services.ScanSites);
            _stargaze = new StargazeVeil(_tuning.Stargaze.Hud, _layout.PromptAnchor,
                _layout.FriendNameAnchor, _layout.RelayTagAnchor, _layout.SalvageRingAnchor, _layout.SiteNameAnchor,
                _layout.BuildReasonAnchor, _layout.TickerLine);
            _lookUpHint = new LookUpHint(_tuning.Stargaze, _ledger);
            _holdHint = new HoldHint(_tuning.Stargaze, _ledger);
            _fitHint = new FitHint(_tuning.Stargaze, _ledger, services.Equipment);
            _firstPrompt = new FirstPrompt(_tuning.Stargaze, _ledger);
            _lookUpChip = new LookUpChip(_layout, _tuning.Stargaze, _localization);
            _nextStep = new NextStepLine(_tuning.Guidance);
            _nextLine = new NextLineView(_layout, _tuning.Guidance, _localization);
            _lowBattery = new LowBatteryHint(_tuning.Guidance, services.Battery, _localization);
            _batteryDial = new BatteryDial(_layout, _tuning.Battery, services.Battery, services.Beam);
            _homeMeter = new HomeMeter(_layout, _tuning.HomeMeter, services.Rover, services.World, _localization);
            _saveTick = new SaveTick(_layout, _tuning.SaveTick);
            _placeCard = new PlaceCard(_layout, _tuning.Guidance, _localization, services.Events);
            _protocol = new ProtocolReadout(_layout.Hud, _tuning.Guidance, _localization, services.Recovery);
            _recipeCard = new RecipeCard(_layout.Hud, _tuning.Guidance.Recipe, _localization, _numbers, services.Shop,
                services.Materials, services.RecipeLook);
            _reticle = new TetherReticle(_layout.Reticle, _layout.ReticleRest, _layout.ReticleHover, _tuning.Reticle);
            _prompt = new ContextPrompt(_layout, _tuning.Prompts, _director, services.View, _glyphs, _localization,
                services.Events, new[] { _layout.RecipeCardRoot, _layout.TowerPanel, _layout.BuildStrip });
            _pickups = new PickupFeed(_layout, _tuning.PickupFeed, _numbers, _localization, services.Materials,
                services.Catalog, services.Events);
            _salvageRing = new SalvageRing(_layout, _tuning.Salvage, _tuning.Prompts, services.Salvage, services.View);
            _siteName = new SiteNameTag(_layout, _tuning.Salvage, _tuning.Prompts, _localization, services.View);
            _card = new MemoryCard(_layout, _tuning.MemoryCard, _localization, services.Events, _relics);
            _friendName = new FriendNameTag(_layout, _tuning.Friends, _tuning.Prompts, services.Friends,
                _localization, services.View);
            _ticker = new RadioTicker(_layout, _tuning.Ticker, _tickerLines);
            _dial = new DialReadout(_layout, _tuning.DialReadout, _localization, services.Radio);
            _hopList = new HopList(_layout, _tuning.Relays, _localization, services.Hop);
            _hopFade = new HopFade(_layout, _tuning.Relays, services.Hop);
            _relayTag = new RelayTag(_layout, _tuning.Relays, _tuning.Prompts, services.Hints, services.Relays,
                services.Materials, _localization, services.View, _numbers);
            _tower = new TowerPanel(_layout, _tuning.TowerPanel, _localization, services.Events, services.Shop,
                services.Board, services.Materials, services.Hints, _numbers);
            _buildStrip = new BuildStripView(_layout, _tuning.TowerPanel.Reveal, _tuning.Prompts, _localization,
                _numbers, services.Materials, services.View, services.BuildField, _structureIcons, _glyphs);
            _kitTitle = new KitTitle(_layout, _tuning.KitTitle, _localization);
            _rewardCard = new RewardCard(_layout, _tuning.RewardCard, _localization, services.Shop);
            _cargo = new CargoScreen(_layout, _tuning.Cargo, _localization, services.Hold,
                services.Home, services.Transfer, services.Dock, services.Equipment, services.Portrait, _roverDiagram,
                _glyphs, _numbers, services.Events, _ledger);
            BindFacePanel(root);
            _unlockCard = new UnlockCard(_layout, _tuning.UnlockCard, _localization, _glyphs);
            _crateNote = new CrateNote(_layout, _tuning.CrateNote, _localization);
            _pause = new PauseMenu(_layout, _tuning.Pause, _tuning.Title, _player, _localization, services.Input,
                services.Events, _settings, services.Radio, services.Relays, services.Flow,
                _numbers, _cursor, Quit,
                new CreditsView(_layout, _tuning.Credits, _localization, _credits.text));
            _textScale = new TextScale(_document.panelSettings);
            _backdrop = new BackdropBlur(transform, _tuning.Backdrop);
            if (_mapKnowledge != null)
            {
                _map = new MapView(root.Q("pause"), _mapSheet, _mapTuning, _mapKnowledge, _mapReach, _mapOverlay,
                    _localization, services.Events, services.Input, services.ScanSites);
            }

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
            else if ((pausePressed || cancelPressed) && _card.CanDismiss)
            {
                _card.Dismiss();
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
            _map?.Tick(deltaTime, paused);
            SetFacePanelVisible(_cargo.IsOpen || _cargo.Openness > 0f);
            _cargo.Tick(deltaTime, CargoScreenSize(), input.ActiveDevice);
            if (_cargo.IsOpen && !_cargo.IsBooting)
            {
                StepCargoInput(menu);
            }

            if (_cargo.IsOpen && ((_cargo.IsTransferView && !_services.Dock.IsDocked) || _cargo.WantsClose))
            {
                _reopenCargo = _cargo.WantsClose;
                CloseCargo();
            }

            SetFacePanelVisible(_cargo.IsOpen || _cargo.Openness > 0f);
            bool onMap = (_map != null && _map.IsOpen) || _cargo.IsOpen;
            _backdrop.Set(Mathf.Max(_pause.Openness, _map != null ? _map.Openness : 0f), _cargo.Openness);
            float hudTime = paused ? 0f : deltaTime;
            _hud.Set(!paused && !onMap && !_labHud.Inside);
            _hud.Tick(deltaTime);
            _saveTick.Tick(deltaTime);
            _stargaze.Tick(deltaTime);
            if (_awake)
            {
                _sinceAwake += hudTime;
            }

            InputDeviceKind device = input.ActiveDevice;
            _textScale.Apply(_player.TextScale);
            _labHud.Tick(deltaTime, !paused && !onMap, input.Drive,
                _services.Hints.TryGet(InteractionKind.Deposit, out _), _glyphs.For(RoverAction.Tether, device), device);
            _board.Tick(deltaTime, !paused);
            bool hopping = _services.Hop.Phase >= RadioHopPhase.Leaving || _hopFade.IsVisible;
            _reticle.Tick(deltaTime, _services.Tether.State, input.TetherHeld);
            _nextStep.Tick(hudTime, _services.Rover.Speed, _player.Guidance);
            _lowBattery.Tick(paused ? 0f : deltaTime, _nextLine.ShowsBattery);
            DecideGuidance(paused, hopping);
            _card.Tick(hudTime, _glyphs.Cancel(device), device,
                !_ticker.IsVisible && !hopping && !_hopList.IsBusy && !_placeCard.IsVisible && !_prompt.IsVisible &&
                !_rewardCard.IsVisible && !_lookUpChip.IsVisible);
            _tower.Tick(hudTime, !paused && _slot.Allows(GuidanceKind.Card), input.ExcavateHeld, input.Winch,
                _services.Tether.State == TetherAimState.Towing, _glyphs.For(RoverAction.Excavate, device), device);
            _kitTitle.Tick(hudTime, !_placeCard.IsVisible && !GarageBeatActive() && !_nextLine.IsVisible);
            _rewardCard.Tick(hudTime, !_placeCard.IsVisible && !_kitTitle.IsVisible && !_card.IsVisible);
            _crateNote.Tick(hudTime, !_card.IsVisible && !_rewardCard.IsVisible && !_unlockCard.IsVisible && !onMap);
            _unlockCard.Tick(hudTime, !_placeCard.IsVisible && !_kitTitle.IsVisible && !_kitTitle.IsWaiting &&
                                      !_card.IsVisible && !_rewardCard.IsVisible && !_rewardCard.IsWaiting && !onMap &&
                                      !GarageBeatActive() && !_nextLine.IsVisible,
                device);
            _pickups.Tick(deltaTime);
            bool cutting = _services.Salvage.IsCutting;
            if (cutting)
            {
                _director.Yield(InteractionKind.Salvage);
            }

            if (_director.Displayed == InteractionKind.Reel && _director.WantsShown && input.Winch != 0f)
            {
                _director.NotifyUsed(InteractionKind.Reel);
            }

            Rect panel = _layout.Root.layout;
            Vector2 panelSize = float.IsNaN(panel.width) ? Vector2.zero : panel.size;
            _buildStrip?.Tick(hudTime, !paused && _awake && !onMap && !_labHud.Inside, panelSize, device);
            _friendName.Tick(hudTime, panelSize);
            _siteName.Tick(hudTime, panelSize, !_prompt.IsVisible && !_placeCard.IsVisible &&
                                               !_lookUpChip.IsVisible && !_ticker.IsVisible && !_kitTitle.IsVisible &&
                                               !_rewardCard.IsVisible && !GarageBeatActive() && !_nextLine.IsVisible);
            _pickUpGrace = _services.Hints.Primary.Kind == InteractionKind.PickUp
                ? PickUpGraceSeconds
                : Mathf.Max(0f, _pickUpGrace - deltaTime);
            // A standing prompt gives way to a waiting card or site name: those moments pass, the prompt returns.
            _prompt.Tick(deltaTime, PromptsOpen(paused, hopping) && _slot.Allows(GuidanceKind.Prompt),
                _services.Hints.Primary, device, panelSize);
            bool tagOpen = !paused && _awake && !_card.IsVisible && !_tower.IsVisible &&
                           !_hopList.IsBusy && !hopping;
            _relayTag.Tick(deltaTime, tagOpen, panelSize,
                _prompt.StackHeight(InteractionKind.Restore, _tuning.Relays.TagStackGap));
            _salvageRing.Tick(deltaTime, tagOpen, panelSize);

            if (_services.Hop.Phase == RadioHopPhase.Choosing && _dial.IsVisible)
            {
                _dial.MakeWay();
            }

            _hopList.Tick(deltaTime, !_dial.IsVisible && !_prompt.IsVisible, _glyphs.For(RoverAction.Excavate, device),
                device);
            _dial.Tick(hudTime, !_prompt.IsVisible && !_hopList.IsBusy);
            _ticker.Tick(hudTime, TickerOpen(hopping) && _slot.Allows(GuidanceKind.Ticker));
            if (_tickerLines.Starts != _tickerStartsAnnounced)
            {
                _tickerStartsAnnounced = _tickerLines.Starts;
                _events.Publish(new TickerLineShown(_tickerLines.Line.Key));
            }
            _hopFade.Tick(deltaTime);
            _protocol.Tick(hudTime);
            // Its own zone (right of centre): it stands beside a place card, so the tower's card never hides it.
            _recipeCard.Tick(hudTime, _awake && !_kitTitle.IsVisible && CardsOpen(paused) &&
                                      _slot.Allows(GuidanceKind.Card));
            // The look-up chip is the gentlest of the prompts: a teaching prompt that wants the screen takes it first.
            bool hintOpen = LookHintsOpen() && _slot.Allows(GuidanceKind.Prompt) &&
                            !_director.Wants(_services.Hints.Primary);
            float stick = _tuning.Stargaze.FirstPromptStick * _tuning.Stargaze.FirstPromptStick;
            bool looking = !paused && (input.LookDelta.sqrMagnitude > 0f || input.LookRate.sqrMagnitude > stick);
            _firstPrompt.Tick(hudTime, hintOpen, looking, !paused && input.Drive.sqrMagnitude > stick);
            _lookUpHint.Tick(hudTime, hintOpen && _firstPrompt.Word == HintWord.None);
            _holdHint.Tick(hudTime, hintOpen && !_cargo.IsOpen && _firstPrompt.Word == HintWord.None &&
                                    !_lookUpHint.WantsShown, AtTower());
            _fitHint.Tick(hudTime, hintOpen && !_cargo.IsOpen && _firstPrompt.Word == HintWord.None &&
                                   !_lookUpHint.WantsShown && !_holdHint.WantsShown, _cargo.IsOpen);
            HintWord hint = _firstPrompt.Word != HintWord.None ? _firstPrompt.Word
                : _lookUpHint.WantsShown ? HintWord.LookUp
                : _holdHint.WantsShown ? HintWord.Hold
                : _fitHint.WantsShown ? HintWord.Fit : HintWord.None;
            _lookUpChip.Tick(deltaTime, paused ? HintWord.None : hint, device);
            _placeCard.Tick(hudTime, PlaceCardsOpen() && _slot.Allows(GuidanceKind.Place));
            _nextLine.Tick(deltaTime, _nextStep, NextLineOpen(paused) && _slot.Allows(GuidanceKind.Next), _lowBattery,
                NextLineOpen(paused) && _slot.Allows(GuidanceKind.Battery));
            bool hudOpen = !paused && _awake && !_stargaze.IsVeiled && !onMap;
            _batteryDial.Tick(deltaTime, _cellRowRevealed, hudOpen);
            _homeMeter.Tick(deltaTime, hudOpen && !_card.IsVisible);
        }

        /// <summary>The cell row exists from the first time the charge dropped below its reveal share.</summary>
        private void OnBatteryArcRevealed(BatteryArcRevealed revealed)
        {
            _cellRowRevealed = true;
        }

        /// <summary>07 is at the tower, offered its build (the chip that teaches Tab waits for this).</summary>
        private bool AtTower()
        {
            return _awake && _tower.AtRadioTower;
        }

        /// <summary>True while 07 is in the garage beat (steps a-d) and the step line names the exact action.</summary>
        private bool GarageBeatActive()
        {
            return _awake && _player.Guidance && ProgressionKeys.IsGarageBeat(_nextStep.Key);
        }

        /// <summary>Everything but the slot that lets the teaching prompt show.</summary>
        private bool PromptsOpen(bool paused, bool hopping)
        {
            return !paused && _awake && _sinceAwake >= _tuning.Prompts.StartDelay && !_card.IsBusy &&
                   !_dial.IsBusy && !_hopList.IsBusy && !hopping && !_siteName.IsVisible && !_siteName.IsWaiting &&
                   !_kitTitle.IsVisible && !_lookUpChip.IsVisible && !GarageBeatActive() &&
                   (_buildStrip == null || !_buildStrip.IsOpen);
        }

        private bool CardsOpen(bool paused)
        {
            return !paused && !_protocol.IsVisible && !_card.IsVisible && !_stargaze.IsVeiled &&
                   !_tickerLines.HasKey(UiKeys.RowFull);
        }

        private bool PlaceCardsOpen()
        {
            return _awake && !_protocol.IsVisible && !_kitTitle.IsVisible && !_card.IsVisible &&
                   !_rewardCard.IsVisible && !_siteName.IsVisible && !GarageBeatActive() &&
                   (_buildStrip == null || !_buildStrip.IsOpen);
        }

        private bool TickerOpen(bool hopping)
        {
            return _awake && !_card.IsBusy && !_digging && !_services.Salvage.IsCutting &&
                   !_reticle.IsVisible && !_dial.IsBusy && !_hopList.IsBusy && !hopping &&
                   !_siteName.IsVisible && !_kitTitle.IsVisible && !_rewardCard.IsVisible && !_rewardCard.IsWaiting &&
                   !GarageBeatActive() && (_buildStrip == null || !_buildStrip.IsOpen);
        }

        private bool NextLineOpen(bool paused)
        {
            return !paused && _awake && !_stargaze.IsVeiled && !_protocol.IsVisible && !_rewardCard.IsVisible &&
                   !_kitTitle.IsVisible && !_unlockCard.IsVisible && (_buildStrip == null || !_buildStrip.IsOpen);
        }

        private bool LookHintsOpen()
        {
            return !_protocol.IsVisible && !_card.IsVisible && !_kitTitle.IsVisible && !_dial.IsBusy &&
                   !_hopList.IsBusy && !_siteName.IsVisible && !GarageBeatActive() &&
                   (_buildStrip == null || !_buildStrip.IsOpen);
        }

        /// <summary>
        /// One guidance element at a time (docs/design/0.5.5-owner-feedback.md §4.1): the teaching prompt, then a
        /// station or recipe card, a place card, the low-battery hint, the radio ticker, and the next line. Each wants
        /// the slot when it could show; the slot goes to the highest and passes on once the screen is clear.
        /// </summary>
        private void DecideGuidance(bool paused, bool hopping)
        {
            bool looks = LookHintsOpen() && (_firstPrompt.Word != HintWord.None || _lookUpHint.WantsShown ||
                                             _holdHint.Wants(AtTower()) || _fitHint.Wants());
            _guidanceWants[(int)GuidanceKind.Prompt] =
                (PromptsOpen(paused, hopping) && _director.Wants(_services.Hints.Primary)) || looks;
            _guidanceWants[(int)GuidanceKind.Card] =
                CardsOpen(paused) && (_tower.Wants || (_awake && !_kitTitle.IsVisible && _recipeCard.Wants));
            _guidanceWants[(int)GuidanceKind.Place] = PlaceCardsOpen() && (_placeCard.IsWaiting || _placeCard.IsVisible);
            _guidanceWants[(int)GuidanceKind.Battery] = NextLineOpen(paused) && _lowBattery.WantsShown;
            _guidanceWants[(int)GuidanceKind.Ticker] = TickerOpen(hopping) && _tickerLines.WantsSlot;
            _guidanceWants[(int)GuidanceKind.Next] = NextLineOpen(paused) && _nextStep.WantsShown;
            _guidanceShown[(int)GuidanceKind.Prompt] = _prompt.IsVisible || _lookUpChip.IsVisible;
            _guidanceShown[(int)GuidanceKind.Card] = _tower.IsVisible || _recipeCard.IsVisible;
            _guidanceShown[(int)GuidanceKind.Place] = _placeCard.IsVisible;
            _guidanceShown[(int)GuidanceKind.Battery] = _nextLine.ShowsBattery;
            _guidanceShown[(int)GuidanceKind.Ticker] = _ticker.IsVisible;
            _guidanceShown[(int)GuidanceKind.Next] = _nextLine.ShowsNext;
            _slot.Step(_guidanceWants, _guidanceShown);
        }

        private Vector2 _lastFaceMouseScreenPos = new Vector2(float.NaN, float.NaN);

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

        private void StepCargoInput(MenuInput menu)
        {
            FaceInventoryView view = _cargo.View;
            if (menu.NavigatePressed && view.LastNavigationFrame != Time.frameCount)
            {
                Vector2 nav = menu.NavigateVector;
                NavigationMoveEvent.Direction dir = Mathf.Abs(nav.x) >= Mathf.Abs(nav.y)
                    ? (nav.x > 0.2f ? NavigationMoveEvent.Direction.Right
                        : nav.x < -0.2f ? NavigationMoveEvent.Direction.Left
                        : NavigationMoveEvent.Direction.None)
                    : (nav.y > 0.2f ? NavigationMoveEvent.Direction.Up
                        : nav.y < -0.2f ? NavigationMoveEvent.Direction.Down
                        : NavigationMoveEvent.Direction.None);
                if (dir != NavigationMoveEvent.Direction.None)
                {
                    view.Navigate(dir);
                }
                else if (view.FocusedHoldCell < 0 && view.FocusedSocketIndex < 0 && view.FocusedHomeCell < 0)
                {
                    _cargo.FocusFirst();
                }
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
            return _awake && !_pause.IsOpen && !(_map != null && _map.IsOpen);
        }

        private void OpenCargo()
        {
            _reopenCargo = false;
            _lastFaceMouseScreenPos = _services.Input.Menu.PointerPosition;
            SetFacePanelVisible(true);
            _cargo.Open();
            _services.FaceScreen?.SetScreenOpen(true, FaceTargetTexture());
            _services.Input.Disable();
            _cursor.Menu();
            _cargo.FocusFirst();
            _events.Publish(new UiCue(UiCueKind.Confirm));
        }

        private void CloseCargo()
        {
            _cargo.Close();
            _services.FaceScreen?.SetScreenOpen(false, FaceTargetTexture());
            _services.Input.Enable();
            _cursor.Drive();
            _events.Publish(new UiCue(UiCueKind.Back));
        }

        private void Quit()
        {
            QuitAction();
        }

        private void OnMaterialsChanged(MaterialsChanged changed)
        {
            if (_bound)
            {
                if (changed.Metal + changed.Wiring + changed.Optics > 0)
                {
                    _ledger.RecordMet(HudVerb.Material);
                }

            }
        }

        private void OnSiteAnswered(SiteAnswered answered)
        {
            if (_bound)
            {
                _siteName.Answered(answered.SiteId, answered.Position);
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

        /// <summary>A prompt retires when its verb completes: the hold gaining a piece just after the pick-up chip.</summary>
        private void OnCargoChanged(CargoChanged changed)
        {
            _cargo?.MarkDirty();
            if (_pickUpGrace > 0f)
            {
                _director.NotifyUsed(InteractionKind.PickUp);
            }
        }

        private void OnRelicStowed(RelicStowed stowed)
        {
            _director.NotifyUsed(InteractionKind.Stow);
        }

        private void OnRoverAwoke(RoverAwoke awoke)
        {
            _awake = true;
            if (_bound)
            {
                if (_journey.TryTake(_services.Save.LoadResult, out TickerLine line))
                {
                    _tickerLines.Enqueue(line);
                }
            }
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

        private void OnGameSaved(GameSaved saved)
        {
            if (_bound)
            {
                _saveTick.Saved();
            }
        }

        /// <summary>A sleeping station answered the sonar: its line on what wakes it goes to the ticker, once.</summary>
        private void OnStationNotYet(StationNotYet notYet)
        {
            // The dark tower answers a listen with its plaque lamp and a tone, never words; every other not-yet notice
            // is guidance too: queued on the ticker, where it waits for the guidance slot.
            if (notYet.ReasonKey != UiKeys.NotYetTower && _notYet.ShouldSay(notYet.ReasonKey))
            {
                _tickerLines.Enqueue(new TickerLine(notYet.ReasonKey));
            }
        }

        private void OnFeelBeat(FeelBeat beat)
        {
            if (_bound)
            {
                _salvageRing.OnBeat(beat);
                _rewardCard.OnBeat(beat);
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

        private void OnCrewCrateOpened(CrewCrateOpened opened)
        {
            if (_bound)
            {
                _crateNote.Opened(opened.SiteId);
            }
        }

        private void OnUpgradePurchased(UpgradePurchased purchased)
        {
            if (_bound)
            {
                _rewardCard.Purchased(purchased.UpgradeId, purchased.Level);
            }
        }

        private void OnStargazingChanged(StargazingChanged changed)
        {
            if (_bound)
            {
                _stargaze.SetStargazing(changed.IsStargazing);
                _lookUpHint.SetStargazing(changed.IsStargazing);
            }
        }

        /// <summary>A place's line, the first time near it; the lab's own shows on the lab's screen.</summary>
        private void OnPlaceFirstApproached(PlaceFirstApproached approached)
        {
            if (!_labHud.TryShowPlace(approached.PlaceId))
            {
                _placeCard.Approached(approached.PlaceId);
            }
        }

        /// <summary>"Fragment 2 of 3 · Cargo Cradle", or the known blueprint's metal, as a note.</summary>
        private void OnFragmentScanned(FragmentScanned scanned)
        {
            _director.NotifyUsed(InteractionKind.Scan);
            if (_bound)
            {
                _crateNote.Scanned(ScanLines.Fragment(_localization, scanned));
            }
        }

        private void OnLabLeft(LabLeft left)
        {
            _labHud?.Leave();
            _board?.Look(false);
        }

        /// <summary>Looking at the tech board shows its labels; looking away hides them (M3-21 §2).</summary>
        private void OnLabStationLooked(LabStationLooked looked)
        {
            if (looked.Station == LabStation.Board)
            {
                _board?.Look(looked.Looking);
            }
        }

        /// <summary>A completed row's quiet ticker line, never on a load (M3-21 §2).</summary>
        private void OnRecoveryProgressChanged(RecoveryProgressChanged changed)
        {
            _board?.MarkDirty();
            // The power target's line is the reward card's "home has power" a moment earlier: not said twice.
            if (!changed.Restored && changed.Lamps > 0 && changed.LitLamps >= changed.Lamps &&
                changed.Target != RecoveryTarget.Power)
            {
                _tickerLines.Enqueue(new TickerLine(UiKeys.RecoveryComplete(changed.Target)));
            }
        }

        /// <summary>A scan site's name and remaining fragment count, the first time 07 reaches it.</summary>
        private void OnScanSiteReached(ScanSiteReached reached)
        {
            _placeCard?.Reached(reached);
        }

        /// <summary>Updates the lab board's torn sheets when a blueprint fragment is scanned.</summary>
        private void OnBoardFragmentScanned(FragmentScanned scanned)
        {
            _board?.OnFragmentScanned(scanned);
        }

        /// <summary>The last fragment is in: one ticker line names the kit and where to build it.</summary>
        private void OnBlueprintCompleted(BlueprintCompleted completed)
        {
            _tickerLines.Enqueue(new TickerLine(UiKeys.ScanComplete, UiKeys.UpgradeName(completed.BlueprintId), true));
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
                _firstPrompt.OpeningBegan();
                _protocol.OpeningBegan();
            }
            else if (reached.Beat == WakeBeat.GazeEarth)
            {
                _protocol.Cue();
            }
            else if (reached.Beat == WakeBeat.ChaseFrame)
            {
                _protocol.Cue();
                _firstPrompt.ChaseFrameReached();
            }
        }

        private void OnWideShotChanged(RoverWideShotChanged changed)
        {
            if (_bound)
            {
                _lookUpHint.SetWide(changed.Wide);
            }
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
            _ledger.RecordMet(HudVerb.Tape);
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
            _ledger.RecordMet(HudVerb.LitMast);
        }

        private void OnRadioHopListChanged(RadioHopListChanged changed)
        {
            if (changed.Open)
            {
                _director.NotifyUsed(InteractionKind.Hop);
            }
        }

        private void OnStationCued(StationCued cued)
        {
            if (_bound && cued.Cue == StationCue.StitchStarted)
            {
                _tower.StationShowed(UpgradeStationKind.RadioTower, cued.UpgradeId);
            }
        }

        private void OnPartFitted(PartFitted fitted)
        {
            if (!PartNames.TryTitle(fitted.Part, out string key))
            {
                Debug.LogError($"{nameof(UISystem)}: {fitted.Part} settled onto 07 (gift: {fitted.Gift}) with no " +
                               "upgrade or gift to name it.", this);
                return;
            }

            if (!_bound)
            {
                return;
            }

            _kitTitle.Enqueue(key);
            if (AbilityTable.TryAbilityOf(fitted.Part, out RoverAbility gained))
            {
                _unlockCard.Gained(gained);
                _cargo.MarkDirty();
            }

            if (!fitted.Gift)
            {
                _ledger.RecordFitTaught();
            }

            if (!fitted.Gift && PartNames.TryUpgradeId(fitted.Part, out string upgradeId))
            {
                _tower.StationShowed(UpgradeStationKind.Workshop, upgradeId);
            }
        }

        private void OnTickerLine(TickerLine line)
        {
            // Gameplay's low-charge line is guidance (priority 4 of the slot), never a voice on the ticker.
            if (LowBatteryHint.IsLowLine(line))
            {
                _lowBattery.Offer(line);
                return;
            }

            _tickerLines.Enqueue(line);
        }

        /// <summary>The ticker's line is a recording's subtitle: it stays up until the voice has finished.</summary>
        private void OnVoiceLineStarted(VoiceLineStarted started)
        {
            _tickerLines.HoldAtLeast(started.Key, started.Seconds + _tuning.Ticker.VoiceTailSeconds);
        }

        /// <summary>A sleeping station's "not yet": the ticker says what wakes it (Audio plays the tone).</summary>
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
            _homeMeter.Relocalize();
            _lookUpChip.Relocalize();
            _lowBattery.Relocalize();
            _nextLine.Relocalize(_nextStep, _lowBattery);
            _placeCard.Relocalize();
            _protocol.Relocalize();
            _recipeCard.Relocalize();
            _labHud.Relocalize();
            _board.Relocalize();
            _dial.Relocalize();
            _hopList.Relocalize();
            _friendName.Relocalize();
            _tower.Relocalize();
            _buildStrip?.Relocalize();
            _kitTitle.Relocalize();
            _pickups.Relocalize();
            _rewardCard.Relocalize();
            _cargo.Relocalize();
            _unlockCard.Relocalize();
            _crateNote.Relocalize();
            _relayTag.Relocalize();
            _siteName.Relocalize();
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
                _hopFade.Dispose();
                _backdrop.Dispose();
                _map?.Dispose();
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
