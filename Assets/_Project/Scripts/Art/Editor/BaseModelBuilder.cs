using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's home base per the M2 content contract (docs/ARCHITECTURE.md): <c>Lander</c>, <c>MuseumShelf</c>,
    /// <c>RadioTower_L1..L3</c>, Kenji's <c>Workbench</c> and Bell's <c>CassetteShelf</c> in Generated/Art/Base.
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
        /// Where gameplay stands Kenji's workbench: right of the museum shelf, slightly behind it, far enough that
        /// its shop pad (3.2 m in front, +Z, radius 2.4 m) stays clear of the shelf.
        /// </summary>
        public static readonly Vector3 WorkshopAnchor = new Vector3(12.5f, 0f, -2f);

        public const string WorkbenchName = "Workbench";

        /// <summary>
        /// Kenji's Rover Bay, the bench station since M3-14 (stands on <see cref="WorkshopAnchor"/>).
        /// </summary>
        public const string RoverBayName = "RoverBay";

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

        private static readonly WeatherProfile WorkbenchWeather = new WeatherProfile(seed: 31, lift: 0.006f,
            panelWidth: 0.5f, panelHeight: 0.45f, paintWear: 1f, mismatched: true, runsPerMetre: 2.2f, rustHeight: 0.5f,
            metalRust: 0.6f, paintRust: 0.25f, tide: 0.55f, groundDust: 0.65f, topDust: 0.65f);

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
            ModelPrefabWriter.Write(CreateLander(), ArtPaths.BaseFolder, material, glowOff, weather);
            ModelPrefabWriter.Write(CreateShelf(), ArtPaths.BaseFolder, material, glowOff, weather);
            ModelPrefabWriter.Write(CreateWorkbench(), ArtPaths.BaseFolder, material, glowOff, weather);
            ModelPrefabWriter.Write(CreateRoverBay(), ArtPaths.BaseFolder, material, glowOff, weather);
            ModelPrefabWriter.Write(CreateCassetteShelf(), ArtPaths.BaseFolder, material, glowOff, weather);
            for (int level = RadioTowerMeshes.MinLevel; level <= RadioTowerMeshes.MaxLevel; level++)
            {
                ModelPrefabWriter.Write(CreateTower(level), ArtPaths.BaseFolder, material, glowOff, weather);
            }

            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateLander()
        {
            LowPolyMeshBuilder hull = LanderMeshes.Hull();
            var lander = new ModelNode(LanderName, Vector3.zero, new ModelMesh(LanderName, hull));
            lander.Add(new ModelNode("Windows", Vector3.zero,
                new ModelMesh(LanderName + "_Windows", LanderMeshes.Windows())));
            lander.Add(new ModelNode("ShelfAnchor", ShelfAnchor));
            lander.Add(new ModelNode("TowerAnchor", TowerAnchor));
            for (int i = 0; i < LanderMeshes.LampCount; i++)
            {
                lander.Add(new ModelNode("LampSocket_" + i.ToString(CultureInfo.InvariantCulture),
                    LanderMeshes.LampPosition(i)));
            }

            lander.Add(new ModelNode("FriendSocket_tilly", LanderMeshes.TillyPerch));
            lander.Add(new ModelNode("WorkshopAnchor", WorkshopAnchor));
            lander.Add(new ModelNode("DockAnchor", ChargingDockMeshes.Anchor,
                Place.Rotation(ChargingDockMeshes.AnchorEuler)));
            lander.Add(new ModelNode("DockGlow", Vector3.zero, Quaternion.identity,
                new ModelMesh(LanderName + "_DockGlow", ChargingDockMeshes.Glow()), ModelMaterial.PaletteGlowOff));
            AddLift(lander);
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
            ModelNode platform = lander.Add(new ModelNode("LiftPlatform", CableLiftMeshes.Jammed,
                new ModelMesh(prefix, deck)));
            platform.Add(new ModelNode("RoverSpot", CableLiftMeshes.RoverSpot,
                Place.Rotation(CableLiftMeshes.RoverSpotEuler)));
            Weathering.Weather(platform, prefix, deck, LiftPlatformWeather, CableLiftMeshes.PlatformRust(),
                CableLiftMeshes.PlatformDrift());
            var cable = new ModelMesh(LanderName + "_LiftCable",
                CableLiftMeshes.Cable(CableLiftMeshes.CableLength(CableLiftMeshes.Jammed)));
            lander.Add(new ModelNode("LiftCable_L", CableLiftMeshes.CableTop(-1), cable));
            lander.Add(new ModelNode("LiftCable_R", CableLiftMeshes.CableTop(1), cable));
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
        /// Kenji's workbench: root on the ground at the bench centre (+Z = the front where 07 parks), a Lights glow
        /// child (the hanging work lamp's bulb) and an empty SparkSocket between the vice jaws.
        /// </summary>
        public static ModelNode CreateWorkbench()
        {
            LowPolyMeshBuilder top = WorkbenchMeshes.Bench();
            var bench = new ModelNode(WorkbenchName, Vector3.zero, new ModelMesh(WorkbenchName, top));
            bench.Add(new ModelNode("Lights", WorkbenchMeshes.LampBulb,
                new ModelMesh(WorkbenchName + "_Lights", WorkbenchMeshes.LampBulbMesh())));
            bench.Add(new ModelNode("SparkSocket", WorkbenchMeshes.Sparks));
            Weathering.Weather(bench, WorkbenchName, top, WorkbenchWeather, WorkbenchMeshes.Rust(), null);
            return bench;
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
