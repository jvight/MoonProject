using System;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// What each station beat plays once (M3-14): every bundle's clunk into the hopper, the tower's hatch opening and
    /// shutting. The feed's hum and the tower's stitch are loops that start and stop on these beats.
    /// </summary>
    public static class StationSounds
    {
        /// <summary>
        /// The one-shot cue of <paramref name="cue"/> (null when the beat only starts or stops a loop). False for a
        /// beat this table does not know yet: it stays silent in the game, and the station sound tests fail.
        /// </summary>
        public static bool TryGetOneShot(StationCue cue, out string id)
        {
            switch (cue)
            {
                case StationCue.FeedStarted:
                case StationCue.Fed:
                case StationCue.StitchStarted:
                    id = null;
                    return true;
                case StationCue.BundleDropped:
                    id = AudioCueIds.HopperClunk;
                    return true;
                case StationCue.HatchOpened:
                    id = AudioCueIds.PortHatchOpen;
                    return true;
                case StationCue.HatchClosed:
                    id = AudioCueIds.PortHatchClose;
                    return true;
                default:
                    id = null;
                    return false;
            }
        }

        /// <summary>Length of a table indexed by <see cref="StationCue"/>: one past its largest beat.</summary>
        public static int TableSize()
        {
            int size = 0;
            foreach (StationCue cue in (StationCue[])Enum.GetValues(typeof(StationCue)))
            {
                size = Math.Max(size, (int)cue + 1);
            }

            return size;
        }
    }
}
