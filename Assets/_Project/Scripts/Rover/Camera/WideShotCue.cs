namespace MoonProject.Rover
{
    /// <summary>What <see cref="WideShot.Step"/> asks the camera rig to do this frame.</summary>
    public enum WideShotCue
    {
        None,

        /// <summary>07 has rested long enough with nothing going on: compose the frame and call Open.</summary>
        Open,

        /// <summary>The player drove or looked, 07 moved, or something began: call HandBack.</summary>
        HandBack,
    }
}
