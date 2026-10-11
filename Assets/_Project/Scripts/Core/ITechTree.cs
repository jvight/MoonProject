namespace MoonProject.Core
{
    /// <summary>
    /// What 07 and the station know and have built (docs/features/M3-21), registered in the <see cref="GameContext"/>
    /// by the Gameplay domain and announced with <see cref="Events.TechNodeChanged"/>. The lab's tech board shows every
    /// node's state; the lander's big porthole lights one lamp per known node. Allocation-free: safe to poll every
    /// frame.
    /// </summary>
    public interface ITechTree
    {
        TechNodeState StateOf(TechNodeId node);

        /// <summary>Nodes that are decoded or built: the porthole's lamp count.</summary>
        int KnownCount { get; }
    }
}
