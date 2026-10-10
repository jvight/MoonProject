using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The basin's crew remnants World places (M3-18), in Generated/Art/Landmarks, meshes only on M_LowPoly, each root
    /// on the ground, +Y up, +Z forward:
    /// <list type="bullet">
    /// <item><c>RoutePost</c> (L2), rover height, with a <c>Reflector</c> on M_LowPolyGlowOff facing both ways along
    /// the road (+Z / -Z), for World to light as 07's lamp passes;</item>
    /// <item><c>ChasmBridge_Near</c> (L4), the near lip's two anchor posts, +Z across the chasm, the lip at z = 0.6;
    /// <c>ChasmBridge_Far</c>, the far anchor facing back (-Z toward the basin) with its <c>Lamp</c> on
    /// M_LowPolyGlowOff; <c>ChasmBridge_Cable</c>, the snapped cable to lay along the trough (X along it);</item>
    /// <item><c>DishFrame_0..2</c> (L7), the antenna farm's toppled dish frames, about 6.5 m across.</item>
    /// </list>
    /// </summary>
    public static class LandmarkModelBuilder
    {
        public const string RoutePostName = "RoutePost";
        public const string ReflectorName = "Reflector";
        public const string BridgeNearName = "ChasmBridge_Near";
        public const string BridgeFarName = "ChasmBridge_Far";
        public const string BridgeCableName = "ChasmBridge_Cable";
        public const string LampName = "Lamp";
        public const string DishFramePrefix = "DishFrame_";

        [MoonBuilder("Art/Landmarks", 195)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            Material glowOff = PaletteAssetBuilder.LoadGlowOffMaterial();
            string folder = ArtPaths.LandmarksFolder;
            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateRoutePost()), folder, material, glowOff);
            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateBridgeNear()), folder, material, glowOff);
            ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateBridgeFar()), folder, material, glowOff);
            ModelPrefabWriter.Write(CreateBridgeCable(), folder, material, glowOff);
            for (int variant = 0; variant < LandmarkMeshes.DishVariants; variant++)
            {
                ModelPrefabWriter.Write(GroundOcclusion.Ground(CreateDishFrame(variant)), folder, material, glowOff);
            }

            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateRoutePost()
        {
            var root = new ModelNode(RoutePostName, Vector3.zero,
                new ModelMesh(RoutePostName, LandmarkMeshes.RoutePost()));
            root.Add(new ModelNode(ReflectorName, LandmarkMeshes.Reflector, Quaternion.identity,
                new ModelMesh(RoutePostName + "_" + ReflectorName, LandmarkMeshes.RouteReflector()),
                ModelMaterial.PaletteGlowOff));
            return root;
        }

        public static ModelNode CreateBridgeNear()
        {
            return new ModelNode(BridgeNearName, Vector3.zero,
                new ModelMesh(BridgeNearName, LandmarkMeshes.NearAnchors()));
        }

        public static ModelNode CreateBridgeFar()
        {
            var root = new ModelNode(BridgeFarName, Vector3.zero,
                new ModelMesh(BridgeFarName, LandmarkMeshes.FarAnchor()));
            root.Add(new ModelNode(LampName, LandmarkMeshes.FarLamp, Quaternion.identity,
                new ModelMesh(BridgeFarName + "_" + LampName, LandmarkMeshes.FarAnchorLamp()),
                ModelMaterial.PaletteGlowOff));
            return root;
        }

        public static ModelNode CreateBridgeCable()
        {
            return new ModelNode(BridgeCableName, Vector3.zero,
                new ModelMesh(BridgeCableName, LandmarkMeshes.TroughCable()));
        }

        public static ModelNode CreateDishFrame(int variant)
        {
            string name = DishFramePrefix + variant;
            return new ModelNode(name, Vector3.zero, new ModelMesh(name, LandmarkMeshes.DishFrame(variant)));
        }
    }
}
