namespace MoonProject.Rover
{
    /// <summary>
    /// Where <see cref="OpeningScript"/> is. The full opening holds one shot of 07's face from the black on: the picture
    /// opens, 07's face wakes and the self-test prints on it. Then the camera makes exactly two eased moves: back and
    /// out to a wide shot for 07's ready performance, and round to the chase frame behind it.
    /// </summary>
    public enum OpeningPhase
    {
        /// <summary>Nothing playing (before a session starts, or after the wake).</summary>
        Idle = 0,

        /// <summary>New Game on the title: the picture fades to black where the camera already is.</summary>
        FadeOut = 1,

        /// <summary>Black while 07's systems thump on; the camera already waits on 07's face.</summary>
        Dark = 2,

        /// <summary>The picture opens from black on 07, still asleep with its face dark.</summary>
        FadeIn = 3,

        /// <summary>07's face screen flickers on, its eyes blink awake and its head lifts toward the camera.</summary>
        EyesOpen = 4,

        /// <summary>The self-test's lines print on the face one by one while each part does its check.</summary>
        Post = 5,

        /// <summary>The readout holds its success, then the eyes come back to the face with a blink.</summary>
        Ready = 6,

        /// <summary>The first move: the camera eases back and out diagonally to the wide front three-quarter.</summary>
        PullBack = 7,

        /// <summary>07's ready performance, seen wide: a double blink, lamps off and on, a dust shiver, a glance.</summary>
        Perform = 8,

        /// <summary>The second move: the camera eases round to the chase frame behind 07.</summary>
        MoveBehind = 9,

        /// <summary>Continue: the lid opens in one move with one blink while the camera eases to the chase frame.</summary>
        ShortWake = 10,
    }
}
