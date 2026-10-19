namespace MoonProject.Rover
{
    /// <summary>
    /// Where <see cref="OpeningScript"/> is. The full opening settles the camera onto 07's dark face (a push-in from a
    /// fresh title, or after the reload a New Game over a save needs), wakes the tube, draws the maker's mark, loads,
    /// and prints the self-test there. Then the camera makes exactly two eased moves: back and out to a wide shot for
    /// 07's ready performance, and round to the chase frame behind it.
    /// </summary>
    public enum OpeningPhase
    {
        /// <summary>Nothing playing (before a session starts, or after the wake).</summary>
        Idle = 0,

        /// <summary>New Game over a save: the picture is covered where the camera is, then App reloads under it.</summary>
        FadeOut = 1,

        /// <summary>New Game on a fresh title: one eased move from the title's shot onto 07's dark face.</summary>
        PushIn = 2,

        /// <summary>After the reload: on 07's dark face while the cover opens and 07 lifts its head.</summary>
        Dark = 3,

        /// <summary>The tube wakes: a dot, a line, then the full raster with a warm bloom.</summary>
        PowerOn = 4,

        /// <summary>The maker's mark draws itself stroke by stroke, its wordmark types, holds and dims away.</summary>
        Logo = 5,

        /// <summary>A short machine load: the field profile fills in five steps.</summary>
        Load = 6,

        /// <summary>The self-test's lines print on 07's face, check by check.</summary>
        Post = 7,

        /// <summary>The readout holds its success, then 07's eyes come back to the face.</summary>
        Ready = 8,

        /// <summary>The first move: the camera eases back and out diagonally to the wide front three-quarter.</summary>
        PullBack = 9,

        /// <summary>07's ready performance, seen wide: a double blink, lamps off and on, a glance.</summary>
        Perform = 10,

        /// <summary>The second move: the camera eases round to the chase frame behind 07.</summary>
        MoveBehind = 11,

        /// <summary>Continue: the lid opens in one move with one blink while the camera eases to the chase frame.</summary>
        ShortWake = 12,
    }
}
