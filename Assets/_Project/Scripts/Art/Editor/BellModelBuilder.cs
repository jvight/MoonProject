using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Bell (docs/features/M3-05-bell-radio-cassettes.md) in Generated/Art/Friends: <c>Bell</c> standing in her rest
    /// pose, <c>Bell_Broken</c> (the same nodes tipped back against a canyon wall: lid open, a rear leg folded under,
    /// the others splayed out with their feet found on the ground, needle slumped, antenna bent, speaker cone gone)
    /// and her part pickups <c>Part_BellKnob|Cone|Valve</c>. The root stands on the ground between her feet, +Z = the
    /// dial face. DialLamp and PartLamp_0..3 are their own renderers on the glow-off material, lit at runtime through
    /// a MaterialPropertyBlock _EmissionColor. Legs are root children (not Body's), so Body can sway on its hips.
    /// </summary>
    public static class BellModelBuilder
    {
        public const string BellName = "Bell";
        public const string BellBrokenName = "Bell_Broken";

        /// <summary>
        /// Needle angle at rest (local Z rotation 0), measured from Body +X towards +Y: the left end of the band as
        /// seen from the front. A positive local Z rotation sweeps it right, to the band's end at
        /// <see cref="NeedleSweepDegrees"/>.
        /// </summary>
        public const float NeedleRestDegrees = BellMeshes.NeedleRestDegrees;

        public const float NeedleSweepDegrees = BellMeshes.NeedleSweepDegrees;

        /// <summary>How far the broken pose sinks into the dust.</summary>
        public const float BrokenSink = 0.015f;

        /// <summary>The broken cabinet: tipped back, turned a little and leaning to its right.</summary>
        public static readonly Vector3 BrokenBodyEuler = new Vector3(-34f, 12f, -5f);

        /// <summary>The broken lid, flopped open on a strained hinge.</summary>
        public static readonly Vector3 BrokenLidEuler = new Vector3(-74f, 0f, 3f);

        /// <summary>The dead needle, slumped just past the left end of the band.</summary>
        public const float BrokenNeedleDegrees = -9f;

        /// <summary>Which leg is folded under her in the broken pose (RL).</summary>
        public const int FoldedLeg = 2;

        /// <summary>Part pickup prefab names fixed for gameplay.</summary>
        public static IReadOnlyList<string> PartNames => new[] { "Part_BellKnob", "Part_BellCone", "Part_BellValve" };

        /// <summary>
        /// Broken legs relative to the tipped cabinet: swing about X (negative = forward), splay about Z (positive =
        /// towards +X) and knee bend about X (positive folds the shin back). Swings of legs that reach the ground are
        /// solved so their feet rest in the dust (front legs stretched out on buckled knees, the right rear leg
        /// flopped out to the side); the folded left rear leg keeps its swing and carries the cabinet.
        /// </summary>
        private static readonly LegPose[] BrokenLegs =
        {
            new LegPose(-30f, -16f, 52f, true),
            new LegPose(-30f, 18f, 34f, true),
            new LegPose(-55f, -4f, 150f, false),
            new LegPose(-12f, 35f, 15f, true),
        };

        private const float BrokenLift = 0.3f;
        private const float SwingSearchMin = -88f;
        private const float SwingSearchMax = 30f;
        private const int SwingSearchSteps = 40;

        [MoonBuilder("Art/Bell", 165)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            Material glowOff = PaletteAssetBuilder.LoadGlowOffMaterial();
            ModelPrefabWriter.Write(CreateBell(false), ArtPaths.FriendFolder, material, glowOff);
            ModelPrefabWriter.Write(CreateBell(true), ArtPaths.FriendFolder, material, glowOff);
            foreach (string part in PartNames)
            {
                ModelPrefabWriter.Write(CreatePart(part), ArtPaths.FriendFolder, material);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>Bell as repaired (<paramref name="broken"/> false) or as found at the canyon terminus.</summary>
        public static ModelNode CreateBell(bool broken)
        {
            var meshes = new Meshes(broken, Place.Rotation(BrokenBodyEuler));
            Pose pose = broken ? BrokenPose(meshes) : RestPose();
            return Assemble(broken ? BellBrokenName : BellName, meshes, pose);
        }

        public static ModelNode CreatePart(string name)
        {
            LowPolyMeshBuilder geometry;
            switch (name)
            {
                case "Part_BellKnob":
                    geometry = FriendPartMeshes.BellKnob();
                    break;
                case "Part_BellCone":
                    geometry = FriendPartMeshes.BellCone();
                    break;
                case "Part_BellValve":
                    geometry = FriendPartMeshes.BellValve();
                    break;
                default:
                    throw new ArgumentException($"Unknown Bell part '{name}'.", nameof(name));
            }

            return new ModelNode(name, Vector3.zero, new ModelMesh(name, RecipeKit.CentredOnMass(geometry)));
        }

        private static Pose RestPose()
        {
            var pose = new Pose
            {
                BodyPosition = Vector3.up * BellLegMeshes.BodyBase,
                BodyRotation = Quaternion.identity,
                LidRotation = Quaternion.identity,
                NeedleRotation = Quaternion.identity,
            };
            for (int i = 0; i < BellLegMeshes.LegCount; i++)
            {
                pose.Hips[i] = BellLegMeshes.Hip(i);
                pose.Legs[i] = Quaternion.identity;
                pose.Shins[i] = Quaternion.identity;
            }

            return pose;
        }

        /// <summary>
        /// Tips the cabinet back, hangs the legs from its hips, settles the cabinet and the folded leg into the dust,
        /// then swings every other leg until its lowest point rests at the same depth.
        /// </summary>
        private static Pose BrokenPose(Meshes meshes)
        {
            Quaternion body = Place.Rotation(BrokenBodyEuler);
            var pose = new Pose
            {
                BodyPosition = Vector3.up * BrokenLift,
                BodyRotation = body,
                LidRotation = Place.Rotation(BrokenLidEuler),
                NeedleRotation = Place.Rotation(new Vector3(0f, 0f, BrokenNeedleDegrees)),
            };
            for (int i = 0; i < BellLegMeshes.LegCount; i++)
            {
                pose.Hips[i] = HipOnBody(pose, i);
                pose.Legs[i] = LegRotation(body, BrokenLegs[i], BrokenLegs[i].Swing);
                pose.Shins[i] = Place.Rotation(new Vector3(BrokenLegs[i].KneeBend, 0f, 0f));
            }

            float lowest = Lowest(BodyNode(meshes, pose), Matrix4x4.identity);
            for (int i = 0; i < BellLegMeshes.LegCount; i++)
            {
                if (!BrokenLegs[i].ReachesGround)
                {
                    lowest = Mathf.Min(lowest, Lowest(LegNode(meshes, pose, i), Matrix4x4.identity));
                }
            }

            pose.BodyPosition += Vector3.down * (lowest + BrokenSink);
            for (int i = 0; i < BellLegMeshes.LegCount; i++)
            {
                pose.Hips[i] = HipOnBody(pose, i);
                if (BrokenLegs[i].ReachesGround)
                {
                    pose.Legs[i] = LegRotation(body, BrokenLegs[i], SolveSwing(meshes, pose, i));
                }
            }

            return pose;
        }

        /// <summary>
        /// Bisects the swing of leg <paramref name="corner"/> so its lowest point sits <see cref="BrokenSink"/> deep,
        /// between the fully forward swing (foot lifted clear) and the fully backward one (foot dug in). A leg that
        /// cannot do both is a tuning error in <see cref="BrokenLegs"/>, so it throws instead of guessing.
        /// </summary>
        private static float SolveSwing(Meshes meshes, Pose pose, int corner)
        {
            float raised = SwingSearchMin;
            float lowered = SwingSearchMax;
            float highest = Depth(meshes, pose, corner, raised);
            float deepest = Depth(meshes, pose, corner, lowered);
            if (highest <= -BrokenSink || deepest > -BrokenSink)
            {
                throw new InvalidOperationException(
                    $"Bell_Broken leg {BellLegMeshes.Corner(corner)} cannot rest its foot in the dust: its lowest " +
                    $"point spans {deepest:F3}..{highest:F3} m over swings {lowered}..{raised} degrees; retune " +
                    "BrokenLegs.");
            }

            for (int step = 0; step < SwingSearchSteps; step++)
            {
                float swing = (raised + lowered) * 0.5f;
                if (Depth(meshes, pose, corner, swing) > -BrokenSink)
                {
                    raised = swing;
                }
                else
                {
                    lowered = swing;
                }
            }

            return (raised + lowered) * 0.5f;
        }

        /// <summary>Lowest point of leg <paramref name="corner"/> swung to <paramref name="swing"/> degrees.</summary>
        private static float Depth(Meshes meshes, Pose pose, int corner, float swing)
        {
            pose.Legs[corner] = LegRotation(pose.BodyRotation, BrokenLegs[corner], swing);
            return Lowest(LegNode(meshes, pose, corner), Matrix4x4.identity);
        }

        private static Vector3 HipOnBody(Pose pose, int corner)
        {
            Vector3 fromBody = BellLegMeshes.Hip(corner) - Vector3.up * BellLegMeshes.BodyBase;
            return pose.BodyPosition + pose.BodyRotation * fromBody;
        }

        private static Quaternion LegRotation(Quaternion body, LegPose leg, float swing)
        {
            return body * Place.Rotation(new Vector3(swing, 0f, leg.Splay));
        }

        private static ModelNode Assemble(string name, Meshes meshes, Pose pose)
        {
            var root = new ModelNode(name, Vector3.zero);
            root.Add(BodyNode(meshes, pose));
            for (int i = 0; i < BellLegMeshes.LegCount; i++)
            {
                root.Add(LegNode(meshes, pose, i));
            }

            return root;
        }

        private static ModelNode BodyNode(Meshes meshes, Pose pose)
        {
            var body = new ModelNode("Body", pose.BodyPosition, pose.BodyRotation, meshes.Body);
            body.Add(new ModelNode("Lid", BellMeshes.LidHinge, pose.LidRotation, meshes.Lid));
            ModelNode dial = body.Add(new ModelNode("DialFace", BellMeshes.DialCentre, meshes.DialFace));
            dial.Add(new ModelNode("Needle", Vector3.zero, pose.NeedleRotation, meshes.Needle));
            dial.Add(new ModelNode("DialLamp", Vector3.zero, Quaternion.identity, meshes.DialLamp,
                ModelMaterial.PaletteGlowOff));
            body.Add(new ModelNode("Speaker", BellMeshes.SpeakerCentre, meshes.Speaker));
            body.Add(new ModelNode("TapeSlot", BellMeshes.TapeSlot));
            body.Add(new ModelNode("Antenna", BellMeshes.AntennaBase, meshes.Antenna));
            for (int i = 0; i < BellMeshes.PartLampCount; i++)
            {
                ModelMesh lamp = i == BellMeshes.PartLampCount - 1 ? meshes.TapeLamp : meshes.PartLamp;
                body.Add(new ModelNode("PartLamp_" + i.ToString(CultureInfo.InvariantCulture),
                    BellMeshes.PartLampPosition(i), Quaternion.identity, lamp, ModelMaterial.PaletteGlowOff));
            }

            return body;
        }

        private static ModelNode LegNode(Meshes meshes, Pose pose, int corner)
        {
            string suffix = BellLegMeshes.Corner(corner);
            var leg = new ModelNode("Leg_" + suffix, pose.Hips[corner], pose.Legs[corner], meshes.Thighs[corner]);
            leg.Add(new ModelNode("Shin_" + suffix, BellLegMeshes.Knee(corner), pose.Shins[corner],
                meshes.Shins[corner]));
            return leg;
        }

        /// <summary>Lowest y reached by any mesh under <paramref name="node"/>, in its parent's space.</summary>
        private static float Lowest(ModelNode node, Matrix4x4 parent)
        {
            Matrix4x4 local = parent * node.LocalMatrix;
            float lowest = float.MaxValue;
            if (node.Mesh != null)
            {
                IReadOnlyList<Vector3> positions = node.Mesh.Geometry.Positions;
                for (int v = 0; v < positions.Count; v++)
                {
                    lowest = Mathf.Min(lowest, local.MultiplyPoint3x4(positions[v]).y);
                }
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                lowest = Mathf.Min(lowest, Lowest(node.Children[i], local));
            }

            return lowest;
        }

        /// <summary>Every mesh of one Bell variant; the legs and the dial parts are shared by both.</summary>
        private sealed class Meshes
        {
            public Meshes(bool broken, Quaternion brokenPose)
            {
                string variant = broken ? BellBrokenName : BellName;
                Body = new ModelMesh(variant + "_Body", BellMeshes.Body(broken, brokenPose));
                Lid = new ModelMesh(BellName + "_Lid", BellMeshes.Lid());
                DialFace = new ModelMesh(BellName + "_DialFace", BellMeshes.DialFace());
                Needle = new ModelMesh(BellName + "_Needle", BellMeshes.Needle());
                DialLamp = new ModelMesh(BellName + "_DialLamp", BellMeshes.DialLamp());
                Speaker = new ModelMesh(variant + "_Speaker", BellMeshes.Speaker(broken));
                Antenna = new ModelMesh(variant + "_Antenna",
                    broken ? BellMeshes.BentAntenna() : BellMeshes.Antenna());
                PartLamp = new ModelMesh(BellName + "_PartLamp", BellMeshes.PartLamp());
                TapeLamp = new ModelMesh(BellName + "_TapeLamp", BellMeshes.TapeLamp());
                Thighs = new ModelMesh[BellLegMeshes.LegCount];
                Shins = new ModelMesh[BellLegMeshes.LegCount];
                for (int i = 0; i < BellLegMeshes.LegCount; i++)
                {
                    string corner = BellLegMeshes.Corner(i);
                    Thighs[i] = new ModelMesh(BellName + "_Leg_" + corner, BellLegMeshes.Thigh(i));
                    Shins[i] = new ModelMesh(BellName + "_Shin_" + corner, BellLegMeshes.Shin(i));
                }
            }

            public ModelMesh Body { get; }

            public ModelMesh Lid { get; }

            public ModelMesh DialFace { get; }

            public ModelMesh Needle { get; }

            public ModelMesh DialLamp { get; }

            public ModelMesh Speaker { get; }

            public ModelMesh Antenna { get; }

            public ModelMesh PartLamp { get; }

            public ModelMesh TapeLamp { get; }

            public ModelMesh[] Thighs { get; }

            public ModelMesh[] Shins { get; }
        }

        /// <summary>Local transforms of every posed node of one variant.</summary>
        private sealed class Pose
        {
            public Vector3 BodyPosition { get; set; }

            public Quaternion BodyRotation { get; set; }

            public Quaternion LidRotation { get; set; }

            public Quaternion NeedleRotation { get; set; }

            public Vector3[] Hips { get; } = new Vector3[BellLegMeshes.LegCount];

            public Quaternion[] Legs { get; } = new Quaternion[BellLegMeshes.LegCount];

            public Quaternion[] Shins { get; } = new Quaternion[BellLegMeshes.LegCount];
        }

        private readonly struct LegPose
        {
            public LegPose(float swing, float splay, float kneeBend, bool reachesGround)
            {
                Swing = swing;
                Splay = splay;
                KneeBend = kneeBend;
                ReachesGround = reachesGround;
            }

            public float Swing { get; }

            public float Splay { get; }

            public float KneeBend { get; }

            public bool ReachesGround { get; }
        }
    }
}
