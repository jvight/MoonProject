namespace MoonProject.Gameplay
{
    /// <summary>
    /// The friends' progress for the UI (the 0/3 parts readout near a broken friend, the repair prompt, the log card),
    /// in the same order as Core's <c>IFriendRoster</c>; registered in the GameContext. Allocation-free: poll it every
    /// frame.
    /// </summary>
    public interface IFriendStatuses
    {
        int Count { get; }

        FriendDefinition Definition(int index);

        FriendStatus Status(int index);
    }
}
