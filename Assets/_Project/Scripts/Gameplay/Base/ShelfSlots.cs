using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Which museum slot a relic takes: the free one nearest to where it was let go.</summary>
    public static class ShelfSlots
    {
        /// <summary>A relic resting on the dust just below the shelf's base still counts as at the shelf (m).</summary>
        private const float BelowShelf = 1f;

        /// <summary>Index of the nearest free slot to <paramref name="point"/>, or -1 when the shelf is full.</summary>
        public static int Nearest(Vector3 point, IReadOnlyList<Vector3> slots, IReadOnlyList<bool> taken)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < slots.Count; i++)
            {
                if (taken[i])
                {
                    continue;
                }

                float distance = (slots[i] - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>
        /// True when <paramref name="point"/> is inside the deposit zone around <paramref name="shelf"/>.
        /// </summary>
        public static bool InZone(Vector3 point, Vector3 shelf, float radius, float height)
        {
            float dy = point.y - shelf.y;
            return dy >= -BelowShelf && dy <= height && SurfaceRules.HorizontalDistance(point, shelf) <= radius;
        }
    }
}
