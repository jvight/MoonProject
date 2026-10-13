namespace MoonProject.Core
{
    /// <summary>
    /// 07's cutter as its fitted kits make it (docs/features/M3-26 §6), registered in the GameContext by Gameplay:
    /// Rover's charge run and lamps, Audio's arpeggio and UI's "07 can" page read it. Allocation-free.
    /// </summary>
    public interface ICutter
    {
        /// <summary>0 bare, 1 with the Focusing Lens, higher with later kits.</summary>
        int Tier { get; }

        /// <summary>Conduit lamps the charge run lights on its way to the head.</summary>
        int LampCount { get; }

        /// <summary>Seconds of a small piece's fresh charge, from the press to ignition (the brace, one stage, the
        /// held breath).</summary>
        float ChargeSeconds { get; }

        /// <summary>Seconds of a small piece's warm charge (a re-press soon after the last firing at the same site:
        /// no brace, a quicker stage).</summary>
        float ResumeChargeSeconds { get; }

        /// <summary>Seconds 07 plants itself before a fresh charge builds (0.5.5 §5.1: every tier).</summary>
        float BraceSeconds { get; }

        /// <summary>A re-press at the same site within this many seconds of the last firing finds the emitter warm.
        /// </summary>
        float WarmWindowSeconds { get; }

        /// <summary>Multiplier on cutting speed once the beam fires (1 at tier 0).</summary>
        float SpeedFactor { get; }

        /// <summary>Multiplier on the battery drain of beam work (1 at tier 0).</summary>
        float DrainFactor { get; }
    }
}
