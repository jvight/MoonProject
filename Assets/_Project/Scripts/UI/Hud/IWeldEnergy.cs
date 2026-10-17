using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// What welding a structure costs 07's battery (docs/design/opening-v2.md section 10): the build menu shows it
    /// next to the materials. Gameplay's battery field answers it; the UI reaches it through this one question.
    /// </summary>
    internal interface IWeldEnergy
    {
        /// <summary>The share (0..1) of 07's battery welding <paramref name="definition"/> takes.</summary>
        float WeldShare01(StructureDefinition definition);
    }
}
