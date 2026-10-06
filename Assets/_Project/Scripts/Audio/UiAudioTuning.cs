using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How UI touches sound under repetition and how the hold swell lets go. Created by the Audio/Tuning builder;
    /// runtime code only reads it.
    /// </summary>
    public sealed class UiAudioTuning : ScriptableObject
    {
        [Header("Rapid repeats (focus steps, slider ticks)")]
        [Tooltip("Focus ticks closer together than this (seconds) are skipped.")]
        [Range(0f, 0.3f)] [SerializeField] private float _focusMinInterval = 0.05f;

        [Tooltip("Slider ticks closer together than this (seconds) are skipped.")]
        [Range(0f, 0.3f)] [SerializeField] private float _sliderMinInterval = 0.045f;

        [Tooltip("Repeats within this many seconds of the previous one count as a streak and get softer.")]
        [Range(0.05f, 1f)] [SerializeField] private float _streakWindow = 0.35f;

        [Tooltip("Volume multiplier applied per repeat within a streak.")]
        [Range(0.5f, 1f)] [SerializeField] private float _streakDecay = 0.86f;

        [Tooltip("Softest a streak gets (volume scale).")]
        [Range(0.1f, 1f)] [SerializeField] private float _streakFloor = 0.55f;

        [Header("Hold swell")]
        [Tooltip("Seconds for the hold swell to hand over to the purchase chord when the ring fills.")]
        [Range(0.01f, 0.5f)] [SerializeField] private float _holdCompleteFade = 0.08f;

        [Tooltip("Seconds of soft release tail when the hold is let go early (or the menu opens over it).")]
        [Range(0.01f, 1f)] [SerializeField] private float _holdReleaseFade = 0.22f;

        public float FocusMinInterval => _focusMinInterval;
        public float SliderMinInterval => _sliderMinInterval;
        public float StreakWindow => _streakWindow;
        public float StreakDecay => _streakDecay;
        public float StreakFloor => _streakFloor;
        public float HoldCompleteFade => _holdCompleteFade;
        public float HoldReleaseFade => _holdReleaseFade;
    }
}
