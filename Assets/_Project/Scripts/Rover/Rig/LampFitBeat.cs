namespace MoonProject.Rover
{
    /// <summary>
    /// The beat after the SSR-3 headlamp is fitted live (pure, EditMode-tested): once the pods have clamped 07 chirps,
    /// then tries its new eyes, its head looking out across the ground to one side and then the other so the lamp's
    /// first beam sweeps across it once. Times are seconds since the fit.
    /// </summary>
    public static class LampFitBeat
    {
        /// <summary>-1 while the head looks to its left, +1 to its right, 0 before and after.</summary>
        public static int LookSide(GrowthSettings settings, float seconds)
        {
            float start = settings.FitLookStartSeconds;
            if (seconds < start)
            {
                return 0;
            }

            float into = seconds - start;
            if (into < settings.FitLookSeconds)
            {
                return -1;
            }

            return into < 2f * settings.FitLookSeconds ? 1 : 0;
        }

        /// <summary>True from the moment of the happy chirp on.</summary>
        public static bool ChirpDue(GrowthSettings settings, float seconds)
        {
            return seconds >= settings.FitChirpSeconds;
        }

        /// <summary>True once the head has looked both ways.</summary>
        public static bool Done(GrowthSettings settings, float seconds)
        {
            return seconds >= settings.FitLookStartSeconds + 2f * settings.FitLookSeconds;
        }
    }
}
