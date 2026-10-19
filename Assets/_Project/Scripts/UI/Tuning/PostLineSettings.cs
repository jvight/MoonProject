using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>How 07's power-on self-test types its lines on the CRT (<see cref="PostLog"/>).</summary>
    [Serializable]
    public sealed class PostLineSettings
    {
        [Tooltip("Characters typed per second. The shortest line lasts 0.6 s, so a line must type well inside it.")]
        [Range(15f, 120f)]
        [SerializeField] private float _charsPerSecond = 90f;

        [Tooltip("Longest a line may take to type: a long block (the task queue) types faster so it fits its step.")]
        [Range(0.3f, 2f)]
        [SerializeField] private float _maxLineSeconds = 0.5f;

        [Tooltip("Seconds between the dots of a check still running (up to three, then they start again).")]
        [Range(0.1f, 0.6f)]
        [SerializeField] private float _dotSeconds = 0.12f;

        [Tooltip("Days 07 stood still before the carrier woke it (story v4: 28 Oct 1988 to the 2024 eclipse).")]
        [Min(0)]
        [SerializeField] private int _resumeDays = 13148;

        [Tooltip("Seconds the finished log stays on the CRT before 07's face takes it back.")]
        [Range(0f, 4f)]
        [SerializeField] private float _lingerSeconds = 0.3f;

        public float CharsPerSecond => _charsPerSecond;

        public float MaxLineSeconds => _maxLineSeconds;

        public float DotSeconds => _dotSeconds;

        public int ResumeDays => _resumeDays;

        public float LingerSeconds => _lingerSeconds;
    }
}
