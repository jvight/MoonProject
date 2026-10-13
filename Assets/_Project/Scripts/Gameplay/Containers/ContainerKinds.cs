using System;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What each of Art's container prefabs is (docs/design/0.5.5-owner-feedback.md §2): Art names them for their
    /// places, gameplay keeps the kind the rest of the game hears in <see cref="Core.Events.ContainerOpened"/>.
    /// </summary>
    public static class ContainerKinds
    {
        public const string CoilCrate = "CoilCrate";
        public const string JunctionBox = "JunctionBox";
        public const string TapeCase = "TapeCase";
        public const string SupplyPod = "SupplyPod";
        public const string TillySled = "TillySled";
        public const string Cabinet = "Cabinet";
        public const string SpareCrate = "SpareCrate";

        /// <summary>
        /// The kind of Art's container prefab <paramref name="prefab"/>; an unknown name is a wiring bug.
        /// </summary>
        public static ContainerKind Of(string prefab)
        {
            switch (prefab)
            {
                case CoilCrate:
                case SpareCrate:
                    return ContainerKind.Crate;
                case JunctionBox:
                    return ContainerKind.Box;
                case TapeCase:
                    return ContainerKind.Case;
                case SupplyPod:
                case TillySled:
                    return ContainerKind.Pod;
                case Cabinet:
                    return ContainerKind.Cabinet;
                default:
                    throw new ArgumentOutOfRangeException(nameof(prefab), prefab, "Not one of Art's containers.");
            }
        }
    }
}
