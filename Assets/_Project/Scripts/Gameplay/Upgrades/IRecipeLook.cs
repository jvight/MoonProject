namespace MoonProject.Gameplay
{
    /// <summary>
    /// What recipe 07's view rests on (docs/features/M3-24 B), registered in the GameContext: looking at the radio
    /// tower's port hopper names the tower, looking at a sheet pinned on Kenji's board names its kit, so UI can show
    /// that recipe's card. Read every frame; allocation-free.
    /// </summary>
    public interface IRecipeLook
    {
        /// <summary>The upgrade id whose next level is looked at, or null when the view rests on no recipe.</summary>
        string Looked { get; }

        /// <summary>True when <see cref="Looked"/> is a kit's sheet on the board (its blueprint is learned).</summary>
        bool IsSheet { get; }
    }
}
