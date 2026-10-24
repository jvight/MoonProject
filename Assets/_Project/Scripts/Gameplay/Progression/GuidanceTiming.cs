using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// When the opening's objective line speaks (docs/design/opening-v3-guidance-audit.md G2, G4): briefly after each
    /// success, and as a safety net for a player with no progress, moving or still.
    /// </summary>
    [Serializable]
    public sealed class GuidanceTiming
    {
        [Tooltip("Seconds the next line stays up after a success (G2).")]
        [Range(2f, 20f)] [SerializeField] private float _nextLineSeconds = 6f;

        [Tooltip("Seconds with no progress, moving or still, before the step's line comes back as a reminder (G4).")]
        [Range(10f, 600f)] [SerializeField] private float _stallSeconds = 25f;

        [Tooltip("Seconds more with no progress before the reminder comes back once more; never a third time (G4).")]
        [Range(10f, 600f)] [SerializeField] private float _againSeconds = 90f;

        [Tooltip("Seconds a reminder stays up.")]
        [Range(5f, 60f)] [SerializeField] private float _reminderSeconds = 20f;

        [Tooltip("A key unused for this long counts as new again: the line leads with its glyph (G2).")]
        [Range(30f, 1800f)] [SerializeField] private float _keyFreshSeconds = 300f;

        [Tooltip("07 closing on the line's place at least this fast (m/s) is progress: no reminder while it drives there.")]
        [Range(0.1f, 3f)] [SerializeField] private float _closingSpeed = 0.5f;

        [Tooltip("Metres from a place within which 07 has arrived: a line that only sends 07 there goes quiet, and " +
                 "the place's own chips lead.")]
        [Range(2f, 40f)] [SerializeField] private float _arrivalReach = 15f;

        public GuidanceTiming()
        {
        }

        public GuidanceTiming(float nextLineSeconds, float stallSeconds, float againSeconds, float reminderSeconds,
            float keyFreshSeconds, float closingSpeed, float arrivalReach)
        {
            _closingSpeed = closingSpeed;
            _arrivalReach = arrivalReach;
            _nextLineSeconds = nextLineSeconds;
            _stallSeconds = stallSeconds;
            _againSeconds = againSeconds;
            _reminderSeconds = reminderSeconds;
            _keyFreshSeconds = keyFreshSeconds;
        }

        public float NextLineSeconds => _nextLineSeconds;
        public float StallSeconds => _stallSeconds;
        public float AgainSeconds => _againSeconds;
        public float ReminderSeconds => _reminderSeconds;
        public float KeyFreshSeconds => _keyFreshSeconds;
        public float ClosingSpeed => _closingSpeed;
        public float ArrivalReach => _arrivalReach;
    }
}
