using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// One hull fragment of Kestrel-3's scrap field: where its root goes, its rotation, which of Art's HullFragment
    /// variants it is and its scale.
    /// </summary>
    public readonly struct ScrapPlate
    {
        public ScrapPlate(Vector3 position, Quaternion rotation, int variant, float scale)
        {
            Scale = scale;
            Position = position;
            Rotation = rotation;
            Variant = variant;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public int Variant { get; }
        public float Scale { get; }
    }
}
