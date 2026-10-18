namespace MoonProject.Rover
{
    /// <summary>
    /// Where <see cref="OpeningScript"/> is. The full opening fades from the title to black and opens straight on the
    /// close-up of 07's face, where the self-test prints. Then the camera makes exactly two eased moves: back and out
    /// to a wide shot for 07's ready performance, and round to the chase frame behind it.
    /// </summary>
    public enum OpeningPhase
    {
        /// <summary>Nothing playing (before a session starts, or after the wake).</summary>
        Idle = 0,

        /// <summary>New Game on the title: the picture fades to black where the camera already is.</summary>
        FadeOut = 1,

        /// <summary>Black while 07's systems thump on; the camera already waits on 07's face.</summary>
        Dark = 2,

        /// <summary>The picture opens on 07's face close-up and the self-test's lines print there, check by check.</summary>
        Post = 3,

        /// <summary>The readout holds its success, then 07's eyes come back to the face.</summary>
        Ready = 4,

        /// <summary>The first move: the camera eases back and out diagonally to the wide front three-quarter.</summary>
        PullBack = 5,

        /// <summary>07's ready performance, seen wide: a double blink, lamps off and on, a dust shiver, a glance.</summary>
        Perform = 6,

        /// <summary>The second move: the camera eases round to the chase frame behind 07.</summary>
        MoveBehind = 7,

        /// <summary>Continue: the lid opens in one move with one blink while the camera eases to the chase frame.</summary>
        ShortWake = 8,
    }
}
