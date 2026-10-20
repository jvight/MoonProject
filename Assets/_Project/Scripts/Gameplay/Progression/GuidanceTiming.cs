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

        public GuidanceTiming()
        {
        }

        public GuidanceTiming(float nextLineSeconds, float stallSeconds, float againSeconds, float reminderSeconds,
            float keyFreshSeconds)
        {
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
    }
}
