using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// One slot of the scrap field (<see cref="IScrapField"/>): where a hull fragment's root goes (on the dust line,
    /// what lies below y 0 is buried), its rotation, which of Art's <c>HullFragment_&lt;n&gt;</c> it is and its scale.
    /// </summary>
    public readonly struct ScrapPlateInfo
    {
        public ScrapPlateInfo(Vector3 position, Quaternion rotation, int variant, float scale)
        {
            Position = position;
            Rotation = rotation;
            Variant = variant;
            Scale = scale;
        }

        public Vector3 Position { get; }

        public Quaternion Rotation { get; }

        /// <summary>Indexes Art's HullFragment_&lt;n&gt; prefabs.</summary>
        public int Variant { get; }

        public float Scale { get; }
    }
}
