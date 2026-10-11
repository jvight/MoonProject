namespace MoonProject.Core
{
    /// <summary>
    /// The four main recovery targets, the only directed part of the game (VISION ruling 16, docs/features/M3-21),
    /// shown as rows of lamps on the recovery panel at the head of the lab's tech board.
    /// </summary>
    public enum RecoveryTarget
    {
        /// <summary>Power the station: radio tower L1, L2, L3.</summary>
        Power = 0,

        /// <summary>Link the network to the Peak: a chain of lit masts from home to the Peak's foot.</summary>
        Link = 1,

        /// <summary>Repair the great dish.</summary>
        Dish = 2,

        /// <summary>Answer Earth: the broadcast.</summary>
        Answer = 3,
    }
}
