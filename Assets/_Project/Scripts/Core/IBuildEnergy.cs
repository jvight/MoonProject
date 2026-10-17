namespace MoonProject.Core
{
    /// <summary>
    /// What building costs 07's battery (opening-v2 §10: every function costs battery). Registered in the
    /// <see cref="GameContext"/> by the Gameplay domain; the build menu shows it beside the materials.
    /// </summary>
    public interface IBuildEnergy
    {
        /// <summary>
        /// The share of 07's battery (0..1 of its present cells) welding the structure <paramref name="structureId"/>
        /// takes. Throws for an id the structure catalog does not hold.
        /// </summary>
        float WeldShare01(string structureId);
    }
}
