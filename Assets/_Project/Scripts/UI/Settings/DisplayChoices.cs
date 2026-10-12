using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The Display page's lists (pure logic, EditMode-tested).</summary>
    internal static class DisplayChoices
    {
        /// <summary>
        /// The monitor's resolutions, largest first, each width and height once (the monitor lists the same size at
        /// every refresh rate), those too small to play in (under <paramref name="minWidth"/>) left out.
        /// </summary>
        public static List<Vector2Int> Distinct(IEnumerable<Vector2Int> offered, int minWidth)
        {
            var sizes = new List<Vector2Int>();
            foreach (Vector2Int size in offered)
            {
                if (size.x >= minWidth && !sizes.Contains(size))
                {
                    sizes.Add(size);
                }
            }

            sizes.Sort((a, b) => a.x != b.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));
            return sizes;
        }

        /// <summary>The index of <paramref name="size"/> in <paramref name="sizes"/>, else of the closest one in area.</summary>
        public static int IndexOf(IReadOnlyList<Vector2Int> sizes, Vector2Int size)
        {
            int nearest = 0;
            long best = long.MaxValue;
            for (int i = 0; i < sizes.Count; i++)
            {
                if (sizes[i] == size)
                {
                    return i;
                }

                long difference = System.Math.Abs((long)sizes[i].x * sizes[i].y - (long)size.x * size.y);
                if (difference < best)
                {
                    best = difference;
                    nearest = i;
                }
            }

            return nearest;
        }

        /// <summary>Steps <paramref name="index"/> by <paramref name="direction"/> in a list of <paramref name="count"/>, wrapping round.</summary>
        public static int Step(int index, int direction, int count)
        {
            return ((index + direction) % count + count) % count;
        }
    }
}
