namespace MoonProject.Gameplay
{
    /// <summary>Where a radio-hop is (<see cref="IRadioHop"/>).</summary>
    public enum RadioHopPhase
    {
        /// <summary>No list, no hop.</summary>
        Closed = 0,

        /// <summary>The tiny list of lit nodes is open on 07's pad.</summary>
        Choosing = 1,

        /// <summary>The static rises and the view eases to a soft dark.</summary>
        Leaving = 2,

        /// <summary>The view rests dark; 07 stands on the target pad.</summary>
        Dark = 3,

        /// <summary>The static resolves and the view eases back in.</summary>
        Arriving = 4,
    }
}
