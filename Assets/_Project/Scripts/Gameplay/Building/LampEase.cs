using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One base lamp's glow: it waits its turn, eases up with a warm flicker as it catches (an old lamp warming),
    /// holds full, and eases off again. Pure logic: <see cref="BaseLamps"/> steps it and shows
    /// <see cref="Level"/> on the lamp.
    /// </summary>
    public sealed class LampEase
    {
        /// <summary>Where the lamp is in its life.</summary>
        public enum Phase
        {
            Off = 0,
            Waiting = 1,
            Rising = 2,
            On = 3,
            Falling = 4,
        }

        private float _wait;
        private float _elapsed;
        private float _fallFrom;
        private float _fallElapsed;

        public Phase State { get; private set; }

        /// <summary>The glow, 0 (dark) to 1 (full).</summary>
        public float Level { get; private set; }

        /// <summary>The lamp is lit or on its way (it is not dark, waiting or falling).</summary>
        public bool WantsLight => State == Phase.Waiting || State == Phase.Rising || State == Phase.On;

        /// <summary>The lamp starts at its steady state with no ease: a base loaded already lit, or dark.</summary>
        public void Snap(bool lit)
        {
            State = lit ? Phase.On : Phase.Off;
            Level = lit ? 1f : 0f;
        }

        /// <summary>The lamp is to come on after <paramref name="delay"/> seconds (a lit lamp stays as it is).</summary>
        public void Light(float delay)
        {
            if (WantsLight)
            {
                return;
            }

            State = Phase.Waiting;
            _wait = Mathf.Max(0f, delay);
            _elapsed = 0f;
        }

        /// <summary>The lamp is to ease off from where it stands.</summary>
        public void Extinguish()
        {
            if (State == Phase.Off || State == Phase.Falling)
            {
                return;
            }

            State = Phase.Falling;
            _fallFrom = Level;
            _fallElapsed = 0f;
        }

        /// <summary>
        /// Advances by <paramref name="deltaTime"/>; true on the frame the lamp begins to rise, after its wait.
        /// </summary>
        public bool Step(float deltaTime, float riseSeconds, float fallSeconds, float flickerDepth,
            float flickerHertz, float flickerSeconds)
        {
            bool began = false;
            switch (State)
            {
                case Phase.Waiting:
                    _wait -= deltaTime;
                    if (_wait <= 0f)
                    {
                        State = Phase.Rising;
                        _elapsed = Mathf.Min(0f, _wait) * -1f;
                        began = true;
                        Level = Rise(riseSeconds, flickerDepth, flickerHertz, flickerSeconds);
                    }

                    break;
                case Phase.Rising:
                    _elapsed += deltaTime;
                    Level = Rise(riseSeconds, flickerDepth, flickerHertz, flickerSeconds);
                    if (_elapsed >= riseSeconds)
                    {
                        State = Phase.On;
                        Level = 1f;
                    }

                    break;
                case Phase.Falling:
                    _fallElapsed += deltaTime;
                    float fall = Mathf.Clamp01(_fallElapsed / Mathf.Max(fallSeconds, 1e-3f));
                    Level = _fallFrom * (1f - Smooth(fall));
                    if (fall >= 1f)
                    {
                        State = Phase.Off;
                        Level = 0f;
                    }

                    break;
            }

            return began;
        }

        private float Rise(float riseSeconds, float flickerDepth, float flickerHertz, float flickerSeconds)
        {
            // Ease out: the filament strikes quickly, then creeps to full; the flicker rides the quick part.
            float left = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(riseSeconds, 1e-3f));
            float rise = 1f - left * left * left;
            float catching = Mathf.Clamp01(1f - _elapsed / Mathf.Max(flickerSeconds, 1e-3f));
            float flicker = Mathf.Abs(Mathf.Sin(_elapsed * flickerHertz * Mathf.PI)) * catching;
            return rise * (1f - flickerDepth * flicker);
        }

        private static float Smooth(float x)
        {
            return x * x * (3f - 2f * x);
        }
    }
}
