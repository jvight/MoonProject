using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Which part sits in which socket, gives which ability and is fitted where (docs/design/0.5.5-owner-feedback.md
    /// §3.5). Structure, not tuning: a later tier is a new row for the same socket.
    /// </summary>
    public sealed class EquipmentTable
    {
        private readonly EquipmentPart[] _parts;

        public EquipmentTable(EquipmentPart[] parts)
        {
            _parts = parts ?? throw new ArgumentNullException(nameof(parts));
            for (int i = 0; i < parts.Length; i++)
            {
                for (int j = i + 1; j < parts.Length; j++)
                {
                    if (parts[i].Piece == parts[j].Piece)
                    {
                        throw new ArgumentException($"{parts[i].Piece} is listed twice.", nameof(parts));
                    }
                }
            }
        }

        public int Count => _parts.Length;

        /// <summary>0.5's parts, as §3.5's table lists them.</summary>
        public static EquipmentTable Basin()
        {
            return new EquipmentTable(new[]
            {
                new EquipmentPart(RoverKitPiece.FocusLens, RoverSocket.Cutter, PartStation.Bay, RoverAbility.FocusLens),
                new EquipmentPart(RoverKitPiece.TetherRing, RoverSocket.Tether, PartStation.Port, RoverAbility.Tether),
                new EquipmentPart(RoverKitPiece.LampBar, RoverSocket.Lamp, PartStation.Bay, RoverAbility.WarmHeadlamp),
                new EquipmentPart(RoverKitPiece.HoverCoils, RoverSocket.Belly, PartStation.Bay, RoverAbility.HoverJump),
                new EquipmentPart(RoverKitPiece.CapacitorDrums, RoverSocket.Flanks, PartStation.Bay,
                    RoverAbility.BoostCoils),
                new EquipmentPart(RoverKitPiece.SideBins, RoverSocket.Panniers, PartStation.Bay, RoverAbility.SideBins),
                new EquipmentPart(RoverKitPiece.BatterySpine, RoverSocket.Back, PartStation.Bay, RoverAbility.CellPack),
                new EquipmentPart(RoverKitPiece.CargoRack, RoverSocket.Hold, PartStation.Bay, RoverAbility.CargoCradle),
                new EquipmentPart(RoverKitPiece.SolarCell, RoverSocket.Wing, PartStation.Gift),
                new EquipmentPart(RoverKitPiece.TapeDeck, RoverSocket.Deck, PartStation.Port),
            });
        }

        public EquipmentPart Part(int index)
        {
            return _parts[index];
        }

        /// <summary>True when <paramref name="piece"/> is a socket part (Bell's fresh paint is not).</summary>
        public bool TryFind(RoverKitPiece piece, out EquipmentPart part)
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i].Piece == piece)
                {
                    part = _parts[i];
                    return true;
                }
            }

            part = default;
            return false;
        }

        /// <summary>
        /// The part that gives <paramref name="ability"/>; false when no part does (e.g. Wide Sonar in 0.5).
        /// </summary>
        public bool TryFindByAbility(RoverAbility ability, out EquipmentPart part)
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i].GivesAbility && _parts[i].Ability == ability)
                {
                    part = _parts[i];
                    return true;
                }
            }

            part = default;
            return false;
        }

        /// <summary>The part for <paramref name="piece"/>; a piece that is not a socket part is a wiring bug.</summary>
        public EquipmentPart Find(RoverKitPiece piece)
        {
            if (!TryFind(piece, out EquipmentPart part))
            {
                throw new ArgumentOutOfRangeException(nameof(piece), piece, "Not a socket part (§3.5).");
            }

            return part;
        }
    }
}
