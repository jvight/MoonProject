using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The pickup feed's numbers (pure logic, EditMode-tested): per material, a count that rolls to each new amount
    /// one unit at a time, and a small pulse for each unit of a rising count (<see cref="TickPulse"/>). A material
    /// that was spent counts down without pulses. Allocation-free.
    /// </summary>
    internal sealed class MaterialTally
    {
        private readonly PickupFeedSettings _settings;
        private readonly CountUp[] _counts;
        private readonly int[] _targets;
        private readonly float[] _tickAge;

        public MaterialTally(PickupFeedSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _counts = new CountUp[Materials.Count];
            _targets = new int[Materials.Count];
            _tickAge = new float[Materials.Count];
            for (int i = 0; i < _counts.Length; i++)
            {
                _counts[i] = new CountUp(settings);
                _tickAge[i] = float.PositiveInfinity;
            }
        }

        /// <summary>True when <paramref name="material"/>'s count has reached its amount.</summary>
        public bool IsCountSettled(SalvageMaterial material)
        {
            return _counts[(int)material].IsSettled;
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

        /// <summary>Takes the amounts as they are, with no count (a loaded save).</summary>
        public void Snap(int metal, int wiring, int optics)
        {
            SnapOne(SalvageMaterial.Metal, metal);
            SnapOne(SalvageMaterial.Wiring, wiring);
            SnapOne(SalvageMaterial.Optics, optics);
        }

        /// <summary>New amounts: each changed material counts to its amount.</summary>
        /// <returns>True when anything changed.</returns>
        public bool Apply(int metal, int wiring, int optics)
        {
            bool metalChanged = ApplyOne(SalvageMaterial.Metal, metal);
            bool wiringChanged = ApplyOne(SalvageMaterial.Wiring, wiring);
            bool opticsChanged = ApplyOne(SalvageMaterial.Optics, optics);
            return metalChanged || wiringChanged || opticsChanged;
        }

        /// <summary>Advances the counts; returns true when a shown number changed.</summary>
        public bool Step(float deltaTime)
        {
            bool changed = false;
            for (int i = 0; i < _counts.Length; i++)
            {
                bool moved = _counts[i].Step(deltaTime);
                changed |= moved;
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
            _tickAge[i] = float.PositiveInfinity;
        }

        private bool ApplyOne(SalvageMaterial material, int amount)
        {
            int i = (int)material;
            if (amount == _targets[i])
            {
                return false;
            }

            _targets[i] = amount;
            _counts[i].SetTarget(amount);
            return true;
        }
    }
}
