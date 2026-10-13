namespace MoonProject.Gameplay
{
    /// <summary>What happened when the UI asked to buy an upgrade level.</summary>
    public enum PurchaseResult
    {
        /// <summary>Crafted: the recipe spent, level raised, effects applied.</summary>
        Purchased = 0,

        /// <summary>07 is not parked at the upgrade's station (the tower pad).</summary>
        NotAtStation = 1,

        /// <summary>Not enough materials yet; nothing changed.</summary>
        CannotAfford = 2,

        /// <summary>Every level is already bought.</summary>
        Maxed = 3,

        /// <summary>No upgrade with that id.</summary>
        Unknown = 4,

        /// <summary>A kit whose blueprint is not pinned on Kenji's board yet (docs/features/M3-16).</summary>
        NotPinned = 5,

        /// <summary>A tower level whose component is not fed to the tower's port yet (docs/features/M3-16 P1).</summary>
        NeedsPart = 6,

        /// <summary>
        /// A kit whose part would join a full parts row (0.5.5 §3.5): fit a part first; nothing changed.
        /// </summary>
        RowFull = 7,
    }
}
