using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's antenna once it can listen (docs/design/first-hour-v2.md §3): from tower level 1 the tip lights cyan, and
    /// each time 07 listens the antenna lifts a little and the tip blinks. Soft edges, nothing faster than 3 per second.
    /// </summary>
    [Serializable]
    public sealed class AntennaListenSettings
    {
        [Tooltip("Tip tint once the tower is awake. It multiplies the tip's warm authored glow, so green and blue sit " +
                 "above 1 to read as the tech cyan (#5FF3FF).")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _litTint = new Color(0.37f, 1.2f, 1.85f, 1f);

        [Tooltip("Steady tip glow added once the tower is awake (linear fraction of authored).")]
        [Range(0f, 2f)]
        [SerializeField] private float _litGlow = 0.35f;

        [Tooltip("Half-life (s) of the tip lighting up when the tower wakes (a loaded game starts lit).")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _litHalfLife = 0.8f;

        [Tooltip("Blinks of the tip each time 07 listens.")]
        [Range(1, 4)]
        [SerializeField] private int _blinkCount = 2;

        [Tooltip("Seconds per blink. At least 1/3 s: nothing blinks at 3 Hz or faster.")]
        [Range(0.34f, 2f)]
        [SerializeField] private float _blinkPeriod = 0.5f;

        [Tooltip("Extra tip glow at the top of a blink (linear fraction of authored).")]
        [Range(0f, 4f)]
        [SerializeField] private float _blinkGlow = 1.2f;

        [Tooltip("Metres the antenna rises along its own axis while 07 listens.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _liftHeight = 0.05f;

        [Tooltip("Seconds the antenna takes to rise (eased out).")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _liftRise = 0.35f;

        [Tooltip("Seconds the antenna stays up, listening.")]
        [Range(0f, 5f)]
        [SerializeField] private float _liftHold = 1.2f;

        [Tooltip("Seconds the antenna takes to settle back (eased in and out).")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _liftFall = 0.8f;

        public Color LitTint => _litTint;
        public float LitGlow => _litGlow;
        public float LitHalfLife => _litHalfLife;
        public int BlinkCount => _blinkCount;
        public float BlinkPeriod => _blinkPeriod;
        public float BlinkGlow => _blinkGlow;
        public float LiftHeight => _liftHeight;
        public float LiftRise => _liftRise;
        public float LiftHold => _liftHold;
        public float LiftFall => _liftFall;
    }
}
