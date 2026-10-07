namespace MoonProject.Rover
{
    /// <summary>
    /// Where the wide shot's camera settles around 07's follow point: the world yaw it looks along, its elevation and
    /// distance (the orbit it eases to; breathing sways around it), and where 07 sits on screen.
    /// </summary>
    public readonly struct WideShotFrame
    {
        public WideShotFrame(float yaw, float elevation, float distance, float screenY)
        {
            Yaw = yaw;
            Elevation = elevation;
            Distance = distance;
            ScreenY = screenY;
        }

        /// <summary>World yaw (deg, 0 = +Z) the camera looks along, sitting behind the follow point.</summary>
        public float Yaw { get; }

        /// <summary>Degrees above the follow point.</summary>
        public float Elevation { get; }

        /// <summary>Metres from the follow point.</summary>
        public float Distance { get; }

        /// <summary>Where 07 sits on screen (0 = centre, +y = lower).</summary>
        public float ScreenY { get; }
    }
}
