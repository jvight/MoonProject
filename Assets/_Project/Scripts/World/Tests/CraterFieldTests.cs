using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// M3-18 V4's middle scale of ground: a few hundred small craters in patchy fields with bare plains between, kept
    /// off every world anchor's ground (no anchor moves, every pad stays flat), and the floor still smooth and
    /// drivable through them.
    /// </summary>
    public sealed class CraterFieldTests
    {
        private const int MinCraters = 150;
        private const int MaxCraters = 600;

        // Patchy, never confetti (ruling 9): much of the floor stays bare, yet the fields are real.
        private const float MinBareShare = 0.6f;
        private const float MaxBareShare = 0.97f;
        private const float GridStep = 6f;

        // The drivable floor's own limits (MoonSurfaceTests): steepest slope and the smoothness of the normal.
        private const float MaxDrivableSlopeDegrees = 20f;
        private const float NormalStep = 0.4f;
        private const float MaxTurnPerStep = 6f;
        private const int AnchorRing = 4;
        private const float HeightTolerance = 1e-4f;

        private SurfaceSettings _settings;
        private MoonSurface _plain;
        private MoonSurface _cratered;
        private WorldAnchors _anchors;
        private CraterField _field;

        [OneTimeSetUp]
        public void BuildWorld()
        {
            _settings = new SurfaceSettings();
            _plain = new MoonSurface(_settings, WorldSettings.DefaultSeed);
            _cratered = new MoonSurface(_settings, WorldSettings.DefaultSeed);
            _anchors = new WorldAnchors(_cratered, _settings.Canyon, new RelaySettings());
            _field = new CraterField(_settings.CraterField, WorldSettings.DefaultSeed,
                _settings.FloorRadius - _settings.RimWarpAmplitude, _settings.PadRadius + _settings.PadBlend, _anchors,
                _cratered.Canyon.Bounds, _cratered.SampleNormal);
            _cratered.AttachCraterField(_field);
        }

        [Test]
        public void Field_HoldsAFewHundredSmallCraters()
        {
            Assert.That(_field.Count, Is.InRange(MinCraters, MaxCraters));
        }

        [Test]
        public void Field_IsPatchy_WithBarePlainsBetweenTheFields()
        {
            float extent = _settings.FloorRadius;
            int floor = 0;
            int bare = 0;
            for (float z = -extent; z <= extent; z += GridStep)
            {
                for (float x = -extent; x <= extent; x += GridStep)
                {
                    if (x * x + z * z > extent * extent)
                    {
                        continue;
                    }

                    float bowl = 0f;
                    float rim = 0f;
                    _field.Sample(x, z, ref bowl, ref rim);
                    floor++;
                    bare += bowl <= 0f && rim <= 0f ? 1 : 0;
                }
            }

            Assert.That((float)bare / floor, Is.InRange(MinBareShare, MaxBareShare));
        }

        [Test]
        public void Anchors_StandOnUntouchedGround()
        {
            for (int i = 0; i < _anchors.Count; i++)
            {
                WorldAnchor anchor = _anchors.Get(i);
                for (int k = 0; k <= AnchorRing; k++)
                {
                    float angle = k * Mathf.PI * 2f / AnchorRing;
                    float reach = k == 0 ? 0f : anchor.Radius;
                    float x = anchor.Position.x + Mathf.Cos(angle) * reach;
                    float z = anchor.Position.z + Mathf.Sin(angle) * reach;
                    Assert.AreEqual(_plain.Sample(x, z).Height, _cratered.Sample(x, z).Height, HeightTolerance,
                        $"{anchor.Id}'s ground at ({x}, {z})");
                }
            }
        }

        [Test]
        public void CrateredFloor_StaysDrivableAndSmooth()
        {
            var random = new SeededRandom(11u);
            int inCraters = 0;
            for (int i = 0; i < 60000; i++)
            {
                float x = random.Range(-_settings.FloorRadius, _settings.FloorRadius);
                float z = random.Range(-_settings.FloorRadius, _settings.FloorRadius);
                float bowl = 0f;
                float rim = 0f;
                _field.Sample(x, z, ref bowl, ref rim);
                if ((bowl <= 0f && rim <= 0f) || !_cratered.IsDrivable(x, z))
                {
                    continue;
                }

                inCraters++;
                Vector3 normal = _cratered.SampleNormal(x, z);
                Assert.LessOrEqual(Vector3.Angle(normal, Vector3.up), MaxDrivableSlopeDegrees,
                    $"slope at ({x}, {z}), {Vector3.Angle(_plain.SampleNormal(x, z), Vector3.up)} without the craters");
                Assert.Less(Vector3.Angle(normal, _cratered.SampleNormal(x + NormalStep, z + NormalStep * 0.5f)),
                    MaxTurnPerStep, $"crater too tightly curved at ({x}, {z})");
            }

            Assert.Greater(inCraters, 100, "the samples should find the craters");
        }
    }
}
