using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The quiet answer to a held E that cannot work (InteractionBlocked, pure, EditMode-tested): the verb chip dims and
    /// gives a small fading shake, and 07's face says why for a moment. No panel of its own, so the one guidance
    /// element stays one.
    /// </summary>
    internal sealed class BlockedCue
    {
        private readonly GuidanceSettings _settings;
        private float _age;
        private bool _active;

        public BlockedCue(GuidanceSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>True while either half of the cue is still playing.</summary>
        public bool IsActive => _active;

        /// <summary>True while 07's face shows the reason.</summary>
        public bool FaceShown => _active && _age < _settings.BlockedFaceSeconds;

        /// <summary>The chip's sideways offset now (px): a shake that fades out over the chip's part of the cue.</summary>
        public float ChipOffset
        {
            get
            {
                float t = ChipT;
                return t >= 1f ? 0f : _settings.BlockedShakePixels * (1f - t) *
                                      Mathf.Sin(2f * Mathf.PI * _settings.BlockedShakeHertz * _age);
            }
        }

        /// <summary>The chip's opacity now: dimmed at once, easing back to full.</summary>
        public float ChipOpacity => Mathf.Lerp(_settings.BlockedChipOpacity, 1f, Mathf.SmoothStep(0f, 1f, ChipT));

        private float ChipT => _active ? Mathf.Clamp01(_age / _settings.BlockedChipSeconds) : 1f;

        /// <summary>A held E was blocked: the cue starts over.</summary>
        public void Start()
        {
            _age = 0f;
            _active = true;
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        public void Tick(float deltaTime)
        {
            if (!_active)
            {
                return;
            }

            _age += deltaTime;
            _active = _age < Mathf.Max(_settings.BlockedChipSeconds, _settings.BlockedFaceSeconds);
        }
    }
}
