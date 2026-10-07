using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>Dust motes in 07's lamp: as visible as the beam catching them, and only while 07 is slow.</summary>
    public sealed class LampMoteLightTests
    {
        private static readonly float CosOuter = Mathf.Cos(31f * Mathf.Deg2Rad);
        private static readonly float CosInner = Mathf.Cos(15f * Mathf.Deg2Rad);

        private RoverFxTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverFxTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private float Beam(Vector3 offset)
        {
            return LampMoteLight.Beam(_tuning, offset, Vector3.forward, CosOuter, CosInner);
        }

        [Test]
        public void SpeedVisibility_FullWhileStillOrCrawling_GoneOnceDriving()
        {
            Assert.AreEqual(1f, LampMoteLight.SpeedVisibility(_tuning, 0f), 1e-4f);
            Assert.AreEqual(1f, LampMoteLight.SpeedVisibility(_tuning, _tuning.MoteFadeSpeed), 1e-4f);
            Assert.AreEqual(0f, LampMoteLight.SpeedVisibility(_tuning, _tuning.MoteGoneSpeed), 1e-4f);
            float halfway = 0.5f * (_tuning.MoteFadeSpeed + _tuning.MoteGoneSpeed);
            Assert.That(LampMoteLight.SpeedVisibility(_tuning, halfway), Is.InRange(0.1f, 0.9f));
        }

        [Test]
        public void Beam_LightsMotesInTheConeNearTheLens_AndNoneOutsideIt()
        {
            float inBeam = Beam(new Vector3(0f, 0f, 1f));
            Assert.Greater(inBeam, 0.5f, "On the axis, a metre out: clearly lit.");
            Assert.Less(Beam(new Vector3(0f, 0f, 0.02f)), 0.05f, "Right at the lens: out of focus, hidden.");
            Assert.AreEqual(0f, Beam(Quaternion.Euler(0f, 45f, 0f) * Vector3.forward), 1e-4f, "Outside the cone.");
            Assert.Less(Beam(Quaternion.Euler(0f, 25f, 0f) * Vector3.forward), inBeam, "Softer at the cone's edge.");
        }

        [Test]
        public void Beam_ThinsWithDistance()
        {
            float near = Beam(new Vector3(0f, 0f, 2f * _tuning.MoteNearFade));
            float far = Beam(new Vector3(0f, 0f, _tuning.MoteReach));
            Assert.AreEqual(1f - _tuning.MoteFalloff, far / near, 0.1f);
        }

        [Test]
        public void Life_FadesInAndOut_NeverPopping()
        {
            Assert.AreEqual(0f, LampMoteLight.Life(_tuning, 0f), 1e-4f);
            Assert.AreEqual(1f, LampMoteLight.Life(_tuning, 0.5f), 1e-4f);
            Assert.AreEqual(0f, LampMoteLight.Life(_tuning, 1f), 1e-4f);
        }

        [Test]
        public void Glint_IsASlowShallowSwell()
        {
            float low = 1f;
            float high = 0f;
            for (float t = 0f; t < _tuning.MoteGlintPeriod; t += 0.05f)
            {
                float glint = LampMoteLight.Glint(_tuning, t, 0.3f);
                low = Mathf.Min(low, glint);
                high = Mathf.Max(high, glint);
            }

            Assert.AreEqual(1f, high, 0.01f);
            Assert.AreEqual(1f - _tuning.MoteGlintDepth, low, 0.01f);
        }
    }
}
