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
        [Range(32f, 96f)]
        [SerializeField] private float _maxCell = 86f;

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

        [Tooltip("Gap (px) between a socket or part and the tooltip that names it.")]
        [Range(0f, 40f)]
        [SerializeField] private float _tooltipGap = 10f;

        [Tooltip("Left X coordinate (px in 1152x768) of the 4x6 hold grid.")]
        [Range(16f, 120f)]
        [SerializeField] private float _holdGridX = 48f;

        [Tooltip("Top Y coordinate (px in 1152x768) of the 4x6 hold and shelf grids.")]
        [Range(24f, 120f)]
        [SerializeField] private float _holdGridY = 58f;

        [Tooltip("Left X coordinate (px in 1152x768) of the right-pane header and docked shelf grid.")]
        [Range(400f, 640f)]
        [SerializeField] private float _shelfGridX = 504f;

        [Tooltip("Horizontal gap (px in 1152x768) between adjacent hold cells.")]
        [Range(2f, 24f)]
        [SerializeField] private float _cellGapX = 10f;

        [Tooltip("Vertical gap (px in 1152x768) between adjacent hold rows.")]
        [Range(2f, 24f)]
        [SerializeField] private float _cellGapY = 7f;

        [Tooltip("X coordinate (px in 1152x768) of the vertical divider between the hold grid and the 07 pane.")]
        [Range(400f, 560f)]
        [SerializeField] private float _dividerX = 474f;

        [Tooltip("Radius (px in 1152x768) of each of the 10 orbital equipment socket bubbles.")]
        [Range(20f, 48f)]
        [SerializeField] private float _bubbleRadius = 31f;

        [Tooltip("Origin X (px in 1152x768) of the side-3/4 Rover 07 line-art drawing.")]
        [Range(600f, 960f)]
        [SerializeField] private float _roverOriginX = 804f;

        [Tooltip("Origin Y (px in 1152x768) of the side-3/4 Rover 07 line-art ground line.")]
        [Range(300f, 620f)]
        [SerializeField] private float _roverOriginY = 456f;

        [Tooltip("Scale (px per rover unit in 1152x768) of the side-3/4 Rover 07 line-art drawing.")]
        [Range(80f, 240f)]
        [SerializeField] private float _roverScale = 154f;

        [Tooltip("Seconds a transient toast message remains visible at the bottom of the panel.")]
        [Range(1f, 8f)]
        [SerializeField] private float _toastDurationSeconds = 3.5f;

        [Header("Two-colour CRT palette (amber + cyan, docs/design/screen-ui.md)")]
        [Tooltip("Dark warm CRT fill inside an unlocked inventory cell or socket bubble.")]
        [SerializeField] private Color32 _cellBackground = new Color32(18, 12, 6, 235);

        [Tooltip("Tinted CRT fill inside the focused, hovered, or drag-target cell or socket bubble.")]
        [SerializeField] private Color32 _cellFocusBackground = new Color32(26, 28, 22, 245);

        [Tooltip("Opaque dark CRT backing plate behind count badges, tooltips, and toasts.")]
        [SerializeField] private Color32 _plateBackground = new Color32(14, 9, 5, 248);

        [Tooltip("Brightest amber stroke and badge color for active and installed items.")]
        [SerializeField] private Color32 _amberBright = new Color32(255, 214, 128, 255);

        [Tooltip("Primary warm amber stroke color for hold item icons and Rover 07 line-art.")]
        [SerializeField] private Color32 _amberText = new Color32(255, 194, 92, 255);

        [Tooltip("Secondary amber border color for filled cells and fitted socket rings.")]
        [SerializeField] private Color32 _amberDim = new Color32(232, 174, 82, 255);

        [Tooltip("Faint amber stroke for empty cells, empty socket rings, and default leader lines.")]
        [SerializeField] private Color32 _amberFaint = new Color32(204, 150, 68, 165);

        [Tooltip("Muted amber stroke for ghosted kit silhouettes inside empty sockets.")]
        [SerializeField] private Color32 _amberMuted = new Color32(204, 150, 68, 95);

        [Tooltip("Subtle amber stroke for locked hold cell dotted borders and padlocks.")]
        [SerializeField] private Color32 _amberLocked = new Color32(204, 150, 68, 68);

        [Tooltip("Bright cyan accent for focused/hovered cells, active drag-target sockets, and their leader lines.")]
        [SerializeField] private Color32 _cyanAccent = new Color32(95, 243, 255, 255);

        [Tooltip("Soft cyan stroke for drag trail lines and secondary focus rings.")]
        [SerializeField] private Color32 _cyanDim = new Color32(146, 240, 252, 180);

        public RevealSettings Reveal => _reveal;

        public float PanelWidth => _panelWidth;

        public float PanelHeight => _panelHeight;

        public float MaxCell => _maxCell;

        public float MinCell => _minCell;

        public float WidthShare => _widthShare;

        public float HeightShare => _heightShare;

        public float DragThreshold => _dragThreshold;

        public float TooltipGap => _tooltipGap;

        public float HoldGridX => _holdGridX;

        public float HoldGridY => _holdGridY;

        public float ShelfGridX => _shelfGridX;

        public float CellGapX => _cellGapX;

        public float CellGapY => _cellGapY;

        public float DividerX => _dividerX;

        public float BubbleRadius => _bubbleRadius;

        public float RoverOriginX => _roverOriginX;

        public float RoverOriginY => _roverOriginY;

        public float RoverScale => _roverScale;

        public float ToastDurationSeconds => _toastDurationSeconds;

        public Color32 CellBackground => _cellBackground;

        public Color32 CellFocusBackground => _cellFocusBackground;

        public Color32 PlateBackground => _plateBackground;

        public Color32 AmberBright => _amberBright;

        public Color32 AmberText => _amberText;

        public Color32 AmberDim => _amberDim;

        public Color32 AmberFaint => _amberFaint;

        public Color32 AmberMuted => _amberMuted;

        public Color32 AmberLocked => _amberLocked;

        public Color32 CyanAccent => _cyanAccent;

        public Color32 CyanDim => _cyanDim;

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

            if (_panelWidth <= 0f || _panelHeight <= 0f || _maxCell <= 0f || _bubbleRadius <= 0f || _roverScale <= 0f)
            {
                return "cargo layout or diagram framing is non-positive";
            }

            return null;
        }
    }
}
