namespace MoonProject.Gameplay
{
    /// <summary>Which station sells an upgrade (the UI can dress its offer panel accordingly).</summary>
    public enum UpgradeStationKind
    {
        /// <summary>The radio tower's pad: base upgrades (signal reach, warmth).</summary>
        RadioTower = 0,

        /// <summary>Kenji's workbench: rover abilities.</summary>
        Workshop = 1,
    }
}
