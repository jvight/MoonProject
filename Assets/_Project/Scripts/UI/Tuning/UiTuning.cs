using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// Every timing and motion choice of the UI (Assets/_Project/Data/Tuning/UI/UiTuning.asset, created by
    /// the UI/Tuning builder). Runtime code only reads it, so values can be tuned live in play mode.
    /// </summary>
    [CreateAssetMenu(fileName = "UiTuning", menuName = "MoonProject/UI/UI Tuning")]
    public sealed class UiTuning : ScriptableObject
    {
        [SerializeField] private PauseSettings _pause = new PauseSettings();
        [SerializeField] private TitleSettings _title = new TitleSettings();
        [SerializeField] private PromptSettings _prompts = new PromptSettings();
        [SerializeField] private ReticleSettings _reticle = new ReticleSettings();
        [SerializeField] private MaterialsChipSettings _materialsChip = new MaterialsChipSettings();
        [SerializeField] private MemoryCardSettings _memoryCard = new MemoryCardSettings();
        [SerializeField] private TowerPanelSettings _towerPanel = new TowerPanelSettings();
        [SerializeField] private KitTitleSettings _kitTitle = new KitTitleSettings();
        [SerializeField] private FriendUiSettings _friends = new FriendUiSettings();
        [SerializeField] private TickerSettings _ticker = new TickerSettings();
        [SerializeField] private DialReadoutSettings _dialReadout = new DialReadoutSettings();
        [SerializeField] private RelayUiSettings _relays = new RelayUiSettings();
        [SerializeField] private SalvageSettings _salvage = new SalvageSettings();
        [SerializeField] private StargazeSettings _stargaze = new StargazeSettings();
        [SerializeField] private GuidanceSettings _guidance = new GuidanceSettings();
        [SerializeField] private BackdropSettings _backdrop = new BackdropSettings();
        [SerializeField] private LabHudSettings _lab = new LabHudSettings();
        [SerializeField] private CellRowSettings _battery = new CellRowSettings();
        [SerializeField] private SafeFrameSettings _safe = new SafeFrameSettings();

        public PauseSettings Pause => _pause;

        public TitleSettings Title => _title;

        public PromptSettings Prompts => _prompts;

        public ReticleSettings Reticle => _reticle;

        public MaterialsChipSettings MaterialsChip => _materialsChip;

        public MemoryCardSettings MemoryCard => _memoryCard;

        public TowerPanelSettings TowerPanel => _towerPanel;

        public KitTitleSettings KitTitle => _kitTitle;

        public FriendUiSettings Friends => _friends;

        public TickerSettings Ticker => _ticker;

        public DialReadoutSettings DialReadout => _dialReadout;

        public RelayUiSettings Relays => _relays;

        public SalvageSettings Salvage => _salvage;

        public StargazeSettings Stargaze => _stargaze;

        public GuidanceSettings Guidance => _guidance;

        public BackdropSettings Backdrop => _backdrop;

        public LabHudSettings Lab => _lab;

        public CellRowSettings Battery => _battery;

        public SafeFrameSettings Safe => _safe;

        /// <summary>Null when the tuning is usable, else the first problem.</summary>
        public string Validate()
        {
            if (_pause == null || _title == null || _prompts == null || _reticle == null || _materialsChip == null ||
                _memoryCard == null || _towerPanel == null || _kitTitle == null || _friends == null ||
                _ticker == null || _dialReadout == null || _relays == null || _salvage == null ||
                _stargaze == null || _guidance == null || _backdrop == null || _battery == null || _lab == null ||
                _safe == null)
            {
                return "a settings section is missing";
            }

            return _prompts.Validate() ?? _safe.Validate() ?? _pause.Validate();
        }
    }
}
