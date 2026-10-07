namespace MoonProject.Gameplay
{
    /// <summary>
    /// Everything Bell's signals could ever point at (cassettes, crew log caches, relics), in a fixed order. The set is
    /// fixed after initialisation; each candidate's state is read fresh on every call. Allocation-free.
    /// </summary>
    public interface ISignalTargets
    {
        int Count { get; }

        SignalCandidate Get(int index);
    }
}
