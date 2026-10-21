using System;

namespace MoonProject.UI
{
    /// <summary>
    /// The keys the controls card adds to its row the first time each becomes relevant (opening v3 guidance audit,
    /// B14): the build menu and the hook cable. The first four (drive, look, 07, lights) are always on the card.
    /// </summary>
    [Flags]
    internal enum ControlsKeys
    {
        None = 0,
        Build = 1,
        Hook = 2,
    }
}
