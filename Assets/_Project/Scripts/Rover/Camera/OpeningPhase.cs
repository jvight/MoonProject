namespace MoonProject.Rover
{
    /// <summary>Where <see cref="OpeningScript"/> is: the lens stages, the camera's pull-out and the power-on self-test.</summary>
    public enum OpeningPhase
    {
        /// <summary>Nothing playing (before a session starts, or after the wake).</summary>
        Idle = 0,

        /// <summary>New Game on the title: the picture fades to black where the camera already is.</summary>
        FadeOut = 1,

        /// <summary>O1: black, the carrier tone.</summary>
        Crackle = 2,

        /// <summary>O2: the lid's first stage, a warm slit.</summary>
        LidCrack = 3,

        /// <summary>O3: the lid's second stage, half open with a shudder.</summary>
        LidHalf = 4,

        /// <summary>O4: the lens hunts for focus.</summary>
        Hunt = 5,

        /// <summary>The camera leaves the lens for the three-quarter front shot of 07's face and body.</summary>
        PullOut = 6,

        /// <summary>The checks: lens, lamps, head, wheels, motor, cutter, solar wing, antenna, battery, assembly, start.</summary>
        Post = 7,

        /// <summary>The camera eases round from the face toward the chase frame.</summary>
        OverShoulder = 8,

        /// <summary>The camera settles into the chase frame.</summary>
        ChaseEase = 9,

        /// <summary>Continue: the lid opens in one move with one blink while the camera eases to the chase frame.</summary>
        ShortWake = 10,
    }
}
