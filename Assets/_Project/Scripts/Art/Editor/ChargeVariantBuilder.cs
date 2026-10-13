using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Preview-only prefabs for the 0.5.5 charge-up pick, <c>Preview_Charge_&lt;Variant&gt;</c> in Generated/Art/Rover.
    /// Groups <c>OnRoot</c>, <c>OnNeck</c> and <c>OnEye</c> parent (local identity) to 07's root, Neck and Eye. Glow
    /// renderers on the glow-off material: <c>ConduitGlow</c> (the deck's and the neck's glass core lines),
    /// <c>StageGlow_1..3</c> (each stage's pair, battery end first) and <c>LockGlow</c> (the ring on the emitter). The
    /// pieces that move per stage are the mirrored pairs <c>Stage_&lt;n&gt;_L</c> and <c>_R</c> on the deck conduits:
    /// coils turn a notch about their Z, vanes stand up from folded flat outward, cells slide their sleeve forward.
    /// ArtLightingPreview's <c>charge_&lt;variant&gt;_&lt;state&gt;</c> scenes pose them.
    /// </summary>
    public static class ChargeVariantBuilder
    {
        public const string PrefabPrefix = "Preview_Charge_";
        public const string OnRootName = "OnRoot";
        public const string OnNeckName = "OnNeck";
        public const string OnEyeName = "OnEye";
        public const string ConduitGlowName = "ConduitGlow";
        public const string StageGlowPrefix = "StageGlow_";
        public const string StagePrefix = "Stage_";
        public const string LockGlowName = "LockGlow";
        public const int Stages = 3;

        // Each variant's stage pieces at the size that reads from the chase camera.
        private const float CoilScale = 1.35f;
        private const float VaneScale = 1.6f;
        private const float CellScale = 1.3f;

        public static string[] Variants => ChargeVariantMeshes.Variants;

        [MoonBuilder("Art/ChargePreviews", 126)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            Material glowOff = PaletteAssetBuilder.LoadGlowOffMaterial();
            foreach (string variant in Variants)
            {
                ModelPrefabWriter.Write(Create(variant), ArtPaths.RoverFolder, material, glowOff);
            }

            AssetDatabase.SaveAssets();
        }

        public static ModelNode Create(string variant)
        {
            string name = PrefabPrefix + variant;
            var root = new ModelNode(name, Vector3.zero);
            ModelNode onRoot = root.Add(new ModelNode(OnRootName, Vector3.zero));
            ModelNode onNeck = root.Add(new ModelNode(OnNeckName, Vector3.zero));
            ModelNode onEye = root.Add(new ModelNode(OnEyeName, Vector3.zero));

            Vector3[] deck = ChargeVariantMeshes.DeckConduit();
            onRoot.Add(new ModelNode("Conduits", Vector3.zero,
                new ModelMesh(name + "_Deck", ChargeVariantMeshes.ConduitPair(deck))));
            onRoot.Add(Glow(ConduitGlowName, Vector3.zero, Quaternion.identity,
                new ModelMesh(name + "_DeckCore", ChargeVariantMeshes.ConduitCore(deck, Vector3.up))));
            Vector3[] neck = ChargeVariantMeshes.NeckConduit();
            onNeck.Add(new ModelNode("Conduits", Vector3.zero,
                new ModelMesh(name + "_Neck", ChargeVariantMeshes.ConduitPair(neck))));
            onNeck.Add(Glow(ConduitGlowName, Vector3.zero, Quaternion.identity,
                new ModelMesh(name + "_NeckCore", ChargeVariantMeshes.ConduitCore(neck, Vector3.back))));

            ModelMesh body;
            ModelMesh glow;
            switch (variant)
            {
                case ChargeVariantMeshes.Coils:
                    body = new ModelMesh(name + "_Coil", Scaled(ChargeVariantMeshes.CoilHousing(), CoilScale));
                    glow = new ModelMesh(name + "_Winding", Scaled(ChargeVariantMeshes.CoilWinding(), CoilScale));
                    break;
                case ChargeVariantMeshes.Vanes:
                    body = new ModelMesh(name + "_Vane", Scaled(ChargeVariantMeshes.VanePlate(), VaneScale));
                    glow = new ModelMesh(name + "_VaneCore", Scaled(ChargeVariantMeshes.VaneCore(), VaneScale));
                    break;
                case ChargeVariantMeshes.Cells:
                    body = new ModelMesh(name + "_Cell", Scaled(ChargeVariantMeshes.CellCanister(), CellScale));
                    glow = new ModelMesh(name + "_Sleeve", Scaled(ChargeVariantMeshes.CellSleeve(), CellScale));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown charge variant.");
            }

            for (int stage = 0; stage < Stages; stage++)
            {
                string index = (stage + 1).ToString(CultureInfo.InvariantCulture);
                Vector3 left = ChargeVariantMeshes.StagePiece(stage);
                foreach ((string side, Vector3 at) in new[] { ("L", left), ("R", ChargeVariantMeshes.Mirror(left)) })
                {
                    ModelNode piece = onRoot.Add(new ModelNode(StagePrefix + index + "_" + side, at, body));
                    piece.Add(Glow(StageGlowPrefix + index, Vector3.zero, Quaternion.identity, glow));
                }
            }

            ModelNode ring = onEye.Add(new ModelNode("LockTrack", Vector3.zero,
                new ModelMesh(name + "_LockTrack", ChargeVariantMeshes.LockTrack())));
            ring.Add(Glow(LockGlowName, Vector3.zero, Quaternion.identity,
                new ModelMesh(name + "_Lock", ChargeVariantMeshes.LockRing())));
            return root;
        }

        private static LowPolyMeshBuilder Scaled(LowPolyMeshBuilder source, float scale)
        {
            var b = new LowPolyMeshBuilder(source.TriangleCount);
            b.Append(source, Matrix4x4.Scale(Vector3.one * scale));
            return b;
        }

        private static ModelNode Glow(string name, Vector3 position, Quaternion rotation, ModelMesh mesh)
        {
            return new ModelNode(name, position, rotation, mesh, ModelMaterial.PaletteGlowOff);
        }
    }
}
