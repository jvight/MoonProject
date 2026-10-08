using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The warm sparks where 07's beam cuts a piece loose: a soft cone of short streaks from the cut face, drifting
    /// down in lunar gravity. One looping particle system, built once; it emits only while the beam works, so idle it
    /// costs nothing.
    /// </summary>
    public sealed class CutSparks
    {
        private const float ConeRadius = 0.04f;
        private const float EndSize = 0.3f;

        private readonly ParticleSystem _particles;
        private readonly Transform _host;
        private readonly float _rate;
        private float _applied = -1f;

        public CutSparks(Transform parent, SalvageTuning tuning, Material material)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            var host = new GameObject("CutSparks");
            host.transform.SetParent(parent, false);
            _host = host.transform;
            _rate = tuning.SparkRate;
            _particles = host.AddComponent<ParticleSystem>();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = _particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(tuning.SparkLifetime.x, tuning.SparkLifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(tuning.SparkSpeed.x, tuning.SparkSpeed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(tuning.SparkSize.x, tuning.SparkSize.y);
            main.startColor = Color.white;
            main.gravityModifier = tuning.SparkGravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = tuning.MaxSparks;

            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = _particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = tuning.SparkSpread;
            shape.radius = ConeRadius;

            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = _particles.colorOverLifetime;
            color.enabled = true;
            color.color = fade;
            ParticleSystem.SizeOverLifetimeModule size = _particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, EndSize));

            var particleRenderer = host.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.velocityScale = tuning.SparkStreak;
            particleRenderer.lengthScale = 1f;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            particleRenderer.lightProbeUsage = LightProbeUsage.Off;
            particleRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _particles.Play(true);
        }

        /// <summary>Live sparks (tests and debugging views).</summary>
        public int ParticleCount => _particles.particleCount;

        /// <summary>
        /// Sparks fly from <paramref name="position"/> along <paramref name="normal"/> at <paramref name="level"/>
        /// (0 = none, 1 = the full rate); sparks already in the air keep falling.
        /// </summary>
        public void Emit(Vector3 position, Vector3 normal, float level)
        {
            float rate = _rate * Mathf.Clamp01(level);
            if (rate > 0f && normal.sqrMagnitude > 1e-6f)
            {
                _host.SetPositionAndRotation(position, Quaternion.LookRotation(normal));
            }

            if (Mathf.Approximately(rate, _applied))
            {
                return;
            }

            _applied = rate;
            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.rateOverTimeMultiplier = rate;
        }
    }
}
