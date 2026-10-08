using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The radio through a radio-hop (M3-06), timed like the hop's view: from <see cref="Begin"/> it eases into
    /// static over the out time as the screen fades, holds through the dark while 07 is moved, then resolves over the
    /// in time into whatever clarity the target node has (the step reports the landing, for its sound).
    /// <see cref="End"/> (the hop finished) starts the resolve at once if it has not begun, from wherever the static
    /// is, so the radio never jumps. Allocation-free.
    /// </summary>
    public sealed class RadioHop
    {
        private enum Phase
        {
            Idle,
            Out,
            Dark,
            In,
        }

        private Phase _phase = Phase.Idle;
        private float _level;
        private float _dark;
        private bool _landingDue;

        /// <summary>0 normal radio .. 1 all static (smoothed at both ends).</summary>
        public float Amount => _level * _level * (3f - 2f * _level);

        /// <summary>True from <see cref="Begin"/> until the radio has resolved.</summary>
        public bool Active => _phase != Phase.Idle;

        public void Begin()
        {
            _phase = Phase.Out;
            _dark = 0f;
            _landingDue = false;
        }

        public void End()
        {
            if (_phase == Phase.Out || _phase == Phase.Dark)
            {
                Land();
            }
        }

        /// <summary>
        /// Advances the hop; returns <see cref="Amount"/>. <paramref name="landing"/> is true on the single step at
        /// which the static starts to resolve (as the view starts easing back in).
        /// </summary>
        public float Step(float deltaTime, float outTime, float darkTime, float inTime, out bool landing)
        {
            float dt = Mathf.Max(0f, deltaTime);
            switch (_phase)
            {
                case Phase.Out:
                    _level = outTime > 0f ? Mathf.MoveTowards(_level, 1f, dt / outTime) : 1f;
                    if (_level >= 1f)
                    {
                        _phase = Phase.Dark;
                    }

                    break;
                case Phase.Dark:
                    _dark += dt;
                    if (_dark >= darkTime)
                    {
                        Land();
                    }

                    break;
                case Phase.In:
                    _level = inTime > 0f ? Mathf.MoveTowards(_level, 0f, dt / inTime) : 0f;
                    if (_level <= 0f)
                    {
                        _phase = Phase.Idle;
                    }

                    break;
            }

            landing = _landingDue;
            _landingDue = false;
            return Amount;
        }

        private void Land()
        {
            _phase = Phase.In;
            _landingDue = true;
        }
    }
}
