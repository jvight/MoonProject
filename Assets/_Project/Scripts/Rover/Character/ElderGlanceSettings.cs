using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's one look at its elder "06" by relay.2's hut (docs/design/0.5.5-owner-feedback.md §1.7, M1): how near it
    /// comes before it notices, how long it holds the look and where on 06 it looks. Quiet, never dramatic.
    /// </summary>
    [Serializable]
    public sealed class ElderGlanceSettings
    {
        [Tooltip("How near (m, flat) 07 comes to the micro.06 site before it notices 06 (its first visit only).")]
        [Range(2f, 40f)]
        [SerializeField] private float _noticeRadius = 12f;

        [Tooltip("Seconds 07 holds its look at 06: a pause, not a stare.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _holdSeconds = 1.5f;

        [Tooltip("Height (m) above the site where 07 looks: 06's slumped head.")]
        [Range(0f, 2f)]
        [SerializeField] private float _lookHeight = 0.5f;

        [Tooltip("Gaze priority of the look (above idle glances, below work like a cut or a scan).")]
        [Range(0, 10)]
        [SerializeField] private int _gazePriority = 5;

        [Tooltip("The chirp's intensity: soft and low, curious rather than sad.")]
        [Range(0f, 1f)]
        [SerializeField] private float _chirpIntensity = 0.25f;

        public float NoticeRadius => _noticeRadius;

        public float HoldSeconds => _holdSeconds;

        public float LookHeight => _lookHeight;

        public int GazePriority => _gazePriority;

        public float ChirpIntensity => _chirpIntensity;
    }
}
