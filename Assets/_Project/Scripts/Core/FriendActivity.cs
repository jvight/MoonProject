namespace MoonProject.Core
{
    /// <summary>What a friend machine is doing right now (drives voices, body language and UI).</summary>
    public enum FriendActivity
    {
        Dormant = 0,
        Repairing = 1,
        Following = 2,
        Spotting = 3,
        Home = 4,
        Napping = 5,
    }
}
