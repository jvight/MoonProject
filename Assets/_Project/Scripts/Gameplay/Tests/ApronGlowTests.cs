using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The apron's bay outline warms while build mode is open and cools when it closes, easing both ways, and keeps
    /// whatever else its property block carries.
    /// </summary>
    public sealed class ApronGlowTests
    {
        private const float FadeSeconds = 0.8f;
        private static readonly int GlowId = Shader.PropertyToID("_BuildModeGlow");
        private static readonly int OtherId = Shader.PropertyToID("_OtherProperty");

        private GameObject _host;
        private MeshRenderer _outline;

        [SetUp]
        public void Build()
        {
            _host = new GameObject("ApronGlowTestOutline");
            _outline = _host.AddComponent<MeshRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void TheOutline_WarmsGraduallyWhileBuildModeIsOpen_AndCoolsWhenItCloses()
        {
            var glow = new ApronGlow(_outline, FadeSeconds);
            Assert.AreEqual(0f, Written(), 1e-4f, "unlit before build mode opens");

            glow.Step(true, FadeSeconds * 0.5f);
            Assert.That(Written(), Is.GreaterThan(0.1f).And.LessThan(0.9f), "halfway through, warming, not lit");

            glow.Step(true, FadeSeconds);
            Assert.AreEqual(1f, Written(), 1e-4f, "fully lit while build mode stays open");

            glow.Step(false, FadeSeconds * 0.5f);
            Assert.That(Written(), Is.GreaterThan(0.1f).And.LessThan(0.9f), "cooling, not snapped off");

            glow.Step(false, FadeSeconds);
            Assert.AreEqual(0f, Written(), 1e-4f, "unlit once build mode has closed");
        }

        [Test]
        public void TheGlow_KeepsWhatElseThePropertyBlockCarries()
        {
            var block = new MaterialPropertyBlock();
            block.SetFloat(OtherId, 0.25f);
            _outline.SetPropertyBlock(block);

            var glow = new ApronGlow(_outline, FadeSeconds);
            glow.Step(true, FadeSeconds);

            _outline.GetPropertyBlock(block);
            Assert.AreEqual(0.25f, block.GetFloat(OtherId), 1e-4f, "World's own values survive the glow");
            Assert.AreEqual(1f, block.GetFloat(GlowId), 1e-4f);
        }

        private float Written()
        {
            var block = new MaterialPropertyBlock();
            _outline.GetPropertyBlock(block);
            return block.GetFloat(GlowId);
        }
    }
}
