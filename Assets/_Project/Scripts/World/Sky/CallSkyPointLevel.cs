using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// How bright the call's point of light is (0..1 of its peak): a glow held between the tones, lifted by the sign's
    /// keying when there is one, dimmer while the call is broken and settling over the tuned time once it is not. With
    /// no keying it simply holds its glow, steady. Plain logic.
    /// </summary>
    public sealed class CallSkyPointLevel
    {
        private float _brokenTarget = 1f;

        /// <summary>How broken the light looks, 1 (broken) to 0 (steady).</summary>
        public float Broken { get; private set; } = 1f;

        /// <summary>Follows the call; <paramref name="snap"/> takes its state at once (a loaded game).</summary>
        public void OnCall(CallState state, bool snap)
        {
            _brokenTarget = state == CallState.Broken ? 1f : 0f;
            if (snap)
            {
                Broken = _brokenTarget;
            }
        }

        public void Step(float deltaTime, float fadeSeconds)
        {
            Broken = Mathf.MoveTowards(Broken, _brokenTarget, deltaTime / Mathf.Max(fadeSeconds, 1e-3f));
        }

        /// <summary>
        /// The level now: the held glow (<paramref name="rest"/>, or <paramref name="brokenRest"/> while broken), lifted
        /// toward 1 by <paramref name="keying"/> (0 when no signal is heard).
        /// </summary>
        public float Level(float keying, float rest, float brokenRest)
        {
            float hold = Mathf.Lerp(rest, brokenRest, Broken);
            return Mathf.Lerp(hold, 1f, Mathf.Clamp01(keying));
        }
    }
}
