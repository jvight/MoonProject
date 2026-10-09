using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Weathering v2 (VISION ruling 12): every piece of home and 07 carries three removable vertex-coloured skins on
    /// the weather material, stacked in order; the paint breaks into tired panels of many tones; rust runs hang from
    /// the walls; the dust rises from the ground; and every build is repeatable.
    /// </summary>
    public sealed class WeatheringTests
    {
        private static readonly string[] Layers = { "Weather_Paint", "Weather_Rust", "Weather_Dust" };

        // How far a skin may spread past its model's own bounds: drifts and debris lie round the feet.
        private const float StrayReach = 1.5f;

        private static IEnumerable<ModelNode> Weathered()
        {
            yield return BaseModelBuilder.CreateLander();
            yield return BaseModelBuilder.CreateShelf();
            yield return BaseModelBuilder.CreateRoverBay();
            yield return BaseModelBuilder.CreateCassetteShelf();
            for (int level = 1; level <= 3; level++)
            {
                yield return BaseModelBuilder.CreateTower(level);
            }

            yield return RoverModelBuilder.CreateModel().GetDescendant("Body");
        }

        [Test]
        public void EveryPiece_HasItsThreeSkins_OnTheWeatherMaterial()
        {
            foreach (ModelNode model in Weathered())
            {
                CollectionAssert.AreEqual(Layers, model.Children.Where(c => c.Name.StartsWith("Weather_"))
                    .Select(c => c.Name), model.Name);
                foreach (string layer in Layers)
                {
                    ModelNode skin = model.Children.First(c => c.Name == layer);
                    Assert.AreEqual(ModelMaterial.PaletteWeather, skin.Material, $"{model.Name}/{layer}");
                    Assert.IsTrue(skin.Mesh.Geometry.HasVertexColours, $"{model.Name}/{layer} carries colours");
                    Assert.IsFalse(MeshChecks.Swatches(skin.Mesh.Geometry).Any(Palette.IsEmissive),
                        $"{model.Name}/{layer} never glows");
                    MeshChecks.AssertWellFormed(skin.Mesh.Geometry);
                }
            }
        }

        [Test]
        public void Skins_HugTheirModel_WithNoStrayFacets()
        {
            foreach (ModelNode model in Weathered())
            {
                Bounds shape = model.Mesh.Geometry.Bounds;
                shape.Expand(new Vector3(StrayReach, StrayReach, StrayReach) * 2f);
                foreach (ModelNode skin in model.Children.Where(c => c.Name.StartsWith("Weather_")))
                {
                    Bounds reach = skin.Mesh.Geometry.Bounds;
                    Assert.IsTrue(shape.Contains(reach.min) && shape.Contains(reach.max),
                        $"{model.Name}/{skin.Name} reaches {reach} beyond its model {shape}");
                }
            }
        }

        [Test]
        public void Skins_StackPaintUnderRustUnderDust()
        {
            var wall = new LowPolyMeshBuilder();
            wall.Box(Place.At(0f, 1f, 0f), new Vector3(3f, 2f, 0.2f), PaletteSwatch.Enamel);
            var node = new ModelNode("Wall", Vector3.zero, new ModelMesh("Wall", wall));
            Weathering.Weather(node, "Wall", wall, Profile(), null, null);

            float front = 0.1f;
            float paint = Front(node, "Weather_Paint") - front;
            float rust = Front(node, "Weather_Rust") - front;
            float dust = Front(node, "Weather_Dust") - front;
            Assert.Greater(paint, 0f, "the paint stands off the wall");
            Assert.Greater(rust, paint, "rust over the paint");
            Assert.Greater(dust, rust, "dust over the rust");
        }

        [Test]
        public void Paint_BreaksABigWallIntoPanelsOfManyTones()
        {
            var wall = new LowPolyMeshBuilder();
            wall.Box(Place.At(0f, 1f, 0f), new Vector3(3f, 2f, 0.2f), PaletteSwatch.Enamel);
            var node = new ModelNode("Wall", Vector3.zero, new ModelMesh("Wall", wall));
            Weathering.Weather(node, "Wall", wall, Profile(), null, null);

            LowPolyMeshBuilder paint = node.GetDescendant("Weather_Paint").Mesh.Geometry;
            var front = new HashSet<Color32>();
            for (int v = 0; v < paint.VertexCount; v++)
            {
                if (paint.Normals[v].z > 0.99f)
                {
                    front.Add(paint.Colours[v]);
                }
            }

            Assert.GreaterOrEqual(front.Count, 4, "a 3 x 2 m wall reads as many tired panels, not one colour");
            Assert.IsFalse(front.Contains(Palette.GetSurface(PaletteSwatch.Enamel)), "no panel is still new");
        }

        [Test]
        public void RustRuns_HangFromTheWalls_AndDustRisesFromTheGround()
        {
            var wall = new LowPolyMeshBuilder();
            wall.Box(Place.At(0f, 1f, 0f), new Vector3(3f, 2f, 0.2f), PaletteSwatch.Enamel);
            var node = new ModelNode("Wall", Vector3.zero, new ModelMesh("Wall", wall));
            Weathering.Weather(node, "Wall", wall, Profile(), null, null);

            LowPolyMeshBuilder rust = node.GetDescendant("Weather_Rust").Mesh.Geometry;
            Assert.Greater(rust.TriangleCount, 0, "rust runs on a big wall");
            Color32 rustColour = Weathering.Rust;
            Assert.IsTrue(rust.Colours.Any(c => Distance(c, rustColour) < 40f), "runs start in bright rust");

            LowPolyMeshBuilder dust = node.GetDescendant("Weather_Dust").Mesh.Geometry;
            Color32 caked = Palette.GetSurface(PaletteSwatch.CakedDust);
            float low = MeanDistance(dust, caked, y => y < 0.2f);
            float high = MeanDistance(dust, caked, y => y > 0.8f && y < 1.2f);
            Assert.Less(low, high, "nearer the ground, nearer the dust's colour");
        }

        [Test]
        public void Weathering_BuildsIdentically()
        {
            MeshChecks.AssertSameModel(BaseModelBuilder.CreateShelf(), BaseModelBuilder.CreateShelf());
            ModelNode first = RoverModelBuilder.CreateModel().GetDescendant("Body");
            ModelNode second = RoverModelBuilder.CreateModel().GetDescendant("Body");
            foreach (string layer in Layers)
            {
                CollectionAssert.AreEqual(first.GetDescendant(layer).Mesh.Geometry.Colours,
                    second.GetDescendant(layer).Mesh.Geometry.Colours, layer);
            }
        }

        private static WeatherProfile Profile()
        {
            return new WeatherProfile(seed: 5, lift: 0.006f, panelWidth: 0.6f, panelHeight: 0.6f, paintWear: 1f,
                mismatched: true, runsPerMetre: 3f, rustHeight: 1f, metalRust: 0.8f, paintRust: 0.4f, tide: 1.2f,
                groundDust: 0.9f, topDust: 0.7f);
        }

        /// <summary>How far in front of the wall's centre a skin's front faces stand (the furthest).</summary>
        private static float Front(ModelNode node, string layer)
        {
            LowPolyMeshBuilder skin = node.GetDescendant(layer).Mesh.Geometry;
            float front = float.MinValue;
            for (int v = 0; v < skin.VertexCount; v++)
            {
                if (skin.Normals[v].z > 0.99f)
                {
                    front = Mathf.Max(front, skin.Positions[v].z);
                }
            }

            return front;
        }

        private static float MeanDistance(LowPolyMeshBuilder skin, Color32 target, System.Func<float, bool> band)
        {
            var distances = new List<float>();
            for (int v = 0; v < skin.VertexCount; v++)
            {
                if (skin.Normals[v].z > 0.99f && band(skin.Positions[v].y))
                {
                    distances.Add(Distance(skin.Colours[v], target));
                }
            }

            Assert.IsNotEmpty(distances, "the dust covers that band");
            return distances.Average();
        }

        private static float Distance(Color32 a, Color32 b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }
    }
}
