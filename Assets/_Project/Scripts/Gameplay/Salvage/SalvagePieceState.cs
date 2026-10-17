namespace MoonProject.Gameplay
{
    /// <summary>Life of a salvage piece.</summary>
    public enum SalvagePieceState
    {
        /// <summary>On its wreck: the beam can cut it.</summary>
        Attached = 0,

        /// <summary>Cut free: coming away from the wreck and folding into a bundle.</summary>
        Breaking = 2,

        /// <summary>The bundle is on its way into 07.</summary>
        Flying = 3,

        /// <summary>Folded into 07's stock; it never comes back.</summary>
        Taken = 4,

        /// <summary>
        /// Cut through while 07's hold had no room for it: it stays on its wreck, held, until there is room, its halo
        /// breathing amber (0.5.5 §2).
        /// </summary>
        Held = 6,
    }
}
