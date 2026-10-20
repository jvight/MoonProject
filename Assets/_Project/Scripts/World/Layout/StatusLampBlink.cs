using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// A status lamp's slow dusk blink: asleep by day, waking over the tuned time once the Sun sinks (sunset or the
    /// earthlit night) and sleeping again when it comes back; each blink eases up and down. Plain logic.
    /// </summary>
    public sealed class StatusLampBlink
    {
        private float _target;

        /// <summary>How awake the blink is, 0 (day) to 1 (dusk and night).</summary>
        public float Awake { get; private set; }

        /// <summary>True from sunset through the earthlit night.</summary>
        public static bool IsDusk(SkyPhase phase) => phase == SkyPhase.Sunset || phase == SkyPhase.Earthlit;

        /// <summary>Follows the sky; <paramref name="snap"/> takes its state at once (a loaded game).</summary>
        public void OnSky(SkyPhase phase, bool snap)
        {
            _target = IsDusk(phase) ? 1f : 0f;
            if (snap)
            {
                Awake = _target;
            }
        }

        public void Step(float deltaTime, float wakeSeconds)
        {
            Awake = Mathf.MoveTowards(Awake, _target, deltaTime / Mathf.Max(wakeSeconds, 1e-3f));
        }

        /// <summary>The lamp's level at <paramref name="time"/>: its blink, as awake as the sky lets it be.</summary>
        public float Level(float time, float blinkSeconds, float onShare, float easeShare)
        {
            return Awake * Pulse(time, blinkSeconds, onShare, easeShare);
        }

        /// <summary>
        /// One blink every <paramref name="blinkSeconds"/>: lit for <paramref name="onShare"/> of it, easing up over
        /// half of <paramref name="easeShare"/> of the lit time and down over the other half; dark the rest.
        /// </summary>
        public static float Pulse(float time, float blinkSeconds, float onShare, float easeShare)
        {
            float phase = Mathf.Repeat(time / Mathf.Max(blinkSeconds, 1e-3f), 1f);
            if (phase >= onShare)
            {
                return 0f;
            }

            float edge = Mathf.Max(onShare * easeShare * 0.5f, 1e-4f);
            float level = Mathf.Min(Mathf.Clamp01(phase / edge), Mathf.Clamp01((onShare - phase) / edge));
            return level * level * (3f - 2f * level);
        }
    }
}
