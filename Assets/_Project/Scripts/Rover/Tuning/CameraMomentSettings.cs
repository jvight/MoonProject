using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// One slow, eased camera moment (design: cinematic moments use eased blends, never cuts): how long it eases in,
    /// holds and eases out, and how far it turns, lifts and pulls back to frame 07 together with something it cares
    /// about. Any look input ends it early with a short ease, so it is always skippable.
    /// </summary>
    [Serializable]
    public sealed class CameraMomentSettings
    {
        [Tooltip("Seconds the moment takes to ease in.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _easeIn = 1f;

        [Tooltip("Seconds it holds the framing.")]
        [Range(0f, 10f)]
        [SerializeField] private float _hold = 1f;

        [Tooltip("Seconds it takes to ease back to the normal chase camera.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _easeOut = 1f;

        [Tooltip("How far (0..1) the point the camera looks at moves from 07 toward the subject.")]
        [Range(0f, 1f)]
        [SerializeField] private float _lookShare = 0.5f;

        [Tooltip("How far (0..1) the camera swings round to look from 07 toward the subject.")]
        [Range(0f, 1f)]
        [SerializeField] private float _yawShare = 0.5f;

        [Tooltip("Farthest (m) the look point may move toward the subject, so 07 always stays in frame.")]
        [Range(0f, 30f)]
        [SerializeField] private float _maxLookShift = 4f;

        [Tooltip("Largest swing (deg) round 07, so subjects behind it never whip the camera about.")]
        [Range(0f, 180f)]
        [SerializeField] private float _maxYawSwing = 60f;

        [Tooltip("Extra orbit elevation (deg) at the height of the moment.")]
        [Range(0f, 40f)]
        [SerializeField] private float _lift;

        [Tooltip("Extra orbit distance (fraction of the normal distance) at the height of the moment.")]
        [Range(0f, 2f)]
        [SerializeField] private float _pullBack;

        [Tooltip("Subjects farther than this (m) from 07 are not turned to (the camera only lifts and pulls back).")]
        [Range(1f, 300f)]
        [SerializeField] private float _maxFocusDistance = 40f;

        public CameraMomentSettings()
        {
        }

        public CameraMomentSettings(float easeIn, float hold, float easeOut, float lookShare, float maxLookShift,
            float yawShare, float maxYawSwing, float lift, float pullBack, float maxFocusDistance)
        {
            _maxLookShift = maxLookShift;
            _easeIn = easeIn;
            _hold = hold;
            _easeOut = easeOut;
            _lookShare = lookShare;
            _yawShare = yawShare;
            _maxYawSwing = maxYawSwing;
            _lift = lift;
            _pullBack = pullBack;
            _maxFocusDistance = maxFocusDistance;
        }

        public float EaseIn => _easeIn;

        public float Hold => _hold;

        public float EaseOut => _easeOut;

        public float LookShare => _lookShare;

        public float MaxLookShift => _maxLookShift;

        public float YawShare => _yawShare;

        public float MaxYawSwing => _maxYawSwing;

        public float Lift => _lift;

        public float PullBack => _pullBack;

        public float MaxFocusDistance => _maxFocusDistance;

        public float Duration => _easeIn + _hold + _easeOut;
    }
}
