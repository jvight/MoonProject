namespace MoonProject.World
{
    /// <summary>Rock classes of the scatter.</summary>
    public enum ScatterKind
    {
        /// <summary>Small rock without a collider: the rover drives over it.</summary>
        Pebble = 0,

        /// <summary>Big rock with a convex collider on Layers.Prop.</summary>
        Boulder = 1,
    }
}
