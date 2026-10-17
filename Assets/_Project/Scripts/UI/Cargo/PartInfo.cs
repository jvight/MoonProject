using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// What <see cref="FaceInventoryView"/> says about each material and story part in 07's hold: a localized name
    /// plus one short description line (at most ~52 characters, zero slot codes, zero key hints).
    /// </summary>
    internal static class PartInfo
    {
        public static string MaterialName(SalvageMaterial material, ILocalization localization)
        {
            return localization.Get(UiKeys.MaterialName(material));
        }

        public static string MaterialDescription(SalvageMaterial material, ILocalization localization)
        {
            return localization.Get(UiKeys.MaterialName(material) + ".job");
        }

        public static string StoryPartName(string partId, ILocalization localization)
        {
            return localization.Get(UiKeys.PickupName(partId));
        }

        public static string StoryPartDescription(string partId, ILocalization localization)
        {
            return localization.Get(UiKeys.PickupName(partId) + ".job");
        }
    }
}
