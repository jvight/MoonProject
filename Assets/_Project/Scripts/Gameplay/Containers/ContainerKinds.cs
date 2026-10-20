using System;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Art's R1 crates by prefab name (Generated/Art/R1/Models/Crates, opening-v2 §4) and what kind of container each
    /// is.
    /// </summary>
    public static class ContainerKinds
    {
        public const string CrateM = "Crate_M";
        public const string CrateL = "Crate_L";

        /// <summary>
        /// The lander's big open cargo crate (B3, B4), at true size: the charging pad's two fragments rest on its
        /// PartSocket_0/1 above the rim, and its RimGlow warms as 07 comes near.
        /// </summary>
        public const string CrateCargoOpen = "Crate_Cargo_Open";

        /// <summary>
        /// The kind of Art's crate prefab <paramref name="prefab"/>; an unknown name is a wiring bug.
        /// </summary>
        public static ContainerKind Of(string prefab)
        {
            switch (prefab)
            {
                case CrateM:
                case CrateL:
                case CrateCargoOpen:
                    return ContainerKind.Crate;
                default:
                    throw new ArgumentOutOfRangeException(nameof(prefab), prefab, "Not one of Art's crates.");
            }
        }
    }
}
