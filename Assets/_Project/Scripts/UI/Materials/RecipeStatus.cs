using System;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// What a recipe asks of 07's materials right now (pure, EditMode-tested): which materials it uses (only those are
    /// shown), which of them 07 is short of (those read dimmed), and the one the quiet need line names: the first short
    /// material in Metal, Wiring, Optics order, with how many more units it takes.
    /// </summary>
    internal readonly struct RecipeStatus
    {
        private readonly Recipe _recipe;
        private readonly int _metal;
        private readonly int _wiring;
        private readonly int _optics;

        public RecipeStatus(Recipe recipe, int metal, int wiring, int optics)
        {
            _recipe = recipe;
            _metal = metal;
            _wiring = wiring;
            _optics = optics;
        }

        /// <summary>True when the stock covers every material of the recipe.</summary>
        public bool IsAffordable
        {
            get
            {
                for (int i = 0; i < Materials.Count; i++)
                {
                    if (IsShort(Materials.At(i)))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool Uses(SalvageMaterial material)
        {
            return _recipe.Of(material) > 0;
        }

        public bool IsShort(SalvageMaterial material)
        {
            return Have(material) < _recipe.Of(material);
        }

        /// <summary>
        /// The material the need line names and how many more of it the recipe takes; false when nothing is short.
        /// </summary>
        public bool TryGetNeed(out SalvageMaterial material, out int missing)
        {
            for (int i = 0; i < Materials.Count; i++)
            {
                SalvageMaterial candidate = Materials.At(i);
                if (IsShort(candidate))
                {
                    material = candidate;
                    missing = _recipe.Of(candidate) - Have(candidate);
                    return true;
                }
            }

            material = SalvageMaterial.Metal;
            missing = 0;
            return false;
        }

        private int Have(SalvageMaterial material)
        {
            switch (material)
            {
                case SalvageMaterial.Metal:
                    return _metal;
                case SalvageMaterial.Wiring:
                    return _wiring;
                case SalvageMaterial.Optics:
                    return _optics;
                default:
                    throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown salvage material.");
            }
        }
    }
}
