using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>Analytic <see cref="ITerrainQuery"/> stand-in matching the colliders a test session builds.</summary>
    public sealed class TestTerrain : ITerrainQuery
    {
        private const float NormalStep = 0.05f;
        private readonly Func<float, float, float> _height;

        public TestTerrain(Func<float, float, float> height)
        {
            _height = height ?? throw new ArgumentNullException(nameof(height));
        }

        public Rect PlayableArea => new Rect(-500f, -500f, 1000f, 1000f);

        public float SampleHeight(float x, float z)
        {
            return _height(x, z);
        }

        public Vector3 SampleNormal(float x, float z)
        {
            float dx = _height(x + NormalStep, z) - _height(x - NormalStep, z);
            float dz = _height(x, z + NormalStep) - _height(x, z - NormalStep);
            return new Vector3(-dx, 2f * NormalStep, -dz).normalized;
        }
    }
}
