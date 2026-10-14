namespace MoonProject.Gameplay
{
    /// <summary>
    /// Things the player can do, for context prompts (design ruling 6: a glyph and one word, near the thing). The
    /// numbers are serialized in the UI's prompt table, so they never shift: 1 was the sonar ping, cut when 07 learned
    /// to listen.
    /// </summary>
    public enum InteractionKind
    {
        None = 0,

        /// <summary>Hold to lift the relic resting in the site's heart 07 is next to.</summary>
        Excavate = 2,

        /// <summary>Hold to latch the tether onto the highlighted relic or drag piece.</summary>
        Tether = 3,

        /// <summary>
        /// Press facing the lab's keepsake shelf: the relic that rode up (or the cradle's) floats into its cubby.
        /// </summary>
        Deposit = 4,

        /// <summary>
        /// Parked on a station's pad (the tower or the Rover Bay): buy what it offers (Ready when affordable).
        /// </summary>
        Upgrade = 5,

        /// <summary>Towing: the winch reels the relic or drag piece in or out.</summary>
        Reel = 6,

        /// <summary>Next to a broken friend with every part gathered: hold to repair it.</summary>
        Repair = 7,

        /// <summary>Parked in front of Bell's dial (Bell home, dial unlocked): turn it one detent.</summary>
        Tune = 8,

        /// <summary>
        /// At a dark relay mast's foot with its part held: hold to restore it (Ready when the materials are there).
        /// </summary>
        Restore = 9,

        /// <summary>
        /// Parked on the pad of home or of a lit mast, not towing: open the radio-hop list (Ready while there is
        /// another lit node to hop to).
        /// </summary>
        Hop = 10,

        /// <summary>A salvage piece is picked (within reach, in the aim cone): hold Excavate to cut it loose.</summary>
        Salvage = 11,

        /// <summary>
        /// The Cargo Cradle is on 07 and empty and a loose relic is highlighted: press Tether to lift it into the rack.
        /// </summary>
        Stow = 12,

        /// <summary>
        /// A part waits within reach (a tower part, a friend's part, a relay spare): press Excavate to lift it onto
        /// 07's back. The hint's <see cref="InteractionHint.NounKey"/> names the part.
        /// </summary>
        PickUp = 13,

        /// <summary>
        /// Beside a free shelf socket at home while home's shelves are full (docs/features/M3-25 §3.1): hold Excavate to
        /// build its shelf (Ready once home and 07 cover its recipe).
        /// </summary>
        Build = 14,

        /// <summary>
        /// A fragment still to scan lies in reach and 07's view rests on it, with no other Excavate verb before it
        /// (0.5.5 §1.2): hold Excavate to scan it.
        /// </summary>
        Scan = 15,
    }
}
