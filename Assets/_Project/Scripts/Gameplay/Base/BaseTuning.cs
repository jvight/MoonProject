using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The dock lock on a charging pad 07 built (docs/design/feel-pass.md §7.1, opening-v2 §5): how its walls rise,
    /// clamp and lower, and how its strips fill with 07's cells. Created by the Gameplay/Tuning builder; runtime code
    /// only reads it.
    /// </summary>
    public sealed class BaseTuning : ScriptableObject
    {
        [Header("Dock lock (feel-pass §7.1)")]
        [Tooltip("Seconds between one wall starting to rise (or lower) and the next.")]
        [Range(0f, 0.3f)] [SerializeField] private float _wallStagger = 0.06f;

        [Tooltip("Seconds each wall takes to rise when 07 docks to charge.")]
        [Range(0.1f, 1.5f)] [SerializeField] private float _wallRiseSeconds = 0.45f;

        [Tooltip("How far past its top a rising wall overshoots before it settles (ease-out back).")]
        [Range(0f, 3f)] [SerializeField] private float _wallOvershoot = 1.2f;

        [Tooltip("Seconds each wall takes to lower once 07 is full (ease-in).")]
        [Range(0.1f, 2f)] [SerializeField] private float _wallLowerSeconds = 0.6f;

        [Tooltip("Seconds the walls take to drop when 07 drives off before it is full.")]
        [Range(0.05f, 1f)] [SerializeField] private float _wallDropSeconds = 0.25f;

        [Tooltip("Metres a lowered wall sinks below its raised pose, hidden in its slot.")]
        [Range(0.2f, 1f)] [SerializeField] private float _wallSink = 0.42f;

        [Tooltip("Seconds the strips take to sweep from cyan to warm gold when 07 is full, before the walls lower.")]
        [Range(0.1f, 2f)] [SerializeField] private float _fullSweepSeconds = 0.7f;

        [Tooltip("Glow of a wall's strip where its cells are charged.")]
        [Range(0f, 4f)] [SerializeField] private float _stripLit = 3.2f;

        [Tooltip("Glow of a raised wall's strip where its cells are not charged yet.")]
        [Range(0f, 1f)] [SerializeField] private float _stripUnlit = 0.12f;

        [Tooltip("Seconds (time constant) for a strip's glow to follow its cells.")]
        [Range(0f, 1f)] [SerializeField] private float _stripEase = 0.15f;

        [Tooltip("The strips' charging tint (a linear multiplier of the lamp glass's warm emission: blue needs a " +
                 "large gain).")]
        [SerializeField] private Color _stripCharging = new Color(0.12f, 1.6f, 14f, 1f);

        [Tooltip("The strips' full tint, warm gold (a linear multiplier of the lamp glass's warm emission: blue " +
                 "needs a large gain).")]
        [SerializeField] private Color _stripFull = new Color(1f, 0.95f, 0.8f, 1f);

        [Tooltip("Seconds for the rim lamps' chase to run from the outer ring to the inner one as 07 rolls on.")]
        [Range(0.2f, 2f)] [SerializeField] private float _rimChaseSeconds = 0.6f;

        public float WallStagger => _wallStagger;
        public float WallRiseSeconds => _wallRiseSeconds;
        public float WallOvershoot => _wallOvershoot;
        public float WallLowerSeconds => _wallLowerSeconds;
        public float WallDropSeconds => _wallDropSeconds;
        public float WallSink => _wallSink;
        public float FullSweepSeconds => _fullSweepSeconds;
        public float StripLit => _stripLit;
        public float StripUnlit => _stripUnlit;
        public float StripEase => _stripEase;
        public Color StripCharging => _stripCharging;
        public Color StripFull => _stripFull;
        public float RimChaseSeconds => _rimChaseSeconds;
    }
}
