using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A small ring buffer of soft light balls that swell and fade where scrap is collected. All flashes are created
    /// up front; spawning reuses the oldest, so a fast arpeggio never allocates.
    /// </summary>
    public sealed class GlowFlashPool
    {
        private readonly Transform[] _flashes;
        private readonly GlowRenderer[] _glows;
        private readonly float[] _ages;
        private readonly float _duration;
        private readonly Vector2 _radius;
        private readonly float _intensity;
        private int _next;

        public GlowFlashPool(Transform parent, Mesh sphere, Material material, int size, float duration,
            Vector2 radius, float intensity)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "The pool needs at least one flash.");
            }

            _flashes = new Transform[size];
            _glows = new GlowRenderer[size];
            _ages = new float[size];
            _duration = Mathf.Max(0.01f, duration);
            _radius = radius;
            _intensity = intensity;
            for (int i = 0; i < size; i++)
            {
                MeshRenderer flash = GlowObject.Create("Flash", parent, sphere, material);
                _flashes[i] = flash.transform;
                _glows[i] = new GlowRenderer(flash);
                _ages[i] = _duration;
            }
        }

        public void Spawn(Vector3 position)
        {
            _flashes[_next].position = position;
            _ages[_next] = 0f;
            _next = (_next + 1) % _flashes.Length;
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _flashes.Length; i++)
            {
                if (_ages[i] >= _duration)
                {
                    continue;
                }

                _ages[i] += deltaTime;
                float t = Mathf.Clamp01(_ages[i] / _duration);
                _flashes[i].localScale = Vector3.one * Mathf.Lerp(_radius.x, _radius.y, Ease.OutCubic(t));
                _glows[i].Apply(_intensity * (1f - Ease.InOutSine(t)));
            }
        }
    }
}
