namespace MoonProject.Rover
{
    /// <summary>
    /// Where <see cref="OpeningScript"/> is. The full opening is one fixed shot of 07's face from the black on: the
    /// picture opens, 07's face wakes, the self-test prints on it, and a short dip to black hands over to the chase frame.
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

        /// <summary>A short dip to black; the chase frame takes over in the black and the picture opens on it.</summary>
        Handover = 6,

        /// <summary>Continue: the lid opens in one move with one blink while the camera eases to the chase frame.</summary>
        ShortWake = 7,
    }
}
