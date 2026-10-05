namespace MoonProject.Art
{
    /// <summary>Silhouette family for <see cref="RockGenerator"/>.</summary>
    public enum RockStyle
    {
        /// <summary>Small, flattened and smooth: dune debris, 0.2-0.6 m.</summary>
        Pebble = 0,

        /// <summary>Chunky, softly lumpy rock: the everyday prop, 0.5-1.5 m.</summary>
        Rounded = 1,

        /// <summary>Wide and flat, like a broken plate of bedrock.</summary>
        Slab = 2,

        /// <summary>Taller than wide with crisp, broken facets.</summary>
        Jagged = 3,

        /// <summary>Big landmark rock with more facets (four times the triangles), 2 m and up.</summary>
        Boulder = 4,
    }
}
