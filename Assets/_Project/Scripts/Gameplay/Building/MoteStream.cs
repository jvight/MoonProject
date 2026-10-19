using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What a piece gives up while 07 takes it down: a gentle stream of small motes from the dissolving edge along the
    /// beam into 07's emitter. They differ in size, leave at uneven, soft intervals and drift on slightly bowed paths,
    /// starting slowly and gathering into the emitter where they shrink away: never a field of identical dots, and
    /// never bright enough to clip. Fixed pool, nothing allocated after construction.
    /// </summary>
    public sealed class MoteStream
    {
        private const float BowMetres = 0.12f;
        private const float GatherPower = 1.6f;
        private const float SizeSpread = 0.5f;
        private const float IntervalJitter = 0.6f;

        private readonly BuildingTuning _tuning;
        private readonly FeelSprites _sprites;
        private readonly Vector3[] _from;
        private readonly Vector3[] _bow;
        private readonly float[] _age;
        private readonly float[] _life;
        private readonly float[] _size;
        private int _live;
        private float _owed;
        private float _nextInterval = 1f;

        public MoteStream(Transform parent, Material sprite, BuildingTuning tuning, int capacity)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _sprites = new FeelSprites("TakeDownMotes", parent, sprite, capacity, false, 0f);
            _from = new Vector3[capacity];
            _bow = new Vector3[capacity];
            _age = new float[capacity];
            _life = new float[capacity];
            _size = new float[capacity];
        }

        /// <summary>Motes in flight (tests and debugging views).</summary>
        public int Live => _live;

        /// <summary>
        /// Advances the stream by <paramref name="deltaTime"/>: while <paramref name="level"/> (0..1, how far the
        /// take-down has come) is above zero, motes leave <paramref name="edge"/> at a rate rising with it and fly to
        /// <paramref name="emitter"/>; motes already in flight always finish their way.
        /// </summary>
        public void Step(float deltaTime, Vector3 edge, Vector3 emitter, float level)
        {
            if (level > 0f)
            {
                float rate = _tuning.TakeDownMoteRate * Mathf.Clamp01(level);
                _owed += rate * deltaTime;
                while (_owed >= _nextInterval)
                {
                    _owed -= _nextInterval;
                    _nextInterval = 1f + UnityEngine.Random.Range(-IntervalJitter, IntervalJitter);
                    Launch(edge);
                }
            }
            else
            {
                _owed = 0f;
            }

            _sprites.Begin();
            Vector2 sizes = _tuning.TakeDownMoteSize;
            Color color = _tuning.TakeDownBeamColor;
            for (int i = _live - 1; i >= 0; i--)
            {
                _age[i] += deltaTime;
                float t = _age[i] / _life[i];
                if (t >= 1f)
                {
                    int last = --_live;
                    _from[i] = _from[last];
                    _bow[i] = _bow[last];
                    _age[i] = _age[last];
                    _life[i] = _life[last];
                    _size[i] = _size[last];
                    continue;
                }

                float travel = Mathf.Pow(t, GatherPower);
                Vector3 at = Vector3.Lerp(_from[i], emitter, travel) + _bow[i] * Mathf.Sin(Mathf.PI * travel);
                float fade = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) * (1f - 0.6f * travel);
                float size = Mathf.Lerp(sizes.x, sizes.y, _size[i]) * (1f - 0.7f * travel);
                Color tint = color;
                tint.a = color.a * fade * _tuning.TakeDownMoteBrightness;
                _sprites.Add(at, size, tint, Vector3.zero);
            }

            _sprites.End();
        }

        private void Launch(Vector3 edge)
        {
            if (_live >= _from.Length)
            {
                return;
            }

            int i = _live++;
            Vector2 travel = _tuning.TakeDownMoteSeconds;
            _from[i] = edge + UnityEngine.Random.insideUnitSphere * 0.05f;
            _bow[i] = UnityEngine.Random.onUnitSphere * BowMetres;
            _age[i] = 0f;
            _life[i] = UnityEngine.Random.Range(travel.x, travel.y);
            _size[i] = Mathf.Clamp01(0.5f + UnityEngine.Random.Range(-SizeSpread, SizeSpread));
        }
    }
}
