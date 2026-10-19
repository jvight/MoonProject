using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// A boulder that wears an apron of rubble: where it lies, how big it is, how far its stones reach and how many it
    /// has, and its place in the scatter plan, which seeds its stones so they come out the same on every visit.
    /// </summary>
    public readonly struct RubbleApron
    {
        public RubbleApron(int index, Vector2 center, float size, float reach, int count)
        {
            Index = index;
            Center = center;
            Size = size;
            Reach = reach;
            Count = count;
        }

        /// <summary>The boulder's place in the scatter plan.</summary>
        public int Index { get; }

        /// <summary>The boulder's middle (x, z).</summary>
        public Vector2 Center { get; }

        /// <summary>The boulder's size, metres.</summary>
        public float Size { get; }

        /// <summary>How far from the middle its stones reach, metres.</summary>
        public float Reach { get; }

        /// <summary>How many stones the apron has (before the ground turns some away).</summary>
        public int Count { get; }
    }
}
