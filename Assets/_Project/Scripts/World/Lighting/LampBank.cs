using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// A row of lamps that each ease toward their own target level (0 dark .. 1 lit): a lamp that lights in play warms
    /// up over the fade, one restored from a save is set at once. Plain logic, allocation-free after construction.
    /// </summary>
    public sealed class LampBank
    {
        private readonly float[] _level;
        private readonly float[] _target;

        public LampBank(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "A lamp bank needs zero or more lamps.");
            }

            _level = new float[count];
            _target = new float[count];
        }

        public int Count => _level.Length;

        /// <summary>Lamp <paramref name="index"/>'s level now (0..1).</summary>
        public float Level(int index)
        {
            return _level[index];
        }

        /// <summary>Aims lamp <paramref name="index"/> at <paramref name="target"/>; <paramref name="instant"/> jumps there.</summary>
        public void Set(int index, float target, bool instant)
        {
            _target[index] = Mathf.Clamp01(target);
            if (instant)
            {
                _level[index] = _target[index];
            }
        }

        /// <summary>Eases every lamp toward its target, a full swing taking <paramref name="fadeSeconds"/>.</summary>
        public void Tick(float deltaTime, float fadeSeconds)
        {
            float step = fadeSeconds > 0f ? deltaTime / fadeSeconds : 1f;
            for (int i = 0; i < _level.Length; i++)
            {
                _level[i] = Mathf.MoveTowards(_level[i], _target[i], step);
            }
        }
    }
}
