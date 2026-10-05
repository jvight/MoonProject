using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    public sealed class RockModelTests
    {
        [Test]
        public void Variants_AreClosedRocks_GrowingFromPebbleToBoulder()
        {
            float previous = 0f;
            for (int i = 0; i < RockModelBuilder.VariantCount; i++)
            {
                ModelNode rock = RockModelBuilder.CreateModel(i);
                LowPolyMeshBuilder geometry = rock.Mesh.Geometry;
                Bounds bounds = geometry.Bounds;
                float footprint = Mathf.Max(bounds.size.x, bounds.size.z);

                Assert.AreEqual($"Rock_{i:00}", rock.Name);
                Assert.AreEqual(0, rock.Children.Count);
                MeshChecks.AssertWellFormed(geometry);
                MeshChecks.AssertClosedAndOutward(geometry);
                Assert.Less(bounds.min.y, 0f, "rocks are partly buried");
                Assert.Greater(bounds.max.y, 0f);
                Assert.Greater(footprint, previous, $"Rock_{i:00} should be larger than its predecessor");
                previous = footprint;
            }
        }

        [Test]
        public void InvalidVariant_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => RockModelBuilder.CreateModel(RockModelBuilder.VariantCount));
        }
    }
}
