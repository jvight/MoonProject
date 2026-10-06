namespace MoonProject.Core
{
    /// <summary>
    /// Every friend machine in the world, registered in the <see cref="GameContext"/> by the Gameplay domain. The set
    /// is fixed after initialisation, so systems may cache the entries.
    /// </summary>
    public interface IFriendRoster
    {
        int Count { get; }

        IFriendState Get(int index);
    }
}
