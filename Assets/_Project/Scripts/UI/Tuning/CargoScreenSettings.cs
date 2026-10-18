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

        [Tooltip("Radius (px in 1152x768) of the status disc in each device frame.")]
        [Range(12f, 32f)]
        [SerializeField] private float _bubbleRadius = 22f;

        [Tooltip("Seconds a transient toast message remains visible at the bottom of the panel.")]
        [Range(1f, 8f)]
        [SerializeField] private float _toastDurationSeconds = 3.5f;

        [Header("07's line drawing on the face (Art/UI/Face/rover_07_line_3q.png)")]
        [Tooltip("Tint over the drawing's cream lines; white keeps them as Art drew them.")]
        [SerializeField] private Color32 _artTint = new Color32(255, 255, 255, 255);

        [Header("Device states: each one its own colour, stroke and mark, so they read apart on the CRT in daylight")]
        [Tooltip("A working device (OK): its state words, frame and disc.")]
        [SerializeField] private Color32 _statusOk = new Color32(40, 230, 150, 255);

        [Tooltip("A faulty device (FAULT): its state words, frame, disc and the warning mark beside its name.")]
        [SerializeField] private Color32 _statusFault = new Color32(255, 96, 32, 255);

        [Tooltip("An empty socket (EMPTY): its state words and its broken frame and disc, quieter than the rest.")]
        [SerializeField] private Color32 _statusEmpty = new Color32(152, 148, 164, 255);

        [Tooltip("Length (px at 1152x768) of each dash of an empty socket's frame.")]
        [Range(4f, 24f)]
        [SerializeField] private float _emptyDash = 10f;

        [Tooltip("Gap (px at 1152x768) between the dashes of an empty socket's frame.")]
        [Range(2f, 16f)]
        [SerializeField] private float _emptyDashGap = 7f;

        [Tooltip("Size (px at 1152x768) of the warning mark beside a faulty device's name.")]
        [Range(16f, 32f)]
        [SerializeField] private float _warningIconSize = 26f;

        [Header("Orbital socket leader lines (faint default, slightly brighter installed, cyan only on focus/drag)")]
        [Tooltip("Faint amber stroke (~27% alpha) for empty socket leader lines so the 10 spokes do not compete with 07.")]
        [SerializeField] private Color32 _leaderEmptyColor = new Color32(236, 188, 104, 76);

        [Tooltip("Stroke width (px at 1152x768) for empty socket leader lines.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _leaderEmptyWidth = 2f;

        [Tooltip("Mount anchor dot radius (px at 1152x768) on 07 for empty socket leader lines.")]
        [Range(1f, 5f)]
        [SerializeField] private float _leaderEmptyDotRadius = 3.5f;

        [Tooltip("Subtle warm amber stroke (~42% alpha) for installed socket leader lines.")]
        [SerializeField] private Color32 _leaderInstalledColor = new Color32(252, 204, 118, 110);

        [Tooltip("Stroke width (px at 1152x768) for installed socket leader lines.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _leaderInstalledWidth = 2.4f;

        [Tooltip("Mount anchor dot radius (px at 1152x768) on 07 for installed socket leader lines.")]
        [Range(1f, 5f)]
        [SerializeField] private float _leaderInstalledDotRadius = 4f;

        [Tooltip("Bright cyan stroke for the single focused, hovered, or active drag-target socket's leader line.")]
        [SerializeField] private Color32 _leaderFocusColor = new Color32(95, 243, 255, 250);

        [Tooltip("Stroke width (px at 1152x768) for the focused or drag-target socket leader line.")]
        [Range(1f, 4f)]
        [SerializeField] private float _leaderFocusWidth = 3.2f;

        [Tooltip("Mount anchor dot radius (px at 1152x768) on 07 for the focused or drag-target socket leader line.")]
        [Range(1.5f, 6f)]
        [SerializeField] private float _leaderFocusDotRadius = 5.5f;

        [Header("Legibility on the 2x face texture: heavier strokes, larger icons, a clear selection")]
        [Tooltip("Stroke width (px at 1152x768) of the outline of an empty hold cell and of the grid rules.")]
        [Range(1f, 6f)]
        [SerializeField] private float _cellStrokeWidth = 2.4f;

        [Tooltip("Stroke width (px at 1152x768) of the outline of a hold cell that holds something.")]
        [Range(1f, 6f)]
        [SerializeField] private float _itemStrokeWidth = 3.2f;

        [Tooltip("Stroke width (px at 1152x768) of the selection ring and corner ticks on a cell or device bubble.")]
        [Range(2f, 8f)]
        [SerializeField] private float _focusStrokeWidth = 5f;

        [Tooltip("Size (px at 1152x768) of the icon in a hold cell.")]
        [Range(30f, 64f)]
        [SerializeField] private float _holdIconSize = 58f;

        [Tooltip("Size (px at 1152x768) of the icon in a device frame's status disc.")]
        [Range(20f, 40f)]
        [SerializeField] private float _deviceIconSize = 36f;

        [Tooltip("Fill of the selected hold cell or device frame: a dark teal, so the CRT keeps it dim and the icon on " +
                 "it stays bright (a lighter fill is lifted to the strokes' brightness and washes the icon out).")]
        [SerializeField] private Color32 _selectionFill = new Color32(6, 34, 38, 255);

        [Tooltip("Fill of a cell or frame for a moment after it is clicked: brighter than the selection, then easing back.")]
        [SerializeField] private Color32 _pressFill = new Color32(10, 44, 48, 255);

        [Tooltip("Seconds the pressed fill takes to ease back to the selection fill.")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float _pressFlashSeconds = 0.22f;

        [Header("Two-colour CRT palette (amber + cyan, near-neutral warm-black background, >= 7:1 stroke contrast)")]
        [Tooltip("Near-neutral warm-black CRT fill (HSV sat <= 0.25, val ~0.075) inside an unlocked inventory cell or socket bubble.")]
        [SerializeField] private Color32 _cellBackground = new Color32(19, 18, 16, 255);

        [Tooltip("Opaque near-neutral warm-black CRT backing plate behind count badges, tooltips, and toasts.")]
        [SerializeField] private Color32 _plateBackground = new Color32(19, 18, 16, 255);

        [Tooltip("Brightest amber stroke and badge color for active and installed items.")]
        [SerializeField] private Color32 _amberBright = new Color32(255, 224, 148, 255);

        [Tooltip("Warm amber stroke of the hold item icons.")]
        [SerializeField] private Color32 _amberText = new Color32(255, 206, 116, 255);

        [Tooltip("Amber stroke for empty cells, empty socket rings, and default leader lines (>= 7:1 contrast).")]
        [SerializeField] private Color32 _amberFaint = new Color32(232, 182, 98, 240);

        [Tooltip("Amber stroke for ghosted kit silhouettes inside empty sockets and grid rules (>= 7:1 contrast).")]
        [SerializeField] private Color32 _amberMuted = new Color32(224, 176, 94, 225);

        [Tooltip("Amber stroke for locked hold cell dotted borders and padlocks (>= 7:1 contrast).")]
        [SerializeField] private Color32 _amberLocked = new Color32(216, 168, 90, 215);

        [Tooltip("Bright cyan accent for focused/hovered cells, active drag-target sockets, and their leader lines.")]
        [SerializeField] private Color32 _cyanAccent = new Color32(95, 243, 255, 255);

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

        public float BubbleRadius => _bubbleRadius;

        public float ToastDurationSeconds => _toastDurationSeconds;

        public Color32 ArtTint => _artTint;

        public Color32 StatusOk => _statusOk;

        public Color32 StatusFault => _statusFault;

        public Color32 StatusEmpty => _statusEmpty;

        public float EmptyDash => _emptyDash;

        public float EmptyDashGap => _emptyDashGap;

        public float WarningIconSize => _warningIconSize;

        public Color32 LeaderEmptyColor => _leaderEmptyColor;

        public float LeaderEmptyWidth => _leaderEmptyWidth;

        public float LeaderEmptyDotRadius => _leaderEmptyDotRadius;

        public Color32 LeaderInstalledColor => _leaderInstalledColor;

        public float LeaderInstalledWidth => _leaderInstalledWidth;

        public float LeaderInstalledDotRadius => _leaderInstalledDotRadius;

        public Color32 LeaderFocusColor => _leaderFocusColor;

        public float LeaderFocusWidth => _leaderFocusWidth;

        public float LeaderFocusDotRadius => _leaderFocusDotRadius;

        public Color32 CellBackground => _cellBackground;

        public Color32 PlateBackground => _plateBackground;

        public Color32 AmberBright => _amberBright;

        public Color32 AmberText => _amberText;

        public Color32 AmberFaint => _amberFaint;

        public Color32 AmberMuted => _amberMuted;

        public Color32 AmberLocked => _amberLocked;

        public Color32 CyanAccent => _cyanAccent;

        public float CellStrokeWidth => _cellStrokeWidth;

        public float ItemStrokeWidth => _itemStrokeWidth;

        public float FocusStrokeWidth => _focusStrokeWidth;

        public float HoldIconSize => _holdIconSize;

        public float DeviceIconSize => _deviceIconSize;

        public Color32 SelectionFill => _selectionFill;

        public Color32 PressFill => _pressFill;

        public float PressFlashSeconds => _pressFlashSeconds;

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

            if (_panelWidth <= 0f || _panelHeight <= 0f || _maxCell <= 0f || _bubbleRadius <= 0f)
            {
                return "cargo layout or diagram framing is non-positive";
            }

            if (_emptyDash <= 0f || _emptyDashGap <= 0f || _warningIconSize <= 0f)
            {
                return "cargo device state marks are non-positive";
            }

            if (_leaderEmptyWidth <= 0f || _leaderInstalledWidth <= 0f || _leaderFocusWidth <= 0f ||
                _cellStrokeWidth <= 0f || _itemStrokeWidth <= 0f || _focusStrokeWidth <= 0f ||
                _holdIconSize <= 0f || _deviceIconSize <= 0f || _pressFlashSeconds <= 0f)
            {
                return "cargo leader line, stroke or icon settings are non-positive";
            }

            if (_dragThreshold <= 0f || _dragFollowSeconds < 0f || _dragReturnSeconds <= 0f || _dragFitSeconds <= 0f)
            {
                return "cargo drag timings are out of range";
            }

            return null;
        }
    }
}
