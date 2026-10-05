namespace MoonProject.Art
{
    /// <summary>How a <see cref="Paint"/> chooses between its main and alternate swatch for each face.</summary>
    public enum PaintRule
    {
        /// <summary>Every face uses the main swatch.</summary>
        Solid = 0,

        /// <summary>Cap faces (prism/frustum/extrude ends, box top and bottom, wedge ends) use the alternate.</summary>
        Caps = 1,

        /// <summary>Faces whose final normal points along the paint direction use the main swatch.</summary>
        Facing = 2,
    }
}
