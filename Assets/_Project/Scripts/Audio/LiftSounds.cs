using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// What each beat of the crew's belly lift plays once (M3-19 §4): the jammed platform's creak as it asks for the
    /// stitch, the clunk at each stop and the hatch sliding open (on an A) and shut. The ride's ratchet and D hum is a
    /// loop that starts and stops on these beats.
    /// </summary>
    public static class LiftSounds
    {
        /// <summary>
        /// The one-shot cue of <paramref name="cue"/> (null when the beat only starts the ride's loop). False for a beat
        /// this table does not know yet: it stays silent in the game, and the lift sound tests fail.
        /// </summary>
        public static bool TryGetOneShot(LiftCue cue, out string id)
        {
            switch (cue)
            {
                case LiftCue.WinchCreaked:
                    id = AudioCueIds.LiftWinchCreak;
                    return true;
                case LiftCue.RideStarted:
                    id = null;
                    return true;
                case LiftCue.RideStopped:
                    id = AudioCueIds.LiftStopClunk;
                    return true;
                case LiftCue.HatchOpened:
                    id = AudioCueIds.LiftHatchOpen;
                    return true;
                case LiftCue.HatchClosed:
                    id = AudioCueIds.LiftHatchClose;
                    return true;
                default:
                    id = null;
                    return false;
            }
        }
    }
}
