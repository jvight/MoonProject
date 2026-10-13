using NUnit.Framework;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI.Tests
{
    /// <summary>The scan's pickup note says which fragment of how many, or what a known blueprint paid.</summary>
    public sealed class ScanLinesTests
    {
        private ILocalization _localization;

        [SetUp]
        public void SetUp()
        {
            _localization = TestTables.Localization(new EventBus(),
                UiKeys.UpgradeName("rover.cargo_cradle"), "Cargo Cradle", "Giá chở hàng",
                UiKeys.ScanFragment, "Fragment {0} of {1} · {2}", "Mảnh {0}/{1} · {2}",
                UiKeys.ScanKnown, "Known blueprint · +{0} Metal", "Bản vẽ đã biết · +{0} Kim loại");
        }

        [Test]
        public void ANewFragment_SaysItsPlaceInTheBlueprint()
        {
            var scanned = new FragmentScanned("fragment.cradle.1", "rover.cargo_cradle", 1, 3, false, 0);
            Assert.AreEqual("Fragment 2 of 3 · Cargo Cradle", ScanLines.Fragment(_localization, scanned));
        }

        [Test]
        public void AFragmentOfAKnownBlueprint_SaysTheMetalItPaid()
        {
            var scanned = new FragmentScanned("fragment.cradle.1", "rover.cargo_cradle", 0, 3, true, 3);
            Assert.AreEqual("Known blueprint · +3 Metal", ScanLines.Fragment(_localization, scanned));
        }
    }
}
