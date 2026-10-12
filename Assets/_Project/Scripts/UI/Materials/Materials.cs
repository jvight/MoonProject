using System;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The materials as the UI walks them: in enum order, so Metal, Wiring and Optics come first (the order every
    /// row, recipe and summary shows them in). Allocation-free.
    /// </summary>
    internal static class Materials
    {
        /// <summary>How many materials there are.</summary>
        public const int Count = SalvageMaterials.Count;

        /// <summary>
        /// How many materials a <c>Recipe</c> can name today (the first three): recipe rows walk these.
        /// </summary>
        public const int RecipeCount = 3;

        private const string KeyPrefix = "material.";

        /// <summary>The material in slot <paramref name="index"/>.</summary>
        public static SalvageMaterial At(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "There is no such material.");
            }

            return (SalvageMaterial)index;
        }

        /// <summary>
        /// The USS modifier naming <paramref name="material"/> ("metal", "circuit_chips"): its name key without the
        /// prefix. Allocates; used when an element is built or changes material.
        /// </summary>
        public static string ClassSuffix(SalvageMaterial material)
        {
            return UiKeys.MaterialName(material).Substring(KeyPrefix.Length);
        }
    }
}
