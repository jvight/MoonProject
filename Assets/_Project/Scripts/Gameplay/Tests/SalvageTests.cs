using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Gameplay.Editor;
using Object = UnityEngine.Object;

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
        public void Yield_GrowsWithThePiece_AndBigPiecesCutInSegments()
        {
            var tuning = Create<SalvageTuning>();
            Assert.AreEqual(2, SalvageYield.Units(PieceSize.Small, 1, tuning));
            Assert.AreEqual(3, SalvageYield.Units(PieceSize.Medium, 1, tuning));
            Assert.AreEqual(5, SalvageYield.Units(PieceSize.Large, 1, tuning));
            Assert.AreEqual(8, SalvageYield.Units(PieceSize.Hull, 1, tuning));
            Assert.AreEqual(6, SalvageYield.Units(PieceSize.Medium, 2, tuning), "the garage's rich seams");

            Assert.AreEqual(1.2f, SalvageYield.CutSeconds(PieceSize.Small, 1f, tuning), 1e-5f, "M3-26 §4.1");
            Assert.AreEqual(2.5f, SalvageYield.CutSeconds(PieceSize.Medium, 1f, tuning), 1e-5f);
            Assert.AreEqual(5f, SalvageYield.CutSeconds(PieceSize.Large, 1f, tuning), 1e-5f, "two segments");
            Assert.AreEqual(10.5f, SalvageYield.CutSeconds(PieceSize.Hull, 1f, tuning), 1e-5f, "three segments");
            Assert.AreEqual(8.1f, SalvageYield.CutSeconds(PieceSize.Hull, 1.3f, tuning), 0.05f, "with the Focus Lens");
            Assert.AreEqual(3, tuning.Size(PieceSize.Hull).Segments);
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
        public void Sites_AndTheirCrates_GiveAboutTwiceWhatThe05PathNeeds_EachLeaningOnItsSignature()
        {
            var tuning = Create<SalvageTuning>();
            var supply = new Recipe(0, 0, 0);
            foreach (string name in SalvageEconomy.SiteNames)
            {
                Recipe site = SiteYield(name, tuning).Plus(CrateOf(name));
                supply = supply.Plus(site);
                TestContext.WriteLine($"{name} (crate in): metal {site.Metal}, wiring {site.Wiring}, " +
                                      $"optics {site.Optics}");
            }

            Recipe needs = PathNeeds();
            TestContext.WriteLine($"All: {supply}; the 0.5 path takes {needs}.");
            Assert.GreaterOrEqual(supply.Total, MostUnderTwice * needs.Total,
                "M3-26 §4.4: about twice (ruling 5), the crew crates included");
            foreach (SalvageMaterial material in Materials)
            {
                Assert.GreaterOrEqual(supply.Of(material), LeastPerMaterial * needs.Of(material),
                    $"no material runs tight: {material}");
                Assert.LessOrEqual(supply.Of(material), MostOverTwice * needs.Of(material),
                    $"about twice, so collecting still matters: not much more {material} than that");
            }

            AssertSignature("depot", SalvageMaterial.Metal, tuning);
            AssertSignature("kestrel", SalvageMaterial.Optics, tuning);
            AssertSignature("drill", SalvageMaterial.Metal, tuning);
            AssertSignature("garage", SalvageMaterial.Wiring, tuning);
            AssertSignature("lander", SalvageMaterial.Metal, tuning);
        }

        [Test]
        public void OneSite_FitsOneTrip_InTheHoldOfItsStage()
        {
            var tuning = Create<SalvageTuning>();
            var cargo = Create<CargoTuning>();
            foreach (string name in SalvageEconomy.SiteNames)
            {
                Recipe site = SiteYield(name, tuning).Plus(CrateOf(name));
                int slots = 0;
                foreach (SalvageMaterial material in Materials)
                {
                    slots += (site.Of(material) + SalvageEconomy.SalvageStack - 1) / SalvageEconomy.SalvageStack;
                }

                bool early = name == "depot" || name == "garage";
                int hold = cargo.HoldSlots + (early ? 0 : cargo.CradleSlots);
                TestContext.WriteLine($"{name}: {slots} slots of {hold}");
                Assert.LessOrEqual(slots, hold, $"M3-25 §4: {name} and its crate fit one trip");
            }
        }

        [Test]
        public void TheCharge_BuildsTheStagesThePieceNeeds_AsTheSpecsTableSays()
        {
            var tuning = Create<SalvageTuning>();
            var cutter = Create<CutterTuning>();
            // 0.5.5 §5.1: the build (brace + stages) per piece and tier, fresh / warm.
            AssertBuild(cutter, tuning, 0, PieceSize.Small, 0.95f, 0.27f);
            AssertBuild(cutter, tuning, 0, PieceSize.Medium, 1.40f, 0.54f);
            AssertBuild(cutter, tuning, 0, PieceSize.Large, 1.85f, 0.81f);
            AssertBuild(cutter, tuning, 0, PieceSize.Hull, 1.85f, 0.81f);
            AssertBuild(cutter, tuning, 1, PieceSize.Small, 0.86f, 0.22f);
            AssertBuild(cutter, tuning, 1, PieceSize.Large, 1.58f, 0.65f);
            Assert.AreEqual(0.12f, cutter.BreathSeconds, 1e-5f, "the held breath before ignition");
            Assert.AreEqual(6f, cutter.WarmWindowSeconds, 1e-5f);

            CutterTier bare = cutter.Tier(0);
            CutterTier lens = cutter.Tier(1);
            Assert.Greater(lens.SpeedFactor, bare.SpeedFactor, "the Focus Lens cuts faster");
            Assert.Less(lens.DrainFactor, bare.DrainFactor, "and cheaper");
        }

        private static void AssertBuild(CutterTuning cutter, SalvageTuning tuning, int tier, PieceSize size,
            float fresh, float warm)
        {
            float stage = cutter.Tier(tier).StageSeconds;
            int stages = tuning.Size(size).Stages;
            Assert.AreEqual(fresh, cutter.BraceSeconds + stages * stage, 0.006f, $"tier {tier} {size} fresh");
            Assert.AreEqual(warm, stages * stage * cutter.WarmFactor, 0.006f, $"tier {tier} {size} warm");
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
            Recipe stock = CutYield("depot", tuning).Plus(CrateOf("depot"));
            stock = Spend(stock, SalvageEconomy.RadioTower[0], "tower L1 from the depot");
            stock = stock.Plus(CutYield("garage", tuning)).Plus(CrateOf("garage"));
            stock = Spend(stock, SalvageEconomy.RadioTower[1], "tower L2 after the garage");
            stock = Spend(stock, SalvageEconomy.HoverJump, "Hover-Jump");
            stock = Spend(stock, SalvageEconomy.CargoCradle, "the Cargo Cradle");
            stock = Spend(stock, relays.CostAfter(0), "the first relay mast (step 7)");
            stock = stock.Plus(CutYield("kestrel", tuning)).Plus(CrateOf("kestrel"));
            Spend(stock, SalvageEconomy.RadioTower[2], "tower L3 after the Kestrel (step 8)");
        }

        /// <summary>About twice (ruling 5, P9): a material's yield stays under this many times its need.</summary>
        private const float MostOverTwice = 2.5f;

        /// <summary>About twice (M3-26 §4.4: ~1.9x with the crates): the whole supply over the path's needs.</summary>
        private const float MostUnderTwice = 1.7f;

        /// <summary>No single material runs tight: each gives at least this many times its need.</summary>
        private const float LeastPerMaterial = 1.6f;

        private static readonly SalvageMaterial[] Materials =
        {
            SalvageMaterial.Metal, SalvageMaterial.Wiring, SalvageMaterial.Optics,
        };

        /// <summary>
        /// What the 0.5 path takes (M3-25 §4, M3-26 §4.4): the tower's three levels, every kit the bay fits and every
        /// relay mast. The shelves are left out: home only fills for a player who hoards, and a shelf is the answer.
        /// </summary>
        private Recipe PathNeeds()
        {
            Recipe needs = SalvageEconomy.HoverJump.Plus(SalvageEconomy.CargoCradle).Plus(SalvageEconomy.WarmHeadlamp)
                .Plus(SalvageEconomy.BoostCoils).Plus(SalvageEconomy.CellPack).Plus(SalvageEconomy.FocusLens)
                .Plus(SalvageEconomy.SideBins).Plus(Create<RelayTuning>().TotalCost(RelayMasts));
            foreach (Recipe level in SalvageEconomy.RadioTower)
            {
                needs = needs.Plus(level);
            }

            return needs;
        }

        /// <summary>What site <paramref name="name"/>'s crew supply crate holds (M3-25 T4).</summary>
        private static Recipe CrateOf(string name)
        {
            SupplyCrate crate = SalvageEconomy.Crate(name);
            var units = new int[3];
            units[(int)crate.Material] = crate.Units;
            return new Recipe(units[0], units[1], units[2]);
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

        /// <summary>A site, its crate counted, gives no more of anything than of its signature material.</summary>
        private static void AssertSignature(string name, SalvageMaterial signature, SalvageTuning tuning)
        {
            Recipe site = SiteYield(name, tuning).Plus(CrateOf(name));
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
            PieceSizeEntry[] sizes = SalvageSizes.Deal(prefab, SalvageEconomy.PieceClasses(name));
            int pieces = 0;
            foreach (Transform node in prefab.transform)
            {
                if (!SalvagePieceName.TryParsePiece(node.name, out int number, out SalvageMaterial material,
                        out bool drag))
                {
                    continue;
                }

                Assert.IsNotNull(node.Find("CutPoint"), $"{name}/{node.name} has its cut point");
                PieceSize size = Array.Find(sizes, entry => entry.Number == number).Size;
                yield[(int)material] += drag && !drags
                    ? 0
                    : SalvageYield.Units(size, SalvageEconomy.YieldScale(name), tuning);
                pieces++;
            }

            Assert.That(pieces, Is.InRange(3, 8), $"{name}: 3 to 8 salvage points (the garage's three rich seams)");
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
