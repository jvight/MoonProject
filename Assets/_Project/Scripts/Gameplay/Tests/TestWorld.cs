using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// Analytic stand-in for the World's surface: a gentle bowl with soft dunes, a drivable disc, an optional steep
    /// mound (to prove slope rules) and The Peak beyond the rim.
    /// </summary>
    internal sealed class TestWorld : ITerrainQuery, IWorldLayout
    {
        public const float DrivableRadius = 300f;
        private const float NormalStep = 0.25f;

        private readonly Func<float, float, float> _height;

        private TestWorld(Func<float, float, float> height)
        {
            _height = height;
            float half = DrivableRadius * 0.70710678f;
            PlayableArea = new Rect(-half, -half, half * 2f, half * 2f);
        }

        /// <summary>Perfectly flat floor at height 0.</summary>
        public static TestWorld Flat()
        {
            return new TestWorld((x, z) => 0f);
        }

        /// <summary>
        /// Bowl rising 6 m to the rim, 1 m dunes, and a 12 m steep mound at <see cref="MoundCentre"/>.
        /// </summary>
        public static TestWorld Basin()
        {
            return new TestWorld((x, z) =>
            {
                float r = Mathf.Sqrt(x * x + z * z) / DrivableRadius;
                float bowl = 6f * r * r;
                float dunes = Mathf.Sin(x / 23f) * Mathf.Cos(z / 19f);
                float mound = Mathf.Max(0f, 12f - 0.8f * Vector2.Distance(new Vector2(x, z),
                    new Vector2(MoundCentre.x, MoundCentre.z)));
                return bowl + dunes + mound;
            });
        }

        public static Vector3 MoundCentre => new Vector3(60f, 0f, 80f);

        public Rect PlayableArea { get; }

        public Vector3 BasePosition => new Vector3(0f, _height(0f, 0f), 0f);

        public Vector3 PeakPosition => new Vector3(90f, 150f, 430f);

        public Vector3 EarthDirection => new Vector3(0f, 0.5f, 0.866f);

        public bool IsDrivable(float x, float z)
        {
            return x * x + z * z <= DrivableRadius * DrivableRadius;
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
