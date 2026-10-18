using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The headlamp glasses follow the road light: dark when it is off, their material's own glow when it is on, in
    /// between while it eases, and each glass keeps its own material's strength as "on".
    /// </summary>
    public sealed class HeadlampGlassTests
    {
        private const string ShaderName = "MoonProject/R1/Lit";
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        private Renderer Glass(float onStrength)
        {
            Shader shader = Shader.Find(ShaderName);
            Assert.IsNotNull(shader, ShaderName);
            var material = new Material(shader);
            material.SetFloat(EmissionStrengthId, onStrength);
            var glass = new GameObject("Glass");
            var renderer = glass.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            _made.Add(material);
            _made.Add(glass);
            return renderer;
        }

        private static float Strength(Renderer glass)
        {
            var block = new MaterialPropertyBlock();
            glass.GetPropertyBlock(block);
            return block.GetFloat(EmissionStrengthId);
        }

        [Test]
        public void TheGlasses_GoDarkWithTheLamp_AndBackToTheirOwnGlow()
        {
            Renderer left = Glass(1f);
            Renderer right = Glass(1.5f);
            Assert.IsTrue(HeadlampGlass.TryCreate(new[] { left, right }, out HeadlampGlass glow, out string problem),
                problem);

            glow.Apply(0f);
            Assert.AreEqual(0f, Strength(left), 1e-5f, "the lamp clicked off: the glass is dark");
            Assert.AreEqual(0f, Strength(right), 1e-5f);

            glow.Apply(1f);
            Assert.AreEqual(1f, Strength(left), 1e-5f, "on: the material's own glow");
            Assert.AreEqual(1.5f, Strength(right), 1e-5f, "each glass keeps its own strength");

            glow.Apply(0.5f);
            Assert.AreEqual(0.5f, Strength(left), 1e-5f, "dimmed with the road light");
            Assert.AreEqual(0.75f, Strength(right), 1e-5f);

            glow.Apply(3f);
            Assert.AreEqual(1f, Strength(left), 1e-5f, "never brighter than its own glow");
        }

        [Test]
        public void AGlassWithoutAnEmissiveMaterial_IsRefused()
        {
            var bare = new GameObject("Bare");
            _made.Add(bare);
            Renderer renderer = bare.AddComponent<MeshRenderer>();
            Assert.IsFalse(HeadlampGlass.TryCreate(new[] { renderer }, out _, out string problem));
            StringAssert.Contains("_EmissionStrength", problem);
            Assert.IsFalse(HeadlampGlass.TryCreate(null, out _, out _));
        }
    }
}
