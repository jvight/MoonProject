using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("_materialsChip")]
        [SerializeField] private PickupFeedSettings _pickupFeed = new PickupFeedSettings();
        [Tooltip("How the build menu eases in and out.")]
        [SerializeField] private RevealSettings _buildStripReveal = new RevealSettings(0.45f, 0.35f, 14f, 0.96f, 1.3f, 0.65f);
        [SerializeField] private CargoScreenSettings _cargo = new CargoScreenSettings();
        [SerializeField] private UnlockCardSettings _unlockCard = new UnlockCardSettings();
        [SerializeField] private PostLineSettings _post = new PostLineSettings();
        [SerializeField] private BootFaceSettings _bootFace = new BootFaceSettings();
        [SerializeField] private SalvageSettings _salvage = new SalvageSettings();
        [SerializeField] private StargazeSettings _stargaze = new StargazeSettings();
        [SerializeField] private GuidanceSettings _guidance = new GuidanceSettings();
        [SerializeField] private ControlsCardSettings _controlsCard = new ControlsCardSettings();
        [SerializeField] private BackdropSettings _backdrop = new BackdropSettings();
        [SerializeField] private SaveTickSettings _saveTick = new SaveTickSettings();
        [SerializeField] private CreditsSettings _credits = new CreditsSettings();
        [SerializeField] private CellRowSettings _battery = new CellRowSettings();
        [SerializeField] private SafeFrameSettings _safe = new SafeFrameSettings();
        [SerializeField] private DatabankSettings _databank = new DatabankSettings();

        public PauseSettings Pause => _pause;

        public TitleSettings Title => _title;

        public PromptSettings Prompts => _prompts;

        public PickupFeedSettings PickupFeed => _pickupFeed;

        public RevealSettings BuildStripReveal => _buildStripReveal;

        public CargoScreenSettings Cargo => _cargo;

        public UnlockCardSettings UnlockCard => _unlockCard;

        public PostLineSettings Post => _post;

        public BootFaceSettings BootFace => _bootFace;

        public SalvageSettings Salvage => _salvage;

        public StargazeSettings Stargaze => _stargaze;

        public GuidanceSettings Guidance => _guidance;

        public ControlsCardSettings ControlsCard => _controlsCard;

        public BackdropSettings Backdrop => _backdrop;

        public SaveTickSettings SaveTick => _saveTick;

        public CreditsSettings Credits => _credits;

        public CellRowSettings Battery => _battery;

        public SafeFrameSettings Safe => _safe;

        public DatabankSettings Databank => _databank;

        /// <summary>Null when the tuning is usable, else the first problem.</summary>
        public string Validate()
        {
            if (_pause == null || _title == null || _prompts == null || _pickupFeed == null || _salvage == null ||
                _stargaze == null || _guidance == null || _backdrop == null || _battery == null || _safe == null ||
                _saveTick == null || _credits == null || _cargo == null || _unlockCard == null || _post == null ||
                _databank == null || _controlsCard == null || _bootFace == null)
            {
                return "a settings section is missing";
            }

            return _title.Validate() ?? _prompts.Validate() ?? _safe.Validate() ?? _pause.Validate() ??
                _cargo.Validate() ?? _databank.Validate() ?? _bootFace.Validate();
        }
    }
}
