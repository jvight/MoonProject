using System;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The light a relic (or a broken friend) leaves when it answers: a tall soft pillar that stands on the horizon for
    /// a while (so the player can navigate by sight instead of pinging again) and a ring on the ground at the spot,
    /// which keeps breathing faintly over a discovered find that is still waiting there.
    /// </summary>
    public sealed class SiteMarker : IDisposable
    {
        /// <summary>If the find moves this far (m) from where it answered, the pillar bows out early.</summary>
        private const float MovedDistance = 3f;

        /// <summary>Seconds a dismissed pillar takes to fade.</summary>
        private const float DismissFade = 1.2f;

        private readonly SonarTuning _tuning;
        private readonly ITerrainQuery _terrain;
        private readonly Transform _pillar;
        private readonly GlowRenderer _pillarGlow;
        private readonly GlowRenderer _ringGlow;
        private readonly TerrainRing _ring;
        private Vector3 _position;
        private float _answerTime = float.NegativeInfinity;
        private float _dismissTime = float.PositiveInfinity;
        private readonly float _scale;
        private float _brightness;
        private float _lingered;
        private bool _hasPosition;

        /// <param name="scale">Brightness scale of every glow of this marker (warm markers read brighter).</param>
        public SiteMarker(string name, Transform parent, SonarTuning tuning, ITerrainQuery terrain,
            Material pillarMaterial, Material ringMaterial, Mesh pillarMesh, float scale)
        {
            _scale = scale;
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            MeshRenderer pillar = GlowObject.Create("Pillar", root.transform, pillarMesh, pillarMaterial);
            _pillar = pillar.transform;
            _pillar.localScale = new Vector3(tuning.PillarRadius, tuning.PillarHeight, tuning.PillarRadius);
            _pillarGlow = new GlowRenderer(pillar);
            _ring = new TerrainRing(tuning.SiteRingSegments);
            _ringGlow = new GlowRenderer(GlowObject.Create("Ring", root.transform, _ring.Mesh, ringMaterial));
        }

        /// <summary>Where the marker stands (valid after the first answer).</summary>
        public Vector3 Position => _position;

        public float PillarIntensity => _pillarGlow.Intensity;

        public float RingIntensity => _ringGlow.Intensity;

        /// <summary>True while the pillar is still on the horizon.</summary>
        public bool PillarStanding(float now)
        {
            return _hasPosition && now < _dismissTime && now - _answerTime < _tuning.PillarLifetime;
        }

        /// <summary>
        /// Stands the marker at <paramref name="groundPosition"/> without an answer (a site discovered in an earlier
        /// session keeps its breathing ring).
        /// </summary>
        public void Place(Vector3 groundPosition)
        {
            if (_hasPosition && SurfaceRules.HorizontalDistance(groundPosition, _position) <= 0.05f)
            {
                return;
            }

            _position = SurfaceRules.OnSurface(_terrain, groundPosition.x, groundPosition.z);
            _pillar.position = _position;
            Vector2 ring = _tuning.SiteRing;
            _ring.Rebuild(_terrain, _position, ring.x - ring.y * 0.5f, ring.x + ring.y * 0.5f, _tuning.RingLift);
            _hasPosition = true;
        }

        /// <summary>The find answered from <paramref name="groundPosition"/> with this brightness.</summary>
        public void Answer(Vector3 groundPosition, float brightness, float now)
        {
            Place(groundPosition);
            _answerTime = now;
            _dismissTime = float.PositiveInfinity;
            _brightness = brightness;
            _lingered = 0f;
        }

        /// <summary>
        /// Keeps a standing pillar (past its rise) up <paramref name="seconds"/> longer, at most <paramref name="cap"/>
        /// seconds in all for this answer: 07's Warm Headlamp on it.
        /// </summary>
        public void Linger(float seconds, float cap, float now)
        {
            if (!PillarStanding(now) || now - _answerTime < _tuning.PillarRise)
            {
                return;
            }

            float extra = Mathf.Min(seconds, cap - _lingered);
            if (extra > 0f)
            {
                _answerTime += extra;
                _lingered += extra;
            }
        }

        /// <summary>
        /// Updates the glows at <paramref name="now"/> for a find now at <paramref name="position"/> that still
        /// answers the sonar (<paramref name="answers"/>) and, if <paramref name="waiting"/> (discovered and still in
        /// place), keeps a breathing ring.
        /// </summary>
        public void Tick(Vector3 position, bool answers, bool waiting, float now)
        {
            if (!_hasPosition)
            {
                return;
            }

            bool moved = SurfaceRules.HorizontalDistance(position, _position) > MovedDistance;
            if ((moved || !answers) && float.IsPositiveInfinity(_dismissTime))
            {
                _dismissTime = now;
            }

            float age = now - _answerTime;
            float breath = 1f - _tuning.PillarBreathDepth *
                (1f - MarkerEnvelope.Breath(age, _tuning.PillarBreathPeriod));
            float pillar = _brightness * breath * MarkerEnvelope.Pillar(age, _tuning.PillarLifetime,
                _tuning.PillarRise, _tuning.PillarFade);
            if (now >= _dismissTime)
            {
                pillar *= 1f - Ease.InOutSine((now - _dismissTime) / DismissFade);
            }

            _pillarGlow.Apply(pillar * _scale);

            float pulse = _brightness / Mathf.Max(0.01f, _tuning.NearBrightness) * _tuning.SiteRingPulse *
                          MarkerEnvelope.Pulse(age, _tuning.SiteRingPulseDuration);
            float glow = waiting && !moved
                ? _tuning.DiscoveredGlow *
                  (1f - _tuning.DiscoveredBreathDepth * (1f - MarkerEnvelope.Breath(now, _tuning.BreathPeriod)))
                : 0f;
            _ringGlow.Apply(Mathf.Max(pulse, glow) * _scale);
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
