using System;
using System.Collections.Generic;
using NUnit.Framework;
using MoonProject.Gameplay;

namespace MoonProject.UI.Tests
{
    /// <summary>Every upgrade station dresses the panel its own way: a name key and a look of its own.</summary>
    public sealed class UpgradeStationTests
    {
        [Test]
        public void EveryStation_HasItsOwnNameKeyAndLook()
        {
            var keys = new HashSet<string>();
            var looks = new HashSet<string>();
            foreach (UpgradeStationKind station in Enum.GetValues(typeof(UpgradeStationKind)))
            {
                string key = UiKeys.StationName(station);
                StringAssert.StartsWith("ui.station.", key);
                Assert.IsTrue(keys.Add(key), $"{station} shares its name key");
                string look = TowerPanel.StationClass(station);
                StringAssert.StartsWith("tower-panel--", look);
                Assert.IsTrue(looks.Add(look), $"{station} shares its look");
            }
        }
    }
}
