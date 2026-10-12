using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Kenji's garage service port (docs/design/first-hour-v2.md §3) in Generated/Art/Sites: <c>GaragePort</c> with
    /// its <c>Tray</c> node (where a piece 07 sets down rests) and its <c>ArmTip</c> node (where the folded arm fits a
    /// piece onto 07). Gameplay stands it by the garage and lays its pad in front.
    /// </summary>
    public static class GaragePortBuilder
    {
        public const string PortName = "GaragePort";
        public const string TrayName = "Tray";
        public const string ArmTipName = "ArmTip";

        [MoonBuilder("Art/GaragePort", 152)]
        public static void Build()
        {
            ModelPrefabWriter.Write(CreatePort(), ArtPaths.SitesFolder, PaletteAssetBuilder.LoadMaterial());
            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreatePort()
        {
            var root = new ModelNode(PortName, Vector3.zero, new ModelMesh(PortName, GaragePortMeshes.Port()));
            root.Add(new ModelNode(TrayName, GaragePortMeshes.TrayTop));
            root.Add(new ModelNode(ArmTipName, GaragePortMeshes.ArmTip));
            return root;
        }
    }
}
