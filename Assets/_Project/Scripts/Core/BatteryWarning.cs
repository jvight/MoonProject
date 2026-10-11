namespace MoonProject.Core
{
    /// <summary>A gentle, early low-charge warning (docs/features/M3-20 "Warnings"): no red, no alarm.</summary>
    public enum BatteryWarning
    {
        /// <summary>
        /// 30%: one soft falling two-note, the eye dims a little, the nearest charger's pillar rises and one line names
        /// home or a lit charger.
        /// </summary>
        Low = 0,

        /// <summary>15%: 07 glances toward the nearest lit charger.</summary>
        VeryLow = 1,
    }
}
