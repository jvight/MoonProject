using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's salvaged materials (docs/features/M3-13), registered in the GameContext for the UI's materials chip and
    /// recipe panel. Read-only and allocation-free; every change is also published as <c>MaterialsChanged</c>.
    /// </summary>
    public interface IMaterialStock
    {
        int Metal { get; }

        int Wiring { get; }

        int Optics { get; }

        /// <summary>Units of every material together.</summary>
        int Total { get; }

        int Of(SalvageMaterial material);

        /// <summary>True when the stock covers every material of <paramref name="recipe"/>.</summary>
        bool Has(Recipe recipe);
    }
}
