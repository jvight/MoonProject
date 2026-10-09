using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The charging dock's sound over time (M3-14): docking brings in a very soft charging hum; after the charge time
    /// 07 is full - a gentle tone, once, and the hum settles away; leaving the dock lets the hum go at once (with the
    /// release click, the caller's). Allocation-free.
    /// </summary>
    public sealed class ChargeModel
    {
        private readonly StationAudioTuning _tuning;
        private readonly LoopFader _hum = new LoopFader();
        private float _docked;
        private bool _charging;
        private bool _fullDue;

        public ChargeModel(StationAudioTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>The charging hum's level 0..1 (eased).</summary>
        public float Hum => _hum.Gain;

        public bool HumAudible => _hum.IsAudible;

        public void Dock()
        {
            _docked = 0f;
            _charging = true;
            _hum.FadeIn();
        }

        public void Undock()
        {
            _charging = false;
            _hum.FadeOut();
        }

        public void Step(float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            if (_charging)
            {
                _docked += dt;
                if (_docked >= _tuning.ChargeSeconds)
                {
                    _charging = false;
                    _fullDue = true;
                    _hum.FadeOut();
                }
            }

            _hum.Step(dt, _tuning.ChargeFadeIn, _tuning.ChargeFadeOut);
        }

        /// <summary>True once when 07 has become full on the dock; clears it.</summary>
        public bool TakeFull()
        {
            bool due = _fullDue;
            _fullDue = false;
            return due;
        }
    }
}
