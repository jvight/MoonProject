namespace MoonProject.Core
{
    /// <summary>
    /// The three stations around the lab's turntable, clockwise from the front (docs/features/M3-19 §5).
    /// </summary>
    public enum LabStation
    {
        /// <summary>Ines's survey chart on the light table, under the big porthole that faces the Peak.</summary>
        Chart = 0,

        /// <summary>Kenji's tech board, with the recovery panel at its head.</summary>
        Board = 1,

        /// <summary>The keepsake shelf, where the beam places the crew's things.</summary>
        Keepsakes = 2,
    }
}
