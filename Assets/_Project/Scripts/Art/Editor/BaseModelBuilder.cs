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
    /// lamps) are separate renderers so gameplay can brighten the base as it comes back to life.
    /// </summary>
    public static class BaseModelBuilder
    {
        public const string LanderName = "Lander";
        public const string ShelfName = "MuseumShelf";
        public const string TowerPrefix = "RadioTower_L";

        /// <summary>Width of the museum shelf (it stands centred on <see cref="ShelfAnchor"/>).</summary>
        public const float ShelfWidth = MuseumShelfMeshes.Width;

        /// <summary>Side of the square plinth every radio tower stage stands on, and its height.</summary>
        public const float TowerPlinthSize = RadioTowerMeshes.PlinthSize;

        public const float TowerPlinthHeight = RadioTowerMeshes.PlinthHeight;

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

        [MoonBuilder("Art/Base", 140)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            ModelPrefabWriter.Write(CreateLander(), ArtPaths.BaseFolder, material);
            ModelPrefabWriter.Write(CreateShelf(), ArtPaths.BaseFolder, material);
            ModelPrefabWriter.Write(CreateWorkbench(), ArtPaths.BaseFolder, material);
            ModelPrefabWriter.Write(CreateCassetteShelf(), ArtPaths.BaseFolder, material);
            for (int level = RadioTowerMeshes.MinLevel; level <= RadioTowerMeshes.MaxLevel; level++)
            {
                ModelPrefabWriter.Write(CreateTower(level), ArtPaths.BaseFolder, material);
            }

            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateLander()
        {
            var lander = new ModelNode(LanderName, Vector3.zero, new ModelMesh(LanderName, LanderMeshes.Hull()));
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
            return lander;
        }

        public static ModelNode CreateShelf()
        {
            var shelf = new ModelNode(ShelfName, Vector3.zero, new ModelMesh(ShelfName, MuseumShelfMeshes.Cabinet()));
            shelf.Add(new ModelNode("Lights", Vector3.zero,
                new ModelMesh(ShelfName + "_Lights", MuseumShelfMeshes.Lights())));
            for (int i = 0; i < MuseumShelfMeshes.SlotCount; i++)
            {
                shelf.Add(new ModelNode("Slot_" + i.ToString(CultureInfo.InvariantCulture),
                    MuseumShelfMeshes.SlotPosition(i)));
            }

            return shelf;
        }

        /// <summary>
        /// Kenji's workbench: root on the ground at the bench centre (+Z = the front where 07 parks), a Lights glow
        /// child (the hanging work lamp's bulb) and an empty SparkSocket between the vice jaws.
        /// </summary>
        public static ModelNode CreateWorkbench()
        {
            var bench = new ModelNode(WorkbenchName, Vector3.zero,
                new ModelMesh(WorkbenchName, WorkbenchMeshes.Bench()));
            bench.Add(new ModelNode("Lights", WorkbenchMeshes.LampBulb,
                new ModelMesh(WorkbenchName + "_Lights", WorkbenchMeshes.LampBulbMesh())));
            bench.Add(new ModelNode("SparkSocket", WorkbenchMeshes.Sparks));
            return bench;
        }

        /// <summary>
        /// Bell's tape rack: root on the ground at the centre of its base (+Z = front), Slot_0..7 empties on the
        /// cubby floors (+Y up, +Z = label facing) where a tape stands on its bottom edge.
        /// </summary>
        public static ModelNode CreateCassetteShelf()
        {
            var shelf = new ModelNode(CassetteShelfName, Vector3.zero,
                new ModelMesh(CassetteShelfName, CassetteShelfMeshes.Rack()));
            for (int i = 0; i < CassetteShelfMeshes.SlotCount; i++)
            {
                shelf.Add(new ModelNode("Slot_" + i.ToString(CultureInfo.InvariantCulture),
                    CassetteShelfMeshes.SlotPosition(i)));
            }

            return shelf;
        }

        /// <summary>Radio tower stage <paramref name="level"/> (1..3).</summary>
        public static ModelNode CreateTower(int level)
        {
            string name = TowerPrefix + level.ToString(CultureInfo.InvariantCulture);
            var tower = new ModelNode(name, Vector3.zero, new ModelMesh(name, RadioTowerMeshes.Structure(level)));
            tower.Add(new ModelNode("Lights", Vector3.zero,
                new ModelMesh(name + "_Lights", RadioTowerMeshes.Lights(level))));
            tower.Add(new ModelNode("BeaconSocket", RadioTowerMeshes.BeaconPosition(level)));
            tower.Add(new ModelNode("BellCorner", BellCorner, Place.Rotation(new Vector3(0f, BellCornerYaw, 0f))));
            tower.Add(new ModelNode("CassetteShelfAnchor", CassetteShelfAnchor,
                Place.Rotation(new Vector3(0f, CassetteShelfYaw, 0f))));
            return tower;
        }
    }
}
