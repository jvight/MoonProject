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
            hub = position;
            if (!JoinsGrid(definition))
            {
                return false;
            }

            float bestSq = float.MaxValue;
            for (int i = 0; i < placed.Count; i++)
            {
                PlacedStructure other = placed.StructureAt(i);
                float otherReach = other.Definition.ConnectionRadius;
                if (!other.IsBuilt || otherReach <= 0f
                    || !PowerGrid.CanLink(position, definition.ConnectionRadius, other.Pose.Position, otherReach))
                {
                    continue;
                }

                float distanceSq = SurfaceRules.HorizontalDistanceSquared(position, other.Pose.Position);
                if (distanceSq < bestSq)
                {
                    bestSq = distanceSq;
                    hub = other.Pose.Position;
                }
            }

            return bestSq < float.MaxValue;
        }
    }
}
