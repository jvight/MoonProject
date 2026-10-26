namespace MoonProject.Gameplay
{
    /// <summary>
    /// Whether 07 can set out for a relay on the charge it has: the drive there, costed as the range ring costs it,
    /// still leaves <c>margin01</c> of a full battery on arrival, where the relay's pad charges once its part is in.
    /// Full cells always set out: charging cannot give more. Plain logic, EditMode-tested.
    /// </summary>
    public static class RelayTrip
    {
        /// <param name="charge">07's charge now (cells).</param>
        /// <param name="cells">07's cells.</param>
        /// <param name="full">07's cells are full.</param>
        /// <param name="tripCost">Cells the drive to the relay costs.</param>
        /// <param name="margin01">The share of a full battery to keep on arrival.</param>
        public static bool Covered(float charge, int cells, bool full, float tripCost, float margin01)
        {
            return full || charge - tripCost >= margin01 * cells;
        }
    }
}
