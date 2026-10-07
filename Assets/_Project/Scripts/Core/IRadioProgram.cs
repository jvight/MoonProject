namespace MoonProject.Core
{
    /// <summary>
    /// What the base radio should play, decided by Gameplay (collected cassettes, Bell's dial) and registered in the
    /// <see cref="GameContext"/> by it. Audio reads it and owns playback; it re-reads everything on
    /// <see cref="Events.RadioProgramChanged"/>.
    /// </summary>
    public interface IRadioProgram
    {
        /// <summary>
        /// False until Bell is repaired: the radio plays as it always has (<see cref="RadioChannel.LumenAfterDark"/>
        /// with the base tracks plus any tapes already collected).
        /// </summary>
        bool DialUnlocked { get; }

        RadioChannel Channel { get; }

        /// <summary>Cassette id playing on <see cref="RadioChannel.TapeDeck"/>; empty when none is chosen.</summary>
        string SelectedTape { get; }

        /// <summary>Collected cassettes, in the order they were collected.</summary>
        int OwnedTapeCount { get; }

        string GetOwnedTape(int index);
    }
}
