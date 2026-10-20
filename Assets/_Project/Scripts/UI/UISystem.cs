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
        private ControlsCard _controlsCard;
        private ControlsCardView _controlsView;
        private readonly ObjectiveChip _objectiveChip = new ObjectiveChip();
        private ObjectiveLine _objective;
        private ObjectiveView _objectiveView;
        private LowBatteryHint _lowBattery;
        private BatteryDial _batteryDial;
        private bool _cellRowRevealed;
        private bool _carrying;
        private bool _scanning;
        private SaveTick _saveTick;
        private ReadoutView _readout;
        private SunArcView _sunArc;
        private readonly OpeningProtocol _openingProtocol = new OpeningProtocol();
        private ContextPrompt _prompt;
        private PickupFeed _pickups;
        private SalvageRing _salvageRing;
        private GuidanceSlot _slot;
        private readonly bool[] _guidanceWants = new bool[GuidanceSlot.KindCount];
        private readonly bool[] _guidanceShown = new bool[GuidanceSlot.KindCount];
        private CargoScreen _cargo;
        private UnlockCard _unlockCard;
        private PostLog _postLog;
        private PostView _postView;
        private bool _postOnCrt;
        private BootFace _bootFace;
        private BootFaceView _bootFaceView;
        private readonly int[] _typedCued = new int[PostLog.Capacity];
        private readonly bool[] _lineCued = new bool[PostLog.Capacity];
        private CoverVignette _cover;
        private CoverVignettePainter _coverPainter;
        private float _coverPainted;
        private bool _titleSeen;
        private bool _titleUp;
        private bool _coverSettlePending;
        private BlockedCue _blockedCue;
        private PageFlip _pages;
        private PageTabs _tabs;
        private VisualElement[] _pageRoots;
        private VisualElement[] _pageChrome;
        private PageShutter _shutter;
        private DatabankPage _databank;
        private DatabankText _databankText;
        private float _databankLaidOutAt = -1f;
        private string _toastText;
        private float _toastLeft;
        private float _replyLeft;
        private bool _replyOnFace;
        private FaceNotice _faceNotice;
        private bool _noticeOnCrt;
        private CallDisplay _call;
        private CallDisplayView _callView;
        private bool _callShown;
        private bool _callAsLine;
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
        private bool _buildWasOpen;

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

        internal ReadoutView Readout => _readout;

        internal SunArcView SunArc => _sunArc;

        internal OpeningProtocol OpeningProtocol => _openingProtocol;

        internal LookUpChip LookUp => _lookUpChip;

        /// <summary>The first controls card after the opening, and its view (tests and captures).</summary>
        internal ControlsCard ControlsCard => _controlsCard;

        internal ControlsCardView ControlsCardView => _controlsView;

        internal BackdropBlur Backdrop => _backdrop;

        internal ObjectiveView Objective => _objectiveView;

        internal ObjectiveLine ObjectiveRule => _objective;

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
            _call = new CallDisplay(_tuning.CallDisplay);
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
            _tokens.Add(events.Subscribe<CallReplyFailed>(_ => _replyLeft = _tuning.Guidance.ReplyFaceSeconds));
            _tokens.Add(events.Subscribe<CallStateChanged>(OnCallStateChanged));
            _tokens.Add(events.Subscribe<CallDecoded>(OnCallDecoded));
            _tokens.Add(events.Subscribe<TitleShown>(OnTitleShown));
            _tokens.Add(events.Subscribe<NewGameRequested>(_ => OnNewGameRequested()));
            _tokens.Add(events.Subscribe<SessionStarted>(OnSessionStarted));
            _tokens.Add(events.Subscribe<ContinueRequested>(_ => OnTitleDismissed()));
            _tokens.Add(events.Subscribe<FeelBeat>(OnFeelBeat));
            _tokens.Add(events.Subscribe<CargoChanged>(OnCargoChanged));
            _tokens.Add(events.Subscribe<GameSaved>(OnGameSaved));
            _tokens.Add(events.Subscribe<StargazingChanged>(OnStargazingChanged));
            _tokens.Add(events.Subscribe<NextStepChanged>(OnNextStepChanged));
            _tokens.Add(events.Subscribe<ScanStarted>(OnScanStarted));
            _tokens.Add(events.Subscribe<SalvageCutStarted>(_ => _objectiveChip.Used(ObjectiveVerb.Cut)));
            _tokens.Add(events.Subscribe<FragmentScanned>(OnFragmentScanned));
            _tokens.Add(events.Subscribe<ScanStopped>(_ => _scanning = false));
            _tokens.Add(events.Subscribe<RoverWakeBeatReached>(OnWakeBeat));
            _tokens.Add(events.Subscribe<BootCheckStarted>(OnBootCheckStarted));
            _tokens.Add(events.Subscribe<BootCheckCompleted>(OnBootCheckCompleted));
            _tokens.Add(events.Subscribe<BootCheckFinished>(_ => _postLog.Finish()));
            _tokens.Add(events.Subscribe<BootCheckFastForwarded>(OnBootFastForwarded));
            _tokens.Add(events.Subscribe<CrtPowerOn>(OnCrtPowerOn));
            _tokens.Add(events.Subscribe<BootLogoBegan>(began => _bootFace?.LogoBegan(began.Seconds)));
            _tokens.Add(events.Subscribe<BootLoadBegan>(began => _bootFace?.LoadBegan(began.Seconds)));
            _tokens.Add(events.Subscribe<BootCheckPulse>(OnBootCheckPulse));
            _tokens.Add(events.Subscribe<LanguageChanged>(OnLanguageChanged));
            _tokens.Add(events.Subscribe<TickerLine>(OnTickerLine));
            _tokens.Add(events.Subscribe<RoverSystemChanged>(OnSystemChanged));
            _tokens.Add(events.Subscribe<CutterRefused>(_ => _readout?.CutterRefused()));
            _tokens.Add(events.Subscribe<OpeningMilestoneReached>(OnMilestoneReached));
            _tokens.Add(events.Subscribe<LanderReserveChanged>(OnLanderReserveChanged));
            _tokens.Add(events.Subscribe<StoryScanned>(OnStoryScanned));
            _tokens.Add(events.Subscribe<InteractionBlocked>(OnInteractionBlocked));
            _tokens.Add(events.Subscribe<DatabankEntryAdded>(OnDatabankEntryAdded));
            _tokens.Add(events.Subscribe<DatabankEntryProgressed>(_ => _databank?.Refresh()));
            _tokens.Add(events.Subscribe<StructureLearned>(OnStructureLearned));
            _tokens.Add(events.Subscribe<BatteryArcRevealed>(OnBatteryArcRevealed));
            _tokens.Add(events.Subscribe<StructurePlaced>(_ => _director.NotifyUsed(InteractionKind.Build)));
            _tokens.Add(events.Subscribe<StoryPartCollected>(collected => _pickups?.PartCollected(collected.PartId)));
            _tokens.Add(events.Subscribe<MaterialSalvaged>(OnMaterialSalvaged));
            _tokens.Add(events.Subscribe<StructureRemoved>(_ => _director.NotifyUsed(InteractionKind.Deconstruct)));

            services.Input.Menu.Enable();
            _cursor.Set(CursorMode.Drive);
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
            _lookUpChip = new LookUpChip(_layout, _tuning.Stargaze, _localization, _glyphs);
            _controlsCard = new ControlsCard(_tuning.ControlsCard);
            _controlsView = new ControlsCardView(_layout, _tuning.ControlsCard, _localization, _glyphs);
            _objective = new ObjectiveLine(_tuning.Guidance, _ledger);
            _objectiveView = new ObjectiveView(_layout, _tuning.Guidance, _localization);
            _slot = new GuidanceSlot(_tuning.Guidance.HandOverSeconds);
            _lowBattery = new LowBatteryHint(_tuning.Guidance, services.Battery, _localization);
            _batteryDial = new BatteryDial(_layout, _tuning.Battery, services.Battery, services.Beam);
            _saveTick = new SaveTick(_layout, _tuning.SaveTick);
            _readout = new ReadoutView(_layout.Hud, _tuning.Guidance, _localization, services.Systems,
                services.BuildField, _openingProtocol);
            _sunArc = new SunArcView(_layout, _tuning.Guidance, _localization, services.Sun);
            _prompt = new ContextPrompt(_layout, _tuning.Prompts, _director, services.View, _glyphs, _localization,
                services.Events, new[] { _layout.BuildStrip });
            _pickups = new PickupFeed(_layout, _tuning.PickupFeed, _numbers, _localization, services.Materials,
                services.Catalog, services.Events, _roverDiagram);
            _salvageRing = new SalvageRing(_layout, _tuning.Salvage, _tuning.Prompts, services.Salvage, services.View);
            _buildStrip = new BuildStripView(_layout, _tuning.BuildStripReveal, _tuning.Prompts, _localization,
                _numbers, services.Materials, services.View, _glyphs, services.Events, services.BuildField,
                _structureIcons, services.WeldEnergy);
            _cargo = new CargoScreen(_layout, _tuning.Cargo, _localization, services.Hold, services.Systems,
                _roverDiagram, _glyphs, _numbers, services.Events, _ledger);
            _postLog = new PostLog(_tuning.Post);
            BindFacePanel(root);
            // The face panel takes the cargo screen when there is one; the log sits beside it either way.
            _postView = new PostView(_layout.Cargo.parent, _postLog);
            BootFaceSettings boot = _tuning.BootFace;
            _bootFace = new BootFace(boot, boot.Linework.StrokeCount);
            _bootFaceView = new BootFaceView(_layout.Cargo.parent, _bootFace, boot.Linework, _localization);
            var coverElement = new VisualElement { name = "cover-vignette", pickingMode = PickingMode.Ignore };
            coverElement.AddToClassList("cover-vignette");
            root.Add(coverElement);
            _cover = new CoverVignette(boot);
            _coverPainter = new CoverVignettePainter(coverElement, _cover, boot);
            _faceNotice = new FaceNotice(_layout.Cargo.parent);
            _callView = new CallDisplayView(_layout.Cargo.parent, _tuning.CallDisplay, _localization);
            _blockedCue = new BlockedCue(_tuning.Guidance);
            _pages = new PageFlip(_tuning.Databank);
            _tabs = new PageTabs(_cargo.Panel, _localization);
            _tabs.SetShown(_faceDocument != null);
            _shutter = new PageShutter(_cargo.Panel, _tuning.Databank);
            _pageChrome = new[] { _tabs.Tab(0), _tabs.Tab(1), _tabs.Underline, _shutter.Edge, _shutter.Body };
            _pageRoots = new VisualElement[]
            {
                _tabs.Tab(0), _tabs.Tab(1), _tabs.Underline, _shutter.Edge, _shutter.Body, null,
            };
            ShowChange();
            _databankText = new DatabankText(_localization);
            _unlockCard = new UnlockCard(_layout, _tuning.UnlockCard, _localization);
            _pause = new PauseMenu(_layout, _tuning.Pause, _tuning.Title, _player, _localization, services.Input,
                services.Events, _settings, services.Flow,
                _numbers, Quit,
                new CreditsView(_layout, _tuning.Credits, _localization, _credits.text));
            _textScale = new TextScale(_document.panelSettings);
            _backdrop = new BackdropBlur(transform, _tuning.Backdrop);
            _bound = true;
            if (_coverSettlePending)
            {
                _coverSettlePending = false;
                SettleUnderCover();
            }

            if (_titleWaiting)
            {
                _titleWaiting = false;
                _pause.OpenTitle(_titleHasSave, !_titleHasSave, afterBoot: true);
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
            BuildModeState build = _buildStrip.State;
            // Esc is both Pause and Cancel: while build mode is open (or closed by this very press) it only leaves
            // build mode, which Gameplay does; the pad's Start, which is only Pause, still pauses.
            bool buildTookCancel = cancelPressed && (build != BuildModeState.Closed || _buildWasOpen);
            if (_pause.IsOpen)
            {
                if (pausePressed || cancelPressed)
                {
                    _pause.Back();
                }
            }
            else if (_cargo.IsOpen)
            {
                // On the databank page the pad's Y is the colour toggle, not the hand-over to build mode.
                if (menu.BuildPressed && _pages.Target == FacePage.Hold)
                {
                    CloseCargo();
                    _services.BuildField?.TryOpen();
                }
                else if (menu.CargoPressed || pausePressed || cancelPressed)
                {
                    CloseCargo();
                }
            }
            else if (menu.CargoPressed && CanOpenCargo())
            {
                // One context at a time: Tab leaves build mode (ghost gone, nothing lost) as it opens 07's screen.
                _services.BuildField?.Close();
                OpenCargo();
            }
            else if (pausePressed && !buildTookCancel)
            {
                _pause.Open();
            }

            if (menu.ClickPressed)
            {
                _cursor.Recapture();
            }

            _pause.Tick(deltaTime);
            StepCover(deltaTime);
            bool paused = _pause.IsOpen;
            StepPost(deltaTime);
            SetFacePanelVisible(_cargo.IsOpen || _cargo.Openness > 0f || FaceReadoutShown);
            _cargo.Tick(deltaTime, CargoScreenSize(), input.ActiveDevice);
            if (_cargo.IsOpen)
            {
                StepPages(menu, deltaTime, input.ActiveDevice);
                if (!_pages.IsFlipping && _pages.Page == FacePage.Hold)
                {
                    StepCargoInput(menu, deltaTime);
                }
                else if (!_pages.IsFlipping)
                {
                    StepDatabankInput(menu, deltaTime, input.ActiveDevice);
                }
            }

            SetFacePanelVisible(_cargo.IsOpen || _cargo.Openness > 0f || FaceReadoutShown);
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
            _objective.Tick(hudTime, _services.Rover.Speed, _player.Guidance, _objectiveView.ShowsObjective,
                _objectiveChip.AtHand(_services.Hints.Primary.Kind));
            _lowBattery.Tick(paused ? 0f : deltaTime, _objectiveView.ShowsBattery);
            float stick = _tuning.Stargaze.FirstPromptStick * _tuning.Stargaze.FirstPromptStick;
            bool looking = !paused && (input.LookDelta.sqrMagnitude > 0f || input.LookRate.sqrMagnitude > stick);
            bool cardOpen = ControlsCardOpen(paused, cargoOpen);
            _controlsCard.Tick(hudTime, cardOpen, _services.Rover.Speed * hudTime, looking, _player.Guidance);
            PromptHolder holder = DecideGuidance(paused, hudTime);
            _controlsView.Tick(deltaTime, holder == PromptHolder.Controls && _controlsCard.WantsShown && cardOpen,
                device);
            _unlockCard.Tick(hudTime, !cargoOpen && holder == PromptHolder.Unlock,
                _services.Rover.Speed >= _tuning.Guidance.StillSpeed || _buildStrip.State != BuildModeState.Closed);
            _pickups.Tick(deltaTime);
            bool cutting = _services.Salvage.IsCutting;
            if (cutting)
            {
                _director.Yield(InteractionKind.Salvage);
            }

            Rect panel = _layout.Root.layout;
            Vector2 panelSize = float.IsNaN(panel.width) ? Vector2.zero : panel.size;
            StepBuildPointer(menu);
            _buildStrip.Tick(hudTime, !paused && _awake && !cargoOpen, panelSize, device);
            _pickUpGrace = _services.Hints.Primary.Kind == InteractionKind.PickUp
                ? PickUpGraceSeconds
                : Mathf.Max(0f, _pickUpGrace - deltaTime);
            _prompt.Tick(deltaTime, PromptsOpen(paused), holder == PromptHolder.Chip, _services.Hints.Primary, device,
                panelSize);
            _salvageRing.Tick(deltaTime, !paused && _awake, panelSize);
            _readout.Tick(hudTime, holder == PromptHolder.Readout);
            // The look-up chip is the gentlest of the prompts: a teaching prompt that wants the screen takes it first.
            bool hintOpen = LookHintsOpen() && holder == PromptHolder.Chip &&
                            !_director.Wants(_services.Hints.Primary);
            _firstPrompt.Tick(hudTime, hintOpen, looking, !paused && input.Drive.sqrMagnitude > stick);
            _holdHint.Tick(hudTime, hintOpen && !cargoOpen && _firstPrompt.Word == HintWord.None, _awake && _carrying);
            HintWord hint = _firstPrompt.Word != HintWord.None ? _firstPrompt.Word
                : _holdHint.WantsShown ? HintWord.Hold : HintWord.None;
            if (BuildMenuOpen())
            {
                _objectiveChip.Used(ObjectiveVerb.Build);
            }

            // The verb chip is the objective's own: it shows only with its line, and a teaching prompt takes the chip.
            hint = hint != HintWord.None ? hint
                : _objectiveChip.Word(_objectiveView.ShowsObjective && _objective.WantsShown,
                    _services.Systems.CanFitIgniter, cargoOpen, OfferedNow(), HeldNow());
            InteractionHint aimed = _services.Hints.Primary;
            bool counting = hint == HintWord.Scan && aimed.Kind == InteractionKind.Scan;
            _lookUpChip.SetCount(counting ? aimed.SetStep : 0, counting ? aimed.SetCount : 0);
            _lookUpChip.Tick(deltaTime, paused ? HintWord.None : hint, device);
            _blockedCue.Tick(paused ? 0f : deltaTime);
            _toastLeft = Mathf.Max(0f, _toastLeft - (paused ? 0f : deltaTime));
            _replyLeft = Mathf.Max(0f, _replyLeft - (paused || !_replyOnFace ? 0f : deltaTime));
            _call.Step(hudTime, _services.Rover.Speed < _tuning.Guidance.StillSpeed, _callShown && _callAsLine);
            _lookUpChip.Nudge(_blockedCue.ChipOffset, _blockedCue.ChipOpacity);
            StepFaceNotice(hudTime);
            _objectiveView.Tick(deltaTime, _objective, ObjectiveOpen(paused) && _slot.Allows(GuidanceKind.Objective),
                _lowBattery, ObjectiveOpen(paused) && _slot.Allows(GuidanceKind.Battery), _buildStrip,
                BuildStepOpen(paused) && _slot.Allows(GuidanceKind.BuildStep));
            bool hudOpen = !paused && _awake && !_inOpening && !_stargaze.IsVeiled && !cargoOpen;
            _batteryDial.Tick(deltaTime, _cellRowRevealed, hudOpen);
            _sunArc.Tick(deltaTime, hudOpen && _openingProtocol.IsPowerOn);
            _buildWasOpen = _buildStrip.State != BuildModeState.Closed;
            ApplyCursor();
        }

        /// <summary>The gameplay's action in reach and ready now, or None.</summary>
        private InteractionKind OfferedNow()
        {
            InteractionHint primary = _services.Hints.Primary;
            return primary.Ready ? primary.Kind : InteractionKind.None;
        }

        /// <summary>
        /// What a held E is doing now (the cut's charge-up counts), or None. A scan counts only while E is down, so a
        /// scan whose end the UI missed can never outrank the target in reach.
        /// </summary>
        private InteractionKind HeldNow()
        {
            ISalvageStatus salvage = _services.Salvage;
            return salvage.IsCutting || salvage.IsBeamCharging ? InteractionKind.Salvage
                : _scanning && _services.Input.ExcavateHeld ? InteractionKind.Scan
                : InteractionKind.None;
        }

        /// <summary>The cursor follows what the mouse is for this frame (<see cref="CursorRule"/>).</summary>
        private void ApplyCursor()
        {
            _cursor.Set(CursorRule.Decide(_pause.IsOpen, _cargo.IsOpen, _buildStrip.State));
        }

        /// <summary>The cell row exists from the first time the charge dropped below its reveal share.</summary>
        private void OnBatteryArcRevealed(BatteryArcRevealed revealed)
        {
            _cellRowRevealed = true;
        }

        /// <summary>Everything but the slot that lets the teaching prompt show.</summary>
        private bool PromptsOpen(bool paused)
        {
            return !paused && _awake && !_inOpening && !_postLog.IsRunning &&
                   _sinceAwake >= _tuning.Prompts.StartDelay && !_lookUpChip.ShowsPrompt && !_readout.IsVisible &&
                   !BuildMenuOpen();
        }

        private bool ObjectiveOpen(bool paused)
        {
            return !paused && _awake && !_inOpening && !_postLog.IsRunning && !_stargaze.IsVeiled && !_cargo.IsOpen &&
                   !BuildMenuOpen();
        }

        /// <summary>07's face is free for the call: awake, live, and neither the self-test nor the hold has it.</summary>
        private bool CallFaceOpen(bool paused)
        {
            return !paused && _awake && !_inOpening && !_postOnCrt && !_cargo.IsOpen;
        }

        /// <summary>The build step's line shows over the live HUD only (not paused, not behind the face screen).</summary>
        private bool BuildStepOpen(bool paused)
        {
            return !paused && _awake && !_inOpening && !_cargo.IsOpen;
        }

        /// <summary>The controls card can be seen: the live HUD after the opening, nothing over it.</summary>
        private bool ControlsCardOpen(bool paused, bool cargoOpen)
        {
            return !paused && _awake && !_inOpening && !_stargaze.IsVeiled && !cargoOpen && !BuildMenuOpen();
        }

        private bool LookHintsOpen()
        {
            return !_postLog.IsRunning && !_readout.IsVisible && !BuildMenuOpen();
        }

        private bool ReadoutOpen(bool paused)
        {
            return !paused && !_postLog.IsRunning && !_stargaze.IsVeiled && !_cargo.IsOpen;
        }

        private bool BuildMenuOpen()
        {
            return _buildStrip != null && _buildStrip.IsOpen;
        }

        /// <summary>Which element of the prompt kind holds the screen: a card first, then a chip.</summary>
        private enum PromptHolder
        {
            None,
            Controls,
            Unlock,
            Readout,
            Chip,
        }

        /// <summary>
        /// One guidance element at a time (docs/design/opening-v2.md §5): a chip or card (the first controls card, the
        /// teaching prompt, the look/drive/hold chips, the blueprint card, 07's readout cards), then the low-battery
        /// hint, then the objective line. Each wants the slot when it could show; the slot goes to the highest and
        /// passes on once the screen is clear. Inside the prompt kind the one already up keeps the screen, else the
        /// controls card, the blueprint card, the readout and the chips take turns in that order.
        /// </summary>
        private PromptHolder DecideGuidance(bool paused, float deltaTime)
        {
            bool looks = LookHintsOpen() && (_firstPrompt.Word != HintWord.None || _holdHint.Wants(_awake && _carrying));
            bool chipWants = (PromptsOpen(paused) && _director.ReadyToShow) || looks;
            bool unlockWants = !_cargo.IsOpen && _unlockCard.IsWaiting;
            bool readoutWants = ReadoutOpen(paused) && _readout.Wants;
            bool controlsWants = _controlsCard.WantsShown && ControlsCardOpen(paused, _cargo.IsOpen);
            bool chipUp = _prompt.IsVisible || _lookUpChip.ShowsPrompt;
            PromptHolder holder = _controlsView.IsVisible ? PromptHolder.Controls
                : _unlockCard.IsVisible ? PromptHolder.Unlock
                : _readout.IsVisible ? PromptHolder.Readout
                : chipUp ? PromptHolder.Chip
                : controlsWants ? PromptHolder.Controls
                : unlockWants ? PromptHolder.Unlock
                : readoutWants ? PromptHolder.Readout
                : chipWants ? PromptHolder.Chip : PromptHolder.None;
            _guidanceWants[(int)GuidanceKind.CallLine] = _replyLeft > 0f || (_call.WantsChange && CallFaceOpen(paused));
            _guidanceShown[(int)GuidanceKind.CallLine] = _replyOnFace || (_callView.IsVisible && _callAsLine);
            _guidanceWants[(int)GuidanceKind.CallStatus] = ObjectiveOpen(paused) && _call.WantsStill;
            _guidanceShown[(int)GuidanceKind.CallStatus] = _callView.IsVisible && !_callAsLine;
            _guidanceWants[(int)GuidanceKind.BuildStep] = BuildStepOpen(paused) && _buildStrip.Step != BuildStep.None;
            _guidanceWants[(int)GuidanceKind.Prompt] = holder != PromptHolder.None;
            _guidanceWants[(int)GuidanceKind.Battery] = ObjectiveOpen(paused) && _lowBattery.WantsShown;
            _guidanceWants[(int)GuidanceKind.Objective] = ObjectiveOpen(paused) && _objective.WantsShown;
            _guidanceShown[(int)GuidanceKind.BuildStep] = _objectiveView.ShowsBuildStep;
            _guidanceShown[(int)GuidanceKind.Prompt] = chipUp || _unlockCard.IsVisible || _readout.IsVisible ||
                                                       _controlsView.IsVisible;
            _guidanceShown[(int)GuidanceKind.Battery] = _objectiveView.ShowsBattery;
            _guidanceShown[(int)GuidanceKind.Objective] = _objectiveView.ShowsObjective;
            _slot.Step(_guidanceWants, _guidanceShown, deltaTime);
            return _slot.Allows(GuidanceKind.Prompt) ? holder : PromptHolder.None;
        }

        private const float CargoNavInitialRepeatDelay = 0.35f;
        private const float CargoNavRepeatInterval = 0.12f;

        private Vector2 _lastFaceMouseScreenPos = new Vector2(float.NaN, float.NaN);
        private Vector2 _lastBuildMouse = new Vector2(float.NaN, float.NaN);
        private NavigationMoveEvent.Direction _lastCargoNavDirection = NavigationMoveEvent.Direction.None;
        private float _cargoNavRepeatTimer;

        /// <summary>True while 07's self-test log is on its CRT: through the checks and a short linger after.</summary>
        private bool PostShown => _postLog.IsRunning ||
                                  (_postLog.IsFinished && _postLog.SinceFinished < _tuning.Post.LingerSeconds);

        /// <summary>
        /// True while 07's CRT shows something of the UI's: the self-test (the face's boot, the lines and their linger),
        /// a face line or the call.
        /// </summary>
        private bool FaceReadoutShown => (_bootFace != null && _bootFace.IsShowing) || PostShown || _noticeOnCrt;

        /// <summary>07's face booting before the lines, and its view (tests and captures).</summary>
        internal BootFace BootFace => _bootFace;

        internal BootFaceView BootFaceView => _bootFaceView;

        /// <summary>The cover over a New Game that reloads (tests and captures).</summary>
        internal CoverVignette Cover => _cover;

        /// <summary>Internal view of the self-test log (tests and captures).</summary>
        internal PostLog Post => _postLog;

        /// <summary>The self-test column beside the cargo screen on the face panel (tests and captures).</summary>
        internal PostView PostView => _postView;

        /// <summary>07's face pages and their flip (tests).</summary>
        internal PageFlip Pages => _pages;

        /// <summary>The databank page, once it has been opened (tests and captures).</summary>
        internal DatabankPage Databank => _databank;

        /// <summary>The line on 07's face that says why a held E did nothing (tests).</summary>
        internal FaceNotice FaceNotice => _faceNotice;

        /// <summary>The call on 07's face (tests).</summary>
        internal CallDisplayView CallView => _callView;

        /// <summary>When the call shows on 07's face (tests).</summary>
        internal CallDisplay Call => _call;

        /// <summary>The face panel's texture, which 07's CRT shows while the UI has the face (captures).</summary>
        internal Texture FaceTexture => FaceTargetTexture();

        private void OnBootCheckStarted(BootCheckStarted started)
        {
            if (started.Step == BootCheckStep.Carrier || _postLog.IsFinished)
            {
                ResetLines();
                _bootFace?.Clear();
                DrainBootCues();
            }

            _postLog.Started(started.Step, PostText.Started(started.Step, _localization, _tuning.Post.ResumeDays),
                PostText.IsInformation(started.Step));
            ShowReadout();
        }

        /// <summary>07's tube wakes: the face's boot begins on a clean CRT.</summary>
        private void OnCrtPowerOn(CrtPowerOn powerOn)
        {
            if (!_bound)
            {
                return;
            }

            ResetLines();
            _bootFace.PowerOn(powerOn.Seconds);
            ShowReadout();
        }

        private void OnBootFastForwarded(BootCheckFastForwarded forwarded)
        {
            _postLog.Rush(forwarded.Scale);
            _bootFace?.Rush(forwarded.Scale);
        }

        private void ResetLines()
        {
            _postLog.Reset();
            _postView.Clear();
            Array.Clear(_typedCued, 0, _typedCued.Length);
            Array.Clear(_lineCued, 0, _lineCued.Length);
        }

        private void ShowReadout()
        {
            if (!_postOnCrt)
            {
                _postOnCrt = true;
                _services.FaceScreen?.SetReadout(FaceTargetTexture());
            }
        }

        private void OnBootCheckPulse(BootCheckPulse pulse)
        {
            if (pulse.Step == BootCheckStep.Battery && _services.Battery != null)
            {
                _postLog.Live(pulse.Step, PostText.Filling(_localization, pulse.Progress01 * _services.Battery.Charge01));
            }
        }

        private void OnBootCheckCompleted(BootCheckCompleted completed)
        {
            float charge = _services.Battery != null ? _services.Battery.Charge01 : 0f;
            _postLog.Completed(completed.Step, completed.Result,
                PostText.Settled(completed.Step, completed.Result, _localization, _tuning.Post.ResumeDays, charge));
        }

        private void StepPost(float deltaTime)
        {
            _bootFace.Tick(deltaTime);
            DrainBootCues();
            _bootFaceView.Refresh();

            bool changed = _postLog.Count > 0 && _postLog.Tick(deltaTime);
            CueTyping();
            if (_postOnCrt && !FaceReadoutShown)
            {
                _postOnCrt = false;
                _services.FaceScreen?.SetReadout(null);
            }

            _postView.SetShown(PostShown && !_cargo.IsOpen);
            if (changed)
            {
                _postView.Refresh();
            }
        }

        /// <summary>The face's boot's cues, as Core's events for Audio.</summary>
        private void DrainBootCues()
        {
            while (_bootFace.TryTakeCue(out BootFaceCue cue))
            {
                switch (cue.Kind)
                {
                    case BootFaceCueKind.StrokeDrawn:
                        _events.Publish(new LogoStrokeDrawn(cue.A));
                        break;
                    case BootFaceCueKind.LoadStep:
                        _events.Publish(new LoadStep(cue.A));
                        break;
                    case BootFaceCueKind.Typed:
                        _events.Publish(new BootTextTyped(cue.A, cue.B));
                        break;
                    default:
                        _events.Publish(new BootLineCompleted(cue.A));
                        break;
                }
            }
        }

        /// <summary>The self-test's typing as Core's events: a cue every few characters, and each line once it settles.</summary>
        private void CueTyping()
        {
            int every = _tuning.BootFace.TypeCueEvery;
            for (int i = 0; i < _postLog.Count; i++)
            {
                int visible = _postLog.Visible(i);
                for (int typed = _typedCued[i] + 1; typed <= visible; typed++)
                {
                    if (typed % every == 0)
                    {
                        _events.Publish(new BootTextTyped(BootFace.FirstPostLine + i, typed - 1));
                    }
                }

                _typedCued[i] = visible;
                if (!_lineCued[i] && _postLog.IsSettled(i))
                {
                    _lineCued[i] = true;
                    _events.Publish(new BootLineCompleted(BootFace.FirstPostLine + i));
                }
            }
        }

        /// <summary>The cover eases toward closed or open and repaints while it moves.</summary>
        private void StepCover(float deltaTime)
        {
            _cover.Tick(deltaTime);
            float cover = _cover.Cover01;
            if (cover != _coverPainted)
            {
                _coverPainted = cover;
                _coverPainter.Refresh();
            }
        }

        /// <summary>
        /// New Game: over a fresh title the camera pushes in and nothing covers it; otherwise App reloads, so the tube's
        /// dark closes in from the edges before it does.
        /// </summary>
        private void OnNewGameRequested()
        {
            bool freshTitle = _titleUp && !_titleHasSave;
            OnTitleDismissed();
            if (_bound && !freshTitle)
            {
                _cover.Close();
            }
        }

        /// <summary>
        /// A fresh game that began without a title in this scene, where a title belongs: the reload of a New Game. It
        /// begins under the closed cover and opens on 07's dark face.
        /// </summary>
        private void OnSessionStarted(SessionStarted started)
        {
            if (!started.FreshGame || _titleSeen || !_services.Flow.ShowsTitle)
            {
                return;
            }

            if (_bound)
            {
                SettleUnderCover();
            }
            else
            {
                _coverSettlePending = true;
            }
        }

        private void SettleUnderCover()
        {
            _cover.SnapClosed();
            _cover.Open();
        }

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
                if (menu.NavigatePressed && view.FocusedHoldCell < 0 && view.FocusedDeviceIndex < 0)
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

            if (menu.SubmitPressed || menu.UsePressed)
            {
                view.SubmitFocused();
            }

            if (!TryFacePointer(menu, out Vector2 panelPos, out bool down, out bool moved, out bool up))
            {
                return;
            }

            if (down && ClickedTab(panelPos))
            {
                return;
            }

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

        /// <summary>
        /// The mouse on the screen's panel this frame, from its top-left: the views' own pointer feed, the only one on
        /// either host, so what they draw at the pointer sits where the mouse is. False when it did nothing or missed.
        /// </summary>
        private bool TryFacePointer(MenuInput menu, out Vector2 panelPos, out bool down, out bool moved, out bool up)
        {
            panelPos = default;
            down = menu.ClickPressed;
            up = menu.ClickReleased;
            moved = false;
            bool onFace = _faceDocument != null;
            if (onFace && (_services.FaceScreen == null || !_services.FaceScreen.IsOpen))
            {
                return false;
            }

            Vector2 rawMouse = menu.PointerPosition;
            if (float.IsNaN(rawMouse.x) || float.IsNaN(rawMouse.y))
            {
                return false;
            }

            moved = float.IsNaN(_lastFaceMouseScreenPos.x)
                ? rawMouse.sqrMagnitude > 0.01f
                : (rawMouse - _lastFaceMouseScreenPos).sqrMagnitude > 0.01f;
            if (!moved && !down && !up)
            {
                return false;
            }

            _lastFaceMouseScreenPos = rawMouse;
            panelPos = (onFace ? FacePanelPoint(rawMouse) : MainPanelPoint(rawMouse)) - _cargo.PanelRect.position;
            return !float.IsNaN(panelPos.x) && !float.IsNaN(panelPos.y);
        }

        /// <summary>The mouse (Input System pixels, bottom-left origin) on 07's curved face, in face panel space.</summary>
        private Vector2 FacePanelPoint(Vector2 mouse)
        {
            Camera cam = _services.View?.Camera;
            float height = cam != null && cam.pixelHeight > 0 ? cam.pixelHeight : Screen.height;
            var uiScreenPos = new Vector2(mouse.x, height - mouse.y);
            return FaceScreenMapper.ScreenToPanel(uiScreenPos, cam, _services.FaceScreen, CargoScreenSize(), height);
        }

        /// <summary>
        /// The mouse (Input System pixels, bottom-left origin) in the HUD panel's space. The UI reads the mouse itself
        /// and hands it to its views, as the face screen does, so a click never depends on UI Toolkit's own pointer
        /// feed reaching the panel.
        /// </summary>
        private Vector2 MainPanelPoint(Vector2 mouse)
        {
            IPanel panel = _layout.Root.panel;
            return panel == null
                ? new Vector2(float.NaN, float.NaN)
                : RuntimePanelUtils.ScreenToPanel(panel, new Vector2(mouse.x, Screen.height - mouse.y));
        }

        /// <summary>The build menu's cards under the mouse: a real move hovers (selects), a press picks.</summary>
        private void StepBuildPointer(MenuInput menu)
        {
            if (_buildStrip.State != BuildModeState.Menu || _pause.IsOpen || _cargo.IsOpen)
            {
                _lastBuildMouse = new Vector2(float.NaN, float.NaN);
                return;
            }

            Vector2 mouse = menu.PointerPosition;
            if (float.IsNaN(mouse.x))
            {
                return;
            }

            bool moved = !float.IsNaN(_lastBuildMouse.x) && (mouse - _lastBuildMouse).sqrMagnitude > 0.01f;
            _lastBuildMouse = mouse;
            bool down = menu.ClickPressed;
            if (!moved && !down)
            {
                return;
            }

            Vector2 panelPos = MainPanelPoint(mouse);
            if (down)
            {
                _buildStrip.PointerDownAt(panelPos);
            }
            else
            {
                _buildStrip.PointerMoveAt(panelPos);
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
            _lastFaceMouseScreenPos = _services.Input.Menu.PointerPosition;
            _lastCargoNavDirection = NavigationMoveEvent.Direction.None;
            _cargoNavRepeatTimer = 0f;
            SetFacePanelVisible(true);
            _cargo.Open();
            _services.FaceScreen?.SetScreenOpen(true, FaceTargetTexture());
            _services.Input.Disable();
            ApplyCursor();
            _cargo.FocusFirst();
            _events.Publish(new UiCue(UiCueKind.Confirm));
        }

        private void CloseCargo()
        {
            _lastCargoNavDirection = NavigationMoveEvent.Direction.None;
            _cargoNavRepeatTimer = 0f;
            if (_pages.Target != FacePage.Hold || _pages.IsFlipping)
            {
                _pages.Snap(FacePage.Hold);
                ShowChange();
                ShowPage(_services.Input.ActiveDevice);
                _events.Publish(new FacePageChanged(FacePage.Hold));
            }

            _cargo.Close();
            _services.FaceScreen?.SetScreenOpen(false, FaceTargetTexture());
            _services.Input.Enable();
            ApplyCursor();
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
            _titleSeen = true;
            _titleUp = true;
            _titleHasSave = shown.HasSave;
            if (_bound)
            {
                _pause.OpenTitle(shown.HasSave, !shown.HasSave);
                return;
            }

            _titleWaiting = true;
        }

        private void OnTitleDismissed()
        {
            _titleUp = false;
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
            _objective.SetKey(changed.LineKey);
            _objectiveChip.SetKey(changed.LineKey);
        }

        /// <summary>The scan beam started: the scan prompt yields to the beam, and the objective's Scan chip retires.</summary>
        /// <summary>
        /// An entry joined 07's databank: the CRT's event line on its face says so once (docs/design/databank.md §3),
        /// and an open page lists it.
        /// </summary>
        private void OnDatabankEntryAdded(DatabankEntryAdded added)
        {
            if (!_services.TryGetDatabank(out IDatabank archive))
            {
                Debug.LogError(
                    $"UISystem: '{added.EntryId}' joined the databank, but no IDatabank is registered.", this);
                return;
            }

            if (archive.TryGetEntry(added.EntryId, out DatabankEntryInfo entry))
            {
                _toastText = _databankText.Toast(entry);
                _toastLeft = _tuning.Databank.ToastSeconds;
            }

            _databank?.Refresh();
        }

        /// <summary>
        /// Q flips 07's face between its pages as the CRT redraws: the old page fades down, the page changes, the new
        /// one draws in from the top while the tab underline glides across; the camera's face shot follows the page.
        /// </summary>
        private void StepPages(MenuInput menu, float deltaTime, InputDeviceKind device)
        {
            if (menu.PagePressed)
            {
                FlipPage();
            }

            _pages.Tick(deltaTime);
            if (_pages.Switched)
            {
                ShowPage(device);
                _shutter.BringToFront();
                _tabs.BringToFront();
            }

            if (_pages.IsFlipping || _pages.Settled)
            {
                ShowChange();
            }

            if (_pages.Settled)
            {
                _events.Publish(new UiCue(UiCueKind.DatabankChannelSettle));
            }

            if (!Mathf.Approximately(_databankLaidOutAt, _cargo.PanelScale))
            {
                _databankLaidOutAt = _cargo.PanelScale;
                _tabs.Layout(_cargo.PanelScale);
                _shutter.Layout(_cargo.PanelScale);
                _databank?.Layout(_cargo.PanelScale);
            }
        }

        /// <summary>The page change as it stands: the lit tab, the underline, the old page's fade, the new one's draw.</summary>
        private void ShowChange()
        {
            _tabs.Light(_pages.Page);
            _tabs.Glide(_pages.TabPosition);
            _cargo.SetPageAlpha(_pages.ContentAlpha, _pageChrome);
            _shutter.Draw(_pages.Drawn);
        }

        /// <summary>Flips to the face's other page (Q, LB / RB, or a click on its tab), on 07's face only.</summary>
        private void FlipPage()
        {
            if (_faceDocument == null || (_pages.Target == FacePage.Hold && !EnsureDatabank()))
            {
                return;
            }

            _pages.Toggle();
            _events.Publish(new FacePageChanged(_pages.Target));
            _events.Publish(new UiCue(UiCueKind.DatabankPageFlip));
        }

        /// <summary>A click on the other page's tab flips to it; true when the click was on a tab.</summary>
        private bool ClickedTab(Vector2 panelPos)
        {
            Vector2 refPos = panelPos / Mathf.Max(0.01f, _cargo.PanelScale);
            if (!PageTabs.TryPageAt(refPos, out FacePage page))
            {
                return false;
            }

            if (page != _pages.Target)
            {
                FlipPage();
            }

            return true;
        }

        /// <summary>The page now on the tube shows; the other steps aside.</summary>
        private void ShowPage(InputDeviceKind device)
        {
            bool databank = _pages.Page == FacePage.Databank;
            _pageRoots[_pageRoots.Length - 1] = _databank?.View.Root;
            _cargo.SetHoldShown(!databank, _pageRoots);
            _databank?.SetOpen(databank, device);
        }

        /// <summary>
        /// A source of the opening gave a material: its pickup row says what it is for ("for the charging pad").
        /// </summary>
        private void OnMaterialSalvaged(MaterialSalvaged salvaged)
        {
            if (_pickups != null && salvaged.Source == MaterialSource.Crate &&
                _services.TryGetSupplyPurposes(out ISupplyPurposes purposes) &&
                purposes.TryPurposeOf(salvaged.SiteId, out string structureId))
            {
                _pickups.ForStructure(salvaged.Material, structureId);
            }
        }

        /// <summary>
        /// Builds the databank page on first use; false (logged) when nothing registered 07's archive.
        /// </summary>
        private bool EnsureDatabank()
        {
            if (_databank != null)
            {
                return true;
            }

            if (!_services.TryGetDatabank(out IDatabank archive))
            {
                Debug.LogError("UISystem: the databank page needs an IDatabank, and none is registered.", this);
                return false;
            }

            _databank = new DatabankPage(_tuning.Databank, archive, _events, _localization, _cargo.Panel);
            _databankLaidOutAt = -1f;
            return true;
        }

        /// <summary>The databank page's input: the list, the colour toggle, the turn by stick or by drag.</summary>
        private void StepDatabankInput(MenuInput menu, float deltaTime, InputDeviceKind device)
        {
            NavigationMoveEvent.Direction dir = QuantizeNavigationDirection(menu.NavigateVector);
            NavigationMoveEvent.Direction step = NavigationMoveEvent.Direction.None;
            if (dir == NavigationMoveEvent.Direction.None)
            {
                _lastCargoNavDirection = NavigationMoveEvent.Direction.None;
            }
            else if (dir != _lastCargoNavDirection)
            {
                _lastCargoNavDirection = dir;
                _cargoNavRepeatTimer = CargoNavInitialRepeatDelay;
                step = dir;
            }
            else
            {
                _cargoNavRepeatTimer -= deltaTime;
                if (_cargoNavRepeatTimer <= 0f)
                {
                    _cargoNavRepeatTimer = CargoNavRepeatInterval;
                    step = dir;
                }
            }

            if (TryFacePointer(menu, out Vector2 panelPos, out bool down, out bool moved, out bool up))
            {
                Vector2 refPos = panelPos / Mathf.Max(0.01f, _cargo.PanelScale);
                if (down && ClickedTab(panelPos))
                {
                    return;
                }

                if (down)
                {
                    _databank.PointerDown(refPos);
                }
                else if (moved)
                {
                    _databank.PointerMove(refPos);
                }

                if (up)
                {
                    _databank.PointerUp();
                }
            }

            _databank.Tick(deltaTime, step, menu.ColourPressed, menu.Turn, device);
        }

        /// <summary>A held E could not work: the chip and 07's face answer it quietly.</summary>
        private void OnInteractionBlocked(InteractionBlocked blocked)
        {
            if (blocked.Reason == BlockReason.NoLineOfSight)
            {
                _blockedCue.Start();
            }
        }

        /// <summary>
        /// 07's face says "no line of sight" while the cue lasts (or the failed reply's line, or a databank toast), and
        /// otherwise shows the call when the guidance slot grants it, unless the self-test or the hold already has the
        /// face; a line cuts the call at once rather than overlapping it. The face is handed back only if nothing else
        /// took it meanwhile.
        /// </summary>
        private void StepFaceNotice(float deltaTime)
        {
            bool blocked = _blockedCue.FaceShown;
            bool faceFree = !_postOnCrt && !_cargo.IsOpen;
            bool reply = _replyLeft > 0f && _slot.Allows(GuidanceKind.CallLine);
            bool show = (reply || blocked || _toastLeft > 0f) && faceFree;
            _replyOnFace = reply && show;
            bool callLine = !reply && _call.WantsChange && _slot.Allows(GuidanceKind.CallLine);
            bool callStill = _call.WantsStill && _slot.Allows(GuidanceKind.CallStatus);
            _callShown = !show && faceFree && (callLine || callStill);
            if (_callShown)
            {
                _callAsLine = callLine;
            }

            if (show || !faceFree)
            {
                _callView.HideNow();
            }
            else
            {
                _callView.Tick(deltaTime, _callShown, _call);
            }

            if (show)
            {
                _faceNotice.Show(reply ? _localization.Get(UiKeys.FaceTxNoRange)
                    : blocked ? _localization.Get(UiKeys.FaceNoLine) : _toastText);
            }
            else
            {
                _faceNotice.SetShown(false);
            }

            if (show || _callView.IsVisible)
            {
                if (!_noticeOnCrt)
                {
                    _noticeOnCrt = true;
                    _services.FaceScreen?.SetReadout(FaceTargetTexture());
                }

                return;
            }

            if (_noticeOnCrt)
            {
                _noticeOnCrt = false;
                if (!_postOnCrt && !_cargo.IsOpen)
                {
                    _services.FaceScreen?.SetReadout(null);
                }
            }
        }

        private void OnCallStateChanged(CallStateChanged changed)
        {
            KnowSender();
            _call.StateChanged(changed.State, changed.Restored);
        }

        private void OnCallDecoded(CallDecoded decoded)
        {
            KnowSender();
            _call.Decode(decoded.CharactersDecoded, decoded.Restored);
        }

        /// <summary>The call is heard: its sender comes from Gameplay's call, which announced it.</summary>
        private void KnowSender()
        {
            if (_call.Sender != null)
            {
                return;
            }

            if (_services.TryGetCall(out ICallSource call))
            {
                _call.SetSender(call.Sender);
                return;
            }

            Debug.LogError($"{nameof(UISystem)}: the call was heard but no {nameof(ICallSource)} is registered, so " +
                           "07's face cannot show its sender.", this);
        }

        /// <summary>A story target's scan ends here, not with <see cref="FragmentScanned"/>.</summary>
        private void OnStoryScanned(StoryScanned scanned)
        {
            _scanning = false;
            _readout?.StoryScanned(scanned.TextKey);
        }

        private void OnFragmentScanned(FragmentScanned scanned)
        {
            _scanning = false;
            string nameKey = StructureIds.NameKey(scanned.BlueprintId);
            if (!scanned.Known && scanned.Count > 1 && _localization.TryGet(nameKey, out _))
            {
                _readout.ScanSetProgress(nameKey, scanned.Index + 1, scanned.Count);
            }

            _director.NotifyUsed(InteractionKind.Scan);
        }

        private void OnScanStarted(ScanStarted started)
        {
            _scanning = true;
            _director.Yield(InteractionKind.Scan);
            _objectiveChip.Used(ObjectiveVerb.Scan);
        }

        private void OnStructureLearned(StructureLearned learned)
        {
            if (_bound && !learned.Restored)
            {
                _unlockCard.Learned(learned.StructureId);
            }
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
                _controlsCard.OpeningBegan();
            }
            else if (reached.Beat == WakeBeat.ChaseFrame)
            {
                _inOpening = false;
                _firstPrompt.ChaseFrameReached();
                _controlsCard.ChaseFrameReached();
            }
        }

        /// <summary>
        /// A milestone of the opening: the protocol line follows it, and a milestone reached now (not replayed from a
        /// save) shows the new line.
        /// </summary>
        private void OnMilestoneReached(OpeningMilestoneReached reached)
        {
            if (_openingProtocol.Reach(reached.Milestone) && !reached.Restored && _bound)
            {
                _readout.ProtocolChanged();
            }
        }

        /// <summary>The lander's old cells gave out: the readout says so once; a loaded game says nothing.</summary>
        private void OnLanderReserveChanged(LanderReserveChanged changed)
        {
            if (_bound && !changed.Restored && changed.Reserve01 <= 0f)
            {
                _readout.ReserveEmpty(Mathf.RoundToInt(changed.Reserve01 * 100f));
            }
        }

        private void OnSystemChanged(RoverSystemChanged changed)
        {
            _cargo?.MarkDirty();
            if (_bound && !changed.Restored)
            {
                _readout.SystemChanged(changed.System);
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
            _controlsView.Relocalize();
            _bootFaceView.Relocalize();
            _tabs.Relocalize();
            _lowBattery.Relocalize();
            _objectiveView.Relocalize(_lowBattery);
            _readout.Relocalize();
            _sunArc.Relocalize();
            _buildStrip?.Relocalize();
            _pickups.Relocalize();
            _callView.Relocalize();
            _cargo.Relocalize();
            _unlockCard.Relocalize();
            _pause.Relocalize();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _bound)
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

            _databank?.Dispose();
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
