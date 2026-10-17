namespace MoonProject.Gameplay
{
    /// <summary>
    /// Things the player can do, for context prompts (design ruling 6: a glyph and one word, near the thing). The
    /// numbers are serialized in the UI's prompt table, so they never shift (the gaps are verbs the opening cut).
    /// </summary>
    public enum InteractionKind
    {
        None = 0,

        /// <summary>A salvage piece or a sealed container is picked (in reach, in the aim): hold E to cut it.</summary>
        Salvage = 11,

        /// <summary>
        /// A piece held on its wreck waits within reach and the hold has room: press E to take it. The hint's
        /// <see cref="InteractionHint.NounKey"/> names its material.
        /// </summary>
        PickUp = 13,

        /// <summary>A placed structure frame in reach: hold E to weld it (Ready while beam work may start).</summary>
        Build = 14,

        /// <summary>
        /// A target still to scan lies in reach and 07's view rests on it, with no other E verb before it
        /// (0.5.5 §1.2): hold E to scan it.
        /// </summary>
        Scan = 15,

        /// <summary>
        /// Looking at a placed structure within reach: hold Deconstruct to fold it down and refund its materials.
        /// </summary>
        Deconstruct = 16,
    }
}
