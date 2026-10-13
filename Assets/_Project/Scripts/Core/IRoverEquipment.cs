using MoonProject.Core.Events;

namespace MoonProject.Core
{
    /// <summary>
    /// 07's equipment (docs/design/0.5.5-owner-feedback.md §3.5), registered in the GameContext by Gameplay:
    /// which part sits in each socket, and the parts row of crafted parts waiting to be fitted. A part's identity is
    /// its <see cref="RoverKitPiece"/> (a later tier is a new piece for the same socket). Parts are fitted, swapped
    /// and taken off only while 07 rests at the part's station: the Rover Bay's turntable pad, or Kenji's garage
    /// port for his parts. Rover reads it at boot to show what a loaded game has fitted (nothing is published on
    /// load). Allocation-free.
    /// </summary>
    public interface IRoverEquipment
    {
        /// <summary>Parts the row holds at most (4): crafting more waits for a fit.</summary>
        int RowCapacity { get; }

        /// <summary>Parts waiting in the row.</summary>
        int RowCount { get; }

        /// <summary>The socket <paramref name="part"/> sits in.</summary>
        RoverSocket SocketOf(RoverKitPiece part);

        /// <summary>The part fitted in <paramref name="socket"/>; false when it is empty.</summary>
        bool TryGetFitted(RoverSocket socket, out RoverKitPiece part);

        /// <summary>
        /// True once a part for <paramref name="socket"/> is crafted or its blueprint complete: the screen names the
        /// socket and shows the bay icon. Unknown sockets are dim outlines with no name.
        /// </summary>
        bool IsKnown(RoverSocket socket);

        /// <summary>The part in row place <paramref name="index"/> (0..<see cref="RowCount"/>-1).</summary>
        RoverKitPiece RowPart(int index);

        /// <summary>
        /// True while 07 rests at <paramref name="part"/>'s station, so it can be fitted or taken off now.
        /// </summary>
        bool CanFit(RoverKitPiece part);

        /// <summary>
        /// Fits the row's part at <paramref name="rowIndex"/> in its socket (swapping out a fitted lower tier, which
        /// the bay recycles): <see cref="PartFitted"/>. False, and nothing happens, away from its station.
        /// </summary>
        bool TryFit(int rowIndex);

        /// <summary>
        /// Takes the part in <paramref name="socket"/> off into the row (its ability pauses until it is refitted):
        /// <see cref="PartRemoved"/>. False away from its station, with the socket empty or the row full.
        /// </summary>
        bool TryUnequip(RoverSocket socket);
    }
}
