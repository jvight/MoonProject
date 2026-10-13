namespace MoonProject.Core
{
    /// <summary>
    /// What a sealed container is (docs/design/0.5.5-owner-feedback.md §2): every find that is not salvage or a
    /// fragment rests in one. Append, never renumber.
    /// </summary>
    public enum ContainerKind
    {
        /// <summary>A mast's service cabinet (a relay spare part).</summary>
        Cabinet = 0,

        /// <summary>A fallen supply pod (the ledge tape).</summary>
        Pod = 1,

        /// <summary>A sealed crate (the depot's coil crate, the port's "07 · SPARE" crate).</summary>
        Crate = 2,

        /// <summary>Ro's tape case beside Bell (the terminus tape).</summary>
        Case = 3,

        /// <summary>The garage's junction box (the transformer).</summary>
        Box = 4,
    }
}
