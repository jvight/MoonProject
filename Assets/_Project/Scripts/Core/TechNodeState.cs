namespace MoonProject.Core
{
    /// <summary>How far a tech node has come (docs/features/M3-21).</summary>
    public enum TechNodeState
    {
        /// <summary>Not found yet: its place on the tech board is an empty pin.</summary>
        Unknown = 0,

        /// <summary>
        /// A kit's blueprint was decoded at the lab's reader: its sheet is on the board and the bay can build it.
        /// Station nodes skip this state.
        /// </summary>
        Decoded = 1,

        /// <summary>Fitted on 07, or built at its port or pad.</summary>
        Built = 2,
    }
}
