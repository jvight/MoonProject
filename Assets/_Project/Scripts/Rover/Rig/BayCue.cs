namespace MoonProject.Rover
{
    /// <summary>What a step of <see cref="BayFitting"/> reports.</summary>
    public enum BayCue
    {
        None,

        /// <summary>The arm set the piece on its socket: it is fitted (the weld begins).</summary>
        Landed,

        /// <summary>The arms are folded, 07 shows the piece and is free again.</summary>
        Finished,
    }
}
