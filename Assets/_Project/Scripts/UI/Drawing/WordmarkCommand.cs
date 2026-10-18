namespace MoonProject.UI
{
    /// <summary>
    /// One drawing step of a <see cref="WordmarkShape"/> path and how many values it reads: MoveTo (x, y), CubicTo
    /// (two controls, then the end), ArcTo (centre x, y, radius, start and end angle in degrees, 1 if clockwise),
    /// Close (none), LineTo (x, y).
    /// </summary>
    public enum WordmarkCommand : byte
    {
        MoveTo = 0,
        CubicTo = 1,
        ArcTo = 2,
        Close = 3,
        LineTo = 4,
    }
}
