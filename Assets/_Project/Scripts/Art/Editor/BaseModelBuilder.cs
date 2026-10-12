using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's home base per the M2 content contract and the lab's (docs/ARCHITECTURE.md): the habitat <c>Lander</c>
    /// with its belly lift and the crew's lab, <c>RadioTower_L1..L3</c>, Kenji's <c>RoverBay</c> and Bell's
    /// <c>CassetteShelf</c> in Generated/Art/Base: three masses with lit ground between them (M3-19 D9). Meshes only,
    /// each pivoted at its ground-contact centre so it stands on an anchor. The lander also carries friend perches
    /// (<c>FriendSocket_&lt;id&gt;</c>: an empty on top of the perch, +Y up, +Z = the front) and every tower stage
    /// carries Bell's corner and her shelf's anchor. Glowing parts (windows, shelf lights, tower
    /// lamps) are separate renderers so gameplay can brighten the base as it comes back to life, and decades of
    /// neglect sit in removable Weather_Paint / Weather_Rust / Weather_Dust children (see <see cref="Weathering"/>) so
    /// it can be cleaned as it does.
    /// </summary>
    public static class BaseModelBuilder
    {
        public const string LanderName = "Lander";
        public const string TowerPrefix = "RadioTower_L";

        /// <summary>
        /// The habitat lander's height to its roof (dish and aerials aside): every radio tower stage stands at least
        /// 1.25 times taller (M3-19 refinement 4).
        /// </summary>
        public const float LanderRoofTop = LanderMeshes.RoofTop;

        /// <summary>Side of the square plinth every radio tower stage stands on, and its height.</summary>
        public const float TowerPlinthSize = RadioTowerMeshes.PlinthSize;

        public const float TowerPlinthHeight = RadioTowerMeshes.PlinthHeight;

        /// <summary>
        /// Front edge (+Z) of every tower stage's footprint: the plinth plus the plate its service port stands on.
        /// </summary>
        public const float TowerFootprintFront = RadioTowerMeshes.FootprintFront;

        /// <summary>
        /// Where gameplay stands the radio tower: well left of the lander, past its legs and a lit gap, slightly
        /// behind, so the base reads as three masses (M3-19 D9).
        /// </summary>
        public static readonly Vector3 TowerAnchor = new Vector3(-11.5f, 0f, -1.5f);

        /// <summary>
        /// Where gameplay stands Kenji's Rover Bay: right of the lander past a lit gap, slightly behind it, its open
        /// front (+Z) towards the plain.
        /// </summary>
        public static readonly Vector3 WorkshopAnchor = new Vector3(12.5f, 0f, -2f);

        /// <summary>
        /// Kenji's Rover Bay, the bench station since M3-14 (stands on <see cref="WorkshopAnchor"/>).
        /// </summary>
        public const string RoverBayName = "RoverBay";

        /// <summary>
        /// Home's warm points as it wakes (M3-16), each its own dark glow renderer lit through the linear
        /// MaterialPropertyBlock contract so World or Gameplay can light them one by one: the lander's "Windows" (the
        /// front pane) with its children Pane_1 (right) and Pane_2 (back), and each LampSocket_n's Bulb.
        /// </summary>
        public const string PanePrefix = "Pane_";

        /// <summary>
        /// Ines's light table at the lab's survey chart (Lab/Station_Chart/LightTable, M3-07, M3-19) under the big
        /// porthole, at 07's lens height on the turntable. Its space: +X across the top, +Y up its slope, +Z out of it
        /// toward the turntable, the origin at the top's middle. LightTable/LightTableGlow is the lit panel under the
        /// paper, a dark glow renderer (MaterialPropertyBlock contract) and a home light; LightTable/MapSheet is the
        /// paper.
        /// </summary>
        public const string LightTableName = "LightTable";

        public const string LightTableGlowName = "LightTableGlow";

        public const string MapSheetName = "MapSheet";

        /// <summary>
        /// LightTable/ChartPen (M3-07), Kenji's chart pen, the plotter's smaller brass sibling on the light table:
        /// ChartPen/ChartPenCarriage rides across the paper along its local X from 0 to <see cref="ChartDrawWidth"/>,
        /// and its child ChartPenHead (the plotter's own pen head) moves along local Y from 0 to
        /// <see cref="ChartDrawHeight"/>, its tip on the paper, as Plotter/PenCarriage and PenHead do.
        /// </summary>
        public const string ChartPenName = "ChartPen";

        public const string ChartPenCarriageName = "ChartPenCarriage";

        public const string ChartPenHeadName = "ChartPenHead";

        public const float ChartDrawWidth = ChartTableMeshes.DrawWidth;

        public const float ChartDrawHeight = ChartTableMeshes.DrawHeight;

        public const string BulbName = "Bulb";

        /// <summary>
        /// The lander's HOME sign (M3-16), pivoted at its post's foot: built leaning with the years (home asleep);
        /// <see cref="HomeSignUpright"/> is its local rotation set straight once home wakes. Gameplay eases between.
        /// </summary>
        public const string HomeSignName = "HomeSign";

        /// <summary>
        /// The station pads (M3-16, M3-15 §4): StationPad_&lt;radius in cm&gt; for each of these radii (metres),
        /// a worn painted circle with PadLamp_0..5 round its rim, dark glow renderers Gameplay lights while the
        /// station is awake and has something for 07 to do. Gameplay hides the pad (or leaves it dark) otherwise.
        /// </summary>
        public static readonly float[] PadRadii = { 1.6f, 2.4f, 3f };

        /// <summary>
        /// The ground power cables (M3-16), children of the lander: PowerCable_Home, PowerCable_Bay and
        /// PowerCable_Chart (tower L3's, to the leg under the big porthole, M3-19 D8) run from the radio tower's foot
        /// to what each wakes, each with Glow_0..n glass segments in
        /// order from the tower out, dark glow renderers Gameplay lights one after another as power reaches the place.
        /// </summary>
        public const string PowerCablePrefix = "PowerCable_";

        public const string CableGlowPrefix = "Glow_";

        public const string PadPrefix = "StationPad_";

        // Seeds the pads' broken paint, one size apart, so no two pads wear alike.
        private const int PadSeed = 70;

        public const string PadLampPrefix = "PadLamp_";

        public static string PadName(int size)
        {
            return PadPrefix + Mathf.RoundToInt(PadRadii[size] * 100f).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Kenji's pen plotter on the bay's back wall above the turntable (M3-19 D7), with its moving carriage and pen
        /// (see PlotterName).
        /// </summary>
        private static void AddPlotter(ModelNode bay)
        {
            Vector3 place = RoverBayMeshes.PlotterOnWall;
            ModelNode plotter = bay.Add(new ModelNode(PlotterName, place, Quaternion.identity,
                new ModelMesh(RoverBayName + "_" + PlotterName,
                    BlueprintMeshes.Plotter(RoverBayMeshes.BackZ - place.z))));
            Quaternion bedTurn = Place.Rotation(BlueprintMeshes.BedTilt);
            ModelNode carriage = plotter.Add(new ModelNode(PenCarriageName, BlueprintMeshes.CarriageHome, bedTurn,
                new ModelMesh(RoverBayName + "_" + PenCarriageName, BlueprintMeshes.Carriage())));
            carriage.Add(new ModelNode(PenHeadName, Vector3.zero,
                new ModelMesh(RoverBayName + "_" + PenHeadName, BlueprintMeshes.PenHead())));
            plotter.Add(new ModelNode(PlotterSheetName, BlueprintMeshes.Bed.MultiplyPoint3x4(new Vector3(0f, 0f,
                0.001f)), bedTurn, new ModelMesh(RoverBayName + "_" + PlotterSheetName, BlueprintMeshes.BedSheet()),
                ModelMaterial.Palette, false));
            plotter.Add(new ModelNode(PlotterLampName, BlueprintMeshes.PlotterLamp, Quaternion.identity,
                new ModelMesh(RoverBayName + "_" + PlotterLampName, BlueprintMeshes.LampGlass()),
                ModelMaterial.PaletteGlowOff));
        }

        /// <summary>The ground power cables from the tower's foot, each with its glass segments (see above).</summary>
        private static void AddPowerCables(ModelNode lander)
        {
            for (int i = 0; i < PowerCableMeshes.Targets.Length; i++)
            {
                string name = PowerCablePrefix + PowerCableMeshes.Targets[i];
                ModelNode cable = lander.Add(new ModelNode(name, Vector3.zero,
                    new ModelMesh(LanderName + "_" + name, PowerCableMeshes.Sheath(i))));
                List<LowPolyMeshBuilder> glow = PowerCableMeshes.Glow(i);
                for (int k = 0; k < glow.Count; k++)
                {
                    string segment = CableGlowPrefix + k.ToString(CultureInfo.InvariantCulture);
                    cable.Add(new ModelNode(segment, Vector3.zero, Quaternion.identity,
                        new ModelMesh(LanderName + "_" + name + "_" + segment, glow[k]), ModelMaterial.PaletteGlowOff));
                }
            }
        }

        public static ModelNode CreateStationPad(int size)
        {
            float radius = PadRadii[size];
            string name = PadName(size);
            var pad = new ModelNode(name, Vector3.zero,
                new ModelMesh(name, StationPadMeshes.Pad(radius, PadSeed + size)));
            var glass = new ModelMesh(name + "_LampGlass", StationPadMeshes.LampGlass());
            for (int i = 0; i < StationPadMeshes.LampCount; i++)
            {
                pad.Add(new ModelNode(PadLampPrefix + i.ToString(CultureInfo.InvariantCulture),
                    StationPadMeshes.Lamp(radius, i), Quaternion.identity, glass, ModelMaterial.PaletteGlowOff));
            }

            return pad;
        }

        /// <summary>
        /// The belly lift (docs/ARCHITECTURE.md, "Contract: the lab (M3-19)"): BellyLift, an empty on the ground at
        /// the shaft's axis, holds the PlatformDown and PlatformUp stops, the Platform (built jammed near PlatformDown,
        /// <see cref="BellyPlatformLevel"/> its level rotation) with RoverSpot, RimLamps, PadGlow and its own weather,
        /// HatchLeafL/R (slide out along ∓X/±X by <see cref="HatchLeafTravel"/>), the HatchRim and BellyLamp glows,
        /// Cable_L/R (hanging along local -Y from their sheaves: scale local Y by length over
        /// <see cref="BellyCableBuiltLength"/>; Cable_R hidden while jammed) and CableJam with its StitchPoint.
        /// </summary>
        public const string BellyLiftName = "BellyLift";

        public const string PlatformName = "Platform";
        public const string PlatformDownName = "PlatformDown";
        public const string PlatformUpName = "PlatformUp";
        public const string RoverSpotName = "RoverSpot";
        public const string RimLampsName = "RimLamps";
        public const string PadGlowName = "PadGlow";
        public const string HatchLeafLeftName = "HatchLeafL";
        public const string HatchLeafRightName = "HatchLeafR";
        public const string HatchRimName = "HatchRim";
        public const string BellyLampName = "BellyLamp";
        public const string CableLeftName = "Cable_L";
        public const string CableRightName = "Cable_R";
        public const string CableJamName = "CableJam";
        public const string StitchPointName = "StitchPoint";

        public static Quaternion BellyPlatformLevel => Quaternion.identity;

        public const float HatchLeafTravel = BellyLiftMeshes.HatchLeafTravel;

        public static float BellyCableBuiltLength => BellyLiftMeshes.BuiltCableLength;

        /// <summary>
        /// The crew's lab (Lab, origin at the cabin floor's middle = PlatformUp): RingRail (radius
        /// <see cref="LabRingRadius"/>), Station_&lt;LabStation&gt; frames (+Z toward the axis, at 07's lens height)
        /// with their parts, and the CabinLamps glow. Hidden by Gameplay while 07 is outside.
        /// </summary>
        public const string LabName = "Lab";

        public const string RingRailName = "RingRail";
        public const string StationPrefix = "Station_";
        public const string ReaderSlotName = "Slot";
        public const string ReaderReelName = "Reel";
        public const string ReaderTapeName = "Tape";
        public const string RecoveryPanelName = "RecoveryPanel";
        public const string RowPrefix = "Row_";
        public const string PinPrefix = "Pin_";
        public const string SheetPrefix = "Sheet_";
        public const string LampPrefix = "Lamp_";
        public const string SlotPrefix = "Slot_";
        public const string ReadingLampPrefix = "ReadingLamp_";
        public const string CabinLampsName = "CabinLamps";

        public const float LabRingRadius = LabMeshes.RingRadius;

        /// <summary>The board's nodes (TechNodeId order), the keepsake shelf's slots and its crew sections.</summary>
        public const int BoardNodeCount = LabStationMeshes.NodeCount;

        public const int KeepsakeSlotCount = LabStationMeshes.KeepsakeSlotCount;

        public static int ReadingLampCount => LabStationMeshes.Crew.Length;

        /// <summary>
        /// The recovery panel's rows (RecoveryTarget order), as Row_&lt;name&gt; with this many lamp sockets.
        /// </summary>
        public static string RecoveryRowName(int row)
        {
            return RowPrefix + LabStationMeshes.RowNames[row];
        }

        public static int RecoveryRowSockets(int row)
        {
            return LabStationMeshes.RowLamps[row];
        }

        /// <summary>The crew section (ReadingLamp_n) keepsake slot <paramref name="slot"/> belongs to.</summary>
        public static int KeepsakeSection(int slot)
        {
            int first = 0;
            for (int section = 0; section < LabStationMeshes.CrewCubbies.Length; section++)
            {
                first += LabStationMeshes.CrewCubbies[section];
                if (slot < first)
                {
                    return section;
                }
            }

            throw new System.ArgumentOutOfRangeException(nameof(slot), slot, "No such keepsake slot.");
        }

        /// <summary>
        /// The big porthole's glass (BigPorthole, a glow) and its lamp ring (PortholeLamps/Lamp_n, n from 0).
        /// </summary>
        public const string BigPortholeName = "BigPorthole";

        public const string PortholeLampsName = "PortholeLamps";

        public const int PortholeLampCount = LanderMeshes.PortholeLampCount;

        /// <summary>
        /// The hand-off pulse (DecodeHandOff): HandOffPath/Point_n empties from the reader's slot to the bay's back
        /// wall, and HandOffCable/Glow_n, the glass beads it lights along the way, in the same order.
        /// </summary>
        public const string HandOffPathName = "HandOffPath";

        public const string HandOffPointPrefix = "Point_";

        public const string HandOffCableName = "HandOffCable";

        public static Quaternion HomeSignUpright => Place.Rotation(LanderMeshes.HomeSignUpright);

        /// <summary>
        /// The bay's roll-up shutter (M3-16 dormant look), pivoted at its roller: local Y scale 1 = down (the bay
        /// asleep), <see cref="ShutterRolledScale"/> = rolled up into its box (tower L2 has powered it). Gameplay eases
        /// between the two.
        /// </summary>
        public const string ShutterName = "Shutter";

        /// <summary>
        /// Kenji's blueprints (M3-16 P7): RoverBay/Blueprints, the cord of pegs along his bench's pegboard, holds one
        /// sheet per kit, Blueprint_&lt;RoverAbility name&gt; (HoverJump, CargoCradle, WarmHeadlamp, BoostCoils,
        /// CellPack). Only Hover-Jump's hangs there at the start (Kenji left it); Gameplay shows each other one once the
        /// plotter has drawn it.
        /// </summary>
        public const string BlueprintsName = "Blueprints";

        public const string BlueprintPrefix = "Blueprint_";

        /// <summary>
        /// Kenji's pen plotter beside his bench (M3-16 P7): it draws a kit's sheet when 07 brings its tube home.
        /// Plotter/PenCarriage rides across the tilted bed along its local X from 0 to <see cref="PlotterDrawWidth"/>;
        /// its child PenHead moves along local Y from 0 to <see cref="PlotterDrawHeight"/>; Plotter/PlotterSheet
        /// (hidden) is the sheet on the bed while it is drawn; Plotter/PlotterLamp is a dark glow renderer, its ready
        /// light.
        /// </summary>
        public const string PlotterName = "Plotter";

        public const string PenCarriageName = "PenCarriage";

        public const string PenHeadName = "PenHead";

        public const string PlotterSheetName = "PlotterSheet";

        public const string PlotterLampName = "PlotterLamp";

        public const float PlotterDrawWidth = BlueprintMeshes.DrawWidth;

        public const float PlotterDrawHeight = BlueprintMeshes.DrawHeight;

        /// <summary>The Shutter node's local Y scale when rolled up.</summary>
        public const float ShutterRolledScale = 0.05f;

        /// <summary>
        /// Inside the Rover Bay, in bay space: from the top of its floor up to its eaves, between its posts' centres
        /// (side to side and back to front). Its arms fold away within it.
        /// </summary>
        public static Bounds RoverBayInside
        {
            get
            {
                var inside = new Bounds();
                inside.SetMinMax(new Vector3(-RoverBayMeshes.HalfWidth, RoverBayMeshes.PadTop, RoverBayMeshes.BackZ),
                    new Vector3(RoverBayMeshes.HalfWidth, RoverBayMeshes.EaveHeight, RoverBayMeshes.FrontZ));
                return inside;
            }
        }

        public const string CassetteShelfName = "CassetteShelf";

        /// <summary>
        /// Where Bell stands, in tower space (the same on every stage): left of the tower, her 2.5 m dance circle
        /// clear of the plinth and of the upgrade pad 3.4 m in front of the tower.
        /// </summary>
        public static readonly Vector3 BellCorner = new Vector3(-4.9f, 0f, 0.4f);

        /// <summary>Bell's heading at home: her dial looks across the pad at the lander's porch.</summary>
        public const float BellCornerYaw = 74f;

        /// <summary>Where Bell's tape rack stands, in tower space: at her right, just outside her circle.</summary>
        public static readonly Vector3 CassetteShelfAnchor = new Vector3(-4.6f, 0f, -2.75f);

        /// <summary>The rack's heading: turned towards the lander, a little more towards the front than Bell.</summary>
        public const float CassetteShelfYaw = 60f;

        // How hard the decades worked each piece of home over (Weathering.Weather): the lander and the tower stood
        // out in it longest and biggest, the lander's paint breaking along its own riveted panels; the furniture
        // weathered under them; the belly platform has sat on the dust under the stage, drifted over from the open
        // side and dusted only from above.
        private static readonly WeatherProfile LanderWeather = new WeatherProfile(seed: 11, lift: 0.006f,
            panelWidth: 1.15f, panelHeight: 1.25f, paintWear: 1f, mismatched: true, runsPerMetre: 3f,
            rustHeight: 2.4f, metalRust: 0.8f, paintRust: 0.45f, tide: 2.1f, groundDust: 0.95f, topDust: 0.9f);

        private static readonly WeatherProfile PlatformWeather = new WeatherProfile(seed: 71, lift: 0.006f,
            panelWidth: 0.6f, panelHeight: 0.6f, paintWear: 1f, mismatched: true, runsPerMetre: 2.2f, rustHeight: 0f,
            metalRust: 0f, paintRust: 0f, tide: 0f, groundDust: 0f, topDust: 0.8f);

        private static readonly WeatherProfile RoverBayWeather = new WeatherProfile(seed: 41, lift: 0.006f,
            panelWidth: 0.8f, panelHeight: 0.7f, paintWear: 1f, mismatched: true, runsPerMetre: 2.2f, rustHeight: 1.1f,
            metalRust: 0.7f, paintRust: 0.3f, tide: 1f, groundDust: 0.72f, topDust: 0.75f);

        // The bay's moving parts: the turntable under 07's wheels for decades (dust on its top, tired panels), the
        // arms' links and tips (tones and dust, rust at their joints), the floor arm rusting in its pit.
        private static readonly WeatherProfile TurntableWeather = new WeatherProfile(seed: 43, lift: 0.004f,
            panelWidth: 0.7f, panelHeight: 0.7f, paintWear: 1f, mismatched: false, runsPerMetre: 0f, rustHeight: 0f,
            metalRust: 0f, paintRust: 0f, tide: 0f, groundDust: 0f, topDust: 0.55f);

        private static readonly WeatherProfile ArmWeather = new WeatherProfile(seed: 47, lift: 0.004f,
            panelWidth: 0.4f, panelHeight: 0.4f, paintWear: 0.8f, mismatched: false, runsPerMetre: 0f,
            rustHeight: 0f, metalRust: 0f, paintRust: 0f, tide: 0f, groundDust: 0f, topDust: 0.35f);

        private const float UpperThickness = 0.16f;
        private const float LowerThickness = 0.13f;
        private const float PitRust = 0.45f;
        private const float PitDust = 0.35f;

        // The floor arm's coats sit this close over it: it slides up through a snug hole.
        private const float PitCoatLift = Weathering.RustLift * 0.25f;

        private static readonly WeatherProfile CassetteShelfWeather = new WeatherProfile(seed: 53, lift: 0.006f,
            panelWidth: 0.3f, panelHeight: 0.3f, paintWear: 1f, mismatched: true, runsPerMetre: 2.4f, rustHeight: 0.4f,
            metalRust: 0.5f, paintRust: 0.3f, tide: 0.45f, groundDust: 0.65f, topDust: 0.7f);

        [MoonBuilder("Art/Base", 140)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            Material glowOff = PaletteAssetBuilder.LoadGlowOffMaterial();
            Material weather = PaletteAssetBuilder.LoadWeatherMaterial();
            var grounded = new List<ModelNode>
            {
                CreateLander(), CreateRoverBay(), CreateCassetteShelf(),
            };
            for (int level = RadioTowerMeshes.MinLevel; level <= RadioTowerMeshes.MaxLevel; level++)
            {
                grounded.Add(CreateTower(level));
            }

            foreach (ModelNode model in grounded)
            {
                ModelPrefabWriter.Write(GroundOcclusion.Ground(model), ArtPaths.BaseFolder, material, glowOff, weather);
            }

            for (int i = 0; i < PadRadii.Length; i++)
            {
                ModelPrefabWriter.Write(CreateStationPad(i), ArtPaths.BaseFolder, material, glowOff);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>The lock's walls under DockGlow: DockWall_0..3 (front, right, back, left), each with a Strip.</summary>
        public const string DockWallPrefix = "DockWall_";

        /// <summary>A dock wall's lamp band segments along its cap, Strip_0..2 (dark glow renderers lit per cell).</summary>
        public const string DockWallStripPrefix = "Strip_";

        /// <summary>The rim lamp rings under DockGlow, DockRim_0 (outer) .. DockRim_2 (inner).</summary>
        public const string DockRimPrefix = "DockRim_";

        /// <summary>
        /// The dock's lock (docs/design/feel-pass.md §7.1): four low walls standing raised in their floor slots round
        /// the pad (Gameplay sinks them until 07 docks), each with a dark lamp strip along its top, and three rings
        /// of rim lamps that chase toward the centre as 07 rolls on. Children of DockGlow, so the lander's own node
        /// order stays; no colliders, so they never touch 07's wheels.
        /// </summary>
        private static void AddDockLock(ModelNode dockGlow)
        {
            for (int i = 0; i < ChargingDockMeshes.WallCount; i++)
            {
                string index = i.ToString(CultureInfo.InvariantCulture);
                ModelNode wall = dockGlow.Add(new ModelNode(DockWallPrefix + index,
                    ChargingDockMeshes.WallPosition(i), ChargingDockMeshes.WallRotation(i),
                    new ModelMesh(LanderName + "_" + DockWallPrefix + index, ChargingDockMeshes.Wall(i))));
                for (int s = 0; s < ChargingDockMeshes.StripSegments; s++)
                {
                    string segment = s.ToString(CultureInfo.InvariantCulture);
                    wall.Add(new ModelNode(DockWallStripPrefix + segment, Vector3.zero, Quaternion.identity,
                        new ModelMesh(LanderName + "_DockWallStrip_" + index + "_" + segment,
                            ChargingDockMeshes.WallStrip(i, s)), ModelMaterial.PaletteGlowOff));
                }
            }

            for (int ring = 0; ring < ChargingDockMeshes.RimRings; ring++)
            {
                string index = ring.ToString(CultureInfo.InvariantCulture);
                dockGlow.Add(new ModelNode(DockRimPrefix + index, Vector3.zero, Quaternion.identity,
                    new ModelMesh(LanderName + "_" + DockRimPrefix + index, ChargingDockMeshes.RimRing(ring)),
                    ModelMaterial.PaletteGlowOff));
            }
        }

        public static ModelNode CreateLander()
        {
            LowPolyMeshBuilder hull = LanderMeshes.Hull();
            BellyLiftMeshes.Frame(hull);
            var lander = new ModelNode(LanderName, Vector3.zero, new ModelMesh(LanderName, hull));
            ModelNode windows = lander.Add(new ModelNode("Windows", Vector3.zero, Quaternion.identity,
                new ModelMesh(LanderName + "_Windows", LanderMeshes.Pane(0)), ModelMaterial.PaletteGlowOff));
            for (int i = 1; i < LanderMeshes.PaneCount; i++)
            {
                windows.Add(new ModelNode(PanePrefix + i.ToString(CultureInfo.InvariantCulture), Vector3.zero,
                    Quaternion.identity, new ModelMesh(LanderName + "_Pane" + i.ToString(CultureInfo.InvariantCulture),
                        LanderMeshes.Pane(i)), ModelMaterial.PaletteGlowOff));
            }

            lander.Add(new ModelNode("TowerAnchor", TowerAnchor));
            var bulb = new ModelMesh(LanderName + "_Bulb", LanderMeshes.Bulb());
            for (int i = 0; i < LanderMeshes.LampCount; i++)
            {
                ModelNode socket = lander.Add(new ModelNode("LampSocket_" + i.ToString(CultureInfo.InvariantCulture),
                    LanderMeshes.LampPosition(i)));
                socket.Add(new ModelNode(BulbName, Vector3.zero, Quaternion.identity, bulb,
                    ModelMaterial.PaletteGlowOff));
            }

            lander.Add(new ModelNode("FriendSocket_tilly", LanderMeshes.TillyPerch));
            lander.Add(new ModelNode("WorkshopAnchor", WorkshopAnchor));
            lander.Add(new ModelNode("DockAnchor", ChargingDockMeshes.Anchor,
                Place.Rotation(ChargingDockMeshes.AnchorEuler)));
            ModelNode dockGlow = lander.Add(new ModelNode("DockGlow", Vector3.zero, Quaternion.identity,
                new ModelMesh(LanderName + "_DockGlow", ChargingDockMeshes.Glow()), ModelMaterial.PaletteGlowOff));
            AddDockLock(dockGlow);
            lander.Add(new ModelNode(HomeSignName, LanderMeshes.HomeSignFoot,
                Place.Rotation(LanderMeshes.HomeSignLean),
                new ModelMesh(LanderName + "_" + HomeSignName, LanderMeshes.HomeSign())));
            AddPowerCables(lander);
            AddBellyLift(lander);
            AddBigPorthole(lander);
            AddLab(lander);
            AddHandOff(lander);
            Weathering.Weather(lander, LanderName, hull, LanderWeather, LanderMeshes.Rust(), LanderMeshes.Drifts());
            return lander;
        }

        /// <summary>
        /// The belly lift's moving and glowing parts (see <see cref="BellyLiftName"/>): the platform jammed on its
        /// stops with its own weather, the two hatch leaves shut, the hatch rim and belly lamp, the two cables (the
        /// right one hidden, snapped) and the jam.
        /// </summary>
        private static void AddBellyLift(ModelNode lander)
        {
            string prefix = LanderName + "_" + BellyLiftName;
            ModelNode lift = lander.Add(new ModelNode(BellyLiftName, Vector3.zero));
            lift.Add(new ModelNode(PlatformDownName, Vector3.up * BellyLiftMeshes.DownY));
            lift.Add(new ModelNode(PlatformUpName, Vector3.up * BellyLiftMeshes.UpY));
            LowPolyMeshBuilder deck = BellyLiftMeshes.Platform();
            ModelNode platform = lift.Add(new ModelNode(PlatformName, BellyLiftMeshes.JammedCentre,
                BellyLiftMeshes.JamTilt, new ModelMesh(prefix + "_" + PlatformName, deck)));
            platform.Add(new ModelNode(RoverSpotName, Vector3.zero, Place.Rotation(new Vector3(0f, 180f, 0f))));
            platform.Add(new ModelNode(RimLampsName, Vector3.zero, Quaternion.identity,
                new ModelMesh(prefix + "_" + RimLampsName, BellyLiftMeshes.RimLamps()), ModelMaterial.PaletteGlowOff));
            platform.Add(new ModelNode(PadGlowName, Vector3.zero, Quaternion.identity,
                new ModelMesh(prefix + "_" + PadGlowName, BellyLiftMeshes.PadGlow()), ModelMaterial.PaletteGlowOff));
            Weathering.Weather(platform, prefix + "_" + PlatformName, deck, PlatformWeather,
                BellyLiftMeshes.PlatformRust(), BellyLiftMeshes.PlatformDrift());
            lift.Add(new ModelNode(HatchLeafLeftName, Vector3.zero,
                new ModelMesh(prefix + "_" + HatchLeafLeftName, BellyLiftMeshes.HatchLeaf(-1))));
            lift.Add(new ModelNode(HatchLeafRightName, Vector3.zero,
                new ModelMesh(prefix + "_" + HatchLeafRightName, BellyLiftMeshes.HatchLeaf(1))));
            lift.Add(new ModelNode(HatchRimName, Vector3.zero, Quaternion.identity,
                new ModelMesh(prefix + "_" + HatchRimName, BellyLiftMeshes.HatchRim()), ModelMaterial.PaletteGlowOff));
            lift.Add(new ModelNode(BellyLampName, Vector3.zero, Quaternion.identity,
                new ModelMesh(prefix + "_" + BellyLampName, BellyLiftMeshes.BellyLamp()),
                ModelMaterial.PaletteGlowOff));
            var cable = new ModelMesh(prefix + "_Cable", BellyLiftMeshes.Cable());
            lift.Add(new ModelNode(CableLeftName, BellyLiftMeshes.CableTop(-1), cable));
            lift.Add(new ModelNode(CableRightName, BellyLiftMeshes.CableTop(1), Quaternion.identity, cable,
                ModelMaterial.Palette, false));
            ModelNode jam = lift.Add(new ModelNode(CableJamName, Vector3.zero,
                new ModelMesh(prefix + "_" + CableJamName, BellyLiftMeshes.Jam())));
            jam.Add(new ModelNode(StitchPointName, BellyLiftMeshes.StitchPoint));
        }

        /// <summary>The big porthole's glass and the ring of lamps round it, one per known tech node.</summary>
        private static void AddBigPorthole(ModelNode lander)
        {
            lander.Add(new ModelNode(BigPortholeName, Vector3.zero, Quaternion.identity,
                new ModelMesh(LanderName + "_" + BigPortholeName, LanderMeshes.BigPorthole()),
                ModelMaterial.PaletteGlowOff));
            ModelNode lamps = lander.Add(new ModelNode(PortholeLampsName, Vector3.zero));
            var glass = new ModelMesh(LanderName + "_PortholeLamp", LanderMeshes.PortholeLampGlass());
            for (int i = 0; i < LanderMeshes.PortholeLampCount; i++)
            {
                lamps.Add(new ModelNode(LampPrefix + i.ToString(CultureInfo.InvariantCulture),
                    LanderMeshes.PortholeLamp(i), Quaternion.identity, glass, ModelMaterial.PaletteGlowOff));
            }
        }

        /// <summary>
        /// The crew's lab (see <see cref="LabName"/>): the room, the camera's ring rail, the four stations on their
        /// frames and the wall lamps.
        /// </summary>
        private static void AddLab(ModelNode lander)
        {
            string prefix = LanderName + "_" + LabName;
            ModelNode lab = lander.Add(new ModelNode(LabName, Vector3.up * LanderMeshes.FloorY,
                new ModelMesh(prefix, LabMeshes.Shell())));
            lab.Add(new ModelNode(RingRailName, Vector3.up * LabMeshes.RingHeight,
                new ModelMesh(prefix + "_" + RingRailName, LabMeshes.RingRail())));
            lab.Add(new ModelNode(CabinLampsName, Vector3.zero, Quaternion.identity,
                new ModelMesh(prefix + "_" + CabinLampsName, LabMeshes.CabinLamps()), ModelMaterial.PaletteGlowOff));
            AddChart(Station(lab, 0, LabStationMeshes.ChartStation()));
            AddReader(Station(lab, 1, LabStationMeshes.Reader()));
            AddBoard(Station(lab, 2, LabStationMeshes.Board()));
            AddKeepsakes(Station(lab, 3, LabStationMeshes.Keepsakes()));
        }

        private static ModelNode Station(ModelNode lab, int index, LowPolyMeshBuilder mesh)
        {
            string name = StationPrefix + LabMeshes.StationNames[index];
            return lab.Add(new ModelNode(name, LabMeshes.StationPosition(index), LabMeshes.StationRotation(index),
                new ModelMesh(LanderName + "_" + name, mesh)));
        }

        /// <summary>
        /// Ines's light table at the survey chart (M3-07): the table on its legs, its lit panel as a dark glow
        /// renderer, the chart paper on it and Kenji's chart pen.
        /// </summary>
        private static void AddChart(ModelNode station)
        {
            string prefix = LanderName + "_" + LightTableName;
            Vector3 centre = LabStationMeshes.TableCentre;
            ModelNode table = station.Add(new ModelNode(LightTableName, centre, ChartTableMeshes.Rotation,
                new ModelMesh(prefix, ChartTableMeshes.Table(centre, LabStationMeshes.FloorY))));
            table.Add(new ModelNode(LightTableGlowName, Vector3.zero, Quaternion.identity,
                new ModelMesh(prefix + "_Glow", ChartTableMeshes.Glow()), ModelMaterial.PaletteGlowOff));
            table.Add(new ModelNode(MapSheetName, Vector3.zero, new ModelMesh(prefix + "_" + MapSheetName,
                ChartTableMeshes.Sheet())));
            ModelNode pen = table.Add(new ModelNode(ChartPenName, Vector3.zero,
                new ModelMesh(prefix + "_" + ChartPenName, ChartTableMeshes.Gantry())));
            ModelNode carriage = pen.Add(new ModelNode(ChartPenCarriageName, ChartTableMeshes.CarriageHome,
                new ModelMesh(prefix + "_" + ChartPenCarriageName, ChartTableMeshes.Carriage())));
            carriage.Add(new ModelNode(ChartPenHeadName, Vector3.zero,
                new ModelMesh(prefix + "_" + ChartPenHeadName, BlueprintMeshes.PenHead())));
        }

        /// <summary>The reader's slot (where the beam feeds), its reel and the paper tape it runs out.</summary>
        private static void AddReader(ModelNode station)
        {
            string prefix = LanderName + "_Reader";
            station.Add(new ModelNode(ReaderSlotName, LabStationMeshes.ReaderSlot));
            station.Add(new ModelNode(ReaderReelName, LabStationMeshes.ReaderReel,
                new ModelMesh(prefix + "_" + ReaderReelName, LabStationMeshes.Reel())));
            station.Add(new ModelNode(ReaderTapeName, LabStationMeshes.TapeStart,
                new ModelMesh(prefix + "_" + ReaderTapeName, LabStationMeshes.Tape())));
        }

        /// <summary>
        /// Kenji's tech board: a pin per node (always there), its sheet (hidden until decoded) and its lamp (dark until
        /// built), and the recovery panel's rows of dark lamps.
        /// </summary>
        private static void AddBoard(ModelNode station)
        {
            string prefix = LanderName + "_Board";
            var pin = new ModelMesh(prefix + "_Pin", LabStationMeshes.Pin());
            var lamp = new ModelMesh(prefix + "_Lamp", LabStationMeshes.LampGlass(LabStationMeshes.NodeLampRadius));
            for (int n = 0; n < LabStationMeshes.NodeCount; n++)
            {
                string index = n.ToString(CultureInfo.InvariantCulture);
                Vector3 at = LabStationMeshes.PinPosition(n);
                station.Add(new ModelNode(PinPrefix + index, at, pin));
                station.Add(new ModelNode(SheetPrefix + index, at, Quaternion.identity,
                    new ModelMesh(prefix + "_" + SheetPrefix + index, LabStationMeshes.Sheet(n)), ModelMaterial.Palette,
                    false));
                station.Add(new ModelNode(LampPrefix + index, LabStationMeshes.LampPosition(n), Quaternion.identity,
                    lamp, ModelMaterial.PaletteGlowOff));
            }

            ModelNode panel = station.Add(new ModelNode(RecoveryPanelName, LabStationMeshes.PanelCentre));
            var small = new ModelMesh(prefix + "_RowLamp", LabStationMeshes.LampGlass(LabStationMeshes.RowLampRadius));
            var peak = new ModelMesh(prefix + "_PeakLamp", LabStationMeshes.LampGlass(LabStationMeshes.PeakLampRadius));
            for (int row = 0; row < LabStationMeshes.RowNames.Length; row++)
            {
                ModelNode rowNode = panel.Add(new ModelNode(RowPrefix + LabStationMeshes.RowNames[row],
                    LabStationMeshes.RowOrigin(row) - LabStationMeshes.PanelCentre));
                for (int i = 0; i < LabStationMeshes.RowLamps[row]; i++)
                {
                    bool isPeak = LabStationMeshes.IsPeakLamp(row, i);
                    rowNode.Add(new ModelNode(LampPrefix + i.ToString(CultureInfo.InvariantCulture),
                        LabStationMeshes.RowLampPosition(row, i) - LabStationMeshes.RowOrigin(row), Quaternion.identity,
                        isPeak ? peak : small, ModelMaterial.PaletteGlowOff));
                }
            }
        }

        /// <summary>The keepsake shelf's slots (a cubby floor each) and its four sections' reading lamps.</summary>
        private static void AddKeepsakes(ModelNode station)
        {
            for (int i = 0; i < LabStationMeshes.KeepsakeSlotCount; i++)
            {
                station.Add(new ModelNode(SlotPrefix + i.ToString(CultureInfo.InvariantCulture),
                    LabStationMeshes.KeepsakeSlot(i)));
            }

            var bulb = new ModelMesh(LanderName + "_ReadingLamp", LabStationMeshes.ReadingBulb());
            for (int i = 0; i < LabStationMeshes.Crew.Length; i++)
            {
                station.Add(new ModelNode(ReadingLampPrefix + i.ToString(CultureInfo.InvariantCulture),
                    LabStationMeshes.ReadingLamp(i), Quaternion.identity, bulb, ModelMaterial.PaletteGlowOff));
            }
        }

        /// <summary>
        /// The hand-off pulse's path and the glass beads it lights (see <see cref="HandOffPathName"/>).
        /// </summary>
        private static void AddHandOff(ModelNode lander)
        {
            ModelNode path = lander.Add(new ModelNode(HandOffPathName, Vector3.zero));
            IReadOnlyList<Vector3> points = HandOffMeshes.Points();
            for (int i = 0; i < points.Count; i++)
            {
                path.Add(new ModelNode(HandOffPointPrefix + i.ToString(CultureInfo.InvariantCulture), points[i]));
            }

            ModelNode cable = lander.Add(new ModelNode(HandOffCableName, Vector3.zero,
                new ModelMesh(LanderName + "_" + HandOffCableName, HandOffMeshes.Sheath())));
            List<LowPolyMeshBuilder> beads = HandOffMeshes.Beads();
            for (int k = 0; k < beads.Count; k++)
            {
                string segment = CableGlowPrefix + k.ToString(CultureInfo.InvariantCulture);
                cable.Add(new ModelNode(segment, Vector3.zero, Quaternion.identity,
                    new ModelMesh(LanderName + "_" + HandOffCableName + "_" + segment, beads[k]),
                    ModelMaterial.PaletteGlowOff));
            }
        }

        /// <summary>
        /// Kenji's Rover Bay (docs/ARCHITECTURE.md, "Contract: Kenji's Rover Bay"): root on the ground at the bay
        /// centre (stands on WorkshopAnchor, +Z = the open front 07 drives in from). A Turntable disc 07 parks on (its
        /// +Z = 07's heading driving in; turn it about local Y and 07 turns with it), three Arm_n gantry arms on the
        /// crane rail (Yaw turns about local Y; Upper / Lower / Tip pitch about local X, each link hanging along its
        /// parent's -Y; SparkSocket on each tip's welding nozzle, as deep below Tip as the tip is long) folded along
        /// the rail at rest, the FloorArm whose FloorLift rises along local Y through the turntable's hole carrying
        /// FloorTip, the HopperMouth 07's beam feeds, two work Lamp_n glasses and the BaySign on the glow-off
        /// material, and the weather skins (the moving parts carry their own).
        /// </summary>
        public static ModelNode CreateRoverBay()
        {
            LowPolyMeshBuilder frame = RoverBayMeshes.Frame();
            var bay = new ModelNode(RoverBayName, Vector3.zero, new ModelMesh(RoverBayName, frame));
            LowPolyMeshBuilder disc = RoverBayMeshes.Turntable();
            ModelNode turntable = bay.Add(new ModelNode("Turntable", RoverBayMeshes.TurntableCentre,
                Place.Rotation(new Vector3(0f, 180f, 0f)), new ModelMesh(RoverBayName + "_Turntable", disc)));
            Weathering.Weather(turntable, RoverBayName + "_Turntable", disc, TurntableWeather, null, null);
            AddArms(bay);
            AddFloorArm(bay);
            bay.Add(new ModelNode("HopperMouth", RoverBayMeshes.HopperMouth,
                Place.Rotation(RoverBayMeshes.HopperFacing)));
            ModelNode board = bay.Add(new ModelNode(BlueprintsName, RoverBayMeshes.BlueprintsOnWall,
                Place.Rotation(RoverBayMeshes.BlueprintsEuler)));
            for (int i = 0; i < BlueprintMeshes.Kits.Length; i++)
            {
                string kit = BlueprintMeshes.Kits[i];
                board.Add(new ModelNode(BlueprintPrefix + kit, BlueprintMeshes.Slot(i), Quaternion.identity,
                    new ModelMesh(RoverBayName + "_" + BlueprintPrefix + kit, BlueprintMeshes.Sheet(kit)),
                    ModelMaterial.Palette, i == 0));
            }

            AddPlotter(bay);
            bay.Add(new ModelNode(ShutterName, RoverBayMeshes.ShutterRoller,
                new ModelMesh(RoverBayName + "_" + ShutterName, RoverBayMeshes.Shutter())));
            bay.Add(new ModelNode("BaySign", Vector3.zero, Quaternion.identity,
                new ModelMesh(RoverBayName + "_BaySign", RoverBayMeshes.SignGlow()), ModelMaterial.PaletteGlowOff));
            var glass = new ModelMesh(RoverBayName + "_LampGlass", RoverBayMeshes.LampGlassMesh());
            for (int i = 0; i < RoverBayMeshes.LampCount; i++)
            {
                bay.Add(new ModelNode("Lamp_" + i.ToString(CultureInfo.InvariantCulture), RoverBayMeshes.LampGlass(i),
                    Place.Rotation(RoverBayMeshes.LampEuler), glass, ModelMaterial.PaletteGlowOff));
            }

            Weathering.Weather(bay, RoverBayName, frame, RoverBayWeather, RoverBayMeshes.Rust(),
                RoverBayMeshes.Drifts());
            return bay;
        }

        /// <summary>
        /// The bay's three gantry arms: a trolley on the rail (Arm_n, turned towards the turntable), the Yaw turret
        /// turned to fold the arm along its stretch of rail, and the links in their rest pose. The links' meshes and
        /// weather skins are shared by the three arms.
        /// </summary>
        private static void AddArms(ModelNode bay)
        {
            var mount = new ModelMesh(RoverBayName + "_ArmMount", RoverBayMeshes.ShoulderMount());
            var turret = new ModelMesh(RoverBayName + "_ArmYaw", RoverBayMeshes.YawJoint());
            LowPolyMeshBuilder upperLink = RoverBayMeshes.Link(RoverBayMeshes.UpperLength, UpperThickness);
            LowPolyMeshBuilder lowerLink = RoverBayMeshes.Link(RoverBayMeshes.LowerLength, LowerThickness);
            LowPolyMeshBuilder tipHead = RoverBayMeshes.Tip();
            var upper = new ModelMesh(RoverBayName + "_ArmUpper", upperLink);
            var lower = new ModelMesh(RoverBayName + "_ArmLower", lowerLink);
            var tip = new ModelMesh(RoverBayName + "_ArmTip", tipHead);
            IReadOnlyList<ModelMesh> upperSkins = Weathering.Skins(upper.Name, upperLink, ArmWeather, null,
                RoverBayMeshes.LinkRust(RoverBayMeshes.UpperLength, UpperThickness), null);
            IReadOnlyList<ModelMesh> lowerSkins = Weathering.Skins(lower.Name, lowerLink, ArmWeather, null,
                RoverBayMeshes.LinkRust(RoverBayMeshes.LowerLength, LowerThickness), null);
            IReadOnlyList<ModelMesh> tipSkins = Weathering.Skins(tip.Name, tipHead, ArmWeather, null, null, null);
            Vector3 rest = RoverBayMeshes.ArmRest;
            for (int i = 0; i < RoverBayMeshes.ArmCount; i++)
            {
                string index = i.ToString(CultureInfo.InvariantCulture);
                ModelNode arm = bay.Add(new ModelNode("Arm_" + index, RoverBayMeshes.Shoulder(i),
                    Place.Rotation(new Vector3(0f, RoverBayMeshes.ShoulderYaw(i), 0f)), mount));
                ModelNode yaw = arm.Add(new ModelNode("Yaw", Vector3.zero,
                    Place.Rotation(new Vector3(0f, RoverBayMeshes.FoldYaw(i), 0f)), turret));
                ModelNode upperNode = yaw.Add(new ModelNode("Upper", Vector3.zero,
                    Place.Rotation(new Vector3(rest.x, 0f, 0f)), upper));
                Weathering.Wear(upperNode, upperSkins);
                ModelNode lowerNode = upperNode.Add(new ModelNode("Lower", Vector3.down * RoverBayMeshes.UpperLength,
                    Place.Rotation(new Vector3(rest.y, 0f, 0f)), lower));
                Weathering.Wear(lowerNode, lowerSkins);
                ModelNode tipNode = lowerNode.Add(new ModelNode("Tip", Vector3.down * RoverBayMeshes.LowerLength,
                    Place.Rotation(new Vector3(rest.z, 0f, 0f)), tip));
                Weathering.Wear(tipNode, tipSkins);
                tipNode.Add(new ModelNode("SparkSocket", new Vector3(0f, -RoverBayMeshes.TipLength, 0.06f),
                    Place.Rotation(new Vector3(90f, 0f, 0f))));
            }
        }

        /// <summary>
        /// The floor arm under the turntable's centre: FloorLift (rises along local Y) resting with its cradle,
        /// FloorTip, just under the turntable's top in its hole, both rusted and dusted from decades in the pit.
        /// </summary>
        private static void AddFloorArm(ModelNode bay)
        {
            ModelNode floorArm = bay.Add(new ModelNode("FloorArm", RoverBayMeshes.FloorArmBase));
            LowPolyMeshBuilder column = RoverBayMeshes.FloorLiftMesh();
            LowPolyMeshBuilder cradle = RoverBayMeshes.FloorTipMesh();
            float liftTop = RoverBayMeshes.FloorTipRest - RoverBayMeshes.FloorTipOffset;
            ModelNode lift = floorArm.Add(new ModelNode("FloorLift", Vector3.up * liftTop,
                new ModelMesh(RoverBayName + "_FloorLift", column)));
            lift.Add(Weathering.CoatNode(Weathering.RustName, Weathering.Coat(RoverBayName + "_FloorLift",
                Weathering.RustName, column, Weathering.DarkRust, PitRust, PitCoatLift)));
            ModelNode tip = lift.Add(new ModelNode("FloorTip", Vector3.up * RoverBayMeshes.FloorTipOffset,
                new ModelMesh(RoverBayName + "_FloorTip", cradle)));
            tip.Add(Weathering.CoatNode(Weathering.DustName, Weathering.Coat(RoverBayName + "_FloorTip",
                Weathering.DustName, cradle, Palette.GetSurface(PaletteSwatch.CakedDust), PitDust,
                PitCoatLift)));
        }

        /// <summary>
        /// Bell's tape rack: root on the ground at the centre of its base (+Z = front), Slot_0..7 empties on the
        /// cubby floors (+Y up, +Z = label facing) where a tape stands on its bottom edge.
        /// </summary>
        public static ModelNode CreateCassetteShelf()
        {
            LowPolyMeshBuilder rack = CassetteShelfMeshes.Rack();
            var shelf = new ModelNode(CassetteShelfName, Vector3.zero, new ModelMesh(CassetteShelfName, rack));
            for (int i = 0; i < CassetteShelfMeshes.SlotCount; i++)
            {
                shelf.Add(new ModelNode("Slot_" + i.ToString(CultureInfo.InvariantCulture),
                    CassetteShelfMeshes.SlotPosition(i)));
            }

            Weathering.Weather(shelf, CassetteShelfName, rack, CassetteShelfWeather, null, null);
            return shelf;
        }

        /// <summary>
        /// A tower stage's lamps (feel-pass §8.2): Lights is ring 0 (the cabinet lamp) and Lights/Ring_1..3 the rings
        /// above it, bottom to top, so gameplay lights one ring per Act as power climbs. Each is a glow renderer on
        /// the MaterialPropertyBlock contract. OfflinePlaque by the hopper carries OfflineLamp (M_LowPolyGlowOff),
        /// dark until gameplay lights it while the stage is down.
        /// </summary>
        public const string TowerLightsName = "Lights";
        public const string TowerLightRingPrefix = "Ring_";
        public const int TowerLightRings = RadioTowerMeshes.LightRings;
        public const string OfflinePlaqueName = "OfflinePlaque";
        public const string OfflineLampName = "OfflineLamp";

        /// <summary>
        /// Radio tower stage <paramref name="level"/> (1..3). Every stage has the same rover-height service port facing
        /// the upgrade pad: a HopperMouth empty (+Z out of the mouth) and a ServiceHatch door pivoted on its left hinge
        /// (swing it about local Y to open).
        /// </summary>
        public static ModelNode CreateTower(int level)
        {
            string name = TowerPrefix + level.ToString(CultureInfo.InvariantCulture);
            LowPolyMeshBuilder structure = RadioTowerMeshes.Structure(level);
            var tower = new ModelNode(name, Vector3.zero, new ModelMesh(name, structure));
            ModelNode lights = tower.Add(new ModelNode(TowerLightsName, Vector3.zero,
                new ModelMesh(name + "_Lights", RadioTowerMeshes.Lights(level, 0))));
            for (int ring = 1; ring < RadioTowerMeshes.LightRings; ring++)
            {
                string ringName = TowerLightRingPrefix + ring.ToString(CultureInfo.InvariantCulture);
                lights.Add(new ModelNode(ringName, Vector3.zero,
                    new ModelMesh(name + "_Lights_" + ringName, RadioTowerMeshes.Lights(level, ring))));
            }

            ModelNode plaque = tower.Add(new ModelNode(OfflinePlaqueName, RadioTowerMeshes.OfflinePlaque,
                new ModelMesh(name + "_" + OfflinePlaqueName, RadioTowerMeshes.OfflinePlaqueMesh())));
            plaque.Add(new ModelNode(OfflineLampName, RadioTowerMeshes.OfflineLamp, Quaternion.identity,
                new ModelMesh(name + "_" + OfflineLampName, RadioTowerMeshes.OfflineLampGlass()),
                ModelMaterial.PaletteGlowOff));
            tower.Add(new ModelNode("BeaconSocket", RadioTowerMeshes.BeaconPosition(level)));
            tower.Add(new ModelNode("BellCorner", BellCorner, Place.Rotation(new Vector3(0f, BellCornerYaw, 0f))));
            tower.Add(new ModelNode("CassetteShelfAnchor", CassetteShelfAnchor,
                Place.Rotation(new Vector3(0f, CassetteShelfYaw, 0f))));
            tower.Add(new ModelNode("HopperMouth", RadioTowerMeshes.PortHopperMouth,
                Place.Rotation(RadioTowerMeshes.PortHopperFacing)));
            tower.Add(new ModelNode("ServiceHatch", RadioTowerMeshes.ServiceHatchHinge, Quaternion.identity,
                new ModelMesh("RadioTower_ServiceHatch",
                    ServiceKit.Hatch(RadioTowerMeshes.ServiceHatchWidth, RadioTowerMeshes.ServiceHatchHeight))));
            Weathering.Weather(tower, name, structure, TowerWeather(level), RadioTowerMeshes.Rust(level),
                RadioTowerMeshes.Drifts());
            return tower;
        }

        /// <summary>A tower stage's weather: the tallest thing at home, out in it as long as the lander.</summary>
        private static WeatherProfile TowerWeather(int level)
        {
            return new WeatherProfile(seed: 60 + level, lift: 0.006f, panelWidth: 0.5f, panelHeight: 0.5f,
                paintWear: 1f, mismatched: true, runsPerMetre: 2.6f, rustHeight: 1.5f, metalRust: 0.75f,
                paintRust: 0.3f, tide: 1.6f, groundDust: 0.85f, topDust: 0.7f);
        }
    }
}
