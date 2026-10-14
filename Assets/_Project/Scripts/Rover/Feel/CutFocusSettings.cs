using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's head and eye through a cut (docs/design/feel-pass.md §3.1, Body column): it turns to the cut point, its
    /// lens narrows and glows brighter while it works, it nods at ignition, then follows the piece.
    /// </summary>
    [Serializable]
    public sealed class CutFocusSettings
    {
        [Tooltip("Eye glow while cutting (multiplier: 1.3 = +30%).")]
        [Range(1f, 3f)]
        [SerializeField] private float _eyeGlow = 1.3f;

        [Tooltip("How far the lid narrows the lens while cutting (share of fully closed).")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _irisNarrow = 0.25f;

        [Tooltip("Half-life (s) of the focus easing in and out.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _halfLife = 0.12f;

        [Tooltip("Gaze priority of the cut point and the piece (above idle glances).")]
        [Range(0, 20)]
        [SerializeField] private int _gazePriority = 7;

        [Tooltip("How long (s) 07 keeps looking after the break or a piece's last follow beat before letting go.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _followSeconds = 1.5f;

        [Tooltip("Ignition: the head's nod (deg down), recovering in ~250 ms.")]
        [SerializeField] private NudgeSettings _nod = new NudgeSettings(0f, 2f, 4f, 1f);

        public float EyeGlow => _eyeGlow;

        public float IrisNarrow => _irisNarrow;

        public float HalfLife => _halfLife;

        public int GazePriority => _gazePriority;

        public float FollowSeconds => _followSeconds;

        public NudgeSettings Nod => _nod;
    }
}
