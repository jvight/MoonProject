using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One frame of what <see cref="BatteryModel"/> needs to know, gathered by <see cref="BatteryField"/>.
    /// </summary>
    public struct BatteryStep
    {
        /// <summary>Metres 07 travelled on the ground this frame (horizontal; airborne metres are free).</summary>
        public float GroundDistance { get; set; }

        /// <summary>07's ground speed (m/s).</summary>
        public float Speed { get; set; }

        /// <summary>Slope (degrees) of the ground along the travel: + uphill, - downhill.</summary>
        public float SlopeDegrees { get; set; }

        /// <summary>07 tows a load on its winch cable: driving costs more and top speed is capped.</summary>
        public bool Towing { get; set; }

        /// <summary>How hard the tow cable pulls back (0 slack .. 1 caught): 07's drive falls as it strains.</summary>
        public float TowStrain01 { get; set; }

        /// <summary>The Boost Coils are engaged.</summary>
        public bool Boosting { get; set; }

        /// <summary>
        /// Seconds of active beam work (cut, dig, stitch, feed) this frame; above 0 a verb is mid-work.
        /// </summary>
        public float BeamSeconds { get; set; }

        /// <summary>Seconds 07's lens scanned this frame.</summary>
        public float ScanSeconds { get; set; }

        /// <summary>Seconds 07 welded a structure this frame.</summary>
        public float WeldSeconds { get; set; }

        /// <summary>07 is still and nothing is happening (the solar trickle's condition).</summary>
        public bool Still { get; set; }

        /// <summary>07's solar wing works (it is jammed through the opening, opening-v2 §10: no trickle).</summary>
        public bool SolarFitted { get; set; }

        /// <summary>The Boost Coils' capacitor drums are on 07.</summary>
        public bool DrumsFitted { get; set; }

        /// <summary>
        /// 07 is on a working charger (a pad it built), at any speed: the model decides when 07 has stopped long
        /// enough to charge.
        /// </summary>
        public bool OnCharger { get; set; }

        public ChargerKind Charger { get; set; }

        /// <summary>The charger's pad centre (valid while <see cref="OnCharger"/>).</summary>
        public Vector3 ChargerPosition { get; set; }

        /// <summary>07's position (the Low warning measures the way to <see cref="NearestLitCharger"/> from it).</summary>
        public Vector3 Position { get; set; }

        /// <summary>The nearest charger that works now: a powered pad 07 built.</summary>
        public Vector3 NearestLitCharger { get; set; }
    }
}
