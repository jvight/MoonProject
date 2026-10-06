using System;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One expanding sonar ring: a band of light that rolls out over the dunes from 07, spreading fast and slowing
    /// as it reaches the edge of its range while it widens and fades.
    /// </summary>
    public sealed class SonarRing : IDisposable
    {
        private readonly SonarTuning _tuning;
        private readonly ITerrainQuery _terrain;
        private readonly TerrainRing _ring;
        private readonly GlowRenderer _glow;
        private Vector3 _origin;
        private float _startTime = float.NegativeInfinity;

        public SonarRing(Transform parent, SonarTuning tuning, ITerrainQuery terrain, Material material)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _ring = new TerrainRing(tuning.RingSegments);
            _glow = new GlowRenderer(GlowObject.Create("SonarRing", parent, _ring.Mesh, material));
        }

        public bool Active { get; private set; }

        /// <summary>Current radius (m), for tests and debugging views.</summary>
        public float Radius { get; private set; }

        public float Intensity => _glow.Intensity;

        public float StartTime => _startTime;

        public void Begin(Vector3 origin, float now)
        {
            _origin = origin;
            _startTime = now;
            Active = true;
            Radius = 0f;
        }

        public void Tick(float now)
        {
            if (!Active)
            {
                return;
            }

            float elapsed = now - _startTime;
            float duration = _tuning.RingDuration;
            if (elapsed >= duration)
            {
                Active = false;
                _glow.Apply(0f);
                return;
            }

            float t = elapsed / duration;
            Radius = SonarSchedule.RingRadius(elapsed, duration, _tuning.Range);
            Vector2 widths = _tuning.RingWidth;
            float width = Mathf.Lerp(widths.x, widths.y, Ease.OutQuad(t));
            float fadeIn = _tuning.RingFadeIn <= 0f ? 1f : Mathf.Clamp01(elapsed / _tuning.RingFadeIn);
            float fadeOut = 1f - Ease.InOutSine(t);
            _ring.Rebuild(_terrain, _origin, Radius - width * 0.5f, Radius + width * 0.5f, _tuning.RingLift);
            _glow.Apply(_tuning.RingIntensity * Ease.OutCubic(fadeIn) * fadeOut);
        }

        public void Dispose()
        {
            if (_ring.Mesh != null)
            {
                Object.Destroy(_ring.Mesh);
            }
        }
    }
}
