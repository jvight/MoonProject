using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// 07 v3's equipment socket anchor positions in rover-root metres (+X right, +Y up, +Z forward), one entry per
    /// <see cref="RoverSocket"/> in enum order (0..11). Extracted at build time from the generated v3 prefab
    /// (<c>Assets/_Project/Generated/Art/R1/Models/Rover/RoverModel.prefab</c>) by <c>UI/Rover Diagram</c> and read by
    /// <see cref="RoverDiagramPainter"/> and <see cref="EquipmentPage"/>.
    /// </summary>
    public sealed class RoverDiagramData : ScriptableObject
    {
        public const int SocketCount = 12;

        [Tooltip("Root-local socket positions on 07 v3, indexed by Core.RoverSocket (0..11).")]
        [SerializeField] private Vector3[] _sockets = Array.Empty<Vector3>();

        public int Count => _sockets != null ? _sockets.Length : 0;

        /// <summary>Root-local position (m) of <paramref name="socket"/> on 07 v3.</summary>
        public Vector3 SocketPosition(RoverSocket socket)
        {
            int index = (int)socket;
            if (_sockets == null || index < 0 || index >= _sockets.Length)
            {
                return Vector3.zero;
            }

            return _sockets[index];
        }

        internal void SetSockets(Vector3[] sockets)
        {
            _sockets = sockets ?? throw new ArgumentNullException(nameof(sockets));
        }

        /// <summary>Null when all 12 socket positions are present and populated, otherwise the problem.</summary>
        public string Validate()
        {
            if (_sockets == null || _sockets.Length != SocketCount)
            {
                return $"needs {SocketCount} socket positions (has {_sockets?.Length ?? 0})";
            }

            bool anyNonZero = false;
            for (int i = 0; i < _sockets.Length; i++)
            {
                if (_sockets[i].sqrMagnitude > 1e-6f)
                {
                    anyNonZero = true;
                    break;
                }
            }

            return anyNonZero ? null : "has uninitialised (all-zero) socket positions";
        }
    }
}
