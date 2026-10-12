namespace MoonProject.Rover
{
    /// <summary>
    /// What the hold's fill means for 07's body (M3-25 twist 1) as plain logic: the suspension sags a step per quarter
    /// of the hold, and 07 glances back once when the hold becomes full (never at the first report, a loaded game's).
    /// </summary>
    public sealed class CargoLoad
    {
        private const int Quarters = 4;

        private bool _known;
        private bool _full;

        /// <summary>The sag (m, down) for <paramref name="used"/> of <paramref name="slots"/>: one step per full quarter.</summary>
        public static float Sag(int used, int slots, float perQuarter)
        {
            if (slots <= 0 || used <= 0)
            {
                return 0f;
            }

            int quarters = used >= slots ? Quarters : used * Quarters / slots;
            return quarters * perQuarter;
        }

        /// <summary>True when this report turns the hold full (the moment 07 looks back at its load).</summary>
        public bool BecameFull(int used, int slots)
        {
            bool full = slots > 0 && used >= slots;
            bool became = _known && full && !_full;
            _known = true;
            _full = full;
            return became;
        }
    }
}
