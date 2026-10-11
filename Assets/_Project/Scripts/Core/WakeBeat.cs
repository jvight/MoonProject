namespace MoonProject.Core
{
    /// <summary>
    /// The beats of 07's wake (docs/design/cutscenes.md §1), published by Rover through
    /// <see cref="Events.RoverWakeBeatReached"/> as each one begins. The opening reaches them in this order; a hurried
    /// opening skips the gaze beats and the flicker, but never the blink. Audio voices each beat; UI waits for
    /// <see cref="ChaseFrame"/>.
    /// </summary>
    public enum WakeBeat
    {
        /// <summary>New Game on the title: the camera has pushed into 07's closed lens and the picture is black.</summary>
        PushedIn = 0,

        /// <summary>O1: darkness; 07's systems thump on and the radio's static swells (RoverAwoke goes with it).</summary>
        Crackle = 1,

        /// <summary>O2: the lid's first stage, a soft slit of warm blur at the bottom of the frame.</summary>
        LidCracked = 2,

        /// <summary>O3: the lid's second stage, up to half open with a small shudder.</summary>
        LidHalf = 3,

        /// <summary>O4: the lens starts hunting for focus, too near, then too far.</summary>
        FocusHunting = 4,

        /// <summary>O4's end: the focus lands at infinity.</summary>
        FocusSettled = 5,

        /// <summary>O5: old electronics, two soft exposure dips, a roll band and a warm lift; 07 is on.</summary>
        Flicker = 6,

        /// <summary>O6: the gaze lifts to the horizon and turns to the sleeping home.</summary>
        GazeHome = 7,

        /// <summary>O7: the gaze turns to the tower's pilot light.</summary>
        GazePilot = 8,

        /// <summary>O8: the gaze crosses the basin to The Peak's red beacon.</summary>
        GazePeak = 9,

        /// <summary>O8: the lens finds the far distance once more.</summary>
        FarFocusSettled = 10,

        /// <summary>O9: the head tilts up and Earth comes into the frame; the lid opens wider.</summary>
        GazeEarth = 11,

        /// <summary>O10: the camera starts to ease backward out of the lens.</summary>
        PullingOut = 12,

        /// <summary>O10's end: the frame has resolved into the low front three-quarter shot on the eye.</summary>
        PulledOut = 13,

        /// <summary>O11: 07 blinks, tilts its head, sighs with its wing and its antenna blinks the call sign.</summary>
        Blinked = 14,

        /// <summary>O12: the camera eases round over 07's shoulder toward Earth.</summary>
        OverShoulder = 15,

        /// <summary>O13: the chase frame; play is fully the player's.</summary>
        ChaseFrame = 16,

        /// <summary>The short wake (Continue): 07's lid opens in one eased move with one blink.</summary>
        Stirred = 17,
    }
}
