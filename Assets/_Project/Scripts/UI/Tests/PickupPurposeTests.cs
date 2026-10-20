using NUnit.Framework;
using UnityEngine;
using MoonProject.Gameplay;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Every structure of the opening's chain (each source's link, OpeningSupplyTests) has its pickup purpose in every
    /// language, a plain line that fits after the row's count ("for the charging pad", opening-v3 §4 E3).
    /// </summary>
    public sealed class PickupPurposeTests
    {
        private const int MostPurposeChars = 26;

        private static readonly string[] Chain =
        {
            StructureIds.ChargingPad, StructureIds.SolarArray, StructureIds.BatteryBank, StructureIds.AntennaMast,
        };

        [Test]
        public void EveryLinkOfTheOpening_HasAPurposeLine_InEveryLanguage()
        {
            foreach (TextAsset asset in StringTableAssets.Load())
            {
                StringTable table = StringTable.Parse(asset.text, asset.name);
                foreach (string structureId in Chain)
                {
                    string key = UiKeys.PickupPurpose(structureId);
                    Assert.IsTrue(table.TryGet(key, out string words), $"{table.Language}: {key}");
                    Assert.LessOrEqual(words.Length, MostPurposeChars, $"{table.Language}: {key} stays one short line");
                }
            }
        }
    }
}
