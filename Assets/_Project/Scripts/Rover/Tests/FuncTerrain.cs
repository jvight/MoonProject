using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>An analytic <see cref="ITerrainQuery"/> from a height function, for composition tests.</summary>
    public sealed class FuncTerrain : ITerrainQuery
    {
        private const float NormalStep = 0.05f;
        private const float HalfExtent = 10000f;

        private readonly Func<float, float, float> _height;

        public FuncTerrain(Func<float, float, float> height)
        {
            _height = height ?? throw new ArgumentNullException(nameof(height));
        }

        public Rect PlayableArea => new Rect(-HalfExtent, -HalfExtent, 2f * HalfExtent, 2f * HalfExtent);

        public bool IsDrivable(float x, float z)
        {
            return true;
        }

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
