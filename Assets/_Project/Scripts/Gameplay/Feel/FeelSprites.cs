using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A pooled set of glowing sprites drawn by one ParticleSystem that never simulates: each frame the owner writes
    /// every live sprite (position, size, colour, velocity for streaks) into a preallocated array and hands it over
    /// in one call, so the feel kit's motes, sparks, beads and flashes cost no garbage and follow exact, tested curves.
    /// </summary>
    public sealed class FeelSprites
    {
        private const float Forever = 1e5f;

        private readonly ParticleSystem _system;
        private readonly ParticleSystem.Particle[] _particles;
        private int _count;

        /// <param name="streaks">Stretch each sprite along its velocity (sparks) instead of a round billboard.</param>
        public FeelSprites(string name, Transform parent, Material material, int capacity, bool streaks,
            float streakScale)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material), $"{name}: the feel sprite material is missing.");
            }

            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            _system = host.AddComponent<ParticleSystem>();
            _system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = _system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.simulationSpeed = 0f;
            main.maxParticles = capacity;
            ParticleSystem.EmissionModule emission = _system.emission;
            emission.enabled = false;
            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = streaks ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            renderer.velocityScale = streaks ? streakScale : 0f;
            renderer.lengthScale = 1f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _particles = new ParticleSystem.Particle[capacity];

            // Playing with no emission and no simulation speed: it only draws what the owner sets each frame.
            _system.Play(true);
        }

        public int Capacity => _particles.Length;

        /// <summary>Sprites written since the last <see cref="Begin"/> (tests and debugging views).</summary>
        public int Count => _count;

        /// <summary>Starts a frame's batch of sprites.</summary>
        public void Begin()
        {
            _count = 0;
        }

        /// <summary>Adds one sprite to this frame's batch (silently full at <see cref="Capacity"/>).</summary>
        public void Add(Vector3 position, float size, Color color, Vector3 velocity)
        {
            if (_count >= _particles.Length)
            {
                return;
            }

            ref ParticleSystem.Particle particle = ref _particles[_count++];
            particle.position = position;
            particle.velocity = velocity;
            particle.startSize = size;
            particle.startColor = color;
            particle.remainingLifetime = Forever;
            particle.startLifetime = Forever;
        }

        /// <summary>Hands this frame's batch to the renderer.</summary>
        public void End()
        {
            _system.SetParticles(_particles, _count);
        }
    }
}
