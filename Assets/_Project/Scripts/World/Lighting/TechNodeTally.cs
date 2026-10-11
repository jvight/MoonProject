using System;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// What the lab's lamps know of the tech tree (docs/features/M3-21), kept from <c>TechNodeChanged</c> alone: each
    /// node's state and how many are known (decoded or built), the big porthole's lamp count. Restored events after a
    /// load rebuild it. Plain logic, allocation-free after construction.
    /// </summary>
    public sealed class TechNodeTally
    {
        private static readonly int NodeCount = Enum.GetValues(typeof(TechNodeId)).Length;

        private readonly TechNodeState[] _states = new TechNodeState[NodeCount];
        private int _known;

        /// <summary>Nodes decoded or built.</summary>
        public int KnownCount => _known;

        public TechNodeState StateOf(TechNodeId node)
        {
            return _states[(int)node];
        }

        /// <summary>Node <paramref name="node"/> is now <paramref name="state"/>.</summary>
        public void Apply(TechNodeId node, TechNodeState state)
        {
            int index = (int)node;
            if (index < 0 || index >= NodeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(node), node, "Not a tech node.");
            }

            bool wasKnown = _states[index] != TechNodeState.Unknown;
            bool isKnown = state != TechNodeState.Unknown;
            _states[index] = state;
            _known += (isKnown ? 1 : 0) - (wasKnown ? 1 : 0);
        }
    }
}
