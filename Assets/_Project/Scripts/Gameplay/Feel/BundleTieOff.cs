using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The bundle's tie-off pop once the piece has folded into it (docs/design/feel-pass.md §3.1): its scale goes
    /// 1 → the pop → 1 over a short time. A few bundles may pop at once while pieces chain; fixed slots, no garbage.
    /// </summary>
    public sealed class BundleTieOff
    {
        private const int Slots = 4;

        private readonly FeelVfxTuning _tuning;
        private readonly Transform[] _bundle = new Transform[Slots];
        private readonly Vector3[] _rest = new Vector3[Slots];
        private readonly float[] _elapsed = new float[Slots];

        public BundleTieOff(FeelVfxTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>
        /// Pops <paramref name="bundle"/> from its current scale (the oldest pop yields if all slots are busy).
        /// </summary>
        public void Play(Transform bundle)
        {
            int slot = 0;
            for (int i = 0; i < Slots; i++)
            {
                if (_bundle[i] == null || _bundle[i] == bundle)
                {
                    slot = i;
                    break;
                }

                slot = _elapsed[i] > _elapsed[slot] ? i : slot;
            }

            if (_bundle[slot] != null && _bundle[slot] != bundle)
            {
                _bundle[slot].localScale = _rest[slot];
            }

            _rest[slot] = _bundle[slot] == bundle ? _rest[slot] : bundle.localScale;
            _bundle[slot] = bundle;
            _elapsed[slot] = 0f;
        }

        public void Step(float deltaTime)
        {
            for (int i = 0; i < Slots; i++)
            {
                if (_bundle[i] == null)
                {
                    continue;
                }

                _elapsed[i] += deltaTime;
                float t = _elapsed[i] / Mathf.Max(_tuning.TieSeconds, 1e-3f);
                _bundle[i].localScale = _rest[i] * FeelCurves.TiePop(t, _tuning.TiePop);
                if (t >= 1f)
                {
                    _bundle[i].localScale = _rest[i];
                    _bundle[i] = null;
                }
            }
        }
    }
}
