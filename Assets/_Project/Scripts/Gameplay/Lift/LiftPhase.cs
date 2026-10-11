namespace MoonProject.Gameplay.Lift
{
    /// <summary>Where the crew's belly lift is in its life (docs/features/M3-19 §4), in order.</summary>
    public enum LiftPhase
    {
        /// <summary>No power: the platform down under a dust drift, the hatch above closed and dark.</summary>
        Dormant = 0,

        /// <summary>Powered with home (tower L1) but jammed, one side hanging low; silent, no prompt.</summary>
        Jammed = 1,

        /// <summary>07 came home carrying something for the lab: the pad lights, waiting for the stitch.</summary>
        Called = 2,

        /// <summary>07 stitches the cable: the platform eases level.</summary>
        Stitching = 3,

        /// <summary>Restored, down under the belly: waits for 07 to rest on the platform.</summary>
        Ready = 4,

        /// <summary>Carrying 07 up through the hatch into the lab.</summary>
        Rising = 5,

        /// <summary>Up in the lab: the platform is the turntable, until Back brings it down.</summary>
        Inside = 6,

        /// <summary>Carrying 07 back down, turning so it drives out forward.</summary>
        Lowering = 7,
    }
}
