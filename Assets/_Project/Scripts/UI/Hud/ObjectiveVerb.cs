using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>The verb an objective step asks the player to do, when it needs a key the player may not know.</summary>
    internal enum ObjectiveVerb
    {
        None = 0,
        Scan = 1,
        Cut = 2,
        Build = 3,
    }

    /// <summary>Which verb each opening step needs (docs/design/opening-v2.md §2, §3).</summary>
    internal static class ObjectiveVerbs
    {
        public static ObjectiveVerb Of(string objectiveKey)
        {
            switch (objectiveKey)
            {
                case OpeningKeys.ScanLander:
                case OpeningKeys.FindSolar:
                case OpeningKeys.ScanTower:
                    return ObjectiveVerb.Scan;
                case OpeningKeys.CutOptics:
                    return ObjectiveVerb.Cut;
                case OpeningKeys.BuildPad:
                case OpeningKeys.BuildSolar:
                case OpeningKeys.BuildBatteryBank:
                case OpeningKeys.BuildMast:
                    return ObjectiveVerb.Build;
                default:
                    return ObjectiveVerb.None;
            }
        }
    }
}
