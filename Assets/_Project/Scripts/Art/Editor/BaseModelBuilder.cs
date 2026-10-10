using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's home base per the M2 content contract (docs/ARCHITECTURE.md): <c>Lander</c>, <c>MuseumShelf</c>,
    /// <c>RadioTower_L1..L3</c>, Kenji's <c>RoverBay</c> and Bell's <c>CassetteShelf</c> in Generated/Art/Base.
    /// Meshes only, each pivoted at its ground-contact centre so it stands on an anchor. The lander also carries
    /// friend perches (<c>FriendSocket_&lt;id&gt;</c>: an empty on top of the perch, +Y up, +Z = the hatch side) and
    /// every tower stage carries Bell's corner and her shelf's anchor. Glowing parts (windows, shelf lights, tower
    /// lamps) are separate renderers so gameplay can brighten the base as it comes back to life, and decades of
    /// neglect sit in removable Weather_Paint / Weather_Rust / Weather_Dust children (see <see cref="Weathering"/>) so
    /// it can be cleaned as it does.
    /// </summary>
    public static class BaseModelBuilder
    {
        public const string LanderName = "Lander";
        public const string ShelfName = "MuseumShelf";
        public const string TowerPrefix = "RadioTower_L";

        /// <summary>Height of the lander's porch deck in front of the hatch (where the cable lift tops out).</summary>
        public const float LanderDeckTop = LanderMeshes.DeckTop;

        /// <summary>Width of the museum shelf (it stands centred on <see cref="ShelfAnchor"/>).</summary>
        public const float ShelfWidth = MuseumShelfMeshes.Width;

        /// <summary>Side of the square plinth every radio tower stage stands on, and its height.</summary>
        public const float TowerPlinthSize = RadioTowerMeshes.PlinthSize;

        public const float TowerPlinthHeight = RadioTowerMeshes.PlinthHeight;

        /// <summary>
        /// Front edge (+Z) of every tower stage's footprint: the plinth plus the plate its service port stands on.
        /// </summary>
        public const float TowerFootprintFront = RadioTowerMeshes.FootprintFront;

        /// <summary>Where gameplay stands the museum shelf: right of the lander, facing the same way (+Z).</summary>
        public static readonly Vector3 ShelfAnchor = new Vector3(6f, 0f, 1.2f);

        /// <summary>Where gameplay stands the radio tower: left of the lander, slightly behind.</summary>
        public static readonly Vector3 TowerAnchor = new Vector3(-6f, 0f, -1f);

        /// <summary>
        /// Where gameplay stands Kenji's Rover Bay: right of the museum shelf, slightly behind it, its open front
        /// (+Z) towards the plain.
        /// </summary>
        public static readonly Vector3 WorkshopAnchor = new Vector3(12.5f, 0f, -2f);

        /// <summary>
        /// Kenji's Rover Bay, the bench station since M3-14 (stands on <see cref="WorkshopAnchor"/>).
        /// </summary>
        public const string RoverBayName = "RoverBay";

        /// <summary>
        /// Home's warm points as it wakes (M3-16), each its own dark glow renderer lit through the linear
        /// MaterialPropertyBlock contract so World or Gameplay can light them one by one: the lander's "Windows" (the
        /// left pane) with its children Pane_1 (right), Pane_2 (back) and Porthole, and each LampSocket_n's Bulb.
        /// </summary>
        public const string PanePrefix = "Pane_";

        public const string PortholeName = "Porthole";

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
        /// PowerCable_Lift run from the radio tower's foot to what each wakes, each with Glow_0..n glass segments in
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

        /// <summary>Kenji's pen plotter beside his bench, with its moving carriage and pen (see PlotterName).</summary>
        private static void AddPlotter(ModelNode bay, Quaternion benchTurn)
        {
            Vector3 place = RoverBayMeshes.BenchPosition + benchTurn * BlueprintMeshes.PlotterPlace;
            ModelNode plotter = bay.Add(new ModelNode(PlotterName, place, benchTurn,
                new ModelMesh(RoverBayName + "_" + PlotterName, BlueprintMeshes.Plotter())));
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
        /// The crew's lift, jammed until tower L3 powers it and 07 restores it (M3-16): LiftPlatform is built hanging
        /// from its left cable, rolled down to the right (<see cref="LiftPlatformLevel"/> is its restored, level
        /// rotation at any height between LiftBottom and LiftTop); LiftCable_R (taut, hidden while jammed) and
        /// LiftCableR_Snapped (a stub and a coil on the dust, shown while jammed) swap when it is restored; WinchLamp
        /// is a dark glow renderer to light then; LiftHopperMouth is where 07 feeds the winch. Each cable hangs from
        /// its drum along local -Y: scale its local Y by the cable length the platform's height needs over
        /// <see cref="LiftCableBuiltLength"/>.
        /// </summary>
        public const string LiftSnappedCableName = "LiftCableR_Snapped";

        public const string WinchLampName = "WinchLamp";

        /// <summary>
        /// LiftPlatform/JamProps: the crew's "OUT OF ORDER - K." board on the dock-side rail and the snapped cable's
        /// end hanging over the deck's front lip, saying "jammed" at a glance. Gameplay hides it once the lift is
        /// restored.
        /// </summary>
        public const string LiftJamPropsName = "JamProps";

        public const string LiftHopperMouthName = "LiftHopperMouth";

        public static Quaternion LiftPlatformLevel => Quaternion.identity;

        public static float LiftCableBuiltLength => CableLiftMeshes.CableLength(CableLiftMeshes.Jammed);

        public static Quaternion HomeSignUpright => Place.Rotation(LanderMeshes.HomeSignUpright);

        /// <summary>
        /// The bay's roll-up shutter (M3-16 dormant look), pivoted at its roller: local Y scale 1 = down (the bay
        /// asleep), <see cref="ShutterRolledScale"/> = rolled up into its box (tower L2 has powered it). Gameplay eases
        /// between the two.
        /// </summary>
        public const string ShutterName = "Shutter";

        /// <summary>
        /// Kenji's blueprints (M3-16 P7): RoverBay/Blueprints, the cord of pegs along his bench's pegboard, holds one
        /// sheet per kit, Blueprint_&lt;RoverAbility name&gt; (HoverJump, CargoCradle, WarmHeadlamp, BoostCoils). Only
        /// Hover-Jump's hangs there at the start (Kenji left it); Gameplay shows each other one once the plotter has
        /// drawn it.
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
        // out in it longest and biggest; the furniture weathered under them; the jammed lift platform has hung off
        // the ground, dusted only from above.
        private static readonly WeatherProfile LanderWeather = new WeatherProfile(seed: 11, lift: 0.006f,
            panelWidth: 0.7f, panelHeight: 0.62f, paintWear: 1f, mismatched: true, runsPerMetre: 3f, rustHeight: 2.4f,
            metalRust: 0.8f, paintRust: 0.45f, tide: 2.1f, groundDust: 0.95f, topDust: 0.9f);

        private static readonly WeatherProfile LiftPlatformWeather = new WeatherProfile(seed: 71, lift: 0.006f,
            panelWidth: 0.6f, panelHeight: 0.6f, paintWear: 1f, mismatched: true, runsPerMetre: 2.2f, rustHeight: 0f,
            metalRust: 0f, paintRust: 0f, tide: 0f, groundDust: 0f, topDust: 0.8f);

        private static readonly WeatherProfile ShelfWeather = new WeatherProfile(seed: 23, lift: 0.006f,
            panelWidth: 0.45f, panelHeight: 0.45f, paintWear: 1f, mismatched: true, runsPerMetre: 2.6f,
            rustHeight: 0.7f, metalRust: 0.6f, paintRust: 0.3f, tide: 0.75f, groundDust: 0.72f, topDust: 0.75f);

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
            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateLander()), ArtPaths.BaseFolder, material, glowOff, weather);
            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateShelf()), ArtPaths.BaseFolder, material, glowOff, weather);
            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateRoverBay()), ArtPaths.BaseFolder, material, glowOff, weather);
            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateCassetteShelf()), ArtPaths.BaseFolder, material, glowOff, weather);
            for (int level = RadioTowerMeshes.MinLevel; level <= RadioTowerMeshes.MaxLevel; level++)
            {
                ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateTower(level)), ArtPaths.BaseFolder, material, glowOff, weather);
            }

            for (int i = 0; i < PadRadii.Length; i++)
            {
                ModelPrefabWriter.Write(CreateStationPad(i), ArtPaths.BaseFolder, material, glowOff);
            }

            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateLander()
        {
            LowPolyMeshBuilder hull = LanderMeshes.Hull();
            var lander = new ModelNode(LanderName, Vector3.zero, new ModelMesh(LanderName, hull));
            ModelNode windows = lander.Add(new ModelNode("Windows", Vector3.zero, Quaternion.identity,
                new ModelMesh(LanderName + "_Windows", LanderMeshes.Pane(0)), ModelMaterial.PaletteGlowOff));
            for (int i = 1; i < LanderMeshes.PaneCount; i++)
            {
                windows.Add(new ModelNode(PanePrefix + i.ToString(CultureInfo.InvariantCulture), Vector3.zero,
                    Quaternion.identity, new ModelMesh(LanderName + "_Pane" + i.ToString(CultureInfo.InvariantCulture),
                        LanderMeshes.Pane(i)), ModelMaterial.PaletteGlowOff));
            }

            windows.Add(new ModelNode(PortholeName, Vector3.zero, Quaternion.identity,
                new ModelMesh(LanderName + "_Porthole", LanderMeshes.Porthole()), ModelMaterial.PaletteGlowOff));
            lander.Add(new ModelNode("ShelfAnchor", ShelfAnchor));
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
            lander.Add(new ModelNode("DockGlow", Vector3.zero, Quaternion.identity,
                new ModelMesh(LanderName + "_DockGlow", ChargingDockMeshes.Glow()), ModelMaterial.PaletteGlowOff));
            AddLift(lander);
            lander.Add(new ModelNode(HomeSignName, LanderMeshes.HomeSignFoot,
                Place.Rotation(LanderMeshes.HomeSignLean),
                new ModelMesh(LanderName + "_" + HomeSignName, LanderMeshes.HomeSign())));
            AddPowerCables(lander);
            Weathering.Weather(lander, LanderName, hull, LanderWeather, LanderMeshes.Rust(), LanderMeshes.Drifts());
            return lander;
        }

        /// <summary>
        /// The cable lift's moving parts on the lander: the platform at its jammed halfway pose with a RoverSpot
        /// child and its own weather layers, the LiftBottom and LiftTop stops it travels between, and the two hoist
        /// cables hanging from the drum to the carriage at the jammed pose.
        /// </summary>
        private static void AddLift(ModelNode lander)
        {
            lander.Add(new ModelNode("LiftBottom", CableLiftMeshes.Bottom));
            lander.Add(new ModelNode("LiftTop", CableLiftMeshes.Top));
            LowPolyMeshBuilder deck = CableLiftMeshes.Platform();
            string prefix = LanderName + "_LiftPlatform";
            ModelNode platform = lander.Add(new ModelNode("LiftPlatform", CableLiftMeshes.JammedTilted,
                CableLiftMeshes.JamTilt,
                new ModelMesh(prefix, deck)));
            platform.Add(new ModelNode(LiftJamPropsName, Vector3.zero,
                new ModelMesh(prefix + "_" + LiftJamPropsName, CableLiftMeshes.JamProps())));
            platform.Add(new ModelNode("RoverSpot", CableLiftMeshes.RoverSpot,
                Place.Rotation(CableLiftMeshes.RoverSpotEuler)));
            Weathering.Weather(platform, prefix, deck, LiftPlatformWeather, CableLiftMeshes.PlatformRust(),
                CableLiftMeshes.PlatformDrift());
            var cable = new ModelMesh(LanderName + "_LiftCable",
                CableLiftMeshes.Cable(CableLiftMeshes.CableLength(CableLiftMeshes.Jammed)));
            lander.Add(new ModelNode("LiftCable_L", CableLiftMeshes.CableTop(-1), cable));
            lander.Add(new ModelNode("LiftCable_R", CableLiftMeshes.CableTop(1), Quaternion.identity, cable,
                ModelMaterial.Palette, false));
            lander.Add(new ModelNode(LiftSnappedCableName, CableLiftMeshes.CableTop(1),
                new ModelMesh(LanderName + "_" + LiftSnappedCableName, CableLiftMeshes.SnappedCable())));
            lander.Add(new ModelNode(WinchLampName, CableLiftMeshes.WinchLamp, Quaternion.identity,
                new ModelMesh(LanderName + "_" + WinchLampName, CableLiftMeshes.WinchLampGlass()),
                ModelMaterial.PaletteGlowOff));
            lander.Add(new ModelNode(LiftHopperMouthName, CableLiftMeshes.HopperMouth,
                Place.Rotation(CableLiftMeshes.HopperFacing)));
        }

        public static ModelNode CreateShelf()
        {
            LowPolyMeshBuilder cabinet = MuseumShelfMeshes.Cabinet();
            var shelf = new ModelNode(ShelfName, Vector3.zero, new ModelMesh(ShelfName, cabinet));
            shelf.Add(new ModelNode("Lights", Vector3.zero,
                new ModelMesh(ShelfName + "_Lights", MuseumShelfMeshes.Lights())));
            for (int i = 0; i < MuseumShelfMeshes.SlotCount; i++)
            {
                shelf.Add(new ModelNode("Slot_" + i.ToString(CultureInfo.InvariantCulture),
                    MuseumShelfMeshes.SlotPosition(i)));
            }

            Weathering.Weather(shelf, ShelfName, cabinet, ShelfWeather, MuseumShelfMeshes.Rust(),
                MuseumShelfMeshes.Drifts());
            return shelf;
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
            Quaternion benchTurn = Place.Rotation(RoverBayMeshes.BenchEuler);
            ModelNode board = bay.Add(new ModelNode(BlueprintsName, RoverBayMeshes.BenchPosition, benchTurn));
            for (int i = 0; i < BlueprintMeshes.Kits.Length; i++)
            {
                string kit = BlueprintMeshes.Kits[i];
                board.Add(new ModelNode(BlueprintPrefix + kit, BlueprintMeshes.Slot(i), Quaternion.identity,
                    new ModelMesh(RoverBayName + "_" + BlueprintPrefix + kit, BlueprintMeshes.Sheet(kit)),
                    ModelMaterial.Palette, i == 0));
            }

            AddPlotter(bay, benchTurn);
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
        /// Radio tower stage <paramref name="level"/> (1..3). Every stage has the same rover-height service port facing
        /// the upgrade pad: a HopperMouth empty (+Z out of the mouth) and a ServiceHatch door pivoted on its left hinge
        /// (swing it about local Y to open).
        /// </summary>
        public static ModelNode CreateTower(int level)
        {
            string name = TowerPrefix + level.ToString(CultureInfo.InvariantCulture);
            LowPolyMeshBuilder structure = RadioTowerMeshes.Structure(level);
            var tower = new ModelNode(name, Vector3.zero, new ModelMesh(name, structure));
            tower.Add(new ModelNode("Lights", Vector3.zero,
                new ModelMesh(name + "_Lights", RadioTowerMeshes.Lights(level))));
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
