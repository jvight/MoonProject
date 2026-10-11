using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Which cubby of the keepsake shelf a relic takes: the free one nearest to it.</summary>
    public static class ShelfSlots
    {
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
    }
}
