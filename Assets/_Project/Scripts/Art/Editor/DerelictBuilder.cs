using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The robots left behind (0.5.5 owner feedback §1.7), <c>Derelict_&lt;name&gt;</c> in Generated/Art/Props, each
    /// root on the ground at its World anchor, +Z the anchor's Forward, laid out like a site: its <c>Skeleton</c>, any
    /// <c>Salvage_&lt;n&gt;_&lt;Material&gt;</c> cut pieces pivoted where they hold with a <c>CutPoint</c> (+Z out of
    /// the cut face), and the <c>FragmentSocket_&lt;Blueprint&gt;_&lt;n&gt;</c> its scan fragments rest on, on its
    /// body.
    /// <list type="bullet">
    /// <item><c>Derelict_06</c> (micro.06, slumped against relay.2's hut): Cell Pack II's three sockets round its open
    /// battery bay.</item>
    /// <item><c>Derelict_Mule</c> (micro.mule, on the canyon road): the Warm Headlamp's two sockets by its cracked lamp
    /// head, and its track skirt to cut (Metal).</item>
    /// <item><c>Derelict_HaulBot</c> (inside the drill site): the Boost Coils' three sockets on its open side door and
    /// rear deck, and its strapped load to cut (a crate, Metal; a cable drum, Wiring).</item>
    /// <item><c>Derelict_Drone</c> (micro.drone, at relay.0's back-left guy stake, Forward toward the mast): its snagged
    /// camera boom to cut (Optics), which frees the <c>Drone</c> node to fall.</item>
    /// </list>
    /// Nothing glows.
    /// </summary>
    public static class DerelictBuilder
    {
        public const string Prefix = "Derelict_";
        public const string ElderName = Prefix + "06";
        public const string MuleName = Prefix + "Mule";
        public const string HaulBotName = Prefix + "HaulBot";
        public const string DroneName = Prefix + "Drone";
        public const string DroneNodeName = "Drone";

        [MoonBuilder("Art/Derelicts", 177)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            foreach (ModelNode derelict in new[] { CreateElder(), CreateMule(), CreateHaulBot(), CreateDrone() })
            {
                ModelPrefabWriter.Write(GroundOcclusion.Ground(derelict), ArtPaths.PropFolder, material);
            }

            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateElder()
        {
            ModelNode root = Root(ElderName, DerelictMeshes.Elder());
            for (int i = 0; i < 3; i++)
            {
                AddSocket(root, "CellPack", i, DerelictMeshes.ElderSocket(i, out Quaternion rotation), rotation);
            }

            return root;
        }

        public static ModelNode CreateMule()
        {
            ModelNode root = Root(MuleName, DerelictMeshes.Mule());
            AddPiece(root, MuleName, 0, "Metal", DerelictMeshes.MulePose.MultiplyPoint3x4(DerelictMeshes.MuleSkirt),
                DerelictMeshes.MuleFallRotation,
                DerelictMeshes.MuleSkirtPiece(), new Vector3(0.02f, -0.1f, 0f), Vector3.right);
            for (int i = 0; i < 2; i++)
            {
                AddSocket(root, "WarmHeadlamp", i, DerelictMeshes.MuleSocket(i, out Quaternion rotation), rotation);
            }

            return root;
        }

        public static ModelNode CreateHaulBot()
        {
            ModelNode root = Root(HaulBotName, DerelictMeshes.HaulSled());
            AddPiece(root, HaulBotName, 0, "Metal", DerelictMeshes.SledLoad[0], Quaternion.identity,
                DerelictMeshes.SledCrate(), new Vector3(0f, 0.32f, 0f), Vector3.up);
            AddPiece(root, HaulBotName, 1, "Wiring", DerelictMeshes.SledLoad[1], Quaternion.identity,
                DerelictMeshes.SledDrum(), new Vector3(0f, 0.4f, 0f), Vector3.up);
            for (int i = 0; i < 3; i++)
            {
                AddSocket(root, "BoostCoils", i, DerelictMeshes.SledSocket(i, out Quaternion rotation), rotation);
            }

            return root;
        }

        public static ModelNode CreateDrone()
        {
            ModelNode root = Root(DroneName, DerelictMeshes.DroneWire());
            AddPiece(root, DroneName, 0, "Optics", DerelictMeshes.DroneSnag, Quaternion.identity,
                DerelictMeshes.DroneBoom(), Vector3.zero, Vector3.right);
            root.Add(new ModelNode(DroneNodeName, DerelictMeshes.DroneHang,
                Place.Rotation(DerelictMeshes.DroneHangEuler),
                new ModelMesh(DroneName + "_" + DroneNodeName, DerelictMeshes.Drone())));
            return root;
        }

        private static ModelNode Root(string name, LowPolyMeshBuilder skeleton)
        {
            var root = new ModelNode(name, Vector3.zero);
            root.Add(new ModelNode(SiteModelBuilder.SkeletonName, Vector3.zero,
                new ModelMesh(name + "_" + SiteModelBuilder.SkeletonName, skeleton)));
            return root;
        }

        private static void AddPiece(ModelNode root, string name, int index, string material, Vector3 pivot,
            Quaternion rotation, LowPolyMeshBuilder geometry, Vector3 cut, Vector3 cutNormal)
        {
            string pieceName = $"Salvage_{index}_{material}";
            ModelNode piece = root.Add(new ModelNode(pieceName, pivot, rotation,
                new ModelMesh($"{name}_Salvage_{index}", geometry)));
            piece.Add(new ModelNode(SiteModelBuilder.CutPointName, cut, Quaternion.LookRotation(cutNormal,
                Mathf.Abs(cutNormal.y) > 0.9f ? Vector3.forward : Vector3.up)));
        }

        private static void AddSocket(ModelNode root, string blueprint, int index, Vector3 position, Quaternion rotation)
        {
            root.Add(new ModelNode(SiteModelBuilder.FragmentSocketPrefix + blueprint + "_" + index, position, rotation));
        }
    }
}
