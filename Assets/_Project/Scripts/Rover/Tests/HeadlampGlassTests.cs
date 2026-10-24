using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The headlamp glasses follow the road light: their slot of the body goes dark when the light is off and back to
    /// its material's own glow when it is on, in between while it eases, and the body's other slots are untouched.
    /// </summary>
    public sealed class HeadlampGlassTests
    {
        private const string ShaderName = "MoonProject/R1/Lit";
        private const int LampSlot = 1;
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

        private Material Lit(float emission)
        {
            Shader shader = Shader.Find(ShaderName);
            Assert.IsNotNull(shader, ShaderName);
            var material = new Material(shader);
            material.SetFloat(EmissionStrengthId, emission);
            _made.Add(material);
            return material;
        }

        /// <summary>A body with a hull slot and the lamp glass slot, like RoverModel's Body.</summary>
        private Renderer Body(float lampGlow)
        {
            var body = new GameObject("Body");
            _made.Add(body);
            var renderer = body.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { Lit(0f), Lit(lampGlow) };
            return renderer;
        }

        private Renderer SingleLens(float lampGlow)
        {
            var lens = new GameObject("Lens_L");
            _made.Add(lens);
            var renderer = lens.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { Lit(lampGlow) };
            return renderer;
        }

        private static float Strength(Renderer body, int slot)
        {
            var block = new MaterialPropertyBlock();
            body.GetPropertyBlock(block, slot);
            return block.GetFloat(EmissionStrengthId);
        }

        private static float RendererStrength(Renderer lens)
        {
            var block = new MaterialPropertyBlock();
            lens.GetPropertyBlock(block);
            return block.GetFloat(EmissionStrengthId);
        }

        private static void WriteSentinelStrength(Renderer lens, float sentinel)
        {
            var block = new MaterialPropertyBlock();
            block.SetFloat(EmissionStrengthId, sentinel);
            lens.SetPropertyBlock(block);
        }

        [Test]
        public void TheGlassSlot_GoesDarkWithTheLamp_AndBackToItsOwnGlow_LeavingTheHullAlone()
        {
            Renderer body = Body(1.5f);
            Assert.IsTrue(HeadlampGlass.TryCreate(body, LampSlot, out HeadlampGlass glow, out string problem), problem);

            glow.Apply(0f);
            Assert.AreEqual(0f, Strength(body, LampSlot), 1e-5f, "the lamp clicked off: the glass is dark");
            Assert.AreEqual(0f, Strength(body, 0), 1e-5f, "the hull's slot is never written");

            glow.Apply(1f);
            Assert.AreEqual(1.5f, Strength(body, LampSlot), 1e-5f, "on: the material's own glow");

            glow.Apply(0.5f);
            Assert.AreEqual(0.75f, Strength(body, LampSlot), 1e-5f, "dimmed with the road light");

            glow.Apply(3f);
            Assert.AreEqual(1.5f, Strength(body, LampSlot), 1e-5f, "never brighter than its own glow");
        }

        [Test]
        public void SingleMaterialLens_WritesRendererPropertyBlockOnce_AndSkipsUnchangedLevels()
        {
            Renderer lens = SingleLens(2.4f);
            Assert.IsTrue(HeadlampGlass.TryCreate(lens, 0, out HeadlampGlass glow, out string problem), problem);

            glow.Apply(1f);
            Assert.AreEqual(2.4f, RendererStrength(lens), 1e-5f, "single-material lens writes renderer property block");

            WriteSentinelStrength(lens, 99f);
            glow.Apply(1f);
            Assert.AreEqual(99f, RendererStrength(lens), 1e-5f,
                "unchanged level skips SetPropertyBlock so steady frames pay no native property-block write");

            glow.Apply(0.5f);
            Assert.AreEqual(1.2f, RendererStrength(lens), 1e-5f, "changed level updates the renderer property block");

            glow.Apply(0f);
            Assert.AreEqual(0f, RendererStrength(lens), 1e-5f, "dark level writes zero once");

            WriteSentinelStrength(lens, 77f);
            glow.Apply(0f);
            Assert.AreEqual(77f, RendererStrength(lens), 1e-5f,
                "repeated zero level skips SetPropertyBlock while the pod or lamp stays off");
        }

        [Test]
        public void AMissingBodyOrSlot_IsRefused()
        {
            Renderer body = Body(1f);
            Assert.IsFalse(HeadlampGlass.TryCreate(body, 5, out _, out string problem));
            StringAssert.Contains("slot 5", problem);
            Assert.IsFalse(HeadlampGlass.TryCreate(null, 0, out _, out _));
        }
    }
}
