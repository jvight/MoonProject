using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// relay.1's lamp from answered to relit (opening v3 R8, 5.1): it keys with the call's sign until the relay is
    /// relit, then warms up with a short, uneven flicker (a lamp coming back on its own power, steadier as it goes)
    /// and holds a calm steady glow. A relit relay loaded from a save is steady at once. The flicker is seeded, so
    /// every warm-up is the same. Plain logic.
    /// </summary>
    public sealed class RelayLampState
    {
        // The flicker's own seed, so the warm-up never matches another lamp's.
        private const uint FlickerSeed = 0x2E1A7u;

        private float _sinceRelit = float.MaxValue;

        /// <summary>The relay has been relit (this session or a loaded one).</summary>
        public bool Relit { get; private set; }

        /// <summary>The relay is relit; <paramref name="restored"/> skips the warm-up (a loaded game).</summary>
        public void OnRelit(bool restored)
        {
            Relit = true;
            _sinceRelit = restored ? float.MaxValue : 0f;
        }

        public void Step(float deltaTime)
        {
            if (Relit && _sinceRelit < float.MaxValue)
            {
                _sinceRelit += deltaTime;
            }
        }

        /// <summary>
        /// The lamp's level, 0..1: <paramref name="keying"/> until relit; then, through the warm-up, flicker steps
        /// each on (rising toward the steady glow) or nearly off, more often on as it warms; then the steady glow.
        /// </summary>
        public float Level(float keying, RelayLampSettings lamp)
        {
            if (!Relit)
            {
                return Mathf.Clamp01(keying);
            }

            float steady = lamp.RelitLevel;
            if (_sinceRelit >= lamp.WarmUpSeconds)
            {
                return steady;
            }

            float warmth = _sinceRelit / Mathf.Max(lamp.WarmUpSeconds, 1e-3f);
            int step = Mathf.FloorToInt(_sinceRelit / Mathf.Max(lamp.FlickerStepSeconds, 1e-3f));
            float onChance = Mathf.Lerp(lamp.FlickerOnFrom, lamp.FlickerOnTo, warmth);
            bool on = Hashing.ToUnit(Hashing.Hash(step, 0, FlickerSeed)) < onChance;
            return on ? steady * Mathf.Lerp(lamp.FlickerRiseFrom, 1f, warmth) : steady * lamp.FlickerOffShare;
        }
    }
}
