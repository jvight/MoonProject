using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>The build menu says a weld's cost to 07's battery in whole percent (docs/design/opening-v2.md section 10).</summary>
    public sealed class WeldEnergyShareTests
    {
        [Test]
        public void TheShare_ReadsAsAWholePercent_AndNeverAsFree()
        {
            Assert.AreEqual(5, WeldEnergyShare.Percent(0.05f), "the pad");
            Assert.AreEqual(8, WeldEnergyShare.Percent(0.075f), "the mast");
            Assert.AreEqual(1, WeldEnergyShare.Percent(0.004f), "a weld that costs anything reads at least 1%");
            Assert.AreEqual(0, WeldEnergyShare.Percent(0f), "and one that costs nothing reads 0%");
            Assert.AreEqual(100, WeldEnergyShare.Percent(3f), "it never reads above a full battery");
        }
    }
}
