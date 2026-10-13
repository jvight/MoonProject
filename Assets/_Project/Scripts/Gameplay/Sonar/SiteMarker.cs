using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The light a relic, a broken friend or a wreck leaves when it answers: a tall soft pillar that stands on the
    /// horizon for a while, so the player can navigate by sight instead of pinging again, then fades. Nothing stays on
    /// the ground (0.5.5 §2): up close the find's own glint on its object marks it.
    /// </summary>
    public sealed class SiteMarker
    {
        /// <summary>If the find moves this far (m) from where it answered, the pillar bows out early.</summary>
        private const float MovedDistance = 3f;

        /// <summary>Seconds a dismissed pillar takes to fade.</summary>
        private const float DismissFade = 1.2f;

        private readonly SonarTuning _tuning;
        private readonly ITerrainQuery _terrain;
        private readonly Transform _pillar;
        private readonly GlowRenderer _pillarGlow;
        private Vector3 _position;
        private float _answerTime = float.NegativeInfinity;
        private float _dismissTime = float.PositiveInfinity;
        private readonly float _scale;
        private float _brightness;
        private float _lingered;
        private bool _hasPosition;

        /// <param name="scale">Brightness scale of every glow of this marker (warm markers read brighter).</param>
        public SiteMarker(string name, Transform parent, SonarTuning tuning, ITerrainQuery terrain,
            Material pillarMaterial, Mesh pillarMesh, float scale)
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
        }

        /// <summary>Where the marker stands (valid after the first answer).</summary>
        public Vector3 Position => _position;

        public float PillarIntensity => _pillarGlow.Intensity;

        /// <summary>True while the pillar is still on the horizon.</summary>
        public bool PillarStanding(float now)
        {
            return _hasPosition && now < _dismissTime && now - _answerTime < _tuning.PillarLifetime;
        }

        /// <summary>Stands the marker at <paramref name="groundPosition"/> without an answer.</summary>
        public void Place(Vector3 groundPosition)
        {
            if (_hasPosition && SurfaceRules.HorizontalDistance(groundPosition, _position) <= 0.05f)
            {
                return;
            }

            _position = SurfaceRules.OnSurface(_terrain, groundPosition.x, groundPosition.z);
            _pillar.position = _position;
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
        /// Updates the pillar at <paramref name="now"/> for a find now at <paramref name="position"/> that still
        /// answers the sonar (<paramref name="answers"/>): moved away or fallen silent, it bows out.
        /// </summary>
        public void Tick(Vector3 position, bool answers, float now)
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
        }
    }
}
