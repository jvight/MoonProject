namespace MoonProject.Gameplay
{
    /// <summary>The world width that covers a given number of screen pixels at a given distance from the camera.</summary>
    public static class ScreenWidth
    {
        private const float MinPixelsPerMetre = 1e-3f;

        /// <summary>
        /// Metres across <paramref name="pixels"/> screen pixels at <paramref name="distance"/> m from the camera, where
        /// <paramref name="pixelsPerMetreAtOne"/> is how many pixels one metre covers at one metre's distance
        /// (screen height over twice the tangent of half the vertical field of view).
        /// </summary>
        public static float Metres(float pixels, float distance, float pixelsPerMetreAtOne)
        {
            return pixels * distance / System.Math.Max(pixelsPerMetreAtOne, MinPixelsPerMetre);
        }
    }
}
