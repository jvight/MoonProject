namespace MoonProject.Gameplay
{
    /// <summary>
    /// Switchable scan look preset (SCAN v4): selects how 07's cheek-lens laser beam, target-conforming surface
    /// shader, and completion collapse into the lens render, with zero code duplication.
    /// </summary>
    public enum ScanVariant
    {
        /// <summary>
        /// Variant A ("Profiler sweep"): thin bright laser line sweeps across the object's surface leaving a fading
        /// raster of fine contour lines; faint collimated wedge from the lens; at completion the object's edges
        /// trace a clean blueprint line drawing that collapses into the lens.
        /// </summary>
        ProfilerSweep = 0,

        /// <summary>
        /// Variant B ("Wireframe reveal"): the object's silhouette and key edges light up as a soft wireframe
        /// hologram in CRT amber that fills in as progress rises; small data ticks stream along the beam to the lens.
        /// </summary>
        WireframeReveal = 1,

        /// <summary>
        /// Variant C ("Point scan"): a scanning dot grid / point-cloud sweeps the object (dots stick to the surface
        /// and fade), the beam is a fine pulsing line; completion contracts the point cloud into the lens.
        /// </summary>
        PointScan = 2,
    }
}
