namespace MoonProject.Gameplay
{
    /// <summary>Life of a salvage piece.</summary>
    public enum SalvagePieceState
    {
        /// <summary>On its wreck. A drag piece must be tethered clear before it can be cut.</summary>
        Attached = 0,

        /// <summary>A drag piece pulled clear of its wreck, resting loose: it can be cut now.</summary>
        Loose = 1,

        /// <summary>Cut free: coming away from the wreck and folding into a bundle.</summary>
        Breaking = 2,

        /// <summary>The bundle is on its way into 07.</summary>
        Flying = 3,

        /// <summary>Folded into 07's stock; it never comes back.</summary>
        Taken = 4,
    }
}
