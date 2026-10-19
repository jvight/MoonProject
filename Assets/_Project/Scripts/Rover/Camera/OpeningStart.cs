namespace MoonProject.Rover
{
    /// <summary>How a full opening begins (<see cref="OpeningScript.BeginFull"/>).</summary>
    public enum OpeningStart
    {
        /// <summary>New Game on a fresh title: the camera pushes in from the title's shot onto 07's dark face.</summary>
        PushIn = 0,

        /// <summary>
        /// New Game that App answers with a reload (over a save): the picture is covered (UI's dark-tube vignette)
        /// before <see cref="Core.WakeBeat.FadedOut"/> lets App reload underneath.
        /// </summary>
        Cover = 1,

        /// <summary>The fresh game the reload started: straight onto 07's dark face, uncovered while it settles.</summary>
        Settle = 2,
    }
}
