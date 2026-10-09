using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The Rover Bay's weld sparks at one gantry arm's tip: a few soft bursts of warm streaks, drifting down in lunar
    /// gravity and following the tip wherever the arm is driven. One particle system, built once at initialisation;
    /// idle (and free) until a fitting plays it.
    /// </summary>
    public sealed class BenchSparks
    {
        private const float ConeRadius = 0.02f;
        private const float EndSize = 0.3f;

        private readonly ParticleSystem _particles;

        public BenchSparks(Transform socket, WorkshopTuning tuning, Material material)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            var host = new GameObject("WeldSparks");
            host.transform.SetParent(socket, false);
            _particles = host.AddComponent<ParticleSystem>();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = _particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = tuning.SparkInterval * tuning.SparkBursts;
            main.startLifetime = new ParticleSystem.MinMaxCurve(tuning.SparkLifetime.x, tuning.SparkLifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(tuning.SparkSpeed.x, tuning.SparkSpeed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(tuning.SparkSize.x, tuning.SparkSize.y);
            main.startColor = Color.white;
            main.gravityModifier = tuning.SparkGravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = tuning.SparkCount * tuning.SparkBursts;

            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.rateOverTime = 0f;
            var bursts = new ParticleSystem.Burst[tuning.SparkBursts];
            for (int i = 0; i < bursts.Length; i++)
            {
                bursts[i] = new ParticleSystem.Burst(tuning.SparkInterval * i, (short)tuning.SparkCount);
            }

            emission.SetBursts(bursts);

            ParticleSystem.ShapeModule shape = _particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = tuning.SparkSpread;
            shape.radius = ConeRadius;
            shape.rotation = new Vector3(-tuning.SparkTilt, 0f, 0f);

            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
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
        }

        /// <summary>Live sparks (tests and debugging views).</summary>
        public int ParticleCount => _particles.particleCount;

        /// <summary>Plays the bursts from the start; sparks still in the air keep falling.</summary>
        public void Burst()
        {
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _particles.Play(true);
        }
    }
}
