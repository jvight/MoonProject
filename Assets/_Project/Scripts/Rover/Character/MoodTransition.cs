namespace MoonProject.Rover
{
    /// <summary>A change reported by <see cref="WakeUpSequence.Step"/> or <see cref="RoverMood.Step"/>.</summary>
    public enum MoodTransition
    {
        None = 0,

        /// <summary>07 began waking at the start of the session (the radio crackles on).</summary>
        BeganWaking = 1,

        /// <summary>07 finished waking and settles into normal behaviour.</summary>
        FinishedWaking = 2,

        /// <summary>Driving pulled 07 out of a deep daydream.</summary>
        WokeFromDaydream = 3,
    }
}
