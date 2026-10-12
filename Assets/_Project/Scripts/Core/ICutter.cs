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

        /// <summary>Seconds of a fresh charge run, from the press to the crack.</summary>
        float ChargeSeconds { get; }

        /// <summary>Seconds of a resumed run, which starts part-way, where the drain left it.</summary>
        float ResumeChargeSeconds { get; }

        /// <summary>Multiplier on cutting speed once the beam fires (1 at tier 0).</summary>
        float SpeedFactor { get; }

        /// <summary>Multiplier on the battery drain of beam work (1 at tier 0).</summary>
        float DrainFactor { get; }
    }
}
