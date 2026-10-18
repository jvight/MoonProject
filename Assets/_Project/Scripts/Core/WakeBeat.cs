namespace MoonProject.Core
{
    /// <summary>
    /// The beats of 07's wake (docs/design/opening-v2.md), published by Rover through
    /// <see cref="Events.RoverWakeBeatReached"/> as each one begins. A fresh game's opening reaches them in this order:
    /// <see cref="FadedOut"/> (from the title), <see cref="Crackle"/>, <see cref="ChecksBegin"/>,
    /// <see cref="EyesOpen"/>, <see cref="ChaseFrame"/>; a loaded game plays only <see cref="Stirred"/>. Audio voices
    /// them; UI waits for <see cref="ChaseFrame"/>.
    /// </summary>
    public enum WakeBeat
    {
        /// <summary>New Game on the title: the picture has faded to black (the wake starts from there).</summary>
        FadedOut = 0,

        /// <summary>Black: 07's systems thump on and the radio's static swells (RoverAwoke goes with it).</summary>
        Crackle = 1,

        /// <summary>The picture opens on the close-up of 07's face and the self-test's lines begin printing there.</summary>
        ChecksBegin = 2,

        /// <summary>The self-test is done: 07's eyes come back to its face (the CRT boot sequence starts).</summary>
        EyesOpen = 3,

        /// <summary>The chase frame, after the camera's move behind 07; play is fully the player's.</summary>
        ChaseFrame = 4,

        /// <summary>The short wake (Continue): 07's lid opens in one eased move with one blink.</summary>
        Stirred = 5,
    }
}
