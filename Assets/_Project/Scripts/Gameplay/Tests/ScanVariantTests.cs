using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// Verifies that <see cref="ScanTuning"/> switches cleanly between the three scan look variants
    /// (<see cref="ScanVariant.ProfilerSweep"/>, <see cref="ScanVariant.WireframeReveal"/>,
    /// <see cref="ScanVariant.PointScan"/>) with no code copies, pushing shader globals and driving
    /// <see cref="ScanBeam"/> and <see cref="ScanRibbon"/> per variant.
    /// </summary>
    public sealed class ScanVariantTests
    {
        private ScanTuning _scanTuning;
        private FeelVfxTuning _vfxTuning;
        private Material _beamMaterial;
        private Material _spriteMaterial;
        private GameObject _parent;
        private ScanBeam _beam;
        private ScanRibbon _ribbon;

        [SetUp]
        public void SetUp()
        {
            _scanTuning = ScriptableObject.CreateInstance<ScanTuning>();
            _vfxTuning = ScriptableObject.CreateInstance<FeelVfxTuning>();
            _beamMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            _spriteMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            _parent = new GameObject("ScanVariantTestParent");
            _beam = new ScanBeam(_parent.transform, _vfxTuning, _beamMaterial, _spriteMaterial, _scanTuning);
            _ribbon = new ScanRibbon(_parent.transform, _vfxTuning, _spriteMaterial, _scanTuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_parent);
            Object.DestroyImmediate(_beamMaterial);
            Object.DestroyImmediate(_spriteMaterial);
            Object.DestroyImmediate(_vfxTuning);
            Object.DestroyImmediate(_scanTuning);
        }

        [Test]
        public void SetVariant_UpdatesShaderGlobalAndTuningVariantAcrossAllThreeLooks()
        {
            int variantParamsId = Shader.PropertyToID("_MoonScanVariantParams");

            _scanTuning.SetVariant(ScanVariant.ProfilerSweep);
            Assert.AreEqual(ScanVariant.ProfilerSweep, _scanTuning.Variant);
            Vector4 v0 = Shader.GetGlobalVector(variantParamsId);
            Assert.AreEqual(0f, v0.x, 1e-4f);
            Assert.Greater(v0.y, 0.01f);

            _scanTuning.SetVariant(ScanVariant.WireframeReveal);
            Assert.AreEqual(ScanVariant.WireframeReveal, _scanTuning.Variant);
            Vector4 v1 = Shader.GetGlobalVector(variantParamsId);
            Assert.AreEqual(1f, v1.x, 1e-4f);
            Assert.Greater(v1.z, 0.5f);

            _scanTuning.SetVariant(ScanVariant.PointScan);
            Assert.AreEqual(ScanVariant.PointScan, _scanTuning.Variant);
            Vector4 v2 = Shader.GetGlobalVector(variantParamsId);
            Assert.AreEqual(2f, v2.x, 1e-4f);
            Assert.Greater(v2.w, 0.05f);
        }

        [TestCase(ScanVariant.ProfilerSweep)]
        [TestCase(ScanVariant.WireframeReveal)]
        [TestCase(ScanVariant.PointScan)]
        public void ScanBeamAndRibbon_StepCleanlyAcrossAllVariants(ScanVariant variant)
        {
            _scanTuning.SetVariant(variant);
            Vector3 lens = new Vector3(0f, 0.9f, 0f);
            Vector3 target = new Vector3(0f, 0.45f, 2.2f);
            Vector3 span = new Vector3(0f, 0.72f, 0f);

            _beam.Begin(target, span, 0.45f, 1.2f);
            _beam.Step(0.08f, lens);
            Assert.IsTrue(_beam.Shown);
            Assert.IsTrue(_beam.WedgeEnabled);
            Assert.Greater(_beam.ActivePointCount, 0);
            Assert.Greater(_beam.ActiveStreakCount, 0);

            _ribbon.Begin(target, span);
            _ribbon.Step(_vfxTuning.ScanRibbonDelay + _vfxTuning.ScanRibbonSeconds * 0.5f, lens);
            Assert.AreEqual(_vfxTuning.ScanRibbonBeads, _ribbon.Shown);
        }
    }
}
