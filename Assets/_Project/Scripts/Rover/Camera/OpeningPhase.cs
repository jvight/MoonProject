namespace MoonProject.Rover
{
    /// <summary>Where <see cref="OpeningScript"/> is: the storyboard's rows (docs/design/cutscenes.md §1.2).</summary>
    public enum OpeningPhase
    {
        /// <summary>Nothing playing (before a session starts, or after the wake).</summary>
        Idle = 0,

        /// <summary>New Game on the title: the picture fades to black where the camera already is.</summary>
        FadeOut = 1,

        Crackle = 2,
        LidCrack = 3,
        LidHalf = 4,
        Hunt = 5,
        Flicker = 6,
        GazeHome = 7,
        GazePilot = 8,
        GazePeak = 9,
        GazeEarth = 10,

        /// <summary>Drive input inside the lens: the remaining lid and focus stages, compressed.</summary>
        HurryLid = 11,

        /// <summary>O10, or the hurried camera going from the lens straight to the chase frame.</summary>
        PullOut = 12,

        /// <summary>O11: the lens shot with 07's blink.</summary>
        BlinkShot = 13,

        /// <summary>O12: over the shoulder toward Earth.</summary>
        OverShoulder = 14,

        /// <summary>O13: easing into the chase frame.</summary>
        ChaseEase = 15,

        /// <summary>Continue: the lid opens in one move with one blink while the camera eases to the chase frame.</summary>
        ShortWake = 16,
    }
}
