using System;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The light a relic leaves when it answers: a tall soft pillar that stands on the horizon for a while (so the
    /// player can navigate by sight instead of pinging again) and a ring on the ground at the spot, which keeps
    /// breathing faintly over a discovered relic that is still in the ground.
    /// </summary>
    public sealed class SiteMarker : IDisposable
    {
        /// <summary>If the relic moves this far (m) from where it answered, the pillar bows out early.</summary>
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
        private float _brightness;
        private bool _hasPosition;

        public SiteMarker(string name, Transform parent, SonarTuning tuning, ITerrainQuery terrain,
            GameplayVisuals visuals, Mesh pillarMesh)
        {
            _tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            MeshRenderer pillar = GlowObject.Create("Pillar", root.transform, pillarMesh, visuals.SitePillar);
            _pillar = pillar.transform;
            _pillar.localScale = new Vector3(tuning.PillarRadius, tuning.PillarHeight, tuning.PillarRadius);
            _pillarGlow = new GlowRenderer(pillar);
            _ring = new TerrainRing(tuning.SiteRingSegments);
            _ringGlow = new GlowRenderer(GlowObject.Create("Ring", root.transform, _ring.Mesh, visuals.SiteRing));
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

        /// <summary>The relic answered from <paramref name="groundPosition"/> with this brightness.</summary>
        public void Answer(Vector3 groundPosition, float brightness, float now)
        {
            Place(groundPosition);
            _answerTime = now;
            _dismissTime = float.PositiveInfinity;
            _brightness = brightness;
        }

        /// <summary>Updates the glows for <paramref name="relic"/> at <paramref name="now"/>.</summary>
        public void Tick(Relic relic, float now)
        {
            if (!_hasPosition)
            {
                return;
            }

            bool inGround = relic.State == RelicState.Buried || relic.State == RelicState.Surfacing;
            bool moved = SurfaceRules.HorizontalDistance(relic.SonarPosition, _position) > MovedDistance;
            if ((moved || !relic.AnswersSonar) && float.IsPositiveInfinity(_dismissTime))
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

            _pillarGlow.Apply(pillar);

            float pulse = _brightness / Mathf.Max(0.01f, _tuning.NearBrightness) * _tuning.SiteRingPulse *
                          MarkerEnvelope.Pulse(age, _tuning.SiteRingPulseDuration);
            float glow = inGround && relic.Discovered && !moved
                ? _tuning.DiscoveredGlow *
                  (1f - _tuning.DiscoveredBreathDepth * (1f - MarkerEnvelope.Breath(now, _tuning.BreathPeriod)))
                : 0f;
            _ringGlow.Apply(Mathf.Max(pulse, glow));
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
