using System;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// Node names of the art box's R1 RoverModel prefab for "07" v3 Amber CRT (docs/ARCHITECTURE.md, "Contract: rover model rig").
    /// Units in metres, +Z forward, +Y up, origin at the centre of the ground contact patch; 4 wheels roll around local X,
    /// front wheels steer around local Y, trailing arms pitch around local X, shocks compress along local Y.
    /// Neck yaws around local Y, NeckLift nods around local X, Head pitches/tilts around local X/Z, Screen carries R1_Screen.
    /// </summary>
    public static class RoverModelNodes
    {
        public const int WheelCount = 4;

        /// <summary>Rig index of each wheel (see <see cref="Wheel"/>).</summary>
        public const int FrontLeftIndex = 0;
        public const int FrontRightIndex = 1;
        public const int RearLeftIndex = 2;
        public const int RearRightIndex = 3;

        public const string Body = "Body";
        public const string WheelFrontLeft = "Wheel_FL";
        public const string WheelFrontRight = "Wheel_FR";
        public const string WheelRearLeft = "Wheel_RL";
        public const string WheelRearRight = "Wheel_RR";

        public const string ArmFrontLeft = "Arm_FL";
        public const string ArmFrontRight = "Arm_FR";
        public const string ArmRearLeft = "Arm_RL";
        public const string ArmRearRight = "Arm_RR";

        public const string ShockFrontLeft = "Shock_FL";
        public const string ShockFrontRight = "Shock_FR";
        public const string ShockRearLeft = "Shock_RL";
        public const string ShockRearRight = "Shock_RR";

        public const string Neck = "Neck";
        public const string NeckLift = "NeckLift";
        public const string Head = "Head";
        public const string Screen = "Screen";
        public const string RearVentStrip = "RearVentStrip";
        public const string Visor = "Visor";
        public const string DeckCover = "DeckCover";
        public const string Eye = "Eye";
        public const string EyeOrigin = "EyeOrigin";

        /// <summary>Alias for <see cref="EyeOrigin"/> on the cheek camera pod.</summary>
        public const string TetherOrigin = EyeOrigin;

        public const string SolarWing = "SolarWing";
        public const string WingSocket = "WingSocket";

        public const string Antenna = "Antenna";
        public const string AntennaTip = "AntennaTip";
        public const string SonarLoop = "SonarLoop";
        public const string AntennaOrigin = "AntennaOrigin";

        public const string CutterTurret = "CutterTurret";
        public const string CutterHead = "CutterHead";
        public const string CutterOrigin = "CutterOrigin";
        public const string StitchOrigin = "StitchOrigin";
        public const string CoilSocketT1 = "CoilSocket_T1";

        /// <summary>Mechanical iris blade count on the chest cutter nozzle aperture (<see cref="CutterOrigin"/>).</summary>
        public const int NozzleBladeCount = 6;
        public const string NozzleHotCore = "NozzleHotCore";
        public const string NozzleSpillLight = "NozzleSpillLight";

        /// <summary>Iris blade child name <paramref name="index"/> (0..5) under <see cref="CutterOrigin"/>.</summary>
        public static string NozzleBlade(int index)
        {
            if (index < 0 || index >= NozzleBladeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Nozzle blade index is 0..5.");
            }

            return "NozzleBlade_" + index;
        }

        public const string DigScoop = "DigScoop";
        public const string ScoopArms = "ScoopArms";
        public const string ScoopBucket = "ScoopBucket";
        public const string ScoopOrigin = "ScoopOrigin";
        public const string ScoopCylinderLeft = "ScoopCyl_L";
        public const string ScoopCylinderRight = "ScoopCyl_R";

        public const string WinchSocket = "WinchSocket";
        public const string WinchSpool = "WinchSpool";
        public const string TowHook = "TowHook";
        public const string WinchOrigin = "WinchOrigin";

        public const string CargoHold = "CargoHold";
        public const string HatchOrigin = "HatchOrigin";
        public const string StoryRackSocket = "StoryRackSocket";
        public const string HatchWindow = "HatchWindow";

        public const string CargoHatch = "CargoHatch";
        public const string UpperCanopy = "UpperCanopy";
        public const string LowerChute = "LowerChute";
        public const string IntakeRoller = "IntakeRoller";

        public const string HeadlampSocket = "HeadlampSocket";
        public const string CargoSocket = "CargoSocket";
        public const string CradleSocket = "CradleSocket";
        public const string DustSocketLeft = "DustSocket_L";
        public const string DustSocketRight = "DustSocket_R";
        public const string CoilSocket = "CoilSocket";
        public const string DrumSocketLeft = "DrumSocket_L";
        public const string DrumSocketRight = "DrumSocket_R";

        /// <summary>On the eye: where the Focus Lens kit mounts (M3-26).</summary>
        public const string LensSocket = "LensSocket";

        /// <summary>The empty node at the lens front where the scan fan mounts (it sweeps about its local Y).</summary>
        public const string ScanFanSocket = "ScanFanSocket";

        /// <summary>Art's empty node marking where equipment socket <paramref name="socket"/> sits on 07 (0.5.5
        /// §3.5), e.g. "CutterSocket".</summary>
        public static string EquipmentSocket(RoverSocket socket)
        {
            return socket + "Socket";
        }

        /// <summary>The charge-up's conduit glass core under Body/ChargeConduits.</summary>
        public const string ConduitGlow = "ConduitGlow";

        /// <summary>The charge-up's stages (0.5.5 §5.4): three mirrored cells, the battery end first.</summary>
        public const int ChargeStages = 3;

        /// <summary>The lock ring's glow round the Focus Lens socket on the eye, lit at the full lock.</summary>
        public const string LockGlow = "LockGlow";

        /// <summary>Stage <paramref name="stage"/>'s cell (1..3) on the left or right deck conduit.</summary>
        public static string StageCell(int stage, bool left)
        {
            return "Stage_" + stage + (left ? "_L" : "_R");
        }

        /// <summary>The glass sleeve of stage <paramref name="stage"/>'s cell on the left or right side.</summary>
        public static string StageGlow(int stage, bool left)
        {
            return "StageGlow_" + stage + (left ? "_L" : "_R");
        }

        /// <summary>The front flanks above the front wheels: where the side bins mount (RoverAbility.SideBins).</summary>
        public const string BinSocketLeft = "BinSocket_L";

        public const string BinSocketRight = "BinSocket_R";

        /// <summary>The hatch window's crate <paramref name="index"/>, one per hold slot (top row L to R, then bottom).</summary>
        public static string HatchCrate(int index)
        {
            return "HatchCrate_" + index;
        }

        /// <summary>Where Cell Pack II's battery spine stands on the lid's right edge (docs/features/M3-20).</summary>
        public const string SpineSocket = "SpineSocket";

        /// <summary>Where the old tape deck rides on the head's side slot (docs/features/M3-24 C).</summary>
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

        /// <summary>A segment's glass: its own glow renderer on R1_LampAmber.</summary>
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
            return "Coil_" + Corner(index, "Coil");
        }

        /// <summary>Glow ring under spring <paramref name="index"/> (its own renderer on M_LowPolyGlowOff).</summary>
        public static string CoilGlow(int index)
        {
            return "Glow_" + Corner(index, "Coil");
        }

        /// <summary>Suspension corner root node name by rig index: 0 FL, 1 FR, 2 RL, 3 RR.</summary>
        public static string Susp(int index)
        {
            return "Susp_" + Corner(index, "Susp");
        }

        /// <summary>Wheel node name by rig index: 0 FL, 1 FR, 2 RL, 3 RR.</summary>
        public static string Wheel(int index)
        {
            switch (index)
            {
                case 0: return WheelFrontLeft;
                case 1: return WheelFrontRight;
                case 2: return WheelRearLeft;
                case 3: return WheelRearRight;
                default: throw new ArgumentOutOfRangeException(nameof(index), index, "Wheel index is 0..3.");
            }
        }

        /// <summary>Trailing suspension arm node name by rig index: 0 FL, 1 FR, 2 RL, 3 RR.</summary>
        public static string Arm(int index)
        {
            switch (index)
            {
                case 0: return ArmFrontLeft;
                case 1: return ArmFrontRight;
                case 2: return ArmRearLeft;
                case 3: return ArmRearRight;
                default: throw new ArgumentOutOfRangeException(nameof(index), index, "Arm index is 0..3.");
            }
        }

        /// <summary>Shock strut node name by rig index: 0 FL, 1 FR, 2 RL, 3 RR.</summary>
        public static string Shock(int index)
        {
            switch (index)
            {
                case 0: return ShockFrontLeft;
                case 1: return ShockFrontRight;
                case 2: return ShockRearLeft;
                case 3: return ShockRearRight;
                default: throw new ArgumentOutOfRangeException(nameof(index), index, "Shock index is 0..3.");
            }
        }

        /// <summary>Wheel axle row by rig index: 0 front, 1 rear.</summary>
        public static int WheelRow(int index)
        {
            return index / 2;
        }

        private static string Corner(int index, string kind)
        {
            switch (index)
            {
                case 0: return "FL";
                case 1: return "FR";
                case 2: return "RL";
                case 3: return "RR";
                default: throw new ArgumentOutOfRangeException(nameof(index), index, $"{kind} index is 0..3.");
            }
        }
    }
}
