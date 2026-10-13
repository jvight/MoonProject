using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>07's equipment for audio tests: the parts a test says are on 07, as a loaded game has.</summary>
    public sealed class FakeEquipment : IRoverEquipment
    {
        private readonly Dictionary<RoverSocket, RoverKitPiece> _fitted =
            new Dictionary<RoverSocket, RoverKitPiece>();

        public int RowCapacity => 0;

        public int RowCount => 0;

        public void Fit(RoverSocket socket, RoverKitPiece part)
        {
            _fitted[socket] = part;
        }

        public RoverSocket SocketOf(RoverKitPiece part)
        {
            foreach (KeyValuePair<RoverSocket, RoverKitPiece> fitted in _fitted)
            {
                if (fitted.Value == part)
                {
                    return fitted.Key;
                }
            }

            return RoverSocket.Cutter;
        }

        public bool TryGetFitted(RoverSocket socket, out RoverKitPiece part)
        {
            return _fitted.TryGetValue(socket, out part);
        }

        public bool IsKnown(RoverSocket socket) => _fitted.ContainsKey(socket);

        public RoverKitPiece RowPart(int index) => default;

        public bool CanFit(RoverKitPiece part) => false;

        public bool TryFit(int rowIndex) => false;

        public bool TryUnequip(RoverSocket socket) => _fitted.Remove(socket);
    }
}
