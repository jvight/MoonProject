namespace MoonProject.Core
{
    /// <summary>
    /// Where a verb is in its beat sequence, anticipation to reward (docs/design/feel-pass.md §2). A verb need not
    /// use every phase, but it always ends in <see cref="Reward"/> or <see cref="Cancel"/> so listeners can ease out.
    /// </summary>
    public enum FeelPhase
    {
        /// <summary>
        /// The build-up before the act (e.g. the beam's charge-up). <see cref="Events.FeelBeat.Seconds"/> is how long
        /// it will take, so a charge can be timed to land on the act.
        /// </summary>
        Anticipate = 0,

        /// <summary>
        /// The act begins (e.g. the beam ignites), then steps on while it lasts (e.g. each cut step), with
        /// <see cref="Events.FeelBeat.Strength"/> saying how far it has come.
        /// </summary>
        Act = 1,

        /// <summary>The decisive moment: the piece breaks, the wheels touch down, the clamp closes.</summary>
        Impact = 2,

        /// <summary>
        /// The follow-through after the impact: the tumble, the fold, the settle. A verb with several follow beats
        /// sends <see cref="Events.FeelBeat.Strength"/> 0 on the first and 1 on the last (the cut: 0 at the tumble,
        /// 1 at the fold), so listeners can answer only the one they care about.
        /// </summary>
        Follow = 3,

        /// <summary>The payoff lands: the bundle is received, the cell is full, the node lights.</summary>
        Reward = 4,

        /// <summary>
        /// The verb stopped before its impact (e.g. the hold let go during the charge or mid-cut; a cut keeps its
        /// progress). Listeners ease out whatever they started.
        /// </summary>
        Cancel = 5,
    }
}
