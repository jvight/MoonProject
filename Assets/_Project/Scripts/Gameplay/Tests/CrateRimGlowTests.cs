using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The lander crate's rim glow (opening-v2 B3/B4): dark beyond its reach, warming as 07 drives in and full inside
    /// the band, easing both ways, cooling off once the crate is emptied, its warm light following, with Art's strength
    /// and bloom cap raised together.
    /// </summary>
    public sealed class CrateRimGlowTests
    {
        private const string R1LitPath = "Assets/_Project/Shaders/R1/R1Lit.shader";
        private const float ArtStrength = 0.34f;
        private const float ArtCap = 0.42f;
        private const float Frame = 1f / 60f;

        private static readonly int StrengthId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int CapId = Shader.PropertyToID("_BloomSafeMax");

        private GameObject _rim;
        private MeshRenderer _renderer;
        private Light _light;
        private OpeningTuning _tuning;
        private Material _material;

        [SetUp]
        public void Build()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(R1LitPath);
            Assert.IsNotNull(shader, "the R1 lit shader Art's rim glow material uses");
            _material = new Material(shader);
            _material.SetFloat(StrengthId, ArtStrength);
            _material.SetFloat(CapId, ArtCap);
            _rim = new GameObject("RimGlow");
            _renderer = _rim.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _light = new GameObject("RimLight").AddComponent<Light>();
            _tuning = ScriptableObject.CreateInstance<OpeningTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rim);
            Object.DestroyImmediate(_light.gameObject);
            Object.DestroyImmediate(_tuning);
            Object.DestroyImmediate(_material);
        }

        [Test]
        public void TheShare_IsNoneBeyondTheReach_AndFullInsideTheBand()
        {
            Assert.AreEqual(0f, CrateRimGlow.Share(31f, 30f, 8f), 1e-5f, "beyond the reach");
            Assert.AreEqual(0.5f, CrateRimGlow.Share(26f, 30f, 8f), 1e-5f, "halfway through the band");
            Assert.AreEqual(1f, CrateRimGlow.Share(22f, 30f, 8f), 1e-5f, "at the band's inner edge");
            Assert.AreEqual(1f, CrateRimGlow.Share(2f, 30f, 8f), 1e-5f, "close by");
        }

        [Test]
        public void TheRim_WarmsAs07DrivesIn_AndCoolsOnceTheCrateIsEmptied()
        {
            var glow = new CrateRimGlow(_rim, _light, _tuning);
            Assert.IsFalse(_rim.activeSelf, "dark until 07 comes within reach");
            Assert.IsFalse(_light.enabled);

            glow.Step(true, _tuning.CrateRimReach + 1f, _tuning.CrateRimFadeSeconds);
            Assert.IsFalse(_rim.activeSelf, "a crate across the basin never glows");

            glow.Step(true, _tuning.CrateRimReach - _tuning.CrateRimBand, Frame);
            Assert.IsTrue(_rim.activeSelf, "lit as 07 comes within the reach");
            Assert.Less(glow.Glow, 0.1f, "warming, not snapped on");

            glow.Step(true, _tuning.CrateRimReach - _tuning.CrateRimBand, _tuning.CrateRimFadeSeconds);
            Assert.AreEqual(1f, glow.Glow, 1e-4f, "full inside the band");
            Assert.AreEqual(ArtStrength * _tuning.CrateRimStrength, Written(StrengthId), 1e-4f);
            Assert.AreEqual(ArtCap * _tuning.CrateRimStrength, Written(CapId), 1e-4f,
                "Art's bloom cap rises with the strength");
            Assert.IsTrue(_light.enabled);
            Assert.AreEqual(_tuning.CrateRimLightIntensity, _light.intensity, 1e-4f, "the warm light follows");

            glow.Step(false, 2f, _tuning.CrateRimFadeSeconds * 0.5f);
            Assert.That(glow.Glow, Is.GreaterThan(0.1f).And.LessThan(0.9f), "cooling, not snapped off");

            glow.Step(false, 2f, _tuning.CrateRimFadeSeconds);
            Assert.AreEqual(0f, glow.Glow, 1e-4f, "dark once emptied, even with 07 beside it");
            Assert.IsFalse(_rim.activeSelf);
            Assert.IsFalse(_light.enabled);
        }

        [Test]
        public void ARimWithoutArtsGlowMaterial_FailsFast()
        {
            _renderer.sharedMaterial = null;
            Assert.Throws<System.ArgumentException>(() => new CrateRimGlow(_rim, _light, _tuning));
        }

        private float Written(int id)
        {
            var block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block);
            return block.GetFloat(id);
        }
    }
}
