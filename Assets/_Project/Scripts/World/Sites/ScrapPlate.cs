using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// One half-buried hull plate of Kestrel-3's scrap field: its centre, rotation and size (length along X, thickness
    /// along Y, width along Z).
    /// </summary>
    public readonly struct ScrapPlate
    {
        public ScrapPlate(Vector3 centre, Quaternion rotation, Vector3 size)
        {
            Centre = centre;
            Rotation = rotation;
            Size = size;
        }

        public Vector3 Centre { get; }
        public Quaternion Rotation { get; }
        public Vector3 Size { get; }
    }
}
