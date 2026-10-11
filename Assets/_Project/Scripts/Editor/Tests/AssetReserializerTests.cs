using System;
using NUnit.Framework;
using MoonProject.Editor.Automation;

namespace MoonProject.Editor.Tests
{
    public sealed class AssetReserializerTests
    {
        private static readonly string[] Paths =
        {
            "Assets/_Project/Data/Tuning/RoverCameraTuning.asset",
            "Assets/_Project/Data/Tuning/Gameplay/BatteryTuning.asset",
            "Assets/_Project/Scripts/Rover/RoverKit.cs",
            "Assets/Elsewhere/Tuning/ForeignTuning.asset",
            "Packages/com.unity.render-pipelines.universal/Runtime/Tuning.asset",
        };

        [Test]
        public void Select_KeepsMatchesUnderTheProjectFolderSorted()
        {
            var selected = AssetReserializer.Select(Paths, @"tuning/.*\.asset$");

            CollectionAssert.AreEqual(new[]
            {
                "Assets/_Project/Data/Tuning/Gameplay/BatteryTuning.asset",
                "Assets/_Project/Data/Tuning/RoverCameraTuning.asset",
            }, selected);
        }

        [Test]
        public void Select_NoMatchIsAnEmptyList()
        {
            Assert.AreEqual(0, AssetReserializer.Select(Paths, "^nothing$").Count);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void Select_RefusesAnEmptyFilter(string pattern)
        {
            Assert.Throws<ArgumentException>(() => AssetReserializer.Select(Paths, pattern));
        }

        [Test]
        public void Select_RefusesNoPaths()
        {
            Assert.Throws<ArgumentNullException>(() => AssetReserializer.Select(null, "x"));
        }
    }
}
