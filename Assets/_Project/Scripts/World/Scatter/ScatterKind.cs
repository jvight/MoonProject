namespace MoonProject.World
{
    /// <summary>Rock classes of the scatter.</summary>
    public enum ScatterKind
    {
        /// <summary>Small rock without a collider: the rover drives over it.</summary>
        Pebble = 0,

        /// <summary>Big rock with a convex collider on Layers.Prop.</summary>
        Boulder = 1,

        /// <summary>
        /// A small clod or stone of the near-field rubble: no collider, seen only close to the camera.
        /// </summary>
        Rubble = 2,
    }
}
