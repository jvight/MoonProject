namespace MoonProject.Core.Events
{
    /// <summary>
    /// 07's CRT eye-face feelings, gestures, and screen channel-switch transitions voiced on the radio bench (R1 AUDIO-INT).
    /// </summary>
    public enum RoverFaceCueKind
    {
        Boot,
        Blink,
        Happy,
        Worried,
        Asleep,
        Startled,
        Proud,
        ScreenOpen,
        ScreenClose,
    }

    /// <summary>
    /// Published when 07's CRT face starts its wake boot, blinks, changes feeling, or opens/closes its CRT screen
    /// channel so Audio can voice the matching R1 face cue at 07's position.
    /// </summary>
    public readonly struct RoverFaceCued
    {
        public RoverFaceCued(RoverFaceCueKind kind)
        {
            Kind = kind;
        }

        public RoverFaceCueKind Kind { get; }
    }

    /// <summary>
    /// Asks 07's radio for one music moment: the dial finds the station, one track fades in, plays through and fades
    /// out, then the set falls back to silence. The owner's rule: music only for a major milestone, arriving in a new
    /// region, or danger; never for an ordinary build. Ignored while a moment is already starting or playing; one
    /// asked for before 07's radio is on plays when it comes on.
    /// </summary>
    public readonly struct MusicMomentRequested
    {
    }
}
