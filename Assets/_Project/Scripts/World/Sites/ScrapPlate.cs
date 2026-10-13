using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// One hull fragment of Kestrel-3's scrap field: where its dust line crosses the ground, its yaw, and which of Art's
    /// HullFragment variants it is.
    /// </summary>
    public readonly struct ScrapPlate
    {
        public ScrapPlate(Vector3 position, Quaternion rotation, int variant)
        {
            Position = position;
            Rotation = rotation;
            Variant = variant;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public int Variant { get; }
    }
}
