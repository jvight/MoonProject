using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using MoonProject.Core;
using MoonProject.Gameplay.Editor;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The opening's item economy (docs/design/opening-v3-purpose.md §4), read from the shipped tuning and structure
    /// assets: every source is fixed and for one structure of the opening's chain; each link's sources cover its
    /// cost (supply >= need) without a glut (<= 1.5x); no source gives a material its link does not spend; and no
    /// small crate is left.
    /// </summary>
    public sealed class OpeningSupplyTests
    {
        private const float MostSupplyPerNeed = 1.5f;

        private static readonly string[] Chain =
        {
            StructureIds.ChargingPad, StructureIds.SolarArray, StructureIds.BatteryBank, StructureIds.AntennaMast,
        };

        private static readonly SalvageMaterial[] Materials =
        {
            SalvageMaterial.Metal, SalvageMaterial.Wiring, SalvageMaterial.Optics,
        };

        private OpeningTuning _tuning;
        private StructureCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _tuning = AssetDatabase.LoadAssetAtPath<OpeningTuning>(GameplayAssetPaths.OpeningTuning);
            _catalog = AssetDatabase.LoadAssetAtPath<StructureCatalog>(GameplayAssetPaths.StructureCatalog);
            Assert.IsNotNull(_tuning, $"{GameplayAssetPaths.OpeningTuning} is missing: run the Gameplay builders");
            Assert.IsNotNull(_catalog, $"{GameplayAssetPaths.StructureCatalog} is missing: run the Gameplay builders");
        }

        [Test]
        public void EachLinksSupply_CoversItsCost_WithoutAGlut()
        {
            Dictionary<string, Recipe> supply = SupplyByLink();
            foreach (string link in Chain)
            {
                Assert.IsTrue(supply.ContainsKey(link), $"{link} has a source in the opening");
                Recipe need = _catalog.Find(link).BuildCost;
                foreach (SalvageMaterial material in Materials)
                {
                    int have = supply[link].Of(material);
                    int needed = need.Of(material);
                    Assert.GreaterOrEqual(have, needed, $"{link}: its sources cover its {material}");
                    Assert.LessOrEqual(have, needed * MostSupplyPerNeed, $"{link}: its {material} is not a glut");
                }
            }
        }

        [Test]
        public void EverySource_IsForOneStructureOfTheChain_AndGivesOnlyWhatThatStructureSpends()
        {
            foreach (SupplyCache source in _tuning.Caches)
            {
                CollectionAssert.Contains(Chain, source.ForStructure, $"{source.Id} is for a structure of the opening");
                Recipe need = _catalog.Find(source.ForStructure).BuildCost;
                Assert.IsFalse(source.Yield.IsFree && source.Parts.Count == 0, $"{source.Id} gives something");
                foreach (SalvageMaterial material in Materials)
                {
                    Assert.IsTrue(source.Yield.Of(material) == 0 || need.Of(material) > 0,
                        $"{source.Id} gives {material}, which {source.ForStructure} does not spend");
                }
            }
        }

        [Test]
        public void TheChargerKit_IsThePadsWholeCost_AndNoSmallCrateIsLeft()
        {
            SupplyCache kit = Find("supply.lander.open");
            Assert.IsTrue(kit.StartsOpen, "the lander's charger kit stands open: the first pickup needs no cutter");
            Assert.AreEqual(StructureIds.ChargingPad, kit.ForStructure);
            Recipe pad = _catalog.Find(StructureIds.ChargingPad).BuildCost;
            Assert.AreEqual(pad.Metal, kit.Yield.Metal, "with an empty start hold the kit alone covers the pad");
            Assert.AreEqual(pad.Wiring, kit.Yield.Wiring);
            Assert.AreEqual(pad.Optics, kit.Yield.Optics);
            foreach (SupplyCache source in _tuning.Caches)
            {
                Assert.AreNotEqual("Crate_S", source.Container, $"{source.Id}: the opening has no small crates");
            }
        }

        [Test]
        public void TheSpareIgniter_LiesOnlyInTheSparesRack_OpenByTheFallenArray()
        {
            const string igniter = "part.igniter";
            Assert.AreEqual(igniter, _tuning.IgniterPart);
            SupplyCache rack = Find("supply.array.spares");
            Assert.IsTrue(rack.StartsOpen, "the cutter has no igniter yet: the rack cannot need cutting");
            Assert.AreEqual("FallenSolarArray", rack.AnchorId, "it stands in the array's frame and moves with it");
            Assert.AreEqual(StructureIds.SolarArray, rack.ForStructure, "it opens the array's link");
            CollectionAssert.Contains(rack.Parts, igniter);
            foreach (SupplyCache source in _tuning.Caches)
            {
                if (source.Id != rack.Id)
                {
                    CollectionAssert.DoesNotContain(source.Parts, igniter, $"{source.Id}: one igniter, in the rack");
                }
            }
        }

        private Dictionary<string, Recipe> SupplyByLink()
        {
            var supply = new Dictionary<string, Recipe>();
            foreach (SupplyCache source in _tuning.Caches)
            {
                supply[source.ForStructure] = supply.TryGetValue(source.ForStructure, out Recipe sum)
                    ? sum.Plus(source.Yield)
                    : source.Yield;
            }

            return supply;
        }

        private SupplyCache Find(string id)
        {
            foreach (SupplyCache source in _tuning.Caches)
            {
                if (source.Id == id)
                {
                    return source;
                }
            }

            Assert.Fail($"the opening has a source {id}");
            return default;
        }
    }
}
