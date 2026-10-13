using MoonProject.Core;

namespace MoonProject.UI.Tests
{
    /// <summary>The three salvage materials at once, so a tally test reads as one stock.</summary>
    internal static class TallyTestExtensions
    {
        public static void Snap(this MaterialTally tally, int metal, int wiring, int optics)
        {
            tally.Snap(SalvageMaterial.Metal, metal);
            tally.Snap(SalvageMaterial.Wiring, wiring);
            tally.Snap(SalvageMaterial.Optics, optics);
        }

        public static bool Apply(this MaterialTally tally, int metal, int wiring, int optics)
        {
            bool metalChanged = tally.Apply(SalvageMaterial.Metal, metal);
            bool wiringChanged = tally.Apply(SalvageMaterial.Wiring, wiring);
            bool opticsChanged = tally.Apply(SalvageMaterial.Optics, optics);
            return metalChanged || wiringChanged || opticsChanged;
        }
    }
}
