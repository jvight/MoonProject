using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Fade state of a gameplay loop (tether hum, excavation rumble): the level ramps linearly at 1 / fade time and
    /// the audible gain follows a smoothstep of it, so fades take exactly their time, start and end with zero
    /// slope (no clicks) and reach true silence, at which point the source can stop.
    /// </summary>
    public sealed class LoopFader
    {
        private float _level;
        private bool _on;

        public bool IsOn => _on;

        /// <summary>True while anything can be heard (including the tail of a fade-out).</summary>
        public bool IsAudible => _level > 0f;

        /// <summary>Gain 0..1 to multiply into the source volume.</summary>
        public float Gain => _level * _level * (3f - 2f * _level);

        public void FadeIn()
        {
            _on = true;
        }

        public void FadeOut()
        {
            _on = false;
        }

        /// <summary>Advances the fade; non-positive fade times switch instantly.</summary>
        public void Step(float deltaTime, float fadeInTime, float fadeOutTime)
        {
            float target = _on ? 1f : 0f;
            float time = _on ? fadeInTime : fadeOutTime;
            _level = time <= 0f ? target : Mathf.MoveTowards(_level, target, Mathf.Max(0f, deltaTime) / time);
        }
    }
}
