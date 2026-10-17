namespace MoonProject.Core
{
    /// <summary>
    /// 07's battery, which is its range and never a threat (VISION ruling 15, docs/features/M3-20). Registered in the
    /// <see cref="GameContext"/> by the Gameplay domain, which drains it from driving, towing, beam work, leaps and
    /// boosts and fills it at chargers. Rover caps the drive and lights the flank lamps from it, UI draws the HUD arc.
    /// Allocation-free: safe to poll every frame.
    /// </summary>
    public interface IRoverBattery
    {
        /// <summary>Capacity in whole cells: 4 at the start, 6 with Cell Pack II.</summary>
        int Cells { get; }

        /// <summary>Charge in cells, 0..<see cref="Cells"/>.</summary>
        float Charge { get; }

        /// <summary><see cref="Charge"/> over <see cref="Cells"/>, 0..1.</summary>
        float Charge01 { get; }

        /// <summary>
        /// True while 07 crawls on Kenji's sealed reserve cell: new beam work, the leap and the boost sleep; the sonar
        /// and a tow already held stay on. It starts only once no beam verb is mid-work (a verb always completes).
        /// </summary>
        bool IsLowPower { get; }

        /// <summary>
        /// The fraction of 07's normal top speed allowed now: 1 normally, the crawl's cap while low on power and a
        /// higher cap while Tilly leads. Gameplay eases it between values, so it never steps.
        /// </summary>
        float DriveCap01 { get; }

        /// <summary>
        /// True when the capacitor drums and the cells can pay for a full leap now. When false the Hover-Jump never
        /// charges, so a leap is never cut short mid-air.
        /// </summary>
        bool CanLeap { get; }

        /// <summary>The capacitor drums' charge, 0..1; 0 while the Boost Coils are not fitted.</summary>
        float Drums01 { get; }

        /// <summary>True while a charger fills the cells.</summary>
        bool IsCharging { get; }

        /// <summary>The charger 07 is on while <see cref="IsCharging"/>; meaningless otherwise.</summary>
        ChargerKind Charger { get; }
    }
}
