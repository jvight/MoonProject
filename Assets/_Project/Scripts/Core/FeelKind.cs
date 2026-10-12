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
    }
}
