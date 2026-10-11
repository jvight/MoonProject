using UnityEngine;

namespace MoonProject.UI.Map
{
    /// <summary>
    /// The crew's map view (docs/features/M3-07): Ines's pencil sheet on Cream, the chart pen's ink, the light table's
    /// glow, how the view eases, pans and zooms, and how a place's card reads. Warm, calm, slow.
    /// </summary>
    [CreateAssetMenu(fileName = "MapViewTuning", menuName = "MoonProject/UI/Map View Tuning")]
    public sealed class MapViewTuning : ScriptableObject
    {
        [Header("Paper")]
        [Tooltip("Paper units across the whole sheet (the view scales the paper to the screen).")]
        [Range(512f, 4096f)]
        [SerializeField] private float _paperSize = 2048f;

        [Tooltip("The paper: Cream.")]
        [SerializeField] private Color _paper = new Color(0.957f, 0.902f, 0.784f, 1f);

        [Tooltip("Ines's pencil: soft warm graphite.")]
        [SerializeField] private Color _pencil = new Color(0.36f, 0.32f, 0.27f, 0.55f);

        [Tooltip("The chart pen's ink: warm brown.")]
        [SerializeField] private Color _ink = new Color(0.35f, 0.21f, 0.12f, 0.92f);

        [Tooltip("A soft warm veil behind the paper, over the blurred world behind it.")]
        [SerializeField] private Color _veil = new Color(0.1f, 0.08f, 0.06f, 0.3f);

        [Tooltip("The light table's warm glow through the paper where the station reaches.")]
        [SerializeField] private Color _glow = new Color(1f, 0.78f, 0.45f, 0.42f);

        [Tooltip("Pencil alpha of an ordinary contour (every fifth contour is drawn at the index alpha).")]
        [Range(0f, 1f)]
        [SerializeField] private float _contourAlpha = 0.22f;

        [Tooltip("Pencil alpha of every fifth contour.")]
        [Range(0f, 1f)]
        [SerializeField] private float _indexContourAlpha = 0.42f;

        [Tooltip("Pencil width (paper units) of contours and craters.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _thinWidth = 1.1f;

        [Tooltip("Pencil width (paper units) of the rim crest, the canyon and the chasm.")]
        [Range(0.2f, 8f)]
        [SerializeField] private float _boldWidth = 2.2f;

        [Tooltip("Ink width (paper units) of a place's outline.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _inkWidth = 3f;

        [Tooltip("Paper units the hand wanders off a traced line.")]
        [Range(0f, 6f)]
        [SerializeField] private float _wobble = 1.4f;

        [Tooltip("Paper units along a line between two wobble samples (the hand's slow drift).")]
        [Range(4f, 120f)]
        [SerializeField] private float _wobbleLength = 36f;

        [Tooltip("How much a pencil stroke's darkness wavers along its length (0 = even, 1 = patchy).")]
        [Range(0f, 1f)]
        [SerializeField] private float _grain = 0.35f;

        [Header("Marks")]
        [Tooltip("Paper units of a place's ring, at least (a place's own radius widens it).")]
        [Range(4f, 80f)]
        [SerializeField] private float _ringRadius = 16f;

        [Tooltip("Paper units of half a mast's cross.")]
        [Range(4f, 60f)]
        [SerializeField] private float _crossSize = 12f;

        [Tooltip("Font size (paper units) of a place's name.")]
        [Range(8f, 80f)]
        [SerializeField] private float _nameSize = 30f;

        [Tooltip("Font size (paper units) of Ines's notes, the title and the tally.")]
        [Range(8f, 80f)]
        [SerializeField] private float _noteSize = 26f;

        [Tooltip("Paper units of one tally stroke's height.")]
        [Range(8f, 80f)]
        [SerializeField] private float _tallyHeight = 30f;

        [Header("View")]
        [Tooltip("Seconds the view takes to ease onto the map and back off it.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _openSeconds = 1.1f;

        [Tooltip("Scale of the paper as the view starts easing onto it (1 at rest).")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _openScale = 0.94f;

        [Tooltip("Fraction of the screen's shorter side the whole sheet fills at the far zoom.")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float _fitFraction = 0.94f;

        [Tooltip("How much closer the near zoom is than the whole-basin view.")]
        [Range(1.2f, 6f)]
        [SerializeField] private float _closeZoom = 2.4f;

        [Tooltip("Paper units per second the view pans at full look input (at the far zoom).")]
        [Range(50f, 3000f)]
        [SerializeField] private float _panSpeed = 520f;

        [Tooltip("Seconds the pan and zoom take to settle on a new target.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _settleSeconds = 0.45f;

        [Tooltip("Paper units around the view's centre in which the nearest place is focused (the soft aim cone).")]
        [Range(10f, 400f)]
        [SerializeField] private float _focusRadius = 120f;

        [Tooltip("Winch input that zooms in or out.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _zoomThreshold = 0.5f;

        [Header("Ink")]
        [Tooltip("Seconds one place's outline takes to ink (match the chart pen's stroke in MapTuning).")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _inkSeconds = 0.6f;

        [Tooltip("Seconds the name and icons take to fade in once the outline is inked.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _nameFadeSeconds = 0.5f;

        [Tooltip("First look: seconds after the last stroke, and between two notes, before the view drifts close to " +
            "the next of Ines's notes beyond the rim (step 3).")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _edgeDriftDelay = 2.2f;

        [Header("Card")]
        [Tooltip("Font size (screen pixels) of the name on a place's card.")]
        [Range(8f, 72f)]
        [SerializeField] private float _cardNameSize = 26f;

        [Tooltip("Font size (screen pixels) of the card's one line.")]
        [Range(8f, 72f)]
        [SerializeField] private float _cardLineSize = 20f;

        [Tooltip("Seconds the card takes to fade in or out.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _cardFadeSeconds = 0.35f;

        public float PaperSize => _paperSize;

        public Color Paper => _paper;

        public Color Pencil => _pencil;

        public Color Ink => _ink;

        public Color Veil => _veil;

        public Color Glow => _glow;

        public float ContourAlpha => _contourAlpha;

        public float IndexContourAlpha => _indexContourAlpha;

        public float ThinWidth => _thinWidth;

        public float BoldWidth => _boldWidth;

        public float InkWidth => _inkWidth;

        public float Wobble => _wobble;

        public float WobbleLength => _wobbleLength;

        public float Grain => _grain;

        public float RingRadius => _ringRadius;

        public float CrossSize => _crossSize;

        public float NameSize => _nameSize;

        public float NoteSize => _noteSize;

        public float TallyHeight => _tallyHeight;

        public float OpenSeconds => _openSeconds;

        public float OpenScale => _openScale;

        public float FitFraction => _fitFraction;

        public float CloseZoom => _closeZoom;

        public float PanSpeed => _panSpeed;

        public float SettleSeconds => _settleSeconds;

        public float FocusRadius => _focusRadius;

        public float ZoomThreshold => _zoomThreshold;

        public float InkSeconds => _inkSeconds;

        public float NameFadeSeconds => _nameFadeSeconds;

        public float EdgeDriftDelay => _edgeDriftDelay;

        public float CardNameSize => _cardNameSize;

        public float CardLineSize => _cardLineSize;

        public float CardFadeSeconds => _cardFadeSeconds;
    }
}
