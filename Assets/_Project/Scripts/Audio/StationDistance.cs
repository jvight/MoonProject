using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// Where 07 stands in the relay network, for the radio (M3-06): the distance from home that would give the same
    /// signal. Home's circle is the radio's clear radius (tower upgrades bloom it in), every lit mast adds its own
    /// reach (<see cref="RelayNode.Radius"/>);
    /// the signal is clear inside any of them and falls off past the nearest edge, so the radio stays warm anywhere
    /// the network reaches and the near-silence lives beyond it. <see cref="IStationReach.IsInReach"/> has the last
    /// word on inside or out. Allocation-free.
    /// </summary>
    public static class StationDistance
    {
        /// <summary>
        /// Home's clear radius plus how far <paramref name="position"/> is past the nearest lit node's edge (negative
        /// inside): a home-relative distance the radio's signal field and the soundscape can use unchanged.
        /// </summary>
        public static float Equivalent(IStationReach reach, Vector3 position, float homeRadius)
        {
            float edge = float.MaxValue;
            for (int i = 0; i < reach.NodeCount; i++)
            {
                RelayNode node = reach.GetNode(i);
                if (!node.Lit)
                {
                    continue;
                }

                // Nodes list home first: its circle is the radio's own, which blooms in after a tower upgrade.
                float radius = i == 0 ? homeRadius : node.Radius;
                edge = Mathf.Min(edge, SignalField.HorizontalDistance(position, node.Position) - radius);
            }

            edge = reach.IsInReach(position) ? Mathf.Min(edge, 0f) : Mathf.Max(edge, 0f);
            return homeRadius + edge;
        }
    }
}
