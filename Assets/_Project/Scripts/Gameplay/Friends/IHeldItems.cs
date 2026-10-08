namespace MoonProject.Gameplay
{
    /// <summary>
    /// What 07 holds besides materials: the items a friend's repair may need next to its parts (Bell needs a cassette,
    /// Moss will need a seed). Allocation-free; friends poll it while they wait.
    /// </summary>
    public interface IHeldItems
    {
        bool Holds(string itemId);
    }
}
