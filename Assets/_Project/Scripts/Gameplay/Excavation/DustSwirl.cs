using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Soft lavender dust that rises and swirls around a dig site while the beam lifts. One particle system, built
    /// once at initialisation; its emission rate is eased per frame (zero when idle, so it costs nothing).
    /// </summary>
    public sealed class DustSwirl
    {
        private const float Duration = 5f;
        private const float RadialDrift = 0.15f;
        private const float FlatShapeHeight = 0.05f;

        private readonly ParticleSystem _particles;
        private readonly float _rate;

        public DustSwirl(Transform parent, ExcavationTuning tuning, Material material)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            _rate = tuning.DustRate;
            var host = new GameObject("DustSwirl");
            host.transform.SetParent(parent, false);
            _particles = host.AddComponent<ParticleSystem>();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = _particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = Duration;
            main.startLifetime = new ParticleSystem.MinMaxCurve(tuning.DustLifetime.x, tuning.DustLifetime.y);
            main.startSize = new ParticleSystem.MinMaxCurve(tuning.DustSize.x, tuning.DustSize.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(tuning.DustRise * 1.8f, tuning.DustRise * 2.8f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            main.startColor = Color.white;
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = tuning.MaxDust;

            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = _particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 32f;
            shape.radius = tuning.DustRadius;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            ParticleSystem.VelocityOverLifetimeModule velocity = _particles.velocityOverLifetime;
            velocity.enabled = false;

            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.75f, 0.55f),
                    new GradientAlphaKey(0f, 1f),
                });
            ParticleSystem.ColorOverLifetimeModule color = _particles.colorOverLifetime;
            color.enabled = true;
            color.color = fade;

            ParticleSystem.SizeOverLifetimeModule size = _particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.95f), new Keyframe(0.6f, 0.95f), new Keyframe(1f, 0.65f)));

            var particleRenderer = host.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.alignment = ParticleSystemRenderSpace.View;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            particleRenderer.lightProbeUsage = LightProbeUsage.Off;
            particleRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _particles.Play();
        }

        /// <summary>Live motes (tests and debugging views).</summary>
        public int ParticleCount => _particles.particleCount;

        public Vector3 Position => _particles.transform.position;

        /// <summary>
        /// Moves the swirl to <paramref name="ground"/> and emits at <paramref name="amount"/> (0..1).
        /// </summary>
        public void Emit(Vector3 ground, float amount)
        {
            if (amount > 0f)
            {
                _particles.transform.position = ground;
            }

            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.rateOverTime = _rate * Mathf.Clamp01(amount);
        }
    }
}
