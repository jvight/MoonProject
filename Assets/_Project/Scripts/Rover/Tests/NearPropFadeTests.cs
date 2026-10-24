using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// Props near the lens thin away by dither and come back (the feel audit's foreground fix): whole beyond the full
    /// distance, gone inside the gone distance, smooth between; 07's own layer and the tow's load are never touched.
    /// </summary>
    public sealed class NearPropFadeTests
    {
        private const float Gone = 1.2f;
        private const float Full = 3f;
        private const float HalfLife = 0.15f;

        private GameObject _prop;
        private Renderer _renderer;
        private NearPropFade _fade;

        [SetUp]
        public void SetUp()
        {
            _fade = new NearPropFade();
            _prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _prop.layer = Layers.Prop;
            _renderer = _prop.GetComponent<Renderer>();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            _fade.Reset();
            Object.DestroyImmediate(_prop);
        }

        private static float KeepOn(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetFloat("_NearKeep");
        }

        [Test]
        public void TheKeep_IsWholeBeyondTheFullDistance_GoneInsideTheGoneOne_AndSmoothBetween()
        {
            Assert.AreEqual(1f, NearPropFade.Keep(5f, Gone, Full));
            Assert.AreEqual(0f, NearPropFade.Keep(0.5f, Gone, Full));
            float middle = NearPropFade.Keep((Gone + Full) * 0.5f, Gone, Full);
            Assert.That(middle, Is.InRange(0.3f, 0.7f));
            Assert.Greater(NearPropFade.Keep(2.5f, Gone, Full), NearPropFade.Keep(1.8f, Gone, Full));
        }

        [Test]
        public void AProp_BetweenTheLensAndOneMetreAway_ThinsAway_AndComesBackWhenTheLensLeaves()
        {
            _prop.transform.position = new Vector3(0f, 0f, 2.4f);
            Physics.SyncTransforms();
            Vector3 lens = Vector3.zero;
            _fade.Step(lens, Gone, Full, HalfLife, 0f, new Renderer[0]);
            Assert.AreEqual(1, _fade.Count, "the prop is thinned");
            Assert.Less(KeepOn(_renderer), 0.9f);

            _fade.Step(lens + Vector3.back * 20f, Gone, Full, HalfLife, 0f, new Renderer[0]);
            Assert.AreEqual(0, _fade.Count, "it is whole again once the lens has gone");
            Assert.AreEqual(1f, KeepOn(_renderer), 1e-4f);
        }

        [Test]
        public void ASpareRenderer_TheTowsLoad_IsNeverThinned()
        {
            _prop.transform.position = new Vector3(0f, 0f, 2f);
            Physics.SyncTransforms();
            _fade.Step(Vector3.zero, Gone, Full, HalfLife, 0f, new[] { _renderer });
            Assert.AreEqual(0, _fade.Count);
        }

        [Test]
        public void SoftlyAtFirst_ItEasesWithTheHalfLife_NotAPop()
        {
            _prop.transform.position = new Vector3(0f, 0f, 2.2f);
            Physics.SyncTransforms();
            _fade.Step(Vector3.zero, Gone, Full, HalfLife, 0.016f, new Renderer[0]);
            float first = _fade.KeepOf(_renderer);
            Assert.Greater(first, 0.85f, "one frame in, it has barely begun to thin");
            for (int i = 0; i < 90; i++)
            {
                _fade.Step(Vector3.zero, Gone, Full, HalfLife, 0.016f, new Renderer[0]);
            }

            Assert.Less(_fade.KeepOf(_renderer), first, "and in a second and a half it is well on its way");
        }

        [Test]
        public void SomethingOnAnotherLayer_IsLeftAlone()
        {
            _prop.layer = Layers.Rover;
            _prop.transform.position = new Vector3(0f, 0f, 1.5f);
            Physics.SyncTransforms();
            _fade.Step(Vector3.zero, Gone, Full, HalfLife, 0f, new Renderer[0]);
            Assert.AreEqual(0, _fade.Count, "07 is never thinned");
        }
    }
}
