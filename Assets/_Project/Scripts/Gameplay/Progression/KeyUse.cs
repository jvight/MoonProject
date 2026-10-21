using System;
using MoonProject.Core.Input;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Which of 07's action keys the player has pressed, and how long ago (the next line's key glyph, guidance audit
    /// G2). Not saved: after a load every key reads as new, so its glyph shows once more. Allocation-free.
    /// </summary>
    public sealed class KeyUse
    {
        private readonly bool[] _pressed;
        private readonly float[] _since;

        public KeyUse()
        {
            int keys = 0;
            foreach (RoverAction action in Enum.GetValues(typeof(RoverAction)))
            {
                keys = Math.Max(keys, (int)action + 1);
            }

            _pressed = new bool[keys];
            _since = new float[keys];
        }

        /// <summary>The player pressed <paramref name="action"/>'s key just now.</summary>
        public void Pressed(RoverAction action)
        {
            _pressed[(int)action] = true;
            _since[(int)action] = 0f;
        }

        /// <param name="deltaTime">Seconds since the last step.</param>
        public void Step(float deltaTime)
        {
            for (int i = 0; i < _since.Length; i++)
            {
                _since[i] += deltaTime;
            }
        }

        /// <summary>True when <paramref name="action"/>'s key was pressed within the last <paramref name="seconds"/>.</summary>
        public bool UsedWithin(RoverAction action, float seconds)
        {
            int i = (int)action;
            return i > 0 && i < _pressed.Length && _pressed[i] && _since[i] < seconds;
        }
    }
}
