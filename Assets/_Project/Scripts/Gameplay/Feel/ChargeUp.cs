using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The charge-up at 07's emitter (docs/design/feel-pass.md §3.1, R6): cyan-white motes spawn on a sphere round the
    /// emitter and are drawn in on curving, accelerating paths while a glow bead grows and the emitter light comes
    /// up; on a cancel the motes scatter outward and fade. After ignition the bead stays as the emitter's glow until
    /// the beam lets go. Pooled: the motes live in fixed arrays.
    /// </summary>
    public sealed class ChargeUp
    {
        // Motes fade in over this share of the charge, and scatter to this many times their distance on a cancel.
        private const float FadeInShare = 0.15f;
        private const float DisperseReach = 2.5f;

        // The bead, its halo and the lens glow.
        private const int GlowSprites = 3;

        private readonly FeelVfxTuning _tuning;
        private readonly FeelSprites _sprites;
        private readonly FeelSprites _glows;
        private readonly LightPulse _light;
        private readonly Vector3[] _start;
        private readonly Vector3[] _axis;
        private readonly Vector3[] _offset;
        private int _motes;
        private float _seconds;
        private float _elapsed;
        private float _disperse = -1f;
        private float _bead;
        private float _beadTarget;
        private float _beadRate;

        public ChargeUp(Transform parent, FeelVfxTuning tuning, Material sprite)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            int pool = tuning.MotePool;
            _sprites = new FeelSprites("ChargeMotes", parent, sprite, pool, true, tuning.MoteStreak);
            _glows = new FeelSprites("ChargeGlow", parent, sprite, GlowSprites, false, 0f);
            _light = new LightPulse("EmitterLight", parent, tuning.MoteColor, tuning.EmitterRange);
            _start = new Vector3[pool];
            _axis = new Vector3[pool];
            _offset = new Vector3[pool];
        }

        /// <summary>True while motes are drawn in (before ignition).</summary>
        public bool Charging => _motes > 0 && _disperse < 0f;

        /// <summary>The glow bead's diameter now (m).</summary>
        public float BeadSize => _bead;

        public float LightLevel => _light.Level;

        /// <summary>Motes drawn this frame (tests and debugging views).</summary>
        public int MotesShown => _sprites.Count;

        /// <summary>Starts a charge of <paramref name="seconds"/> (a resume's is shorter).</summary>
        public void Begin(float seconds)
        {
            _seconds = Mathf.Max(seconds, 1e-3f);
            _elapsed = 0f;
            _disperse = -1f;
            _motes = _tuning.MoteCount;
            for (int i = 0; i < _motes; i++)
            {
                _start[i] = UnityEngine.Random.onUnitSphere * _tuning.MoteRadius;
                _axis[i] = Vector3.Cross(_start[i], UnityEngine.Random.onUnitSphere).normalized;
                if (_axis[i].sqrMagnitude < 1e-6f)
                {
                    _axis[i] = Vector3.up;
                }
            }

            GrowBead(_tuning.BeadSize, _seconds);
            _light.Ramp(_tuning.EmitterLight, _seconds, true);
        }

        /// <summary>The charge was released before ignition: the motes scatter and the light and bead fade.</summary>
        public void Cancel()
        {
            if (_motes > 0 && _disperse < 0f)
            {
                _disperse = 0f;
            }

            GrowBead(0f, _tuning.MoteDisperseSeconds);
            _light.Ramp(0f, _tuning.CutFadeSeconds, false);
        }

        /// <summary>
        /// Ignition: the motes have arrived; the bead stays as the emitter's glow and the light hands over.
        /// </summary>
        public void Ignite()
        {
            _motes = 0;
            _bead = _tuning.BeadSize;
            _beadTarget = _bead;
            _light.Ramp(0f, _tuning.FlashDecaySeconds, false);
        }

        /// <summary>
        /// The beam let go (a pause or the crack): the emitter's glow fades over <paramref name="seconds"/>.
        /// </summary>
        public void Fade(float seconds)
        {
            GrowBead(0f, seconds);
            _light.Ramp(0f, seconds, false);
        }

        public void Step(float deltaTime, Vector3 emitter)
        {
            _light.MoveTo(emitter);
            _light.Step(deltaTime);
            _bead = Mathf.MoveTowards(_bead, _beadTarget, _beadRate * deltaTime);
            _sprites.Begin();
            if (_motes > 0 && _disperse < 0f)
            {
                _elapsed += deltaTime;
                float t = Mathf.Clamp01(_elapsed / _seconds);
                Color color = _tuning.MoteColor;
                color.a = Mathf.Clamp01(t / FadeInShare);
                float ahead = Mathf.Min(t + deltaTime / _seconds, 1f);
                for (int i = 0; i < _motes; i++)
                {
                    _offset[i] = FeelCurves.MoteOffset(_start[i], _axis[i], t, _tuning.MoteEase, _tuning.MoteSwirl);
                    Vector3 next = FeelCurves.MoteOffset(_start[i], _axis[i], ahead, _tuning.MoteEase,
                        _tuning.MoteSwirl);
                    Vector3 velocity = (next - _offset[i]) / Mathf.Max(deltaTime, 1e-4f);
                    _sprites.Add(emitter + _offset[i], _tuning.MoteSize, color, velocity);
                }
            }
            else if (_motes > 0)
            {
                _disperse += deltaTime;
                float u = Mathf.Clamp01(_disperse / _tuning.MoteDisperseSeconds);
                Color color = _tuning.MoteColor;
                color.a = 1f - u;
                for (int i = 0; i < _motes; i++)
                {
                    _sprites.Add(emitter + _offset[i] * (1f + (DisperseReach - 1f) * u), _tuning.MoteSize, color,
                        _offset[i]);
                }

                if (u >= 1f)
                {
                    _motes = 0;
                }
            }

            _sprites.End();
            _glows.Begin();
            if (_bead > 0f)
            {
                float charge = _bead / _tuning.BeadSize;
                Color halo = _tuning.MoteColor;
                halo.a = _tuning.BeadHaloAlpha * charge;
                Color lens = _tuning.MoteColor;
                lens.a = _tuning.LensGlowAlpha * charge;
                _glows.Add(emitter, _tuning.LensGlow, lens, Vector3.zero);
                _glows.Add(emitter, _bead * _tuning.BeadHalo, halo, Vector3.zero);
                _glows.Add(emitter, _bead, _tuning.MoteColor, Vector3.zero);
            }

            _glows.End();
        }

        private void GrowBead(float size, float seconds)
        {
            _beadTarget = size;
            _beadRate = Mathf.Abs(size - _bead) / Mathf.Max(seconds, 1e-3f);
        }
    }
}
