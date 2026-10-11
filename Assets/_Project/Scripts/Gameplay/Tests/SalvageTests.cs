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
            };

            SalvageSaveData copy = JsonUtility.FromJson<SalvageSaveData>(JsonUtility.ToJson(data));
            Assert.AreEqual("site.depot", copy.sites[0].id);
            Assert.IsTrue(copy.sites[0].discovered);
            CollectionAssert.AreEqual(new[] { 0, 3 }, copy.sites[0].taken);
            Assert.AreEqual(0.4f, copy.sites[0].pieces[0].progress, 1e-6f);
            Assert.IsTrue(copy.sites[0].pieces[1].loose);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), copy.sites[0].pieces[1].position);
        }

        [Test]
        public void Sites_GiveAboutTwiceWhatStepsZeroToEightNeed_EachLeaningOnItsSignature()
        {
            var tuning = Create<SalvageTuning>();
            var yield = new Recipe(0, 0, 0);
            foreach (string name in SalvageEconomy.SiteNames)
            {
                Recipe site = SiteYield(name, tuning);
                yield = yield.Plus(site);
                TestContext.WriteLine($"{name}: metal {site.Metal}, wiring {site.Wiring}, optics {site.Optics}");
            }

            Recipe needs = StepsZeroToEight();
            TestContext.WriteLine($"All: {yield}; steps 0-8 take {needs}.");
            foreach (SalvageMaterial material in Materials)
            {
                Assert.GreaterOrEqual(yield.Of(material), 2 * needs.Of(material),
                    $"VISION ruling 5 (M3-16 P9): the moon gives at least twice the {material} steps 0-8 take");
                Assert.LessOrEqual(yield.Of(material), MostOverTwice * needs.Of(material),
                    $"P9: about twice, so collecting still matters: not much more {material} than that");
            }

            AssertSignature("depot", SalvageMaterial.Metal, tuning);
            AssertSignature("kestrel", SalvageMaterial.Optics, tuning);
            AssertSignature("drill", SalvageMaterial.Metal, tuning);
            AssertSignature("garage", SalvageMaterial.Wiring, tuning);
            AssertSignature("lander", SalvageMaterial.Metal, tuning);
        }

        [Test]
        public void Depot_HasFourCuts_AndPaysTheTowersFirstLevel_WithALittleOver()
        {
            var tuning = Create<SalvageTuning>();
            Recipe depot = SiteYield("depot", tuning);
            Assert.AreEqual(4, Pieces("depot"), "P10: four good cuts, the last one reveals the heart");
            Assert.IsTrue(Covers(depot, SalvageEconomy.RadioTower[0]), "the depot alone wakes home");
            Assert.LessOrEqual(depot.Total, 2 * (SalvageEconomy.RadioTower[0].Total + SalvageEconomy.HoverJump.Total),
                "but no more than a little over: the moon is out there");
        }

        [Test]
        public void TheGoldenPathsOrder_IsAffordableStepByStep_FromWhatTheBeamCuts()
        {
            var tuning = Create<SalvageTuning>();
            var relays = Create<RelayTuning>();
            Recipe stock = CutYield("depot", tuning);
            stock = Spend(stock, SalvageEconomy.RadioTower[0], "tower L1 from the depot");
            stock = stock.Plus(CutYield("garage", tuning));
            stock = Spend(stock, SalvageEconomy.RadioTower[1], "tower L2 after the garage");
            stock = Spend(stock, SalvageEconomy.HoverJump, "Hover-Jump");
            stock = Spend(stock, SalvageEconomy.CargoCradle, "the Cargo Cradle");
            stock = Spend(stock, relays.CostAfter(0), "the first relay mast (step 7)");
            stock = stock.Plus(CutYield("kestrel", tuning));
            Spend(stock, SalvageEconomy.RadioTower[2], "tower L3 after the Kestrel (step 8)");
        }

        /// <summary>About twice (ruling 5, P9): a material's yield stays under this many times its need.</summary>
        private const float MostOverTwice = 2.5f;

        private static readonly SalvageMaterial[] Materials =
        {
            SalvageMaterial.Metal, SalvageMaterial.Wiring, SalvageMaterial.Optics,
        };

        /// <summary>
        /// What steps 0-8 of the wake-up take (M3-16): the tower's three levels, Hover-Jump, the Cargo Cradle and the
        /// first relay mast.
        /// </summary>
        private Recipe StepsZeroToEight()
        {
            Recipe needs = SalvageEconomy.HoverJump.Plus(SalvageEconomy.CargoCradle)
                .Plus(Create<RelayTuning>().CostAfter(0));
            foreach (Recipe level in SalvageEconomy.RadioTower)
            {
                needs = needs.Plus(level);
            }

            return needs;
        }

        private static Recipe Spend(Recipe stock, Recipe cost, string what)
        {
            Assert.IsTrue(Covers(stock, cost), $"{what}: {cost} from a stock of {stock}");
            return new Recipe(stock.Metal - cost.Metal, stock.Wiring - cost.Wiring, stock.Optics - cost.Optics);
        }

        private static bool Covers(Recipe stock, Recipe cost)
        {
            return stock.Metal >= cost.Metal && stock.Wiring >= cost.Wiring && stock.Optics >= cost.Optics;
        }

        private static void AssertSignature(string name, SalvageMaterial signature, SalvageTuning tuning)
        {
            Recipe site = SiteYield(name, tuning);
            foreach (SalvageMaterial other in Materials)
            {
                if (other != signature)
                {
                    Assert.GreaterOrEqual(site.Of(signature), site.Of(other),
                        $"{name} leans on its signature {signature}: nothing gives more");
                }
            }
        }

        private static int Pieces(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayAssetPaths.SitePrefab(name));
            int pieces = 0;
            foreach (Transform node in prefab.transform)
            {
                pieces += SalvagePieceName.TryParsePiece(node.name, out _, out _, out _) ? 1 : 0;
            }

            return pieces;
        }

        /// <summary>What Art's site prefab <paramref name="name"/> gives (its contract checked).</summary>
        private static Recipe SiteYield(string name, SalvageTuning tuning)
        {
            return SiteYield(name, tuning, true);
        }

        /// <summary>What the site gives 07's beam alone, without the drag pieces only the tether pulls clear.</summary>
        private static Recipe CutYield(string name, SalvageTuning tuning)
        {
            return SiteYield(name, tuning, false);
        }

        private static Recipe SiteYield(string name, SalvageTuning tuning, bool drags)
        {
            var yield = new int[3];
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
                Vector3 size = Vector3.Scale(mesh.bounds.size, node.localScale);
                yield[(int)material] += drag && !drags ? 0 : SalvageYield.Units(size, drag, tuning);
                pieces++;
            }

            Assert.That(pieces, Is.InRange(4, 8), $"{name}: 4 to 8 salvage points");
            return new Recipe(yield[0], yield[1], yield[2]);
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
