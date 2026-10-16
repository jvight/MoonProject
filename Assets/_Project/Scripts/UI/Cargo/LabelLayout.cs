using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// Where the socket names sit on the 07 tab's portrait so none overlaps another (pure, EditMode-tested): sockets in
    /// the left half of the picture get their name in a column down the left edge, the others in a column down the
    /// right edge, each name as near its own height as the names above it allow. The sockets themselves stay on their
    /// true points; a short leader line joins each to its name. Allocation-free.
    /// </summary>
    internal static class LabelLayout
    {
        /// <summary>
        /// Fills <paramref name="centreY"/> and <paramref name="right"/> for every index of
        /// <paramref name="points"/> that <paramref name="wants"/> a name.
        /// </summary>
        /// <param name="points">True socket points in pixels of the portrait, origin top left.</param>
        /// <param name="wants">Which sockets carry a name.</param>
        /// <param name="width">Portrait width in pixels.</param>
        /// <param name="height">Portrait height in pixels.</param>
        /// <param name="labelHeight">Height of one name in pixels.</param>
        /// <param name="gap">Pixels kept between two names in a column.</param>
        /// <param name="centreY">Out: each name's vertical centre.</param>
        /// <param name="right">Out: true when the name sits in the right column.</param>
        public static void Place(Vector2[] points, bool[] wants, float width, float height, float labelHeight,
            float gap, float[] centreY, bool[] right)
        {
            if (points.Length != wants.Length || points.Length != centreY.Length || points.Length != right.Length)
            {
                throw new ArgumentException("One entry per socket.");
            }

            Span<float> heights = stackalloc float[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                heights[i] = labelHeight;
                right[i] = points[i].x >= width * 0.5f;
            }

            PlaceColumn(points, wants, right, false, height, heights, gap, centreY);
            PlaceColumn(points, wants, right, true, height, heights, gap, centreY);
        }

        public static void Place(Vector2[] points, bool[] wants, float width, float height, float[] labelHeights,
            float gap, float[] centreY, bool[] right)
        {
            if (points.Length != wants.Length || points.Length != labelHeights.Length ||
                points.Length != centreY.Length || points.Length != right.Length)
            {
                throw new ArgumentException("One entry per socket.");
            }

            for (int i = 0; i < points.Length; i++)
            {
                right[i] = points[i].x >= width * 0.5f;
            }

            PlaceColumn(points, wants, right, false, height, labelHeights, gap, centreY);
            PlaceColumn(points, wants, right, true, height, labelHeights, gap, centreY);
        }

        private static void PlaceColumn(Vector2[] points, bool[] wants, bool[] right, bool rightColumn, float height,
            ReadOnlySpan<float> labelHeights, float gap, float[] centreY)
        {
            Span<int> order = stackalloc int[points.Length];
            int count = 0;
            for (int i = 0; i < points.Length; i++)
            {
                if (!wants[i] || right[i] != rightColumn)
                {
                    continue;
                }

                int at = count++;
                while (at > 0 && points[order[at - 1]].y > points[i].y)
                {
                    order[at] = order[at - 1];
                    at--;
                }

                order[at] = i;
            }

            if (count == 0)
            {
                return;
            }

            Span<float> half = stackalloc float[count];
            for (int k = 0; k < count; k++)
            {
                half[k] = labelHeights[order[k]] * 0.5f;
                centreY[order[k]] = Mathf.Clamp(points[order[k]].y, half[k], Mathf.Max(half[k], height - half[k]));
            }

            for (int pass = 0; pass < 6; pass++)
            {
                for (int k = 1; k < count; k++)
                {
                    int prev = order[k - 1];
                    int curr = order[k];
                    float minSep = half[k - 1] + half[k] + gap;
                    if (centreY[curr] < centreY[prev] + minSep)
                    {
                        float mid = (centreY[prev] + centreY[curr]) * 0.5f;
                        centreY[prev] = mid - minSep * 0.5f;
                        centreY[curr] = mid + minSep * 0.5f;
                    }
                }

                ClampColumn(order, half, count, height, gap, centreY);
            }
        }

        private static void ClampColumn(ReadOnlySpan<int> order, ReadOnlySpan<float> half, int count, float height,
            float gap, float[] centreY)
        {
            for (int k = 0; k < count; k++)
            {
                int idx = order[k];
                float min = k == 0
                    ? half[k]
                    : centreY[order[k - 1]] + half[k - 1] + half[k] + gap;
                centreY[idx] = Mathf.Max(centreY[idx], min);
            }

            // Names pushed past the bottom edge come back up, keeping their order and spacing.
            for (int k = count - 1; k >= 0; k--)
            {
                int idx = order[k];
                float max = k == count - 1
                    ? Mathf.Max(half[k], height - half[k])
                    : centreY[order[k + 1]] - (half[k + 1] + half[k] + gap);
                centreY[idx] = Mathf.Clamp(centreY[idx], half[k], max);
            }
        }
    }
}
