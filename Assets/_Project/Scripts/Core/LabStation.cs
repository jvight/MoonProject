namespace MoonProject.Core
{
    /// <summary>
    /// The four stations around the lab's turntable, clockwise from the front (docs/features/M3-19 §5).
    /// </summary>
    public enum LabStation
    {
        /// <summary>Ines's survey chart on the light table, under the big porthole that faces the Peak.</summary>
        Chart = 0,

        /// <summary>The reader: 07's beam feeds a blueprint tube into its slot and it decodes.</summary>
        Reader = 1,

        /// <summary>Kenji's tech board, with the recovery panel at its head.</summary>
        Board = 2,

        /// <summary>The keepsake shelf, where the beam places the crew's things.</summary>
        Keepsakes = 3,
    }
}
