using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class LookSettingsTests
    {
        private const float Frame = 1f / 60f;
        private RoverCameraTuning _tuning;
        private LookSettings _look;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            _look = new LookSettings(_tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void StartsAsTuned()
        {
            Assert.AreEqual(1f, _look.Sensitivity);
            Assert.AreEqual(_tuning.InvertY, _look.InvertY);
        }

        [Test]
        public void InvertDefault_ComesFromTuning()
        {
            var serialized = new SerializedObject(_tuning);
            serialized.FindProperty("_invertY").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(new LookSettings(_tuning).InvertY);
        }

        [TestCase(0.01f)]
        [TestCase(-2f)]
        public void Sensitivity_BelowRange_ClampsToMinimum(float requested)
        {
            _look.Sensitivity = requested;
            Assert.AreEqual(_tuning.MinSensitivity, _look.Sensitivity);
        }

        [Test]
        public void Sensitivity_AboveRange_ClampsToMaximum()
        {
            _look.Sensitivity = 50f;
            Assert.AreEqual(_tuning.MaxSensitivity, _look.Sensitivity);
        }

        [Test]
        public void Sensitivity_NaN_FallsBackToTuned()
        {
            _look.Sensitivity = 2f;
            _look.Sensitivity = float.NaN;
            Assert.AreEqual(1f, _look.Sensitivity);
        }

        [Test]
        public void Sensitivity_ScalesMouseAndStickAlike()
        {
            Vector2 mouse = _look.OrbitDegrees(new Vector2(10f, 0f), Vector2.zero, Frame);
            Vector2 stick = _look.OrbitDegrees(Vector2.zero, new Vector2(1f, 0f), Frame);
            _look.Sensitivity = 2f;
            Assert.AreEqual(2f * mouse.x, _look.OrbitDegrees(new Vector2(10f, 0f), Vector2.zero, Frame).x, 1e-5f);
            Assert.AreEqual(2f * stick.x, _look.OrbitDegrees(Vector2.zero, new Vector2(1f, 0f), Frame).x, 1e-5f);
        }

        [Test]
        public void Mouse_IsPerFrame_StickIsARate()
        {
            Assert.AreEqual(10f * _tuning.MouseSensitivity,
                _look.OrbitDegrees(new Vector2(10f, 0f), Vector2.zero, 0.5f).x, 1e-5f);
            Assert.AreEqual(_tuning.StickRate * 0.5f,
                _look.OrbitDegrees(Vector2.zero, new Vector2(1f, 0f), 0.5f).x, 1e-4f);
        }

        [Test]
        public void PushingUp_LooksUp_UnlessInverted()
        {
            float normal = _look.OrbitDegrees(new Vector2(0f, 10f), Vector2.zero, Frame).y;
            _look.InvertY = true;
            float inverted = _look.OrbitDegrees(new Vector2(0f, 10f), Vector2.zero, Frame).y;
            Assert.Less(normal, 0f, "Mouse up lowers the orbit elevation, so the view tilts up.");
            Assert.AreEqual(-normal, inverted, 1e-5f);
        }
    }
}
