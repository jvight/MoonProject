namespace MoonProject.Core
{
    /// <summary>
    /// The named content anchors of the generated world, registered in the <see cref="GameContext"/> by the World
    /// domain. The set is fixed after initialisation (same seed, same anchors), so systems may cache the entries.
    /// </summary>
    public interface IWorldAnchors
    {
        int Count { get; }

        WorldAnchor Get(int index);

        /// <summary>Looks an anchor up by id (see <see cref="WorldAnchorIds"/>). For initialisation, not per frame.</summary>
        bool TryGet(string id, out WorldAnchor anchor);
    }
}
