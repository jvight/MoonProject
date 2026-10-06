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
