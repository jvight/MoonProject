namespace MoonProject.Audio
{
    /// <summary>
    /// Which chime note a scrap pickup plays for its combo step. The chain climbs the pentatonic one note per
    /// pickup; once it reaches the top it weaves up and down over the highest notes (a gentle arpeggio) instead of
    /// climbing forever like a siren.
    /// </summary>
    public static class ScrapMelody
    {
        /// <summary>Note index 0..<paramref name="noteCount"/>-1 for <paramref name="comboStep"/>.</summary>
        /// <param name="topWindow">How many of the highest notes long chains weave over (at least 2).</param>
        public static int NoteIndex(int comboStep, int noteCount, int topWindow)
        {
            if (noteCount <= 1 || comboStep <= 0)
            {
                return 0;
            }

            int top = noteCount - 1;
            if (comboStep <= top)
            {
                return comboStep;
            }

            int window = topWindow < 2 ? 2 : (topWindow > noteCount ? noteCount : topWindow);
            int period = 2 * (window - 1);
            int phase = (comboStep - top) % period;
            int depth = phase <= window - 1 ? phase : period - phase;
            return top - depth;
        }
    }
}
