namespace MoonProject.UI
{
    /// <summary>What 07's face shows as it boots, before its self-test lines (<see cref="BootFace"/>).</summary>
    internal enum BootFaceStage
    {
        /// <summary>Not booting: dark, or the self-test's lines.</summary>
        None = 0,

        /// <summary>The tube wakes: dot, line, raster.</summary>
        PowerOn = 1,

        /// <summary>The maker's mark and its wordmark.</summary>
        Logo = 2,

        /// <summary>The field profile loads.</summary>
        Load = 3,
    }
}
