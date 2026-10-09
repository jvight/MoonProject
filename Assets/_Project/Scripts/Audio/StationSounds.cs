using System;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// What each station beat plays once (M3-14): the last bundle's clunk into the hopper, the tower's hatch opening
    /// and shutting. The feed's hum and the tower's stitch are loops that start and stop on these beats.
    /// </summary>
    public static class StationSounds
    {
        /// <summary>The one-shot cue of <paramref name="cue"/>, or null when the beat only starts a loop.</summary>
        public static string OneShot(StationCue cue)
        {
            switch (cue)
            {
                case StationCue.FeedStarted:
                case StationCue.StitchStarted:
                    return null;
                case StationCue.Fed:
                    return AudioCueIds.HopperClunk;
                case StationCue.HatchOpened:
                    return AudioCueIds.PortHatchOpen;
                case StationCue.HatchClosed:
                    return AudioCueIds.PortHatchClose;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cue), cue, "Unknown station cue.");
            }
        }
    }
}
