namespace MoonProject.Gameplay
{
    /// <summary>Where the beam is in one hold on a piece (docs/design/feel-pass.md §3.1).</summary>
    public enum CutPhase
    {
        /// <summary>No hold on a piece.</summary>
        Idle = 0,

        /// <summary>The emitter charges up before ignition; letting go cancels it and nothing is lost.</summary>
        Charging = 1,

        /// <summary>Ignited: the beam cuts, the progress climbs and the battery draws.</summary>
        Cutting = 2,
    }
}
