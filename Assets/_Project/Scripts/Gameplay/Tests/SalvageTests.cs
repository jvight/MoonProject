using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Gameplay.Editor;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// Salvage's pure rules (docs/features/M3-13): the Art site contract's node names, what a piece yields and how long
    /// it takes to cut, the save data, and VISION ruling 5 over the real Art sites: they yield at least twice every
    /// recipe, per material and in all.
    /// </summary>
    public sealed class SalvageTests
    {
        /// <summary>relay.0 to relay.3, the World's relay anchors (docs/features/M3-06).</summary>
        private const int RelayMasts = 4;

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void PieceNames_FollowTheSiteContract()
        {
            Assert.IsTrue(SalvagePieceName.TryParsePiece("Salvage_0_Optics_Drag", out int number,
                out SalvageMaterial material, out bool drag));
            Assert.AreEqual(0, number);
            Assert.AreEqual(SalvageMaterial.Optics, material);
            Assert.IsTrue(drag);
            Assert.IsTrue(SalvagePieceName.TryParsePiece("Salvage_6_Wiring", out number, out material, out drag));
            Assert.AreEqual(6, number);
            Assert.AreEqual(SalvageMaterial.Wiring, material);
            Assert.IsFalse(drag);

            foreach (string name in new[]
                     {
                         "Skeleton", "Heart", "Salvage_x_Metal", "Salvage_1_Gold", "Salvage_1_Metal_Big",
                         "Salvage_1", "Salvage_-1_Metal", "Debris_Metal_0",
                     })
            {
                Assert.IsFalse(SalvagePieceName.TryParsePiece(name, out _, out _, out _), name);
            }

            Assert.IsTrue(SalvagePieceName.TryParseDebris("Debris_Optics_1", out material));
            Assert.AreEqual(SalvageMaterial.Optics, material);
            Assert.IsFalse(SalvagePieceName.TryParseDebris("Debris_Optics", out _));
            Assert.IsFalse(SalvagePieceName.TryParseDebris("Salvage_0_Metal", out _));
        }

        [Test]
        public void Yield_GrowsWithThePiece_AndEveryCutIsAShortCalmBeat()
        {
            var tuning = Create<SalvageTuning>();
            Assert.AreEqual(tuning.SmallYield, SalvageYield.Units(new Vector3(0.9f, 0.9f, 0.9f), false, tuning));
            Assert.AreEqual(tuning.MediumYield, SalvageYield.Units(new Vector3(1.5f, 1f, 1.5f), false, tuning));
            Assert.AreEqual(tuning.LargeYield, SalvageYield.Units(new Vector3(2f, 2f, 2f), false, tuning));
            Assert.AreEqual(tuning.DragYield, SalvageYield.Units(new Vector3(0.5f, 0.5f, 0.5f), true, tuning),
                "a drag piece is worth its fixed amount, whatever its size");

            Vector2 range = tuning.CutSeconds;
            Assert.AreEqual(range.x, SalvageYield.CutSeconds(1, tuning), 1e-5f, "never shorter than the range");
            Assert.AreEqual(3f, SalvageYield.CutSeconds(3, tuning), 1e-5f, "about a second per unit");
            Assert.AreEqual(range.y, SalvageYield.CutSeconds(9, tuning), 1e-5f, "never longer than the range");
            Assert.AreEqual(2f, range.x, 1e-5f, "the brief's 2-4 s");
            Assert.AreEqual(4f, range.y, 1e-5f);
        }

        [Test]
        public void SaveData_RoundTripsThroughJson()
        {
            var data = new SalvageSaveData
            {
                sites = new[]
                {
                    new SalvageSiteSaveData
                    {
                        id = "site.depot", discovered = true, taken = new[] { 0, 3 },
                        pieces = new[]
                        {
                            new SalvagePieceSaveData { number = 1, progress = 0.4f },
                            new SalvagePieceSaveData
                            {
                                number = 5, progress = 0.2f, loose = true, position = new Vector3(1f, 2f, 3f),
                                rotation = Quaternion.Euler(0f, 40f, 0f),
                            },
                        },
                    },
                },
                trail = new[] { 2 },
            };

            SalvageSaveData copy = JsonUtility.FromJson<SalvageSaveData>(JsonUtility.ToJson(data));
            Assert.AreEqual("site.depot", copy.sites[0].id);
            Assert.IsTrue(copy.sites[0].discovered);
            CollectionAssert.AreEqual(new[] { 0, 3 }, copy.sites[0].taken);
            Assert.AreEqual(0.4f, copy.sites[0].pieces[0].progress, 1e-6f);
            Assert.IsTrue(copy.sites[0].pieces[1].loose);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), copy.sites[0].pieces[1].position);
            CollectionAssert.AreEqual(new[] { 2 }, copy.trail);
        }

        [Test]
        public void Sites_FollowTheContract_AndYieldAtLeastTwiceEveryRecipe()
        {
            var tuning = Create<SalvageTuning>();
            var yield = new int[3];
            foreach (string name in SalvageEconomy.SiteNames)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayAssetPaths.SitePrefab(name));
                Assert.IsNotNull(prefab, $"Art's site prefab for '{name}'");
                Assert.IsNotNull(prefab.transform.Find("Skeleton"), $"{name} has its skeleton");
                Assert.IsNotNull(prefab.transform.Find("Heart"), $"{name} has its heart");
                int pieces = 0;
                foreach (Transform node in prefab.transform)
                {
                    if (!SalvagePieceName.TryParsePiece(node.name, out _, out SalvageMaterial material, out bool drag))
                    {
                        continue;
                    }

                    Assert.IsNotNull(node.Find("CutPoint"), $"{name}/{node.name} has its cut point");
                    Mesh mesh = node.GetComponent<MeshFilter>().sharedMesh;
                    yield[(int)material] += SalvageYield.Units(Vector3.Scale(mesh.bounds.size, node.localScale), drag,
                        tuning);
                    pieces++;
                }

                Assert.That(pieces, Is.InRange(4, 8), $"{name}: 4 to 8 salvage points");
            }

            foreach (string bit in SalvageEconomy.TrailBits)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(GameplayAssetPaths.DebrisPrefab(bit)), bit);
                Assert.IsTrue(SalvagePieceName.TryParseDebris(bit, out SalvageMaterial material), bit);
                yield[(int)material] += tuning.TrailYield;
            }

            var relays = Create<RelayTuning>();
            Recipe costs = SalvageEconomy.HoverJump.Plus(relays.TotalCost(RelayMasts))
                .Plus(SalvageEconomy.CargoCradle).Plus(SalvageEconomy.WarmHeadlamp).Plus(SalvageEconomy.BoostCoils);
            foreach (Recipe level in SalvageEconomy.RadioTower)
            {
                costs = costs.Plus(level);
            }

            TestContext.WriteLine($"Yield metal {yield[0]}, wiring {yield[1]}, optics {yield[2]}; recipes take " +
                                  $"metal {costs.Metal}, wiring {costs.Wiring}, optics {costs.Optics}.");
            foreach (SalvageMaterial material in new[]
                     {
                         SalvageMaterial.Metal, SalvageMaterial.Wiring, SalvageMaterial.Optics,
                     })
            {
                Assert.GreaterOrEqual(yield[(int)material], 2 * costs.Of(material),
                    $"VISION ruling 5: the sites give at least twice every recipe's {material}");
            }

            Assert.GreaterOrEqual(yield[0] + yield[1] + yield[2], 2 * costs.Total, "and at least twice in all");
        }

        [Test]
        public void BenchKit_LeansOnItsOwnMaterial()
        {
            AssertLeansOn(SalvageEconomy.CargoCradle, SalvageMaterial.Metal, "the Cargo Cradle is a metal rack");
            AssertLeansOn(SalvageEconomy.WarmHeadlamp, SalvageMaterial.Optics, "the Warm Headlamp is lenses and lamps");
            AssertLeansOn(SalvageEconomy.BoostCoils, SalvageMaterial.Wiring, "the Boost Coils are wound wiring");
        }

        private static void AssertLeansOn(Recipe recipe, SalvageMaterial material, string what)
        {
            Assert.IsFalse(recipe.IsFree, what);
            foreach (SalvageMaterial other in new[]
                     {
                         SalvageMaterial.Metal, SalvageMaterial.Wiring, SalvageMaterial.Optics,
                     })
            {
                if (other != material)
                {
                    Assert.Greater(recipe.Of(material), recipe.Of(other), what);
                }
            }
        }

        private T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
        }
    }
}
