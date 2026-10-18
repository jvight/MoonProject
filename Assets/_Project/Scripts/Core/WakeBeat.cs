namespace MoonProject.Core
{
    /// <summary>
    /// The beats of 07's wake (docs/design/opening-v2.md), published by Rover through
    /// <see cref="Events.RoverWakeBeatReached"/> as each one begins. A fresh game's opening is one still shot of 07's
    /// face and reaches them in this order: <see cref="FadedOut"/> (from the title), <see cref="Crackle"/>,
    /// <see cref="PictureOpens"/>, <see cref="EyesOpen"/>, <see cref="Blinked"/>, <see cref="ChecksBegin"/>,
    /// <see cref="ChaseFrame"/>; a loaded game plays only <see cref="Stirred"/>. Audio voices them; UI waits for
    /// <see cref="ChaseFrame"/>.
    /// </summary>
    public enum WakeBeat
    {
        /// <summary>New Game on the title: the picture has faded to black (the wake starts from there).</summary>
        FadedOut = 0,

        /// <summary>Black: 07's systems thump on and the radio's static swells (RoverAwoke goes with it).</summary>
        Crackle = 1,

        /// <summary>The picture opens from black on 07, still asleep with its face dark.</summary>
        PictureOpens = 2,

        /// <summary>07's face screen flickers on and its eyes open (the CRT boot sequence starts).</summary>
        EyesOpen = 3,

        /// <summary>07's eyes blink awake; the self-test's lines print on its face after this.</summary>
        Blinked = 4,

        /// <summary>The self-test's lines begin on the face (the static gives way).</summary>
        ChecksBegin = 5,

        /// <summary>The chase frame, after the handover's dip to black; play is fully the player's.</summary>
        ChaseFrame = 6,

        /// <summary>The short wake (Continue): 07's lid opens in one eased move with one blink.</summary>
        Stirred = 7,
    }
}
