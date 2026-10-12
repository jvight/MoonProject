using System;

namespace MoonProject.Rover
{
    /// <summary>
    /// Node names of the art box's RoverModel prefab for "07" (docs/ARCHITECTURE.md, "Contract: rover model rig").
    /// Units in metres, +Z forward, +Y up, origin at the centre of the ground contact patch; wheels roll around
    /// local X.
    /// Neck yaws around local Y, Head pitches around local X, Eyelid closes and SolarWing opens with + local X.
    /// </summary>
    public static class RoverModelNodes
    {
        public const int WheelCount = 6;

        /// <summary>Rig index of each wheel (see <see cref="Wheel"/>).</summary>
        public const int FrontLeftIndex = 0;
        public const int FrontRightIndex = 1;
        public const int RearLeftIndex = 4;
        public const int RearRightIndex = 5;

        public const string Body = "Body";
        public const string BogieLeft = "Bogie_L";
        public const string BogieRight = "Bogie_R";
        public const string WheelFrontLeft = "Wheel_FL";
        public const string WheelFrontRight = "Wheel_FR";
        public const string WheelMiddleLeft = "Wheel_ML";
        public const string WheelMiddleRight = "Wheel_MR";
        public const string WheelRearLeft = "Wheel_RL";
        public const string WheelRearRight = "Wheel_RR";
        public const string Neck = "Neck";
        public const string Head = "Head";
        public const string Eye = "Eye";
        public const string TetherOrigin = "TetherOrigin";
        public const string Eyelid = "Eyelid";
        public const string SolarWing = "SolarWing";
        public const string Antenna = "Antenna";
        public const string AntennaTip = "AntennaTip";
        public const string HeadlampSocket = "HeadlampSocket";
        public const string CargoSocket = "CargoSocket";
        public const string DustSocketLeft = "DustSocket_L";
        public const string DustSocketRight = "DustSocket_R";
        public const string CoilSocket = "CoilSocket";
        public const string DrumSocketLeft = "DrumSocket_L";
        public const string DrumSocketRight = "DrumSocket_R";
        public const string TubeSocket = "TubeSocket";

        /// <summary>On the eye: where the Focus Lens kit mounts (M3-26).</summary>
        public const string LensSocket = "LensSocket";

        /// <summary>The front flanks above the front wheels: where the side bins mount (RoverAbility.SideBins).</summary>
        public const string BinSocketLeft = "BinSocket_L";

        public const string BinSocketRight = "BinSocket_R";

        /// <summary>The rear cargo hatch on its top hinge (opens about local +X), holding the crate window.</summary>
        public const string CargoHatch = "CargoHatch";

        /// <summary>The hatch window's crate <paramref name="index"/>, one per hold slot (top row L to R, then bottom).</summary>
        public static string HatchCrate(int index)
        {
            return "HatchCrate_" + index;
        }

        /// <summary>Where Cell Pack II's battery spine stands on the lid's right edge (docs/features/M3-20).</summary>
        public const string SpineSocket = "SpineSocket";

        /// <summary>Where the old tape deck rides on the left flank (docs/features/M3-24 C).</summary>
        public const string DeckSocket = "DeckSocket";

        /// <summary>Where Kenji's tether ring sits, low on the back of the head (Head/RingSocket).</summary>
        public const string RingSocket = "RingSocket";

        /// <summary>The tether ring's glass band (Art's Kit_TetherRing child), lit by RoverKit.</summary>
        public const string RingGlow = "Glow";

        /// <summary>Friend gifts on RoverModel, hidden until given (SolarWing/, Body/ and Antenna/ children).</summary>
        public const string CellFilled = "CellFilled";
        public const string Decal07Fresh = "Decal07Fresh";
        public const string Pennant = "Pennant";

        /// <summary>Nodes of art's kit prefabs (ARCHITECTURE "Visible kit (M3-11)"): glasses, band and seat.</summary>
        public const int KitLampCount = 3;
        public const string DrumGlow = "Glow";
        public const string RelicSeat = "RelicSeat";

        /// <summary>The lamp bar's glass <paramref name="index"/> (0..2), its own glow renderer.</summary>
        public static string KitLamp(int index)
        {
            if (index < 0 || index >= KitLampCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Lamp index is 0..2.");
            }

            return "Lamp_" + index;
        }

        /// <summary>
        /// The battery light bar's segments, one per cell (docs/features/M3-20): Body children 'BatteryLamp_0..7'
        /// (left to right along the lid's rear-top edge), each with its glass 'BatteryGlass_&lt;i&gt;'.
        /// </summary>
        public const int FlankLampCount = 8;

        /// <summary>Light-bar segment <paramref name="index"/> (0..7, left first).</summary>
        public static string FlankLamp(int index)
        {
            return "BatteryLamp_" + Segment(index);
        }

        /// <summary>A segment's glass: its own glow renderer on M_LowPolyGlowOff (WarmLamp).</summary>
        public static string FlankGlass(int index)
        {
            return "BatteryGlass_" + Segment(index);
        }

        private static int Segment(int index)
        {
            if (index < 0 || index >= FlankLampCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Battery lamp index is 0..7.");
            }

            return index;
        }

        /// <summary>Springs of art's HoverCoils prefab (mounted on <see cref="CoilSocket"/>).</summary>
        public const int CoilCount = 4;

        /// <summary>Hover-Jump spring node name by index: 0 FL, 1 FR, 2 RL, 3 RR (pivot at its top).</summary>
        public static string Coil(int index)
        {
            return "Coil_" + CoilCorner(index);
        }

        /// <summary>Glow ring under spring <paramref name="index"/> (its own renderer on M_LowPolyGlowOff).</summary>
        public static string CoilGlow(int index)
        {
            return "Glow_" + CoilCorner(index);
        }

        private static string CoilCorner(int index)
        {
            switch (index)
            {
                case 0: return "FL";
                case 1: return "FR";
                case 2: return "RL";
                case 3: return "RR";
                default: throw new ArgumentOutOfRangeException(nameof(index), index, "Coil index is 0..3.");
            }
        }

        /// <summary>Wheel node name by rig index: 0 FL, 1 FR, 2 ML, 3 MR, 4 RL, 5 RR.</summary>
        public static string Wheel(int index)
        {
            switch (index)
            {
                case 0: return WheelFrontLeft;
                case 1: return WheelFrontRight;
                case 2: return WheelMiddleLeft;
                case 3: return WheelMiddleRight;
                case 4: return WheelRearLeft;
                case 5: return WheelRearRight;
                default: throw new ArgumentOutOfRangeException(nameof(index), index, "Wheel index is 0..5.");
            }
        }

        /// <summary>Wheel axle row by rig index: 0 front, 1 middle, 2 rear.</summary>
        public static int WheelRow(int index)
        {
            return index / 2;
        }
    }
}
