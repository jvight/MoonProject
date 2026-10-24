using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The pulses one base cable carries (base-home-and-crater-exits §3): while power flows, a pulse leaves the
    /// source's port at once and then every interval, eases out of the port and into the far end over its travel time,
    /// and glows up and back down on its way. One pulse on a cable at a time; when the power stops, the pulse in flight
    /// still finishes and no new one leaves. Pure logic, allocation-free.
    /// </summary>
    public sealed class PowerPulseTrain
    {
        private float _age = -1f;
        private float _wait;

        /// <summary>A pulse is on the cable now.</summary>
        public bool InFlight => _age >= 0f;

        /// <summary>How far along the cable the pulse is (0 at the source's port, 1 at the far end), eased.</summary>
        public float Along { get; private set; }

        /// <summary>The pulse's glow (0..1): soft at both ends of the cable, full mid-way.</summary>
        public float Glow { get; private set; }

        /// <summary>
        /// Advances the train by <paramref name="deltaTime"/>; true on the step a pulse reaches the far end.
        /// </summary>
        /// <param name="flowing">Power flows on this cable now.</param>
        /// <param name="interval">Seconds from one pulse leaving to the next.</param>
        /// <param name="travel">Seconds a pulse takes along the cable.</param>
        public bool Step(float deltaTime, bool flowing, float interval, float travel)
        {
            bool arrived = false;
            if (_age >= 0f)
            {
                _age += deltaTime;
                if (_age >= travel)
                {
                    _age = -1f;
                    arrived = true;
                }
            }

            if (flowing)
            {
                _wait -= deltaTime;
                if (_wait <= 0f && _age < 0f)
                {
                    _age = 0f;
                    _wait = interval;
                }
            }
            else
            {
                _wait = 0f;
            }

            float t = _age >= 0f ? Mathf.Clamp01(_age / travel) : 0f;
            Along = t * t * (3f - 2f * t);
            Glow = _age >= 0f ? Mathf.Sin(t * Mathf.PI) : 0f;
            return arrived;
        }

        /// <summary>The cable was laid again: no pulse on it, and the next leaves as soon as power flows.</summary>
        public void Reset()
        {
            _age = -1f;
            _wait = 0f;
            Along = 0f;
            Glow = 0f;
        }
    }
}
