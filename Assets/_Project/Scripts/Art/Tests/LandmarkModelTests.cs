using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>M3-18 L2, L4, L7: the crew remnants World places across the basin.</summary>
    public sealed class LandmarkModelTests
    {
        private const int MaxSwatches = 4;

        private static IEnumerable<ModelNode> All()
        {
            yield return LandmarkModelBuilder.CreateRoutePost();
            yield return LandmarkModelBuilder.CreateBridgeNear();
            yield return LandmarkModelBuilder.CreateBridgeFar();
            yield return LandmarkModelBuilder.CreateBridgeCable();
            for (int variant = 0; variant < LandmarkMeshes.DishVariants; variant++)
            {
                yield return LandmarkModelBuilder.CreateDishFrame(variant);
            }
        }

        [Test]
        public void Remnants_AreWeatheredLowPoly_InAtMostFourSwatches_AndNeverGlowOnTheirOwn()
        {
            foreach (ModelNode model in All())
            {
                Assert.AreEqual(Vector3.zero, model.LocalPosition, $"{model.Name}'s root sits at its ground point");
                List<PaletteSwatch> swatches = MeshChecks.Swatches(model.Mesh.Geometry);
                Assert.LessOrEqual(swatches.Count, MaxSwatches, model.Name);
                Assert.IsFalse(swatches.Exists(Palette.IsEmissive), $"{model.Name}'s body never glows");
                CollectionAssert.DoesNotContain(swatches, PaletteSwatch.FadedPaint, "pale is a bug (V8)");
            }
        }

        [Test]
        public void RoutePost_StandsAtRoverHeight_WithADarkReflectorBothWaysAlongTheRoad()
        {
            ModelNode post = LandmarkModelBuilder.CreateRoutePost();
            Assert.That(post.Mesh.Geometry.Bounds.max.y, Is.InRange(1.1f, 1.6f), "07's lamp height");
            ModelNode reflector = post.GetDescendant(LandmarkModelBuilder.ReflectorName);
            Assert.AreEqual(ModelMaterial.PaletteGlowOff, reflector.Material);
            Bounds glass = reflector.Mesh.Geometry.Bounds;
            Assert.Greater(glass.max.z, 0f, "faces +Z along the road");
            Assert.Less(glass.min.z, 0f, "and -Z");
        }

        [Test]
        public void Bridge_NearAnchorsFaceTheChasm_AndTheFarOneCarriesItsLamp()
        {
            Bounds near = LandmarkModelBuilder.CreateBridgeNear().Mesh.Geometry.Bounds;
            Assert.GreaterOrEqual(near.size.x, LandmarkMeshes.AnchorSpan, "two anchors side by side");
            Assert.Less(near.min.y, -1f, "their snapped cables hang down over the lip");
            ModelNode far = LandmarkModelBuilder.CreateBridgeFar();
            ModelNode lamp = far.GetDescendant(LandmarkModelBuilder.LampName);
            Assert.AreEqual(ModelMaterial.PaletteGlowOff, lamp.Material, "World lights the far lamp warm");
            Assert.Greater(lamp.LocalPosition.y, LandmarkMeshes.AnchorHeight, "on top of the far anchor");
            Bounds cable = LandmarkModelBuilder.CreateBridgeCable().Mesh.Geometry.Bounds;
            Assert.Greater(cable.size.x, 10f, "the cable runs a long way down the trough");
            Assert.Less(cable.size.y, 0.5f, "it lies in the trough");
        }

        [Test]
        public void DishFrames_LieToppled_WideAndLow()
        {
            for (int variant = 0; variant < LandmarkMeshes.DishVariants; variant++)
            {
                Bounds frame = LandmarkModelBuilder.CreateDishFrame(variant).Mesh.Geometry.Bounds;
                Assert.Greater(Mathf.Max(frame.size.x, frame.size.z), 4.5f, $"variant {variant} reads at 120 m");
                Assert.Less(frame.max.y, LandmarkMeshes.DishRadius * 2f, $"variant {variant} lies toppled");
                Assert.Greater(frame.max.y, 1f, $"variant {variant} is a frame on its rim, not a flat ring");
            }
        }
    }
}
