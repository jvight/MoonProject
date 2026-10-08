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
        [SerializeField] private FriendUiSettings _friends = new FriendUiSettings();
        [SerializeField] private TickerSettings _ticker = new TickerSettings();
        [SerializeField] private DialReadoutSettings _dialReadout = new DialReadoutSettings();
        [SerializeField] private RelaySettings _relays = new RelaySettings();
        [SerializeField] private SalvageSettings _salvage = new SalvageSettings();

        public PauseSettings Pause => _pause;

        public TitleSettings Title => _title;

        public PromptSettings Prompts => _prompts;

        public ReticleSettings Reticle => _reticle;

        public MaterialsChipSettings MaterialsChip => _materialsChip;

        public MemoryCardSettings MemoryCard => _memoryCard;

        public TowerPanelSettings TowerPanel => _towerPanel;

        public FriendUiSettings Friends => _friends;

        public TickerSettings Ticker => _ticker;

        public DialReadoutSettings DialReadout => _dialReadout;

        public RelaySettings Relays => _relays;

        public SalvageSettings Salvage => _salvage;

        /// <summary>Null when the tuning is usable, else the first problem.</summary>
        public string Validate()
        {
            if (_pause == null || _title == null || _prompts == null || _reticle == null || _materialsChip == null ||
                _memoryCard == null || _towerPanel == null || _friends == null || _ticker == null ||
                _dialReadout == null || _relays == null ||
                _salvage == null)
            {
                return "a settings section is missing";
            }

            return _prompts.Validate();
        }
    }
}
