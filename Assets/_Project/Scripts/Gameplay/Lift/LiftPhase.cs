namespace MoonProject.Gameplay.Lift
{
    /// <summary>Where the crew's lift is in its life (docs/features/M3-16 step 8), in order.</summary>
    public enum LiftPhase
    {
        /// <summary>No power: jammed and dark, the tower has not reached its third level.</summary>
        Dormant = 0,

        /// <summary>Powered but jammed: waits for 07 to rest on the winch's feed pad.</summary>
        Jammed = 1,

        /// <summary>07's beam feeds the winch hopper.</summary>
        Feeding = 2,

        /// <summary>07 stitches the winch: the platform eases level and down, the cable comes back taut.</summary>
        Stitching = 3,

        /// <summary>Restored, at the bottom stop: waits for 07 to rest on the platform.</summary>
        Ready = 4,

        /// <summary>Carrying 07 up to the lander deck.</summary>
        Rising = 5,

        /// <summary>At the deck: the lookout, until the player gives any input.</summary>
        Top = 6,

        /// <summary>Carrying 07 back down.</summary>
        Lowering = 7,
    }
}
