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
                UiKeys.StructureName("charging_pad"), "Charging Pad", "Bệ sạc",
                UiKeys.ScanFragment, "Fragment {0} of {1} · {2}", "Mảnh {0}/{1} · {2}",
                UiKeys.ScanKnown, "Known blueprint · +{0} Metal", "Bản vẽ đã biết · +{0} Kim loại");
        }

        [Test]
        public void ANewFragment_SaysItsPlaceInTheBlueprint()
        {
            var scanned = new FragmentScanned("fragment.pad.1", "charging_pad", 1, 3, false, 0);
            Assert.AreEqual("Fragment 2 of 3 · Charging Pad", ScanLines.Fragment(_localization, scanned));
        }

        [Test]
        public void AFragmentOfAKnownBlueprint_SaysTheMetalItPaid()
        {
            var scanned = new FragmentScanned("fragment.pad.1", "charging_pad", 0, 3, true, 3);
            Assert.AreEqual("Known blueprint · +3 Metal", ScanLines.Fragment(_localization, scanned));
        }
    }
}
