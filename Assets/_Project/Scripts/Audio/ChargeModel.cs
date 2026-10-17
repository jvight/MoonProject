using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// A charger's sound (M3-14, M3-20): while a charger fills 07's cells a very soft hum breathes; when the last cell
    /// fills, a gentle "full" tone, once, and the hum settles away; driving off lets the hum go. Allocation-free.
    /// </summary>
    public sealed class ChargeModel
    {
        private readonly FeelAudioTuning _tuning;
        private readonly LoopFader _hum = new LoopFader();
        private bool _fullDue;

        public ChargeModel(FeelAudioTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>The charging hum's level 0..1 (eased).</summary>
        public float Hum => _hum.Gain;

        public bool HumAudible => _hum.IsAudible;

        /// <summary>A charger started filling the cells.</summary>
        public void Begin()
        {
            _hum.FadeIn();
        }

        /// <summary>The charger stopped: <paramref name="full"/> when the cells are full, false when 07 drove off.</summary>
        public void End(bool full)
        {
            _fullDue |= full;
            _hum.FadeOut();
        }

        public void Step(float deltaTime)
        {
            _hum.Step(Mathf.Max(0f, deltaTime), _tuning.ChargeFadeIn, _tuning.ChargeFadeOut);
        }

        /// <summary>True once when 07 has become full on a charger; clears it.</summary>
        public bool TakeFull()
        {
            bool due = _fullDue;
            _fullDue = false;
            return due;
        }
    }
}
