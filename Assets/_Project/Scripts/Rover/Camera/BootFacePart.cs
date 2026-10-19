namespace MoonProject.Rover
{
    /// <summary>The parts of 07's face waking before its self-test lines (<see cref="PostEventKind.FaceBegan"/>).</summary>
    public enum BootFacePart
    {
        /// <summary>The tube wakes: dot, line, raster.</summary>
        PowerOn = 0,

        /// <summary>The maker's mark and wordmark.</summary>
        Logo = 1,

        /// <summary>The field profile loads.</summary>
        Load = 2,
    }
}
