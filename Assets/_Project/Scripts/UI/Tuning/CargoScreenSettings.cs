using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The cargo screen (docs/features/M3-25 §2.4): Tab or View opens it over a blur; the game does not pause.</summary>
    [Serializable]
    public sealed class CargoScreenSettings
    {
        [Tooltip("The screen easing in and out (about 200 ms: the grids slide in with it).")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.2f, 0.18f, 18f, 0.98f, 1.8f, 0.7f);

        [Tooltip("Fixed width of the unified cargo panel (px at 1080p); clamped to WidthShare on narrow screens.")]
        [Range(720f, 1600f)]
        [SerializeField] private float _panelWidth = 1152f;

        [Tooltip("Fixed height of the unified cargo panel (px at 1080p); clamped to HeightShare on short screens.")]
        [Range(400f, 900f)]
        [SerializeField] private float _panelHeight = 768f;

        [Tooltip("Largest slot cell (px at 1080p); the cells shrink to fit when the screen is narrow or the text big.")]
        [Range(32f, 112f)]
        [SerializeField] private float _maxCell = 96f;

        [Tooltip("Smallest slot cell (px): below this the screen would be unreadable, so it stops shrinking.")]
        [Range(24f, 64f)]
        [SerializeField] private float _minCell = 34f;

        [Tooltip("Share of the screen's width the panel may use before clamping.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _widthShare = 0.94f;

        [Tooltip("Share of the screen's height the panel may use before clamping.")]
        [Range(0.6f, 1f)]
        [SerializeField] private float _heightShare = 0.92f;

        [Header("The first Tab press: the hold boots (docs/design/0.5.5-owner-feedback.md §3, about 1.2 s)")]
        [Tooltip("Seconds 07's one line of system text takes to type in, with the screen dark around it.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _bootTypeSeconds = 0.3f;

        [Tooltip("Seconds between one slot outline lighting and the next, left to right.")]
        [Range(0.02f, 0.15f)]
        [SerializeField] private float _bootLightStepSeconds = 0.06f;

        [Tooltip("Seconds between one starter item starting to slide into its slot and the next.")]
        [Range(0.02f, 0.3f)]
        [SerializeField] private float _bootItemStepSeconds = 0.1f;

        [Tooltip("Seconds one starter item takes to slide into its slot.")]
        [Range(0.1f, 0.6f)]
        [SerializeField] private float _bootSlideSeconds = 0.22f;

        [Tooltip("How far (px at 1080p) a starter item starts from its slot, to the side it comes from.")]
        [Range(0f, 200f)]
        [SerializeField] private float _bootSlideDistance = 64f;

        [Tooltip("Seconds the rear hatch stays open past the end of the boot before it shuts.")]
        [Range(0f, 1f)]
        [SerializeField] private float _bootHatchLingerSeconds = 0.2f;

        [Header("FaceInventoryView layout & orbit (docs/design/screen-ui.md Round 2 Variant A)")]
        [Tooltip("Pixels the pointer must move with a part held before it counts as a drag and not a press.")]
        [Range(2f, 20f)]
        [SerializeField] private float _dragThreshold = 6f;

        [Tooltip("Seconds the dragged part's icon takes to close most of the gap to the pointer (a soft follow).")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _dragFollowSeconds = 0.05f;

        [Tooltip("Seconds a refused part's icon eases back into its own slot.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _dragReturnSeconds = 0.32f;

        [Tooltip("Seconds a fitted part's icon eases into the device that took it before it is gone.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _dragFitSeconds = 0.26f;

        [Tooltip("Opacity of the part left behind in its slot while its icon is dragged.")]
        [Range(0f, 1f)]
        [SerializeField] private float _dragSourceAlpha = 0.3f;

        [Tooltip("Gap (px) between a socket or part and the tooltip that names it.")]
        [Range(0f, 40f)]
        [SerializeField] private float _tooltipGap = 10f;

        [Tooltip("Left X coordinate (px in 1152x768) of the 4x6 hold grid.")]
        [Range(16f, 120f)]
        [SerializeField] private float _holdGridX = 52f;

        [Tooltip("Top Y coordinate (px in 1152x768) of the 4x6 hold and shelf grids.")]
        [Range(24f, 120f)]
        [SerializeField] private float _holdGridY = 74f;

        [Tooltip("Left X coordinate (px in 1152x768) of the right-pane header and docked shelf grid.")]
        [Range(400f, 640f)]
        [SerializeField] private float _shelfGridX = 512f;

        [Tooltip("Horizontal gap (px in 1152x768) between adjacent hold cells.")]
        [Range(2f, 24f)]
        [SerializeField] private float _cellGapX = 12f;

        [Tooltip("Vertical gap (px in 1152x768) between adjacent hold rows.")]
        [Range(2f, 24f)]
        [SerializeField] private float _cellGapY = 6f;

        [Tooltip("X coordinate (px in 1152x768) of the vertical divider between the hold grid and the 07 pane.")]
        [Range(400f, 560f)]
        [SerializeField] private float _dividerX = 480f;

        [Tooltip("Seconds a transient toast message remains visible at the bottom of the panel.")]
        [Range(1f, 8f)]
        [SerializeField] private float _toastDurationSeconds = 3.5f;

        [Header("07's line drawing on the face (Art/UI/Face/rover_07_line_3q.png)")]
        [Tooltip("Tint over the drawing's cream lines: a quiet neutral, so the hold and the bays read first.")]
        [SerializeField] private Color32 _artTint = new Color32(176, 160, 132, 255);

        [Header("Look A, quiet amber CRT: one accent over warm neutrals, nothing drawn that carries nothing")]
        [Tooltip("The hold's item icons and the dragged part.")]
        [SerializeField] private Color32 _iconColor = new Color32(206, 192, 166, 255);

        [Tooltip("Quiet strokes: empty slot outlines, the brackets of a bay at rest and resting leaders.")]
        [SerializeField] private Color32 _faintColor = new Color32(96, 88, 74, 255);

        [Tooltip("The frame of a slot that holds something: clearer than an empty slot's, still quieter than the words.")]
        [SerializeField] private Color32 _itemFrameColor = new Color32(140, 129, 109, 255);

        [Tooltip("The one accent: the selection, the bays the carried part fits and what can be fitted right now.")]
        [SerializeField] private Color32 _accentColor = new Color32(255, 184, 72, 255);

        [Tooltip("Soft fill of a bay the carried part fits, and of a cell or bay for a moment after a press.")]
        [SerializeField] private Color32 _accentFill = new Color32(255, 184, 72, 20);

        [Tooltip("Opaque near-neutral warm-black CRT backing plate behind count badges, tooltips, and toasts.")]
        [SerializeField] private Color32 _plateBackground = new Color32(19, 18, 16, 255);

        [Tooltip("Stroke width (px at 1152x768) of a slot's outline, empty or holding something.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _slotStrokeWidth = 1.5f;

        [Tooltip("Stroke width (px at 1152x768) of the selection outline on a cell or a bay.")]
        [Range(1f, 6f)]
        [SerializeField] private float _focusStrokeWidth = 2.5f;

        [Tooltip("Size (px at 1152x768) of the icon in a hold cell; 40 or more so its silhouette reads.")]
        [Range(40f, 64f)]
        [SerializeField] private float _holdIconSize = 52f;

        [Tooltip("Seconds the press fill takes to ease away after a cell or a bay is pressed.")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float _pressFlashSeconds = 0.22f;

        [Header("Device bays: where a part from the hold can be dropped")]
        [Tooltip("Stroke width (px at 1152x768) of the corner brackets of a bay at rest or ready.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _bayStrokeWidth = 1.5f;

        [Tooltip("Stroke width (px at 1152x768) of the solid outline of a bay the carried part fits.")]
        [Range(1f, 5f)]
        [SerializeField] private float _bayLitStrokeWidth = 2f;

        [Tooltip("Stroke width (px at 1152x768) of that outline while the part is over it or the selection is on it.")]
        [Range(1f, 6f)]
        [SerializeField] private float _bayHoverStrokeWidth = 3.5f;

        [Tooltip("Seconds of one breath of a lit bay: its outline and fill ease down and back up.")]
        [Range(0.4f, 3f)]
        [SerializeField] private float _bayBreathSeconds = 1.2f;

        [Tooltip("How far a lit bay's outline and fill ease down at the bottom of each breath (0: no breath).")]
        [Range(0f, 1f)]
        [SerializeField] private float _bayBreathDepth = 0.45f;

        [Tooltip("Opacity of a device that takes nothing from the part being dragged.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _bayDimAlpha = 0.4f;

        [Tooltip("Stroke width (px at 1152x768) of a leader from a bay to its part on 07.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _leaderWidth = 1.5f;

        [Tooltip("Stroke width (px at 1152x768) of a lit bay's leader: brighter and a little heavier.")]
        [Range(1f, 4f)]
        [SerializeField] private float _leaderLitWidth = 2.5f;

        [Tooltip("Radius (px at 1152x768) of the dot where a leader meets 07's part.")]
        [Range(1f, 6f)]
        [SerializeField] private float _leaderDotRadius = 3.5f;

        public RevealSettings Reveal => _reveal;

        public float PanelWidth => _panelWidth;

        public float PanelHeight => _panelHeight;

        public float MaxCell => _maxCell;

        public float MinCell => _minCell;

        public float WidthShare => _widthShare;

        public float HeightShare => _heightShare;

        public float DragThreshold => _dragThreshold;

        public float DragFollowSeconds => _dragFollowSeconds;

        public float DragReturnSeconds => _dragReturnSeconds;

        public float DragFitSeconds => _dragFitSeconds;

        public float DragSourceAlpha => _dragSourceAlpha;

        public float TooltipGap => _tooltipGap;

        public float HoldGridX => _holdGridX;

        public float HoldGridY => _holdGridY;

        public float ShelfGridX => _shelfGridX;

        public float CellGapX => _cellGapX;

        public float CellGapY => _cellGapY;

        public float DividerX => _dividerX;

        public float ToastDurationSeconds => _toastDurationSeconds;

        public Color32 ArtTint => _artTint;

        public Color32 IconColor => _iconColor;

        public Color32 FaintColor => _faintColor;

        public Color32 ItemFrameColor => _itemFrameColor;

        public Color32 AccentColor => _accentColor;

        public Color32 AccentFill => _accentFill;

        public Color32 PlateBackground => _plateBackground;

        public float SlotStrokeWidth => _slotStrokeWidth;

        public float FocusStrokeWidth => _focusStrokeWidth;

        public float HoldIconSize => _holdIconSize;

        public float PressFlashSeconds => _pressFlashSeconds;

        public float BayStrokeWidth => _bayStrokeWidth;

        public float BayLitStrokeWidth => _bayLitStrokeWidth;

        public float BayHoverStrokeWidth => _bayHoverStrokeWidth;

        public float BayBreathSeconds => _bayBreathSeconds;

        public float BayBreathDepth => _bayBreathDepth;

        public float BayDimAlpha => _bayDimAlpha;

        public float LeaderWidth => _leaderWidth;

        public float LeaderLitWidth => _leaderLitWidth;

        public float LeaderDotRadius => _leaderDotRadius;

        public float BootTypeSeconds => _bootTypeSeconds;

        public float BootLightStepSeconds => _bootLightStepSeconds;

        public float BootItemStepSeconds => _bootItemStepSeconds;

        public float BootSlideSeconds => _bootSlideSeconds;

        public float BootSlideDistance => _bootSlideDistance;

        public float BootHatchLingerSeconds => _bootHatchLingerSeconds;

        public string Validate()
        {
            if (_reveal == null)
            {
                return "cargo reveal is missing";
            }

            if (_panelWidth <= 0f || _panelHeight <= 0f || _maxCell <= 0f)
            {
                return "cargo layout is non-positive";
            }

            if (_slotStrokeWidth <= 0f || _focusStrokeWidth <= 0f || _holdIconSize <= 0f || _pressFlashSeconds <= 0f ||
                _bayStrokeWidth <= 0f || _bayLitStrokeWidth <= 0f || _bayHoverStrokeWidth <= 0f ||
                _bayBreathSeconds <= 0f || _leaderWidth <= 0f || _leaderLitWidth <= 0f || _leaderDotRadius <= 0f)
            {
                return "cargo stroke, icon, bay or leader settings are non-positive";
            }

            if (_dragThreshold <= 0f || _dragFollowSeconds < 0f || _dragReturnSeconds <= 0f || _dragFitSeconds <= 0f)
            {
                return "cargo drag timings are out of range";
            }

            return null;
        }
    }
}
