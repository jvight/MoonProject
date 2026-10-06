using System.Globalization;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's home base per the M2 content contract (docs/ARCHITECTURE.md): <c>Lander</c>, <c>MuseumShelf</c> and
    /// <c>RadioTower_L1..L3</c> in Generated/Art/Base. Meshes only, each pivoted at its ground-contact centre so it
    /// stands on an anchor. The lander also carries friend perches (<c>FriendSocket_&lt;id&gt;</c>: an empty on
    /// top of the perch, +Y up, +Z = the hatch side). Glowing parts (windows, shelf lights, tower lamps) are
    /// separate renderers so gameplay can brighten the base as it comes back to life.
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

        [MoonBuilder("Art/Base", 140)]
        public static void Build()
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            ModelPrefabWriter.Write(CreateLander(), ArtPaths.BaseFolder, material);
            ModelPrefabWriter.Write(CreateShelf(), ArtPaths.BaseFolder, material);
            ModelPrefabWriter.Write(CreateWorkbench(), ArtPaths.BaseFolder, material);
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

        /// <summary>Radio tower stage <paramref name="level"/> (1..3).</summary>
        public static ModelNode CreateTower(int level)
        {
            string name = TowerPrefix + level.ToString(CultureInfo.InvariantCulture);
            var tower = new ModelNode(name, Vector3.zero, new ModelMesh(name, RadioTowerMeshes.Structure(level)));
            tower.Add(new ModelNode("Lights", Vector3.zero,
                new ModelMesh(name + "_Lights", RadioTowerMeshes.Lights(level))));
            tower.Add(new ModelNode("BeaconSocket", RadioTowerMeshes.BeaconPosition(level)));
            return tower;
        }
    }
}
