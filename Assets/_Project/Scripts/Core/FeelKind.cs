namespace MoonProject.Core
{
    /// <summary>
    /// The player verb a <see cref="Events.FeelBeat"/> belongs to (docs/design/feel-pass.md §4). Listeners pick their
    /// response by kind and phase. Never saved, but tuning tables may key on the value: append, never renumber.
    /// </summary>
    public enum FeelKind
    {
        /// <summary>07's beam cutting a salvage piece free.</summary>
        Cut = 0,

        /// <summary>07's beam digging into the ground (excavation).</summary>
        Dig = 1,

        /// <summary>07's beam stitching a machine back together.</summary>
        Repair = 2,

        /// <summary>The tether reaching, latching, lifting and letting go.</summary>
        Tether = 3,

        /// <summary>The Hover-Jump's charge and launch.</summary>
        Leap = 4,

        /// <summary>07 touching down after being airborne.</summary>
        Land = 5,

        /// <summary>07 starting, stopping and turning.</summary>
        Drive = 6,

        /// <summary>07 settling onto the home dock and charging there.</summary>
        Dock = 7,

        /// <summary>07 charging under a relay mast.</summary>
        MastCharge = 8,

        /// <summary>The belly lift carrying 07 into the lander.</summary>
        Lift = 9,

        /// <summary>The lab's reader decoding a tube, and the plotter drawing.</summary>
        Decode = 10,

        /// <summary>A kit piece being fitted on 07.</summary>
        Install = 11,

        /// <summary>A bundle reaching 07 and the stock counting up.</summary>
        Collect = 12,

        /// <summary>07 running out of charge, and coming back from it.</summary>
        LowPower = 13,

        /// <summary>A letter's signal catching on the radio.</summary>
        Letter = 14,

        /// <summary>
        /// A structure comes online: each radio tower level-up now, build pads in 0.6 (feel-pass §8.2).
        /// </summary>
        Build = 15,

        /// <summary>
        /// 07's hold (M3-25 §5): Anticipate the hatch opening (Seconds = its lead), Act one crate onto the belt at the
        /// dock (Strength = the unload's progress), Impact a bundle bumping the closed hatch when the hold is full,
        /// Cancel the belt stopping because home is full, Reward the last crate on the shelves.
        /// </summary>
        Cargo = 16,

        /// <summary>
        /// 07 scanning a fragment (0.5.5 §1.2): Anticipate the scan starting, Act at each quarter
        /// (Strength = progress), Cancel letting go (the progress is kept), Reward the dissolve beginning.
        /// </summary>
        Scan = 17,
    }
}
