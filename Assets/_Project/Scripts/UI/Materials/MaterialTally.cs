using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The materials chip's numbers (pure logic, EditMode-tested): per material, a count that eases to each new amount
    /// and, when the amount grew, a glow that swells and settles over <see cref="MaterialsChipSettings.PulseSeconds"/>.
    /// Each unit of a rising count also gives a small pulse (<see cref="TickPulse"/>). A material that was spent counts
    /// down without a glow or pulses. Allocation-free.
    /// </summary>
    internal sealed class MaterialTally
    {
        private readonly MaterialsChipSettings _settings;
        private readonly CountUp[] _counts;
        private readonly int[] _targets;
        private readonly float[] _pulseAge;
        private readonly float[] _tickAge;

        public MaterialTally(MaterialsChipSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _counts = new CountUp[Materials.Count];
            _targets = new int[Materials.Count];
            _pulseAge = new float[Materials.Count];
            _tickAge = new float[Materials.Count];
            for (int i = 0; i < _counts.Length; i++)
            {
                _counts[i] = new CountUp(settings);
                _pulseAge[i] = float.PositiveInfinity;
                _tickAge[i] = float.PositiveInfinity;
            }
        }

        /// <summary>True when every count has reached its amount.</summary>
        public bool IsSettled
        {
            get
            {
                for (int i = 0; i < _counts.Length; i++)
                {
                    if (!_counts[i].IsSettled)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>True while any material still glows.</summary>
        public bool IsGlowing
        {
            get
            {
                for (int i = 0; i < _pulseAge.Length; i++)
                {
                    if (_pulseAge[i] < _settings.PulseSeconds)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>The number to show for <paramref name="material"/> right now.</summary>
        public int Shown(SalvageMaterial material)
        {
            return _counts[(int)material].Shown;
        }

        /// <summary>The units <paramref name="material"/>'s count moved in the last step, signed.</summary>
        public int Moved(SalvageMaterial material)
        {
            return _counts[(int)material].Moved;
        }

        /// <summary>The units <paramref name="material"/>'s count has rolled in this roll so far.</summary>
        public int Rolled(SalvageMaterial material)
        {
            return _counts[(int)material].Rolled;
        }

        /// <summary>The units <paramref name="material"/>'s count still has to roll.</summary>
        public int Remaining(SalvageMaterial material)
        {
            return _counts[(int)material].Remaining;
        }

        /// <summary>0..1 of the small pulse the last unit of a rising count gave <paramref name="material"/>.</summary>
        public float TickPulse(SalvageMaterial material)
        {
            float age = _tickAge[(int)material];
            float duration = _settings.TickPulseSeconds;
            return age < duration ? Mathf.Sin(Mathf.PI * age / duration) : 0f;
        }

        /// <summary>The amount <paramref name="material"/> is counting towards.</summary>
        public int Target(SalvageMaterial material)
        {
            return _targets[(int)material];
        }

        /// <summary>0..1 how strongly <paramref name="material"/> glows (a swell and settle after it grew).</summary>
        public float Glow(SalvageMaterial material)
        {
            float age = _pulseAge[(int)material];
            float duration = _settings.PulseSeconds;
            return age < duration ? Mathf.Sin(Mathf.PI * age / duration) : 0f;
        }

        /// <summary>Takes the amounts as they are, with no count and no glow (a loaded save).</summary>
        public void Snap(int metal, int wiring, int optics)
        {
            SnapOne(SalvageMaterial.Metal, metal);
            SnapOne(SalvageMaterial.Wiring, wiring);
            SnapOne(SalvageMaterial.Optics, optics);
        }

        /// <summary>New amounts: each changed material counts to its amount; one that grew glows.</summary>
        /// <returns>True when anything changed.</returns>
        public bool Apply(int metal, int wiring, int optics)
        {
            bool metalChanged = ApplyOne(SalvageMaterial.Metal, metal);
            bool wiringChanged = ApplyOne(SalvageMaterial.Wiring, wiring);
            bool opticsChanged = ApplyOne(SalvageMaterial.Optics, optics);
            return metalChanged || wiringChanged || opticsChanged;
        }

        /// <summary>Advances the counts and glows; returns true when a shown number changed.</summary>
        public bool Step(float deltaTime)
        {
            bool changed = false;
            for (int i = 0; i < _counts.Length; i++)
            {
                bool moved = _counts[i].Step(deltaTime);
                changed |= moved;
                if (_pulseAge[i] < _settings.PulseSeconds)
                {
                    _pulseAge[i] += deltaTime;
                }

                if (moved && _counts[i].Moved > 0)
                {
                    _tickAge[i] = 0f;
                }
                else if (_tickAge[i] < _settings.TickPulseSeconds)
                {
                    _tickAge[i] += deltaTime;
                }
            }

            return changed;
        }

        private void SnapOne(SalvageMaterial material, int amount)
        {
            int i = (int)material;
            _targets[i] = amount;
            _counts[i].Snap(amount);
            _pulseAge[i] = float.PositiveInfinity;
            _tickAge[i] = float.PositiveInfinity;
        }

        private bool ApplyOne(SalvageMaterial material, int amount)
        {
            int i = (int)material;
            if (amount == _targets[i])
            {
                return false;
            }

            if (amount > _targets[i])
            {
                _pulseAge[i] = 0f;
            }

            _targets[i] = amount;
            _counts[i].SetTarget(amount);
            return true;
        }
    }
}
