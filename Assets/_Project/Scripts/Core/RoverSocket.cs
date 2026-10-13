namespace MoonProject.Core
{
    /// <summary>
    /// Where a part sits on 07's body (docs/design/0.5.5-owner-feedback.md §3.5): one socket per part type, and a
    /// better tier swaps into the same socket. Saved by value: append, never renumber.
    /// </summary>
    public enum RoverSocket
    {
        /// <summary>The emitter on the head (Focus Lens, later Cutter Ribs and the Thermal Cutter).</summary>
        Cutter = 0,

        /// <summary>The ring round the emitter (Kenji's spare tether ring).</summary>
        Tether = 1,

        /// <summary>The front (Warm Headlamp, later Floodlights).</summary>
        Lamp = 2,

        /// <summary>Under the body (Hover-Jump, later Long Leap).</summary>
        Belly = 3,

        /// <summary>Both sides, a mirrored pair (Boost Coils).</summary>
        Flanks = 4,

        /// <summary>Low between the wheels, a mirrored pair (Side Bins).</summary>
        Panniers = 5,

        /// <summary>The spine (Cell Pack II, later Cell Pack III).</summary>
        Back = 6,

        /// <summary>The rear (Cargo Cradle, later the Cargo Pod).</summary>
        Hold = 7,

        /// <summary>The centred solar panel (Tilly's solar cell, a gift).</summary>
        Wing = 8,

        /// <summary>The back, centred under the antenna (Ro's tape deck, a found part).</summary>
        Deck = 9,

        /// <summary>The six wheels (Magnetic Treads, 0.6): dim in 0.5.</summary>
        Wheels = 10,

        /// <summary>The mast (Wide Sonar, 0.6): dim in 0.5.</summary>
        Antenna = 11,
    }
}
