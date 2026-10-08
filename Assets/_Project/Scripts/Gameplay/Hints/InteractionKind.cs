namespace MoonProject.Gameplay
{
    /// <summary>
    /// Things the player can do, for context prompts (design ruling 6: a glyph and one word, near the thing).
    /// </summary>
    public enum InteractionKind
    {
        None = 0,

        /// <summary>Send a sonar ping (Ready while the sonar is not cooling down).</summary>
        Ping = 1,

        /// <summary>Hold to lift the relic resting in the site's heart 07 is next to.</summary>
        Excavate = 2,

        /// <summary>Hold to latch the tether onto the highlighted relic or drag piece.</summary>
        Tether = 3,

        /// <summary>Let go here (or press, for a relic in the cradle): it will float onto the museum shelf.</summary>
        Deposit = 4,

        /// <summary>
        /// Parked on a station's pad (the tower or the workbench): buy what it offers (Ready when affordable).
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
    }
}
