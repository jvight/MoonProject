using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>One part 07 can wear: its socket, the ability it gives (if any) and where it is fitted.</summary>
    public readonly struct EquipmentPart
    {
        public EquipmentPart(RoverKitPiece piece, RoverSocket socket, PartStation station)
        {
            Piece = piece;
            Socket = socket;
            Station = station;
            GivesAbility = false;
            Ability = default;
        }

        public EquipmentPart(RoverKitPiece piece, RoverSocket socket, PartStation station, RoverAbility ability)
        {
            Piece = piece;
            Socket = socket;
            Station = station;
            GivesAbility = true;
            Ability = ability;
        }

        public RoverKitPiece Piece { get; }

        public RoverSocket Socket { get; }

        public PartStation Station { get; }

        public bool GivesAbility { get; }

        /// <summary>The ability it gives while fitted (only when <see cref="GivesAbility"/>).</summary>
        public RoverAbility Ability { get; }
    }
}
