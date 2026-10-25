using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The network link the range ring shows with it (opening-v3-payoff-audit §2: the mast is the hub, a relay a node
    /// that needs a part, then a charge point): drawn on the ground from the hub to the node, dashed while the node
    /// still needs its part, solid once it charges.
    /// </summary>
    public interface IRingLink
    {
        /// <summary>The link now, from the hub to the node; false when there is none to show yet.</summary>
        /// <param name="needsPart">The node still waits for its part: the link is dashed.</param>
        bool TryGetLink(out Vector3 hub, out Vector3 node, out bool needsPart);
    }
}
