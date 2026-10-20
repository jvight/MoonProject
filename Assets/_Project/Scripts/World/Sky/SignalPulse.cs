using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The ring that blooms out of the call's point of light as each tone of the sign begins: a keying that rises past
    /// the on threshold starts a pulse, and a new one can only start once the keying has fallen back under the off
    /// threshold, so a tone's soft edge or a dropout's flutter never fires twice. Plain logic.
    /// </summary>
    public sealed class SignalPulse
    {
        private bool _keyed;

        /// <summary>Seconds since the last tone began (large before the first).</summary>
        public float Age { get; private set; } = float.MaxValue;

        public void Step(float keying, float deltaTime, float onThreshold, float offThreshold)
        {
            if (Age < float.MaxValue)
            {
                Age += deltaTime;
            }

            if (!_keyed && keying >= onThreshold)
            {
                _keyed = true;
                Age = 0f;
            }
            else if (_keyed && keying <= offThreshold)
            {
                _keyed = false;
            }
        }

        /// <summary>How far through its bloom the ring is, 0 (just begun) to 1 (gone).</summary>
        public float Progress(float pulseSeconds)
        {
            return Mathf.Clamp01(Age / Mathf.Max(pulseSeconds, 1e-3f));
        }
    }
}
