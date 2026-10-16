using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Builds <c>RoverModel.prefab</c>, the rover "07" (docs/VISION.md), with exactly the hierarchy, pivots and axes
    /// of the rover rig contract in docs/ARCHITECTURE.md: metres, +Z forward, +Y up, origin at the centre of the
    /// ground contact patch, every node with identity scale and (except the sockets noted) identity rotation.
    /// </summary>
    public static class RoverModelBuilder
    {
        public const string ModelName = "RoverModel";
        public const float WheelRadius = 0.35f;

        /// <summary>|x| of every wheel centre.</summary>
        public const float WheelTrack = 0.64f;

        /// <summary>|z| of the front and rear wheel centres (middle wheels sit at z = 0).</summary>
        public const float WheelBase = 0.8f;

        /// <summary>Body-side hinge of the rocker-bogie arms (right side; the left mirrors x).</summary>
        public static readonly Vector3 BogieHinge = new Vector3(0.48f, 0.66f, 0.12f);

        /// <summary>Base of the neck on the body lid.</summary>
        public static readonly Vector3 NeckBase = new Vector3(0f, RoverMeshes.LidTop, 0.3f);

        /// <summary>Hinge of the solar wing (front edge of the folded panel).</summary>
        public static readonly Vector3 SolarWingHinge = new Vector3(0f, RoverMeshes.LidTop + 0.012f, 0.06f);

        /// <summary>Base of the whip antenna.</summary>
        public static readonly Vector3 AntennaBase = new Vector3(0.32f, RoverMeshes.LidTop, -0.5f);

        /// <summary>Road light, low on the body front, tilted slightly down.</summary>
        public static readonly Vector3 HeadlampPosition = new Vector3(0f, RoverMeshes.LampHeight, 0.745f);

        public const float HeadlampPitchDegrees = 6f;

        /// <summary>Under the chassis at the bottom of the belly plate, where the Hover-Jump coils mount.</summary>
        public static readonly Vector3 CoilSocket = new Vector3(0f, 0.28f, 0.03f);

        public const string HoverCoilsName = "HoverCoils";

        /// <summary>
        /// The cutter's charge-up (0.5.5 owner feedback §5.4, the owner's pick "Cells"), every glow on the glow-off
        /// material, dark until rover drives its _EmissionColor:
        /// <list type="bullet">
        /// <item><c>ChargeConduits</c> under the root (the deck pair, from the battery bar's ends along both sides of
        /// the wing to the neck's base) and under <c>Neck</c> (up both sides of the pole), each with its glass core
        /// <c>ConduitGlow</c>: lit while energy flows, held faint through the warm-emitter window.</item>
        /// <item><c>Stage_&lt;n&gt;_L</c> and <c>Stage_&lt;n&gt;_R</c> (n = 1..3, the battery end first), the mirrored
        /// capacitor cells on the deck conduits; each holds <c>StageGlow_&lt;n&gt;</c>, its glass sleeve, which slides
        /// along its local +Z by <see cref="ChargeSleeveTravel"/> from its rest seat and lights as its stage
        /// fills.</item>
        /// <item><c>Neck/Head/Eye/LockRing</c>, the dark track round the Focus Lens socket, with <c>LockGlow</c>, lit
        /// at full lock.</item>
        /// </list>
        /// <c>FocusLens</c> mounts on <c>Neck/Head/Eye/LensSocket</c> with identity: its frame, and the <c>Glass</c>
        /// the charge's arrival lights.
        /// </summary>
        public const string ChargeConduitsName = "ChargeConduits";

        public const string ConduitGlowName = "ConduitGlow";
        public const string ChargeStagePrefix = "Stage_";
        public const string StageGlowPrefix = "StageGlow_";
        public const string LockRingName = "LockRing";
        public const string LockGlowName = "LockGlow";
        public const int ChargeStages = 3;
        public const float ChargeSleeveTravel = ChargeRunMeshes.SleeveTravel;
        public const string FocusLensName = "FocusLens";
        public const string FocusLensGlassName = "Glass";
        public const string LensSocketName = "LensSocket";

        /// <summary>
        /// The scan's fan of light (0.5.5 §1.2): <c>ScanFan.prefab</c>, a few hair-thin cyan rays from the lens along
        /// +Z, <see cref="ScanFanHalfAngle"/> degrees either side and <see cref="ScanFanReach"/> m long, on the glow-off
        /// material. Rover mounts it on <c>Neck/Head/Eye/ScanFanSocket</c> with identity, keeps it dark until a scan
        /// lights it (_EmissionColor) and yaws it to sweep (about its local Y). Its own prefab, so 07's size stays
        /// 07's.
        /// </summary>
        public const string ScanFanName = "ScanFan";

        public const string ScanFanSocketName = "ScanFanSocket";

        public const float ScanFanHalfAngle = 12f;
        public const float ScanFanReach = 4f;

        /// <summary>A sleeve's rest seat on its cell (cell space): over the canister's back end.</summary>
        public static readonly Vector3 SleeveRest = new Vector3(0f, 0f, -0.006f);

        public static readonly Vector3 CargoPosition = new Vector3(0f, 0.48f, -0.64f);

        /// <summary>
        /// Right capacitor drum socket at the top of the lid edge above the middle wheel (+X outward), high enough
        /// that the drum clears the wheel at the top of its suspension travel; the left one mirrors x and turns half
        /// round, so its +X points out too.
        /// </summary>
        public static readonly Vector3 DrumSocket = new Vector3(0.47f, 0.95f, -0.12f);

        /// <summary>
        /// Right side bin socket (Kit_SideBin) on the body's flank above the front wheel, high enough that the bin
        /// clears the tyre at the top of its travel, ahead of the drum; the left one mirrors x and turns half round so
        /// its +X points out too.
        /// </summary>
        /// <summary>
        /// 07's cargo hatch (M3-25 T1): <c>CargoHatch</c>, the rear leaf pivoted on its top hinge (rotate about local X,
        /// positive lifts it out and up between the Cargo Cradle's rails), holding <c>HatchWindow</c> with
        /// <c>HatchCrate_0..7</c>, one per hold slot (top row left to right, then the bottom row, as seen from behind);
        /// gameplay shows a crate per filled slot.
        /// </summary>
        public const string CargoHatchName = "CargoHatch";

        public const string HatchWindowName = "HatchWindow";
        public const string HatchCratePrefix = "HatchCrate_";
        public const int HatchCrates = RoverHatchMeshes.Crates;

        /// <summary>The hatch's hinge, along the top of the rear face under the stripe.</summary>
        public static readonly Vector3 CargoHatchHinge = new Vector3(0f, 0.69f, RoverMeshes.BodyBack);

        public static readonly Vector3 BinSocket = new Vector3(RoverMeshes.BodyHalfWidth, 0.86f, 0.4f);

        /// <summary>How far the middle wheels rise at the top of their suspension travel (Rover's rig).</summary>
        public const float MiddleWheelTravel = 0.16f;

        public const string Decal07FreshName = "Decal07Fresh";

        /// <summary>
        /// Where a lid-mounted part stands on 07's lid (M3-16 P1): right of the neck, ahead of
        /// <see cref="SpineSocket"/> so both read apart from the chase camera. +Y up; the part's base sits on it.
        /// </summary>
        public const string PartSocketName = "PartSocket";

        public static readonly Vector3 PartSocket = new Vector3(0.27f, RoverMeshes.LidTop + 0.005f, 0.32f);

        /// <summary>
        /// Where Cell Pack II's battery spine stands (M3-20, the back slot): along the lid's right edge, ahead of the
        /// antenna, the only stretch of lid the folded wing leaves free. +Y up.
        /// </summary>
        public const string SpineSocketName = "SpineSocket";

        public static readonly Vector3 SpineSocket = new Vector3(0.385f, RoverMeshes.LidTop, -0.2f);

        /// <summary>
        /// Where the old tape deck rides (M3-24 C, 0.5.5 §3.5 the Deck socket): on the back's centreline, its bracket
        /// on the rear stripe above the cargo hatch's hinge and under the battery bar, so 07 stays symmetric. The node
        /// faces out of the back (+X of the deck is 07's -Z, its +Z runs to 07's right).
        /// </summary>
        public const string DeckSocketName = "DeckSocket";

        /// <summary>
        /// Where Kenji's tether ring sits (first-hour v2 §3): low on the back of the head, facing back, clear of the eye.
        /// </summary>
        public const string RingSocketName = "RingSocket";

        public static readonly Vector3 RingSocket = new Vector3(0f, 0.1f, -0.2f);

        public static readonly Vector3 DeckSocket = new Vector3(0f, CargoHatchHinge.y + DeckAboveHinge,
            RoverMeshes.BodyBack - RoverMeshes.PaintProud - RoverMeshes.PaintThickness * 0.5f);

        // The deck's middle above the hatch hinge: its straps clear the hinge, its keys stay under the battery bar.
        private const float DeckAboveHinge = 0.085f;

        /// <summary>
        /// The equipment screen's socket markers (0.5.5 §3.5): one empty <c>&lt;RoverSocket&gt;Socket</c> per Core
        /// <c>RoverSocket</c>, at the spot where that part sits on 07, for ui and rover to draw on the live render.
        /// Paths: <c>Neck/Head/Eye/CutterSocket</c>, <c>Neck/Head/TetherSocket</c>, <c>LampSocket</c>,
        /// <c>BellySocket</c>, <c>FlanksSocket</c> and <c>PanniersSocket</c> (the right of each mirrored pair),
        /// <c>BackSocket</c>, <c>HoldSocket</c>, <c>SolarWing/WingSocket</c>, <c>DeckSocket</c> (the tape deck's own
        /// mount), <c>Wheel_MR/WheelsSocket</c> and <c>Antenna/AntennaSocket</c>.
        /// </summary>
        public static readonly IReadOnlyList<string> EquipmentSockets = new[]
        {
            "Cutter", "Tether", "Lamp", "Belly", "Flanks", "Panniers", "Back", "Hold", "Wing", "Deck", "Wheels",
            "Antenna",
        };

        public const string EquipmentSocketSuffix = "Socket";

        // 07 weathered as itself, just old (VISION ruling 11): no mismatched panels, rust low down and at the bolts,
        // dust on the lid and lower body; its skins stand off by a hair so Bell's fresh "07" sits over all of them.
        // 07's head: tired paint and dust on the hood, no rust; it is off the ground.
        private static readonly WeatherProfile HeadWeather = new WeatherProfile(seed: 9, lift: 0.0015f,
            panelWidth: 0.25f, panelHeight: 0.2f, paintWear: 0.5f, mismatched: false, runsPerMetre: 2.5f,
            rustHeight: 0f, metalRust: 0f, paintRust: 0f, tide: 0f, groundDust: 0f, topDust: 0.45f);

        // Wheels and bogies are caked over whole: they turn, so the dust has no top.
        private const float WheelDust = 0.45f;
        private const float BogieRust = 0.4f;
        private const float BogieDust = 0.35f;
        private const float CoatLift = 0.002f;

        private static readonly WeatherProfile BodyWeather = new WeatherProfile(seed: 7, lift: 0.0018f,
            panelWidth: 0.45f, panelHeight: 0.3f, paintWear: 0.75f, mismatched: false, runsPerMetre: 3f,
            rustHeight: 0.6f, metalRust: 0.6f, paintRust: 0.55f, tide: 0.85f, groundDust: 0.7f, topDust: 0.6f);
        public const string CellFilledName = "CellFilled";
        public const string PennantName = "Pennant";

        /// <summary>
        /// The battery's light bar (docs/features/M3-20): Body children "BatteryLamp_0..7" (left to right along the
        /// lid's rear-top edge), each holding its glass "BatteryGlass_<i>" on the glow-off material.
        /// </summary>
        public const string BatteryLampPrefix = "BatteryLamp_";
        public const string BatteryGlassPrefix = "BatteryGlass_";

        /// <summary>Writes the rover sub-prefabs (HoverCoils, ScanFan, FocusLens); RoverModel.prefab is built by R1ModelBuilder.</summary>
        [MoonBuilder("Art/Rover", 120)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            ModelPrefabWriter.Write(CreateHoverCoils(), ArtPaths.RoverFolder, material,
                PaletteAssetBuilder.LoadGlowOffMaterial());
            ModelPrefabWriter.Write(CreateScanFan(), ArtPaths.RoverFolder, material,
                PaletteAssetBuilder.LoadGlowOffMaterial());
            ModelPrefabWriter.Write(CreateFocusLens(), ArtPaths.RoverFolder, material,
                PaletteAssetBuilder.LoadGlowOffMaterial());
            AssetDatabase.SaveAssets();
        }

        /// <summary>The rig as a node tree; pure geometry, no assets touched.</summary>
        public static ModelNode CreateModel()
        {
            var bogieRight = new ModelMesh(ModelName + "_BogieR", RoverMeshes.Bogie());
            var bogieLeft = new ModelMesh(ModelName + "_BogieL", Mirrored(bogieRight.Geometry));
            var wheelRight = new ModelMesh(ModelName + "_WheelR", RoverMeshes.Wheel());
            var wheelLeft = new ModelMesh(ModelName + "_WheelL", Mirrored(wheelRight.Geometry));
            var spareLeft = new ModelMesh(ModelName + "_WheelSpareL", Mirrored(RoverMeshes.SpareWheel()));

            var root = new ModelNode(ModelName, Vector3.zero);
            LowPolyMeshBuilder shell = RoverMeshes.Body();
            ModelNode body = root.Add(new ModelNode("Body", Vector3.zero, new ModelMesh(ModelName + "_Body", shell)));
            body.Add(Gift(Decal07FreshName, Vector3.zero, ModelName + "_Decal07Fresh", RoverGiftMeshes.FreshSerial()));
            Weathering.Weather(body, ModelName, shell, BodyWeather, RoverMeshes.Rust(), RoverMeshes.DustPatches());
            BatteryBar(body);
            var coats = new Dictionary<(ModelMesh, string), ModelMesh>();
            Grimed(root.Add(new ModelNode("Bogie_L", MirrorX(BogieHinge), bogieLeft)), coats);
            Grimed(root.Add(new ModelNode("Bogie_R", BogieHinge, bogieRight)), coats);
            Dusty(root.Add(new ModelNode("Wheel_FL", WheelCentre(-1f, 1f), wheelLeft)), coats);
            Dusty(root.Add(new ModelNode("Wheel_FR", WheelCentre(1f, 1f), wheelRight)), coats);
            Dusty(root.Add(new ModelNode("Wheel_ML", WheelCentre(-1f, 0f), wheelLeft)), coats);
            ModelNode middleRight = root.Add(new ModelNode("Wheel_MR", WheelCentre(1f, 0f), wheelRight));
            Dusty(middleRight, coats);
            Dusty(root.Add(new ModelNode("Wheel_RL", WheelCentre(-1f, -1f), spareLeft)), coats);
            Dusty(root.Add(new ModelNode("Wheel_RR", WheelCentre(1f, -1f), wheelRight)), coats);

            ModelNode neck = root.Add(new ModelNode("Neck", NeckBase,
                new ModelMesh(ModelName + "_Neck", RoverMeshes.Neck())));
            LowPolyMeshBuilder hooded = RoverMeshes.Head();
            ModelNode head = neck.Add(new ModelNode("Head", RoverMeshes.HeadHinge,
                new ModelMesh(ModelName + "_Head", hooded)));
            Weathering.Weather(head, ModelName + "_Head", hooded, HeadWeather, RoverMeshes.HoodScuffs(), null, null);
            ModelNode eye = head.Add(new ModelNode("Eye", RoverMeshes.EyeCentre,
                new ModelMesh(ModelName + "_Eye", RoverMeshes.Eye())));
            eye.Add(new ModelNode("TetherOrigin", new Vector3(0f, 0f, RoverMeshes.LensFront)));
            eye.Add(new ModelNode(LensSocketName, new Vector3(0f, 0f, RoverMeshes.LensFront)));
            eye.Add(new ModelNode(ScanFanSocketName, new Vector3(0f, 0f, RoverMeshes.LensFront)));
            AddChargeUp(root, neck, eye);

            head.Add(new ModelNode(RingSocketName, RingSocket, Place.Rotation(new Vector3(0f, 180f, 0f))));
            head.Add(new ModelNode("Eyelid", RoverMeshes.EyeCentre,
                new ModelMesh(ModelName + "_Eyelid", RoverMeshes.Eyelid())));

            ModelNode wing = root.Add(new ModelNode("SolarWing", SolarWingHinge,
                new ModelMesh(ModelName + "_SolarWing", RoverMeshes.SolarWing())));
            wing.Add(Gift(CellFilledName, RoverGiftMeshes.CellFilledCentre, ModelName + "_CellFilled",
                RoverGiftMeshes.ReplacementCell()));
            ModelNode antenna = root.Add(new ModelNode("Antenna", AntennaBase,
                new ModelMesh(ModelName + "_Antenna", RoverMeshes.Antenna())));
            antenna.Add(new ModelNode("AntennaTip", RoverMeshes.AntennaTipPosition,
                new ModelMesh(ModelName + "_AntennaTip", RoverMeshes.AntennaTip())));
            antenna.Add(Gift(PennantName, RoverGiftMeshes.PennantClip, ModelName + "_Pennant",
                RoverGiftMeshes.Pennant()));

            root.Add(new ModelNode("HeadlampSocket", HeadlampPosition,
                Place.Rotation(new Vector3(HeadlampPitchDegrees, 0f, 0f))));
            root.Add(new ModelNode("CargoSocket", CargoPosition));
            AddCargoHatch(body);
            root.Add(new ModelNode("DustSocket_L", new Vector3(-WheelTrack, 0f, -WheelBase)));
            root.Add(new ModelNode("DustSocket_R", new Vector3(WheelTrack, 0f, -WheelBase)));
            root.Add(new ModelNode("CoilSocket", CoilSocket));
            root.Add(new ModelNode("DrumSocket_L", MirrorX(DrumSocket), Place.Rotation(new Vector3(0f, 180f, 0f))));
            root.Add(new ModelNode("DrumSocket_R", DrumSocket));
            root.Add(new ModelNode("BinSocket_L", MirrorX(BinSocket), Place.Rotation(new Vector3(0f, 180f, 0f))));
            root.Add(new ModelNode("BinSocket_R", BinSocket));
            root.Add(new ModelNode(PartSocketName, PartSocket));
            root.Add(new ModelNode(SpineSocketName, SpineSocket));
            root.Add(new ModelNode(DeckSocketName, DeckSocket, Place.Rotation(new Vector3(0f, 90f, 0f))));
            AddEquipmentSockets(root, eye, head, wing, antenna, middleRight);
            return root;
        }

        /// <summary>The battery's eight light-bar segments along the lid's rear-top edge, left to right.</summary>
        private static void BatteryBar(ModelNode body)
        {
            var tray = new ModelMesh(ModelName + "_BatteryLamp", RoverBatteryBarMeshes.Tray());
            var glass = new ModelMesh(ModelName + "_BatteryGlass", RoverBatteryBarMeshes.Glass());
            for (int i = 0; i < RoverBatteryBarMeshes.SegmentCount; i++)
            {
                ModelNode segment = body.Add(new ModelNode(BatteryLampPrefix + i,
                    RoverBatteryBarMeshes.SegmentPosition(i), RoverBatteryBarMeshes.SegmentRotation, tray));
                segment.Add(new ModelNode(BatteryGlassPrefix + i, RoverBatteryBarMeshes.GlassCentre,
                    Quaternion.identity, glass, ModelMaterial.PaletteGlowOff));
            }
        }

        /// <summary>A wheel caked in dust from rim to hub (one coat per wheel mesh, shared by its wheels).</summary>
        private static void Dusty(ModelNode wheel, Dictionary<(ModelMesh, string), ModelMesh> coats)
        {
            wheel.Add(Weathering.CoatNode(Weathering.DustName, Coat(coats, wheel.Mesh, Weathering.DustName,
                mesh => Weathering.Coat(mesh.Name, Weathering.DustName, mesh.Geometry,
                    Palette.GetSurface(PaletteSwatch.CakedDust), WheelDust, CoatLift))));
        }

        /// <summary>A bogie gone rusty, then dusty over the rust.</summary>
        private static void Grimed(ModelNode bogie, Dictionary<(ModelMesh, string), ModelMesh> coats)
        {
            ModelMesh rust = Coat(coats, bogie.Mesh, Weathering.RustName, mesh => Weathering.Coat(mesh.Name,
                Weathering.RustName, mesh.Geometry, Weathering.DarkRust, BogieRust, CoatLift));
            bogie.Add(Weathering.CoatNode(Weathering.RustName, rust));
            bogie.Add(Weathering.CoatNode(Weathering.DustName, Coat(coats, rust, Weathering.DustName,
                mesh => Weathering.Coat(bogie.Mesh.Name, Weathering.DustName, mesh.Geometry,
                    Palette.GetSurface(PaletteSwatch.CakedDust), BogieDust, CoatLift))));
        }

        /// <summary>
        /// The coat made for <paramref name="part"/> under <paramref name="layer"/>, made once per part mesh so every
        /// node showing the part shares it (the prefab writer needs one mesh per name).
        /// </summary>
        private static ModelMesh Coat(Dictionary<(ModelMesh, string), ModelMesh> coats, ModelMesh part, string layer,
            Func<ModelMesh, ModelMesh> make)
        {
            if (!coats.TryGetValue((part, layer), out ModelMesh coat))
            {
                coat = make(part);
                coats.Add((part, layer), coat);
            }

            return coat;
        }

        /// <summary>A friend's gift: kept in the prefab but hidden until rover shows it.</summary>
        private static ModelNode Gift(string name, Vector3 position, string meshName, LowPolyMeshBuilder geometry)
        {
            return new ModelNode(name, position, Quaternion.identity, new ModelMesh(meshName, geometry),
                ModelMaterial.Palette, false);
        }

        /// <summary>
        /// The Hover-Jump coils (HoverCoils.prefab), mounted on CoilSocket with identity: a plate, Coil_FL/FR/RL/RR
        /// springs pivoted at their tops (squash them by local Y while charging) and a Glow_* ring under each, on
        /// the glow-off material so it lights only when gameplay raises its _EmissionColor.
        /// </summary>
        public static ModelNode CreateHoverCoils()
        {
            var root = new ModelNode(HoverCoilsName, Vector3.zero,
                new ModelMesh(HoverCoilsName + "_Mount", HoverCoilMeshes.Mount()));
            var coil = new ModelMesh(HoverCoilsName + "_Coil", HoverCoilMeshes.Coil());
            var glow = new ModelMesh(HoverCoilsName + "_Glow", HoverCoilMeshes.GlowRing());
            string[] corners = { "FL", "FR", "RL", "RR" };
            for (int i = 0; i < HoverCoilMeshes.CoilCount; i++)
            {
                ModelNode spring = root.Add(new ModelNode("Coil_" + corners[i], HoverCoilMeshes.CoilTop(i), coil));
                spring.Add(new ModelNode("Glow_" + corners[i], HoverCoilMeshes.GlowCentre, Quaternion.identity, glow,
                    ModelMaterial.PaletteGlowOff));
            }

            return root;
        }

        private static void AddCargoHatch(ModelNode body)
        {
            ModelNode hatch = body.Add(new ModelNode(CargoHatchName, CargoHatchHinge,
                new ModelMesh(ModelName + "_" + CargoHatchName, RoverHatchMeshes.Leaf())));
            ModelNode window = hatch.Add(new ModelNode(HatchWindowName, RoverHatchMeshes.Window,
                new ModelMesh(ModelName + "_" + HatchWindowName, RoverHatchMeshes.Hold())));
            var wood = new ModelMesh(ModelName + "_HatchCrate", RoverHatchMeshes.CrateMesh(false));
            var stained = new ModelMesh(ModelName + "_HatchCrateStained", RoverHatchMeshes.CrateMesh(true));
            for (int i = 0; i < HatchCrates; i++)
            {
                window.Add(new ModelNode(HatchCratePrefix + i.ToString(CultureInfo.InvariantCulture),
                    RoverHatchMeshes.Crate(i), (i + i / 4) % 2 == 0 ? wood : stained));
            }
        }

        private static void AddChargeUp(ModelNode root, ModelNode neck, ModelNode eye)
        {
            string prefix = ModelName + "_";
            Vector3[] deck = ChargeRunMeshes.DeckConduit();
            Vector3[] pole = ChargeRunMeshes.NeckConduit();
            foreach ((ModelNode part, Vector3[] path, Vector3 up, string tag) in new[]
            {
                (root, deck, Vector3.up, "Deck"), (neck, pole, Vector3.back, "Neck"),
            })
            {
                ModelNode conduits = part.Add(new ModelNode(ChargeConduitsName, Vector3.zero,
                    new ModelMesh(prefix + tag + ChargeConduitsName, ChargeRunMeshes.ConduitPair(path))));
                conduits.Add(new ModelNode(ConduitGlowName, Vector3.zero, Quaternion.identity,
                    new ModelMesh(prefix + tag + ConduitGlowName, ChargeRunMeshes.ConduitCore(path, up)),
                    ModelMaterial.PaletteGlowOff));
            }

            var cell = new ModelMesh(prefix + "ChargeCell", ChargeRunMeshes.Cell());
            var sleeve = new ModelMesh(prefix + "ChargeSleeve", ChargeRunMeshes.Sleeve());
            for (int stage = 0; stage < ChargeStages; stage++)
            {
                string index = (stage + 1).ToString(CultureInfo.InvariantCulture);
                Vector3 left = ChargeRunMeshes.Cell(stage);
                foreach ((string side, Vector3 at) in new[] { ("L", left), ("R", ChargeRunMeshes.Mirror(left)) })
                {
                    ModelNode piece = root.Add(new ModelNode(ChargeStagePrefix + index + "_" + side, at, cell));
                    piece.Add(new ModelNode(StageGlowPrefix + index, SleeveRest, Quaternion.identity, sleeve,
                        ModelMaterial.PaletteGlowOff));
                }
            }

            ModelNode ring = eye.Add(new ModelNode(LockRingName, Vector3.zero,
                new ModelMesh(prefix + LockRingName, ChargeRunMeshes.LockTrack())));
            ring.Add(new ModelNode(LockGlowName, Vector3.zero, Quaternion.identity,
                new ModelMesh(prefix + LockGlowName, ChargeRunMeshes.LockRing()), ModelMaterial.PaletteGlowOff));
        }

        /// <summary>The scan's fan of light (see <see cref="ScanFanName"/>): one glow-off renderer.</summary>
        public static ModelNode CreateScanFan()
        {
            return new ModelNode(ScanFanName, Vector3.zero, Quaternion.identity, new ModelMesh(ScanFanName,
                ChargeRunMeshes.ScanFan(ScanFanHalfAngle, ScanFanReach)), ModelMaterial.PaletteGlowOff);
        }

        /// <summary>The Focus Lens (see <see cref="ChargeRunPrefix"/>), clipped into 07's eye.</summary>
        public static ModelNode CreateFocusLens()
        {
            var root = new ModelNode(FocusLensName, Vector3.zero,
                new ModelMesh(FocusLensName + "_Frame", ChargeRunMeshes.LensFrame()));
            root.Add(new ModelNode(FocusLensGlassName, ChargeRunMeshes.LensSeat, Quaternion.identity,
                new ModelMesh(FocusLensName + "_Glass", ChargeRunMeshes.LensGlass()), ModelMaterial.PaletteGlowOff));
            return root;
        }

        // Each marker sits on the part it stands for, under the node that moves it (head, wing, wheel), so it follows
        // the live pose; the Deck's is the tape deck's own mount, added with the other mounts.
        private static void AddEquipmentSockets(ModelNode root, ModelNode eye, ModelNode head, ModelNode wing,
            ModelNode antenna, ModelNode middleRight)
        {
            eye.Add(new ModelNode("Cutter" + EquipmentSocketSuffix, new Vector3(0f, 0f, RoverMeshes.LensFront)));
            head.Add(new ModelNode("Tether" + EquipmentSocketSuffix, RingSocket,
                Place.Rotation(new Vector3(0f, 180f, 0f))));
            root.Add(new ModelNode("Lamp" + EquipmentSocketSuffix, HeadlampPosition));
            root.Add(new ModelNode("Belly" + EquipmentSocketSuffix, CoilSocket));
            root.Add(new ModelNode("Flanks" + EquipmentSocketSuffix, DrumSocket));
            root.Add(new ModelNode("Panniers" + EquipmentSocketSuffix, BinSocket));
            root.Add(new ModelNode("Back" + EquipmentSocketSuffix, SpineSocket));
            root.Add(new ModelNode("Hold" + EquipmentSocketSuffix, CargoPosition));
            wing.Add(new ModelNode("Wing" + EquipmentSocketSuffix, RoverMeshes.WingCentre));
            middleRight.Add(new ModelNode("Wheels" + EquipmentSocketSuffix, RoverMeshes.WheelOuterFace));
            antenna.Add(new ModelNode("Antenna" + EquipmentSocketSuffix, Vector3.zero));
        }

        private static Vector3 WheelCentre(float side, float row)
        {
            return new Vector3(side * WheelTrack, WheelRadius, row * WheelBase);
        }

        private static Vector3 MirrorX(Vector3 point)
        {
            return new Vector3(-point.x, point.y, point.z);
        }

        private static LowPolyMeshBuilder Mirrored(LowPolyMeshBuilder source)
        {
            var mirrored = new LowPolyMeshBuilder(source.TriangleCount);
            mirrored.Append(source, Place.MirrorX);
            return mirrored;
        }
    }
}
