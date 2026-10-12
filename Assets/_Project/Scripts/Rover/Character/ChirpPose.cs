using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's body in time with its chirp (docs/design/feel-pass.md §7.2) as plain logic: each syllable pulses the eye;
    /// the phrase's mood poses the head (Curious tilts it, Worried dips it, Tired lowers it slowly), held a moment
    /// after the last syllable and then eased back. Happy's bounce and the antenna flick are kicks the caller applies.
    /// </summary>
    public sealed class ChirpPose
    {
        private readonly ChirpSettings _settings;
        private ChirpMood _mood;
        private float _pulseLeft;
        private float _poseLeft;
        private float _roll;
        private float _dip;

        public ChirpPose(ChirpSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Eye glow multiplier from the syllable pulse (1 = none).</summary>
        public float EyeGlow => 1f + (_pulseLeft > 0f ? _settings.EyePulse : 0f);

        /// <summary>The head's sideways tilt (deg).</summary>
        public float HeadRoll => _roll;

        /// <summary>How far the head dips (deg down).</summary>
        public float HeadDip => _dip;

        /// <summary>A syllable of <paramref name="mood"/> starts, lasting <paramref name="seconds"/>.</summary>
        public void Syllable(ChirpMood mood, int index, int count, float seconds)
        {
            _mood = mood;
            _pulseLeft = _settings.EyePulseSeconds;
            float left = seconds + (index >= count - 1 ? _settings.PoseHold : seconds);
            _poseLeft = Mathf.Max(_poseLeft, left);
        }

        public void Step(float deltaTime)
        {
            _pulseLeft -= deltaTime;
            _poseLeft -= deltaTime;
            bool posing = _poseLeft > 0f;
            float roll = posing && _mood == ChirpMood.Curious ? _settings.CuriousTilt : 0f;
            float dip = !posing ? 0f
                : _mood == ChirpMood.Worried ? _settings.WorriedDip
                : _mood == ChirpMood.Tired ? _settings.TiredLower
                : 0f;
            float dipHalfLife = _mood == ChirpMood.Tired ? _settings.TiredHalfLife : _settings.PoseHalfLife;
            _roll = Smoothing.Damp(_roll, roll, _settings.PoseHalfLife, deltaTime);
            _dip = Smoothing.Damp(_dip, dip, dipHalfLife, deltaTime);
        }
    }
}
