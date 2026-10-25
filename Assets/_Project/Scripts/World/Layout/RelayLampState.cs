using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// relay.1's lamp from answered to relit (opening v3 R8, 5.1): it keys with the call's sign until the relay wakes,
    /// then warms up with a short, uneven flicker (a lamp coming back on its own power, steadier as it goes), blinks
    /// the network's reply (3 short, 1 long) when it comes, and holds a calm steady glow. A relit relay loaded from a
    /// save is steady at once. The flicker is seeded, so every warm-up is the same. Plain logic.
    /// </summary>
    public sealed class RelayLampState
    {
        // The flicker's own seed, so the warm-up never matches another lamp's.
        private const uint FlickerSeed = 0x2E1A7u;

        private float _sinceRelit = float.MaxValue;
        private float _sinceReply = float.MaxValue;

        /// <summary>The relay has been relit (this session or a loaded one).</summary>
        public bool Relit { get; private set; }

        /// <summary>The relay is relit; <paramref name="restored"/> skips the warm-up (a loaded game).</summary>
        public void OnRelit(bool restored)
        {
            Relit = true;
            _sinceRelit = restored ? float.MaxValue : 0f;
        }

        /// <summary>
        /// The lamp blinks the network's reply (3 short, 1 long) and then holds steady; <paramref name="restored"/>
        /// skips it.
        /// </summary>
        public void OnReply(bool restored)
        {
            _sinceReply = restored ? float.MaxValue : 0f;
        }

        public void Step(float deltaTime)
        {
            if (Relit && _sinceRelit < float.MaxValue)
            {
                _sinceRelit += deltaTime;
            }

            if (_sinceReply < float.MaxValue)
            {
                _sinceReply += deltaTime;
            }
        }

        /// <summary>Seconds the network's reply takes, its blinks and the gaps between them.</summary>
        public static float ReplySeconds(RelayLampSettings lamp)
        {
            return lamp.ReplyShorts * (lamp.ReplyShortSeconds + lamp.ReplyGapSeconds) + lamp.ReplyLongSeconds;
        }

        /// <summary>True while the reply's blink is lit <paramref name="elapsed"/> seconds into it.</summary>
        public static bool ReplyLit(float elapsed, RelayLampSettings lamp)
        {
            float beat = lamp.ReplyShortSeconds + lamp.ReplyGapSeconds;
            float shorts = lamp.ReplyShorts * beat;
            if (elapsed < shorts)
            {
                return elapsed - Mathf.Floor(elapsed / beat) * beat < lamp.ReplyShortSeconds;
            }

            return elapsed - shorts < lamp.ReplyLongSeconds;
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
            if (_sinceReply < ReplySeconds(lamp))
            {
                return ReplyLit(_sinceReply, lamp) ? steady : steady * lamp.ReplyLowShare;
            }

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
