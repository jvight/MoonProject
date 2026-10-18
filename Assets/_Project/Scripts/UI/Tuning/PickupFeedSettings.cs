using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The pickup feed: when 07's metal, wiring or optics grow, a small glass row slides in at the left, rolls its
    /// "+N" one unit at a time, shows the new total, lingers a moment and leaves. Its rolling pace also sets the pace of
    /// every counter in the UI (<see cref="CountUp"/>).
    /// </summary>
    [Serializable]
    public sealed class PickupFeedSettings
    {
        [Tooltip("A row easing in and out (fade and a soft settle; the slide in from the left is SlideDistance).")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.4f, 0.7f, 0f, 0.96f, 1.8f, 0.6f);

        [Tooltip("Rows the feed shows at once, shared by every material: a new pickup takes a free row or the one that " +
                 "has rested longest.")]
        [Range(1, 6)]
        [SerializeField] private int _maxRows = 3;

        [Tooltip("Rows for story parts (the spare igniter, a core sample), apart from the material rows: a part " +
                 "takes a free row or the one that has rested longest.")]
        [Range(1, 4)]
        [SerializeField] private int _partRows = 2;

        [Tooltip("Seconds a story part's row stays up before it fades (a part is read, not counted).")]
        [Range(1f, 10f)]
        [SerializeField] private float _partLingerSeconds = 4f;

        [Tooltip("Pixels (at 1080p) a row slides in from the left as it appears.")]
        [Range(0f, 200f)]
        [SerializeField] private float _slideDistance = 72f;

        [Tooltip("Seconds between one unit and the next as a count rolls (the count never jumps).")]
        [Range(0.02f, 0.3f)]
        [SerializeField] private float _stepSeconds = 0.08f;

        [Tooltip("Longest roll, even for a big haul: a bigger change steps quicker to stay within it.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _maxCountSeconds = 1.4f;

        [Tooltip("Seconds a row lingers after its count settles before it fades away.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _lingerSeconds = 2.5f;

        [Tooltip("Seconds of the small pulse each unit of a rising count gives its icon.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _tickPulseSeconds = 0.14f;

        [Tooltip("How much that icon swells at the top of a unit's pulse (0.1 = 10 % larger).")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _tickPulseScale = 0.12f;

        public RevealSettings Reveal => _reveal;

        public int MaxRows => _maxRows;

        public int PartRows => _partRows;

        public float PartLingerSeconds => _partLingerSeconds;

        public float SlideDistance => _slideDistance;

        public float StepSeconds => _stepSeconds;

        public float MaxCountSeconds => _maxCountSeconds;

        public float LingerSeconds => _lingerSeconds;

        public float TickPulseSeconds => _tickPulseSeconds;

        public float TickPulseScale => _tickPulseScale;
    }
}
