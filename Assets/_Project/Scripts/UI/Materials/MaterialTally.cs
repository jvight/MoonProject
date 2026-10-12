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

        /// <summary>Takes <paramref name="amount"/> as it is, with no count (a loaded save).</summary>
        public void Snap(SalvageMaterial material, int amount)
        {
            int i = (int)material;
            _targets[i] = amount;
            _counts[i].Snap(amount);
            _tickAge[i] = float.PositiveInfinity;
        }

        /// <summary>A new amount: the material counts to it, one unit at a time.</summary>
        /// <returns>True when the amount changed.</returns>
        public bool Apply(SalvageMaterial material, int amount)
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
    }
}
