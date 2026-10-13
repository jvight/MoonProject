using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// <see cref="IRoverEquipment"/> stand-in: what a loaded game has fitted, set by a test before 07's first frame
    /// (<see cref="Fitted"/>). Fitting in play is Gameplay's; tests publish <see cref="PartFitted"/> themselves.
    /// </summary>
    public sealed class TestEquipment : IRoverEquipment
    {
        private const int Capacity = 4;

        private readonly Dictionary<RoverSocket, RoverKitPiece> _fitted = new Dictionary<RoverSocket, RoverKitPiece>();

        public int RowCapacity => Capacity;

        public int RowCount => 0;

        /// <summary>The socket each piece fits (0.5.5 §3.5's table).</summary>
        public static RoverSocket SocketFor(RoverKitPiece part)
        {
            switch (part)
            {
                case RoverKitPiece.HoverCoils:
                    return RoverSocket.Belly;
                case RoverKitPiece.LampBar:
                    return RoverSocket.Lamp;
                case RoverKitPiece.CapacitorDrums:
                    return RoverSocket.Flanks;
                case RoverKitPiece.CargoRack:
                    return RoverSocket.Hold;
                case RoverKitPiece.SolarCell:
                    return RoverSocket.Wing;
                case RoverKitPiece.BatterySpine:
                    return RoverSocket.Back;
                case RoverKitPiece.TapeDeck:
                    return RoverSocket.Deck;
                case RoverKitPiece.TetherRing:
                    return RoverSocket.Tether;
                case RoverKitPiece.FocusLens:
                    return RoverSocket.Cutter;
                case RoverKitPiece.SideBins:
                    return RoverSocket.Panniers;
                default:
                    throw new ArgumentOutOfRangeException(nameof(part), part, "Not a socket part.");
            }
        }

        /// <summary>A loaded game has <paramref name="part"/> fitted.</summary>
        public void Fitted(RoverKitPiece part)
        {
            _fitted[SocketFor(part)] = part;
        }

        public RoverSocket SocketOf(RoverKitPiece part)
        {
            return SocketFor(part);
        }

        public bool TryGetFitted(RoverSocket socket, out RoverKitPiece part)
        {
            return _fitted.TryGetValue(socket, out part);
        }

        public bool IsKnown(RoverSocket socket)
        {
            return _fitted.ContainsKey(socket);
        }

        public RoverKitPiece RowPart(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "The stand-in's row is empty.");
        }

        public bool CanFit(RoverKitPiece part)
        {
            return false;
        }

        public bool TryFit(int rowIndex)
        {
            return false;
        }

        public bool TryUnequip(RoverSocket socket)
        {
            return false;
        }
    }
}
