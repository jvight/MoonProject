using NUnit.Framework;
using UnityEngine;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The Variant C CRT HUD's rules: the home meter's compass point.
    /// </summary>
    public sealed class GlassHudTests
    {
        [Test]
        public void TheCompassPoint_IsNorthAlongPlusZ_AndTurnsClockwise()
        {
            Assert.AreEqual(0, CompassPoint.Of(Vector3.zero, new Vector3(0f, 0f, 100f)), "north");
            Assert.AreEqual(1, CompassPoint.Of(Vector3.zero, new Vector3(70f, 5f, 70f)), "north-east; height ignored");
            Assert.AreEqual(2, CompassPoint.Of(Vector3.zero, new Vector3(100f, 0f, 0f)), "east");
            Assert.AreEqual(4, CompassPoint.Of(Vector3.zero, new Vector3(0f, 0f, -100f)), "south");
            Assert.AreEqual(6, CompassPoint.Of(Vector3.zero, new Vector3(-100f, 0f, 0f)), "west");
            Assert.AreEqual(0, CompassPoint.Of(Vector3.zero, new Vector3(-5f, 0f, 100f)), "just west of north is north");
            Assert.AreEqual("ui.compass.ne", CompassPoint.Key(1));
            Assert.AreEqual("ui.compass.n", CompassPoint.Key(0));
        }
    }
}
