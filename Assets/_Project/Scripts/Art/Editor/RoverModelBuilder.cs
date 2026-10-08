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
        public static readonly Vector3 SolarWingHinge = new Vector3(-0.18f, RoverMeshes.LidTop + 0.012f, 0.06f);

        /// <summary>Base of the whip antenna.</summary>
        public static readonly Vector3 AntennaBase = new Vector3(0.32f, RoverMeshes.LidTop, -0.5f);

        /// <summary>Road light, low on the body front, tilted slightly down.</summary>
        public static readonly Vector3 HeadlampPosition = new Vector3(0f, RoverMeshes.LampHeight, 0.745f);

        public const float HeadlampPitchDegrees = 6f;

        /// <summary>Under the chassis at the bottom of the belly plate, where the Hover-Jump coils mount.</summary>
        public static readonly Vector3 CoilSocket = new Vector3(0f, 0.28f, 0.03f);

        public const string HoverCoilsName = "HoverCoils";

        public static readonly Vector3 CargoPosition = new Vector3(0f, 0.48f, -0.64f);

        /// <summary>
        /// Right capacitor drum socket on the lid edge above the middle wheel (+X outward); the left one mirrors x
        /// and turns half round, so its +X points out too.
        /// </summary>
        public static readonly Vector3 DrumSocket = new Vector3(0.47f, 0.87f, -0.12f);

        public const string Decal07FreshName = "Decal07Fresh";
        public const string CellFilledName = "CellFilled";
        public const string PennantName = "Pennant";

        /// <summary>Writes the rover meshes and prefab (the palette material must exist).</summary>
        [MoonBuilder("Art/Rover", 120)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            ModelPrefabWriter.Write(CreateModel(), ArtPaths.RoverFolder, material);
            ModelPrefabWriter.Write(CreateHoverCoils(), ArtPaths.RoverFolder, material,
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
            ModelNode body = root.Add(new ModelNode("Body", Vector3.zero,
                new ModelMesh(ModelName + "_Body", RoverMeshes.Body())));
            body.Add(Gift(Decal07FreshName, Vector3.zero, ModelName + "_Decal07Fresh", RoverGiftMeshes.FreshSerial()));
            root.Add(new ModelNode("Bogie_L", MirrorX(BogieHinge), bogieLeft));
            root.Add(new ModelNode("Bogie_R", BogieHinge, bogieRight));
            root.Add(new ModelNode("Wheel_FL", WheelCentre(-1f, 1f), wheelLeft));
            root.Add(new ModelNode("Wheel_FR", WheelCentre(1f, 1f), wheelRight));
            root.Add(new ModelNode("Wheel_ML", WheelCentre(-1f, 0f), wheelLeft));
            root.Add(new ModelNode("Wheel_MR", WheelCentre(1f, 0f), wheelRight));
            root.Add(new ModelNode("Wheel_RL", WheelCentre(-1f, -1f), spareLeft));
            root.Add(new ModelNode("Wheel_RR", WheelCentre(1f, -1f), wheelRight));

            ModelNode neck = root.Add(new ModelNode("Neck", NeckBase,
                new ModelMesh(ModelName + "_Neck", RoverMeshes.Neck())));
            ModelNode head = neck.Add(new ModelNode("Head", RoverMeshes.HeadHinge,
                new ModelMesh(ModelName + "_Head", RoverMeshes.Head())));
            ModelNode eye = head.Add(new ModelNode("Eye", RoverMeshes.EyeCentre,
                new ModelMesh(ModelName + "_Eye", RoverMeshes.Eye())));
            eye.Add(new ModelNode("TetherOrigin", new Vector3(0f, 0f, RoverMeshes.LensFront)));
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
            root.Add(new ModelNode("DustSocket_L", new Vector3(-WheelTrack, 0f, -WheelBase)));
            root.Add(new ModelNode("DustSocket_R", new Vector3(WheelTrack, 0f, -WheelBase)));
            root.Add(new ModelNode("CoilSocket", CoilSocket));
            root.Add(new ModelNode("DrumSocket_L", MirrorX(DrumSocket), Place.Rotation(new Vector3(0f, 180f, 0f))));
            root.Add(new ModelNode("DrumSocket_R", DrumSocket));
            return root;
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
