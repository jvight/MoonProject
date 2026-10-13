using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The containers finds live in (0.5.5 owner feedback §2), in Generated/Art/Props, each root on the ground at its
    /// middle, +Z its front, placed by gameplay:
    /// <list type="bullet">
    /// <item><c>CoilCrate</c> (the depot's heart, the Tower Coil), <c>TapeCase</c> (Ro's, beside Bell, the terminus
    /// tape), <c>JunctionBox</c> (the garage's heart, the transformer, on its post) and <c>SupplyPod</c> (the fallen
    /// pod at canyon.ledge, the ledge tape): a sealed body whose <c>Lid</c> is pivoted on its back hinge (turn it
    /// about local X, negative, to lift it open) holding <c>CutPoint</c> (+Z out of the front seam) and <c>Seal</c>
    /// (M_LowPolyGlowOff, the seam's glow until the cut), and an <c>ItemSocket</c> where the find sits inside. The pod
    /// also has <c>Glow</c>, its cracked beacon (M_LowPolyGlowOff, World lights it warm).</item>
    /// <item><c>SpareCrate</c>, the tape deck's sealed crate stencilled "07 · SPARE" (0.5.5 §2), the same layout,
    /// sized for Kenji's bench and set on the garage site's <c>SpareCrateSocket</c> with identity: no dust skirt, it
    /// stands on the bench, not the ground.</item>
    /// <item><c>Cabinet</c>, a relay mast's service cabinet (the relay spares), set on the mast's
    /// <c>CabinetSocket</c> with identity: the same layout, but its <c>Lid</c> is a door hinged on its left edge
    /// (turn it about local Y by <see cref="LidOpenDegrees"/> to swing it open), with <c>CutPoint</c> and
    /// <c>Seal</c> on its free edge.</item>
    /// <item><c>TillySled</c>, Tilly's crashed survey sled in her crater: <c>PartSocket_0</c> wedged in its frame behind
    /// the <c>Clamp</c> (with <c>CutPoint</c> and <c>Seal</c>; one cut frees it), <c>PartSocket_1</c> in the dark gap
    /// under its tipped hull (dug) and the <c>SensorPod</c> container (its <c>Lid</c>, <c>CutPoint</c>, <c>Seal</c>)
    /// holding <c>PartSocket_2</c>.</item>
    /// </list>
    /// </summary>
    public static class LootContainerBuilder
    {
        public const string CoilCrateName = "CoilCrate";
        public const string TapeCaseName = "TapeCase";
        public const string JunctionBoxName = "JunctionBox";
        public const string SupplyPodName = "SupplyPod";
        public const string TillySledName = "TillySled";
        public const string CabinetName = "Cabinet";
        public const string SpareCrateName = "SpareCrate";
        public const string LidName = "Lid";
        public const string CutPointName = SiteModelBuilder.CutPointName;
        public const string SealName = "Seal";
        public const string ItemSocketName = "ItemSocket";
        public const string GlowName = "Glow";
        public const string ClampName = "Clamp";
        public const string SensorPodName = "SensorPod";
        public const string PartSocketPrefix = "PartSocket_";

        /// <summary>
        /// How far a lid turns to open: about its local X, negative lifts it; a cabinet's door turns about its local
        /// Y, negative swings it out.
        /// </summary>
        public const float LidOpenDegrees = -105f;

        private const float JunctionPost = 0.5f;

        // The sled's layout (sled space): the frame along X, the hull tipped onto its side at the back, the sensor pod
        // thrown clear in front.
        private static readonly Vector3 SledHull = new Vector3(-0.15f, 0f, -0.42f);
        private static readonly Vector3 SledClamp = new Vector3(0.38f, 0.32f, 0.18f);
        private static readonly Vector3 SledPod = new Vector3(0.55f, 0f, 0.75f);

        [MoonBuilder("Art/Containers", 176)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            Material glowOff = PaletteAssetBuilder.LoadGlowOffMaterial();
            foreach (ModelNode container in new[]
            {
                CreateCoilCrate(), CreateTapeCase(), CreateJunctionBox(), CreateSupplyPod(), CreateTillySled(),
                CreateCabinet(),
            })
            {
                ModelPrefabWriter.Write(GroundOcclusion.Ground(container), ArtPaths.PropFolder, material, glowOff);
            }

            ModelPrefabWriter.Write(CreateSpareCrate(), ArtPaths.PropFolder, material, glowOff);
            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateCoilCrate()
        {
            return Container(CoilCrateName, LootContainerMeshes.CoilCrate, 0f);
        }

        public static ModelNode CreateTapeCase()
        {
            return Container(TapeCaseName, LootContainerMeshes.TapeCase, 0f);
        }

        public static ModelNode CreateJunctionBox()
        {
            return Container(JunctionBoxName, LootContainerMeshes.JunctionBox, JunctionPost);
        }

        public static ModelNode CreateSupplyPod()
        {
            LootContainerMeshes.Shape hatch = LootContainerMeshes.PodHatch;
            var root = new ModelNode(SupplyPodName, Vector3.zero,
                new ModelMesh(SupplyPodName, LootContainerMeshes.PodBody()));
            AddLid(root, SupplyPodName, hatch, LootContainerMeshes.PodHatchHinge);
            root.Add(new ModelNode(ItemSocketName, LootContainerMeshes.PodItem));
            root.Add(new ModelNode(GlowName, LootContainerMeshes.PodBeacon, Quaternion.identity,
                new ModelMesh(SupplyPodName + "_" + GlowName, LootContainerMeshes.PodBeaconGlass()),
                ModelMaterial.PaletteGlowOff));
            return root;
        }

        public static ModelNode CreateSpareCrate()
        {
            LootContainerMeshes.Shape shape = LootContainerMeshes.SpareCrate;
            var root = new ModelNode(SpareCrateName, Vector3.zero,
                new ModelMesh(SpareCrateName, LootContainerMeshes.SpareCrateBody()));
            AddLid(root, SpareCrateName, shape, shape.Hinge);
            root.Add(new ModelNode(ItemSocketName, shape.Item));
            return root;
        }

        public static ModelNode CreateCabinet()
        {
            var root = new ModelNode(CabinetName, Vector3.zero, new ModelMesh(CabinetName, RelayMeshes.CabinetBody()));
            ModelNode door = root.Add(new ModelNode(LidName, RelayMeshes.CabinetHinge,
                new ModelMesh(CabinetName + "_" + LidName, RelayMeshes.CabinetDoor())));
            door.Add(new ModelNode(CutPointName, RelayMeshes.CabinetSealPoint, Quaternion.identity));
            door.Add(new ModelNode(SealName, Vector3.zero, Quaternion.identity,
                new ModelMesh(CabinetName + "_" + SealName, RelayMeshes.CabinetSeal()), ModelMaterial.PaletteGlowOff));
            root.Add(new ModelNode(ItemSocketName, RelayMeshes.CabinetItem));
            return root;
        }

        public static ModelNode CreateTillySled()
        {
            var root = new ModelNode(TillySledName, Vector3.zero, new ModelMesh(TillySledName, SledFrame()));
            ModelNode clamp = root.Add(new ModelNode(ClampName, SledClamp,
                new ModelMesh(TillySledName + "_" + ClampName, SledClampMesh())));
            clamp.Add(new ModelNode(CutPointName, new Vector3(0f, 0f, 0.05f), Quaternion.identity));
            clamp.Add(new ModelNode(SealName, Vector3.zero, Quaternion.identity,
                new ModelMesh(TillySledName + "_ClampSeal", SledClampSeal()), ModelMaterial.PaletteGlowOff));
            root.Add(new ModelNode(PartSocketPrefix + "0", SledClamp + new Vector3(0f, 0f, -0.12f)));
            root.Add(new ModelNode(PartSocketPrefix + "1", SledHull + new Vector3(0.05f, 0.02f, 0.2f)));
            root.Add(Container(SensorPodName, SensorPodShape, 0f, TillySledName + "_", SledPod,
                Rotation(new Vector3(0f, -25f, 0f)), PartSocketPrefix + "2"));
            return root;
        }

        private static readonly LootContainerMeshes.Shape SensorPodShape = new LootContainerMeshes.Shape(
            new Vector3(0.3f, 0.2f, 0.26f), 0.05f, PaletteSwatch.Cream, PaletteSwatch.Honey);

        private static ModelNode Container(string name, LootContainerMeshes.Shape shape, float post)
        {
            return Container(name, shape, post, string.Empty, Vector3.zero, Quaternion.identity, ItemSocketName);
        }

        private static ModelNode Container(string name, LootContainerMeshes.Shape shape, float post, string meshPrefix,
            Vector3 position, Quaternion rotation, string itemName)
        {
            var root = new ModelNode(name, position, rotation,
                new ModelMesh(meshPrefix + name, LootContainerMeshes.Body(shape, post)));
            AddLid(root, meshPrefix + name, shape, shape.Hinge + Vector3.up * post);
            root.Add(new ModelNode(itemName, shape.Item + Vector3.up * post));
            return root;
        }

        private static void AddLid(ModelNode root, string meshName, LootContainerMeshes.Shape shape, Vector3 hinge)
        {
            ModelNode lid = root.Add(new ModelNode(LidName, hinge,
                new ModelMesh(meshName + "_" + LidName, LootContainerMeshes.Lid(shape))));
            lid.Add(new ModelNode(CutPointName, shape.SealPoint, Quaternion.identity));
            lid.Add(new ModelNode(SealName, Vector3.zero, Quaternion.identity,
                new ModelMesh(meshName + "_" + SealName, LootContainerMeshes.Seal(shape)),
                ModelMaterial.PaletteGlowOff));
        }

        /// <summary>
        /// The sled's wreck (sled space): two skids and their cross bars, a bent instrument mast, and the hull tipped
        /// onto its side at the back, propped on a rock so there's a dark gap under its edge.
        /// </summary>
        private static LowPolyMeshBuilder SledFrame()
        {
            var b = new LowPolyMeshBuilder(700);
            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * 0.28f;
                SiteKit.Bar(b, new Vector3(-0.7f, 0.05f, z), new Vector3(0.62f, 0.05f, z), 0.05f, PaletteSwatch.Metal);
                SiteKit.Bar(b, new Vector3(0.62f, 0.05f, z), new Vector3(0.75f, 0.16f, z), 0.05f, PaletteSwatch.Metal);
                for (int post = -1; post <= 1; post += 2)
                {
                    SiteKit.Bar(b, new Vector3(post * 0.45f, 0.05f, z), new Vector3(post * 0.45f, 0.36f, z), 0.04f,
                        PaletteSwatch.Metal);
                }
            }

            foreach (float x in new[] { -0.45f, 0.45f })
            {
                SiteKit.Bar(b, new Vector3(x, 0.36f, -0.28f), new Vector3(x, 0.36f, 0.28f), 0.04f, PaletteSwatch.Metal);
            }

            var kink = new Vector3(0.52f, 0.78f, 0.12f);
            SiteKit.Bar(b, new Vector3(0.45f, 0.36f, 0f), kink, 0.035f, PaletteSwatch.Metal);
            SiteKit.Bar(b, kink, new Vector3(0.8f, 0.86f, 0.3f), 0.03f, PaletteSwatch.Metal);
            b.Box(At(new Vector3(0f, 0.38f, 0f)), new Vector3(0.95f, 0.025f, 0.6f), PaletteSwatch.Charcoal);

            b.Icosphere(At(SledHull + new Vector3(0.35f, 0.07f, 0.12f)), 0.1f, 0, PaletteSwatch.RockDark,
                new Displacement(77, 0.02f, 8f));
            b.Prism(At(SledHull + new Vector3(0f, 0.005f, 0.18f)), 0.24f, 0.01f, 8, PaletteSwatch.Charcoal);
            Matrix4x4 hull = At(SledHull + new Vector3(0f, 0.22f, 0f), new Vector3(-62f, 8f, 0f));
            MeshRange shell = b.Box(hull, new Vector3(0.82f, 0.18f, 0.5f), PaletteSwatch.Cream, 0.03f);
            SiteKit.DustCap(b, shell);
            b.Box(hull * At(0f, 0.095f, 0.1f), new Vector3(0.7f, 0.01f, 0.08f), PaletteSwatch.Honey);
            b.Box(hull * At(0.25f, 0.095f, -0.12f), new Vector3(0.14f, 0.01f, 0.14f), PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>The bracket that pins a part in the frame (clamp space, +Z out of its face).</summary>
        private static LowPolyMeshBuilder SledClampMesh()
        {
            var b = new LowPolyMeshBuilder(60);
            b.Box(Matrix4x4.identity, new Vector3(0.16f, 0.12f, 0.05f), PaletteSwatch.Charcoal, 0.008f);
            b.Box(At(0f, 0f, 0.03f), new Vector3(0.04f, 0.04f, 0.02f), PaletteSwatch.Metal);
            return b;
        }

        private static LowPolyMeshBuilder SledClampSeal()
        {
            var b = new LowPolyMeshBuilder(24);
            b.Box(At(0f, 0f, 0.027f), new Vector3(0.17f, 0.012f, 0.008f), PaletteSwatch.LampGlass);
            return b;
        }
    }
}
