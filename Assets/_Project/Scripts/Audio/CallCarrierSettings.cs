using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How the call sounds (docs/design/opening-v3-purpose.md §2): the carrier's level and how it swells as 07 faces
    /// the light beside Earth, how it breaks up while 07 is on reserve, and the static under it until the night clears
    /// it. Every level is a share of the cue's own volume; the carrier stays under the ambience.
    /// </summary>
    [Serializable]
    public sealed class CallCarrierSettings
    {
        private const float MinSpan = 0.01f;

        [Header("Level and facing")]
        [Tooltip("The carrier's level when 07 faces the light squarely (share of the cue volume).")]
        [Range(0f, 1f)]
        [SerializeField] private float _level = 0.6f;

        [Tooltip("Share of the level left when 07 faces directly away (it never quite goes: the call is always there).")]
        [Range(0f, 1f)]
        [SerializeField] private float _awayFloor = 0.15f;

        [Tooltip("How sharply the swell gathers toward facing the light (1 = even over the turn, higher = mostly at " +
                 "the end, like tuning a radio).")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _facingSharpness = 2.5f;

        [Tooltip("Seconds for the level to follow 07's turning (time constant).")]
        [Range(0.01f, 2f)]
        [SerializeField] private float _facingSmoothing = 0.25f;

        [Tooltip("Metres from 07 toward the light where the call is placed: inside the mix's near distance, so it pans " +
                 "from the sky point at full level.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _placementDistance = 5f;

        [Tooltip("3D amount of the call (1 = fully from the sky point, lower keeps it closer to centre).")]
        [Range(0f, 1f)]
        [SerializeField] private float _spatialBlend = 0.8f;

        [Tooltip("Each decoded character's note (share of the cue volume): very quiet.")]
        [Range(0f, 1f)]
        [SerializeField] private float _decodedVolume = 0.6f;

        [Header("Broken (on reserve)")]
        [Tooltip("Shortest dropout, seconds.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _dropoutMin = 0.08f;

        [Tooltip("Longest dropout, seconds.")]
        [Range(0.02f, 2f)]
        [SerializeField] private float _dropoutMax = 0.45f;

        [Tooltip("Shortest stretch of signal between dropouts, seconds.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _holdMin = 0.3f;

        [Tooltip("Longest stretch of signal between dropouts, seconds.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _holdMax = 1.6f;

        [Tooltip("Gain left inside a dropout (0 = gone).")]
        [Range(0f, 1f)]
        [SerializeField] private float _dropoutGain = 0.08f;

        [Tooltip("Seconds for a dropout to fall and recover (keeps the gaps soft, never clicky).")]
        [Range(0.005f, 0.2f)]
        [SerializeField] private float _dropoutEase = 0.04f;

        [Tooltip("Wow depth while broken: pitch wanders this far either way (0.01 = 1%).")]
        [Range(0f, 0.05f)]
        [SerializeField] private float _wowDepth = 0.012f;

        [Tooltip("Wow rate while broken, cycles per second.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _wowRate = 0.4f;

        [Header("Static")]
        [Tooltip("Static under the carrier while broken (share of the static cue's volume).")]
        [Range(0f, 1f)]
        [SerializeField] private float _brokenStatic = 0.35f;

        [Tooltip("Static under the carrier once Locked; Clear and Answered have none.")]
        [Range(0f, 1f)]
        [SerializeField] private float _lockedStatic = 0.15f;

        [Tooltip("Seconds for the static and the break-up to settle into a new state.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _stateFade = 1.2f;

        public float Level => _level;
        public float AwayFloor => _awayFloor;
        public float FacingSharpness => _facingSharpness;
        public float FacingSmoothing => _facingSmoothing;
        public float PlacementDistance => _placementDistance;
        public float SpatialBlend => _spatialBlend;
        public float DecodedVolume => _decodedVolume;
        public float DropoutMin => _dropoutMin;
        public float DropoutMax => Mathf.Max(_dropoutMax, _dropoutMin + MinSpan);
        public float HoldMin => _holdMin;
        public float HoldMax => Mathf.Max(_holdMax, _holdMin + MinSpan);
        public float DropoutGain => _dropoutGain;
        public float DropoutEase => _dropoutEase;
        public float WowDepth => _wowDepth;
        public float WowRate => _wowRate;
        public float BrokenStatic => _brokenStatic;
        public float LockedStatic => _lockedStatic;
        public float StateFade => _stateFade;
    }
}
