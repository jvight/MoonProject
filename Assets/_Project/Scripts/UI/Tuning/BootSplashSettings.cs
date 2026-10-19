using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The Outsight Worlds boot splash on the boot's dark (Logs/studio/final/BOOT_SPLASH_SPEC.md): the ring emerges,
    /// the leaving stroke draws out of it, a warm glint blooms at its tip, the wordmark rises in, a calm hold, then a
    /// dissolve with the glint lingering last. Every window is in seconds from the splash's start.
    /// </summary>
    [Serializable]
    public sealed class BootSplashSettings
    {
        private const float LayerPixels = 1024f;

        [Tooltip("The ring layer (Assets/_Project/UI/Brand/boot_layer_ring.png).")]
        [SerializeField] private Texture2D _ring;

        [Tooltip("The leaving stroke layer (boot_layer_stroke.png).")]
        [SerializeField] private Texture2D _stroke;

        [Tooltip("The 4-point glint layer (boot_layer_glint.png).")]
        [SerializeField] private Texture2D _glint;

        [Tooltip("The OUTSIGHT WORLDS wordmark layer (boot_layer_wordmark.png).")]
        [SerializeField] private Texture2D _wordmark;

        [Tooltip("Seconds the whole splash takes, start to the glint's last light.")]
        [Range(1f, 8f)]
        [SerializeField] private float _duration = 4f;

        [Tooltip("Longest frame the splash counts (s): a load hitch never skips the draw or the glint.")]
        [Range(0.01f, 0.2f)]
        [SerializeField] private float _maxStep = 0.05f;

        [Tooltip("Share of the screen's height the 1024-px layer square takes.")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _heightShare = 0.8f;

        [Tooltip("Ring fade-in starts (s).")]
        [Range(0f, 4f)]
        [SerializeField] private float _ringIn0 = 0.10f;

        [Tooltip("Ring fade-in ends (s).")]
        [Range(0f, 4f)]
        [SerializeField] private float _ringIn1 = 0.95f;

        [Tooltip("Stroke fade-in starts (s).")]
        [Range(0f, 4f)]
        [SerializeField] private float _strokeIn0 = 0.15f;

        [Tooltip("Stroke fade-in ends (s).")]
        [Range(0f, 4f)]
        [SerializeField] private float _strokeIn1 = 0.65f;

        [Tooltip("The stroke starts drawing out of the ring (s).")]
        [Range(0f, 4f)]
        [SerializeField] private float _strokeDraw0 = 0.20f;

        [Tooltip("The stroke is fully drawn (s).")]
        [Range(0f, 4f)]
        [SerializeField] private float _strokeDraw1 = 1.45f;

        [Tooltip("Where the stroke's art begins and ends across the layer (px of 1024): the draw sweeps between them.")]
        [SerializeField] private Vector2 _strokeSpanPixels = new Vector2(334f, 651f);

        [Tooltip("Glint fade-in starts (s).")]
        [Range(0f, 4f)]
        [SerializeField] private float _glintIn0 = 1.30f;

        [Tooltip("Glint fade-in ends (s).")]
        [Range(0f, 4f)]
        [SerializeField] private float _glintIn1 = 1.72f;

        [Tooltip("The glint's bloom peaks (s), from its start scale to its peak scale since the fade-in began.")]
        [Range(0f, 4f)]
        [SerializeField] private float _glintPeak = 1.62f;

        [Tooltip("The glint has settled to full size (s).")]
        [Range(0f, 4f)]
        [SerializeField] private float _glintSettled = 1.95f;

        [Tooltip("The glint's scale as it ignites.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _glintStartScale = 0.35f;

        [Tooltip("The glint's scale at the top of its bloom (a weightless overshoot, no lens flare).")]
        [Range(1f, 1.5f)]
        [SerializeField] private float _glintPeakScale = 1.12f;

        [Tooltip("The glint's scale as it fades at the very end.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _glintEndScale = 0.88f;

        [Tooltip("Where the glint's centre sits on the layer (px of 1024): it blooms around that point.")]
        [SerializeField] private Vector2 _glintCentrePixels = new Vector2(662f, 295f);

        [Tooltip("The wordmark starts rising in (s).")]
        [Range(0f, 4f)]
        [SerializeField] private float _wordIn0 = 1.50f;

        [Tooltip("The wordmark is in place (s).")]
        [Range(0f, 4f)]
        [SerializeField] private float _wordIn1 = 2.20f;

        [Tooltip("How far below its place the wordmark starts (px of 1024).")]
        [Range(0f, 40f)]
        [SerializeField] private float _wordRisePixels = 8f;

        [Tooltip("Ring, stroke and wordmark start dissolving (s).")]
        [Range(0f, 8f)]
        [SerializeField] private float _dissolve0 = 3.12f;

        [Tooltip("Ring, stroke and wordmark are gone (s).")]
        [Range(0f, 8f)]
        [SerializeField] private float _dissolve1 = 3.84f;

        [Tooltip("The lingering glint starts fading (s).")]
        [Range(0f, 8f)]
        [SerializeField] private float _glintOut0 = 3.48f;

        [Tooltip("The glint is gone (s; the splash's end).")]
        [Range(0f, 8f)]
        [SerializeField] private float _glintOut1 = 4.00f;

        [Tooltip("Seconds a skipped splash takes to fade out (any key or click, never on the very first launch).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _skipFadeSeconds = 0.25f;

        public Texture2D Ring => _ring;

        public Texture2D Stroke => _stroke;

        public Texture2D Glint => _glint;

        public Texture2D Wordmark => _wordmark;

        public float Duration => _duration;

        public float MaxStep => _maxStep;

        public float HeightShare => _heightShare;

        public float RingIn0 => _ringIn0;

        public float RingIn1 => _ringIn1;

        public float StrokeIn0 => _strokeIn0;

        public float StrokeIn1 => _strokeIn1;

        public float StrokeDraw0 => _strokeDraw0;

        public float StrokeDraw1 => _strokeDraw1;

        /// <summary>The stroke art's left and right edges as shares of the layer's width.</summary>
        public Vector2 StrokeSpan => _strokeSpanPixels / LayerPixels;

        public float GlintIn0 => _glintIn0;

        public float GlintIn1 => _glintIn1;

        public float GlintPeak => _glintPeak;

        public float GlintSettled => _glintSettled;

        public float GlintStartScale => _glintStartScale;

        public float GlintPeakScale => _glintPeakScale;

        public float GlintEndScale => _glintEndScale;

        /// <summary>The glint's centre as shares of the layer (x right, y down).</summary>
        public Vector2 GlintCentre => _glintCentrePixels / LayerPixels;

        public float WordIn0 => _wordIn0;

        public float WordIn1 => _wordIn1;

        /// <summary>The wordmark's starting drop as a share of the layer's height.</summary>
        public float WordRise => _wordRisePixels / LayerPixels;

        public float Dissolve0 => _dissolve0;

        public float Dissolve1 => _dissolve1;

        public float GlintOut0 => _glintOut0;

        public float GlintOut1 => _glintOut1;

        public float SkipFadeSeconds => _skipFadeSeconds;

        /// <summary>Null when usable, else the first problem.</summary>
        public string Validate()
        {
            if (_ring == null || _stroke == null || _glint == null || _wordmark == null)
            {
                return "the boot splash's layers are not assigned (run the UI/Tuning builder)";
            }

            return _glintOut1 > _duration ? "the boot splash's glint outlasts its duration" : null;
        }
    }
}
