using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// Home's answer to a newly lit mast (M3-06): which node it links to (the nearest other lit node, as the ground
    /// pulse runs) and when the answer comes back (when the pulse arrives there). Allocation-free.
    /// </summary>
    public static class RelayLink
    {
        /// <summary>The nearest lit node other than <paramref name="relayId"/> to <paramref name="from"/>; false when
        /// there is none.</summary>
        public static bool TryFindLinkedNode(IStationReach reach, string relayId, Vector3 from, out Vector3 position)
        {
            position = default;
            float best = float.MaxValue;
            for (int i = 0; i < reach.NodeCount; i++)
            {
                RelayNode node = reach.GetNode(i);
                if (!node.Lit || string.Equals(node.Id, relayId, StringComparison.Ordinal))
                {
                    continue;
                }

                float distance = SignalField.HorizontalDistance(from, node.Position);
                if (distance < best)
                {
                    best = distance;
                    position = node.Position;
                }
            }

            return best < float.MaxValue;
        }

        /// <summary>Seconds for the ground pulse to cover <paramref name="distance"/> at <paramref name="speed"/>,
        /// capped so a long link still answers while the moment lasts.</summary>
        public static float AnswerDelay(float distance, float speed, float maxDelay)
        {
            return speed > 0f ? Mathf.Min(Mathf.Max(0f, distance) / speed, maxDelay) : 0f;
        }
    }
}
