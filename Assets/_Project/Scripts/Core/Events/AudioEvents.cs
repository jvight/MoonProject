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
    /// Surface kind for UI typewriter ticks (<c>ui_type_card</c> vs <c>ui_type_terminal</c>).
    /// </summary>
    public enum UiTypeKind
    {
        Card,
        Terminal,
    }

    /// <summary>
    /// Published by UI card or terminal text reveals per stepped character/word so <c>UiAudio</c> can play the
    /// rate-limited <c>ui_type_card</c> or <c>ui_type_terminal</c> cue.
    /// </summary>
    public readonly struct UiTypewriterTick
    {
        public UiTypewriterTick(UiTypeKind kind)
        {
            Kind = kind;
        }

        public UiTypeKind Kind { get; }
    }
}
