using System;

namespace MoonProject.Autoplay
{
    /// <summary>What the game was doing in a frame, so a slow one can be put down to something.</summary>
    [Flags]
    public enum FrameTag
    {
        None = 0,

        /// <summary>07's face screen (Tab, the inventory) is open.</summary>
        Face = 1,

        /// <summary>The face screen opened or closed within the last <see cref="FrameTagger.TransitionSeconds"/>.</summary>
        Transition = 2,

        /// <summary>Build mode is open (menu, placing or welding).</summary>
        Build = 4,

        /// <summary>A scan is running.</summary>
        Scan = 8,

        /// <summary>07 rests on a charging pad.</summary>
        Dock = 16,
    }
}
