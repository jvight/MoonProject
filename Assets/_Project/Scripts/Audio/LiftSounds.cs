using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// What each beat of the crew's lift plays once (M3-16 step 8): the jammed winch's creak and the clunk at each
    /// stop. The ride's ratchet and hum is a loop that starts and stops on these beats.
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
                default:
                    id = null;
                    return false;
            }
        }
    }
}
