using System;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The three salvage materials as the UI walks them: always Metal, Wiring, Optics, the order every chip, recipe and
    /// summary shows them in. Allocation-free.
    /// </summary>
    internal static class Materials
    {
        /// <summary>How many materials there are (one slot each).</summary>
        public const int Count = 3;

        /// <summary>The material in slot <paramref name="index"/>.</summary>
        public static SalvageMaterial At(int index)
        {
            switch (index)
            {
                case 0:
                    return SalvageMaterial.Metal;
                case 1:
                    return SalvageMaterial.Wiring;
                case 2:
                    return SalvageMaterial.Optics;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, "There are three materials.");
            }
        }

        /// <summary>How much of <paramref name="material"/> the stock holds.</summary>
        public static int Of(IMaterialStock stock, SalvageMaterial material)
        {
            switch (material)
            {
                case SalvageMaterial.Metal:
                    return stock.Metal;
                case SalvageMaterial.Wiring:
                    return stock.Wiring;
                case SalvageMaterial.Optics:
                    return stock.Optics;
                default:
                    throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown salvage material.");
            }
        }

        /// <summary>The USS modifier naming <paramref name="material"/> ("metal", "wiring", "optics").</summary>
        public static string ClassSuffix(SalvageMaterial material)
        {
            switch (material)
            {
                case SalvageMaterial.Metal:
                    return "metal";
                case SalvageMaterial.Wiring:
                    return "wiring";
                case SalvageMaterial.Optics:
                    return "optics";
                default:
                    throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown salvage material.");
            }
        }
    }
}
