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
        [SerializeField] private float _panelWidth = 1220f;

        [Tooltip("Fixed height of the unified cargo panel (px at 1080p); clamped to HeightShare on short screens.")]
        [Range(400f, 900f)]
        [SerializeField] private float _panelHeight = 560f;

        [Tooltip("Largest slot cell (px at 1080p); the cells shrink to fit when the screen is narrow or the text big.")]
        [Range(32f, 96f)]
        [SerializeField] private float _maxCell = 60f;

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

        [Header("07's portrait and equipment sockets (docs/design/0.5.5-owner-feedback.md §3.5)")]
        [Tooltip("Pixels the pointer must move with a part held before it counts as a drag and not a press.")]
        [Range(2f, 20f)]
        [SerializeField] private float _dragThreshold = 6f;

        [Tooltip("Width (px) of the right-hand column down the side of the portrait where socket names sit.")]
        [Range(80f, 280f)]
        [SerializeField] private float _labelWidth = 158f;

        [Tooltip("Width (px) of the left-hand column down the side of the portrait where socket names sit.")]
        [Range(80f, 320f)]
        [SerializeField] private float _leftLabelWidth = 158f;

        [Tooltip("Height (px) of one socket name.")]
        [Range(14f, 40f)]
        [SerializeField] private float _labelHeight = 24f;

        [Tooltip("Gap (px) kept between two socket names in a column.")]
        [Range(0f, 16f)]
        [SerializeField] private float _labelGap = 6f;

        [Tooltip("Gap (px) between the portrait's edge and a column of names.")]
        [Range(0f, 40f)]
        [SerializeField] private float _labelMargin = 8f;

        [Tooltip("Portrait width (px at 1080p) at which socket label columns use their full width before scaling down.")]
        [Range(400f, 1200f)]
        [SerializeField] private float _portraitReferenceWidth = 798f;

        [Tooltip("Gap (px) between a socket or part and the tooltip that names it.")]
        [Range(0f, 40f)]
        [SerializeField] private float _tooltipGap = 10f;

        [Header("07's 3/4 schematic diagram framing")]
        [Tooltip("Camera bearing round 07 (deg from its nose, + toward its right flank) in the 3/4 schematic diagram.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _diagramYaw = 55f;

        [Tooltip("Camera elevation (deg above 07's deck) in the 3/4 schematic diagram.")]
        [Range(-10f, 60f)]
        [SerializeField] private float _diagramElevation = 14f;

        [Tooltip("Camera distance (m) from the look point in the 3/4 schematic diagram.")]
        [Range(2f, 12f)]
        [SerializeField] private float _diagramDistance = 3.9f;

        [Tooltip("Height (m above 07's root) of the point the 3/4 schematic diagram camera looks at.")]
        [Range(0f, 2f)]
        [SerializeField] private float _diagramLookHeight = 0.56f;

        [Tooltip("Vertical field of view (deg) of the 3/4 schematic diagram camera.")]
        [Range(10f, 60f)]
        [SerializeField] private float _diagramFieldOfView = 30f;

        public RevealSettings Reveal => _reveal;

        public float PanelWidth => _panelWidth;

        public float PanelHeight => _panelHeight;

        public float MaxCell => _maxCell;

        public float MinCell => _minCell;

        public float WidthShare => _widthShare;

        public float HeightShare => _heightShare;

        public float DragThreshold => _dragThreshold;

        public float TooltipGap => _tooltipGap;

        public float LabelWidth => _labelWidth;

        public float LeftLabelWidth => _leftLabelWidth;

        public float LabelHeight => _labelHeight;

        public float LabelGap => _labelGap;

        public float LabelMargin => _labelMargin;

        public float PortraitReferenceWidth => _portraitReferenceWidth;

        public float DiagramYaw => _diagramYaw;

        public float DiagramElevation => _diagramElevation;

        public float DiagramDistance => _diagramDistance;

        public float DiagramLookHeight => _diagramLookHeight;

        public float DiagramFieldOfView => _diagramFieldOfView;

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

            if (_panelWidth <= 0f || _panelHeight <= 0f || _labelWidth <= 0f || _leftLabelWidth <= 0f ||
                _labelHeight <= 0f || _portraitReferenceWidth <= 0f || _diagramDistance <= 0f ||
                _diagramFieldOfView <= 0f)
            {
                return "cargo layout or diagram framing is non-positive";
            }

            return null;
        }
    }
}
