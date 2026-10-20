namespace MoonProject.Core
{
    /// <summary>
    /// The call's keying as heard now (docs/design/opening-v3-purpose.md §2), registered in the
    /// <see cref="GameContext"/> by Audio from the carrier's own playhead: the one source of truth for when the sign
    /// sounds, so the point of light beside Earth blinks in phase with it. Its breaks while the call is broken are in it.
    /// </summary>
    public interface ICallSignal
    {
        /// <summary>
        /// How strongly the sign keys now, 0 (between its tones, or dropped out) to 1 (a tone at full strength): its
        /// three short and one long with their soft edges, times the carrier's dropouts while broken. Not the swell
        /// as 07 faces the light, nor any volume setting.
        /// </summary>
        float Keying { get; }
    }
}
