using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What a structure set down at a spot would join in the base's power grid (the build ghost's cable preview): the
    /// nearest built hub, a structure with its own reach, that <see cref="PowerGrid.CanLink"/> links it to, by the same
    /// rule the grid itself connects with. Plain logic, allocation-free.
    /// </summary>
    public static class GridLinkPreview
    {
        /// <summary>True when <paramref name="definition"/> takes part in the power grid at all.</summary>
        public static bool JoinsGrid(StructureDefinition definition)
        {
            return definition.ProduceWatts > 0f || definition.ConsumeWatts > 0f || definition.StoreWattHours > 0f
                || definition.ConnectionRadius > 0f;
        }

        /// <summary>
        /// The nearest built hub a <paramref name="definition"/> standing at <paramref name="position"/> would link
        /// to; false when nothing is in reach (it would stand off the grid).
        /// </summary>
        public static bool TryFindLink(PlacedBase placed, StructureDefinition definition, Vector3 position,
            out Vector3 hub)
        {
            bool found = TryFindHub(placed, definition, position, placed.Count, 0, out PlacedStructure nearest);
            hub = found ? nearest.Pose.Position : position;
            return found;
        }

        /// <summary>
        /// Where the cable of a <paramref name="definition"/> standing at <paramref name="pose"/> runs: from its port
        /// nearest the far end to the nearest built hub's port nearest it, among the first <paramref name="before"/>
        /// structures and never itself (<paramref name="self"/>, 0 for a ghost), or, with no hub in reach, a charging
        /// pad on the apron to the lander's cable channel (<paramref name="hubId"/> 0). False when it runs nowhere.
        /// The build ghost's preview and the built cables both ask this, so a piece gets the cable it was shown.
        /// </summary>
        public static bool TryFindCable(PlacedBase placed, StructureDefinition definition, StructurePose pose,
            int before, int self, ApronCable apron, out Vector3 from, out Vector3 to, out int hubId)
        {
            from = pose.Position;
            to = pose.Position;
            hubId = 0;
            if (TryFindHub(placed, definition, pose.Position, before, self, out PlacedStructure hub))
            {
                to = NearestPort(hub.Definition, hub.Pose, pose.Position);
                from = NearestPort(definition, pose, to);
                hubId = hub.InstanceId;
                return true;
            }

            if (JoinsGrid(definition) && apron.Takes(definition, pose.Position))
            {
                to = apron.Channel;
                from = NearestPort(definition, pose, to);
                return true;
            }

            return false;
        }

        /// <summary>The world point of <paramref name="definition"/>'s port nearest <paramref name="toward"/>.</summary>
        public static Vector3 NearestPort(StructureDefinition definition, StructurePose pose, Vector3 toward)
        {
            Vector3 best = pose.Position;
            float bestSq = float.MaxValue;
            for (int i = 0; i < definition.Ports.Count; i++)
            {
                Vector3 port = definition.Ports[i].WorldPosition(pose);
                float distanceSq = SurfaceRules.HorizontalDistanceSquared(port, toward);
                if (distanceSq < bestSq)
                {
                    bestSq = distanceSq;
                    best = port;
                }
            }

            return best;
        }

        private static bool TryFindHub(PlacedBase placed, StructureDefinition definition, Vector3 position,
            int before, int self, out PlacedStructure hub)
        {
            hub = default;
            if (!JoinsGrid(definition))
            {
                return false;
            }

            float bestSq = float.MaxValue;
            int count = Mathf.Min(before, placed.Count);
            for (int i = 0; i < count; i++)
            {
                PlacedStructure other = placed.StructureAt(i);
                float otherReach = other.Definition.ConnectionRadius;
                if (other.InstanceId == self || !other.IsBuilt || otherReach <= 0f
                    || !PowerGrid.CanLink(position, definition.ConnectionRadius, other.Pose.Position, otherReach))
                {
                    continue;
                }

                float distanceSq = SurfaceRules.HorizontalDistanceSquared(position, other.Pose.Position);
                if (distanceSq < bestSq)
                {
                    bestSq = distanceSq;
                    hub = other;
                }
            }

            return bestSq < float.MaxValue;
        }
    }
}
