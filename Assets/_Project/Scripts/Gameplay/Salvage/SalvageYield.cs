using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What a piece is worth and how long it takes to cut (docs/features/M3-26 §4): its size class sets its units and
    /// its segments, each cut in turn; the cutter's speed divides the time (<see cref="Core.ICutter.SpeedFactor"/>).
    /// Pure maths, shared by the salvage sites and the economy tests (VISION ruling 5).
    /// </summary>
    public static class SalvageYield
    {
        /// <summary>Units a piece of <paramref name="size"/> yields at a site of <paramref name="yieldScale"/>.</summary>
        public static int Units(PieceSize size, int yieldScale, SalvageTuning tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            return Math.Max(1, yieldScale) * tuning.Size(size).Units;
        }

        /// <summary>
        /// Seconds of lit beam a piece of <paramref name="size"/> takes at cutting speed <paramref name="speed"/>
        /// (every segment; the slides between them and the charge-up apart).
        /// </summary>
        public static float CutSeconds(PieceSize size, float speed, SalvageTuning tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            PieceSizeRow row = tuning.Size(size);
            return row.Segments * row.SegmentSeconds / Math.Max(0.01f, speed);
        }
    }
}
