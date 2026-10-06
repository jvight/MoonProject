namespace MoonProject.Gameplay
{
    /// <summary>Life of a relic. Saved as an int: append new states, never renumber.</summary>
    public enum RelicState
    {
        /// <summary>Under the ground at its site; answers the sonar.</summary>
        Buried = 0,

        /// <summary>Partly lifted by the tractor beam; progress is kept when the beam stops.</summary>
        Surfacing = 1,

        /// <summary>A free physics body on the Relic layer: the tether can tow it.</summary>
        Loose = 2,

        /// <summary>Floating onto its museum shelf slot.</summary>
        Depositing = 3,

        /// <summary>On display at the base.</summary>
        Displayed = 4,

        /// <summary>Drifted off the drivable floor (or below it) and is floating gently back within reach.</summary>
        Returning = 5,
    }
}
