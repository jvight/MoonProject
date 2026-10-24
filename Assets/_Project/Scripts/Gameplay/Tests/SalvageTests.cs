using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// Salvage's pure rules (docs/features/M3-13): the Art site contract's node names, what a piece yields and how long
    /// it takes to cut, the save data, and the charge each piece builds.
    /// </summary>
    public sealed class SalvageTests
    {
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
                        id = "site.kestrel", taken = new[] { 0, 3 },
                        pieces = new[]
                        {
                            new SalvagePieceSaveData { number = 1, progress = 0.4f },
                            new SalvagePieceSaveData { number = 5, progress = 1f, held = true },
                        },
                    },
                },
            };

            SalvageSaveData copy = JsonUtility.FromJson<SalvageSaveData>(JsonUtility.ToJson(data));
            Assert.AreEqual("site.kestrel", copy.sites[0].id);
            CollectionAssert.AreEqual(new[] { 0, 3 }, copy.sites[0].taken);
            Assert.AreEqual(0.4f, copy.sites[0].pieces[0].progress, 1e-6f);
            Assert.IsTrue(copy.sites[0].pieces[1].held);
        }

        [Test]
        public void TheCharge_BuildsTheStagesThePieceNeeds_AsTheSpecsTableSays()
        {
            var tuning = Create<SalvageTuning>();
            var cutter = Create<CutterTuning>();
            // 0.5.5 §5.1 with weight: the build (brace + stages, each lock held) per piece and tier, fresh / warm.
            AssertBuild(cutter, tuning, 0, PieceSize.Small, 0.85f, 0.21f);
            AssertBuild(cutter, tuning, 0, PieceSize.Medium, 1.35f, 0.51f);
            AssertBuild(cutter, tuning, 0, PieceSize.Large, 1.85f, 0.81f);
            AssertBuild(cutter, tuning, 0, PieceSize.Hull, 1.85f, 0.81f);
            AssertBuild(cutter, tuning, 1, PieceSize.Small, 0.78f, 0.17f);
            AssertBuild(cutter, tuning, 1, PieceSize.Large, 1.64f, 0.68f);
            Assert.AreEqual(0.15f, cutter.LockHoldSeconds, 1e-5f, "each lock is felt");
            Assert.AreEqual(0.35f, cutter.BreathSeconds, 1e-5f, "the held breath before ignition: a beat");
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
            float holds = (stages - 1) * cutter.LockHoldSeconds;
            Assert.AreEqual(fresh, cutter.BraceSeconds + stages * stage + holds, 0.006f, $"tier {tier} {size} fresh");
            Assert.AreEqual(warm, (stages * stage + holds) * cutter.WarmFactor, 0.006f, $"tier {tier} {size} warm");
        }

        private T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
        }
    }
}
