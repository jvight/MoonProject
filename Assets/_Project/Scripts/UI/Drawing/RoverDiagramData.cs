using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// 07 v3's equipment socket anchor positions and wheel centres in rover-root metres (+X right, +Y up, +Z forward).
    /// Extracted at build time from the generated v3 prefab
    /// (<c>Assets/_Project/Generated/Art/R1/Models/Rover/RoverModel.prefab</c>) by <c>UI/Rover Diagram</c> and read by
    /// <see cref="RoverDiagramPainter"/> and <see cref="EquipmentPage"/>.
    /// </summary>
    public sealed class RoverDiagramData : ScriptableObject
    {
        public const int SocketCount = 12;
        public const int WheelCount = 4;

        [Tooltip("Root-local socket positions on 07 v3, indexed by Core.RoverSocket (0..11).")]
        [SerializeField] private Vector3[] _sockets = Array.Empty<Vector3>();

        [Tooltip("Root-local wheel centres on 07 v3 (Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR).")]
        [SerializeField] private Vector3[] _wheels = Array.Empty<Vector3>();

        public int Count => _sockets != null ? _sockets.Length : 0;

        /// <summary>Contract transform name on <c>RoverModel.prefab</c> for wheel <paramref name="index"/> (0..3).</summary>
        public static string WheelNodeName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Wheel_FL";
                case 1:
                    return "Wheel_FR";
                case 2:
                    return "Wheel_RL";
                case 3:
                    return "Wheel_RR";
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Wheel index must be 0..3.");
            }
        }

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

        /// <summary>Root-local centre (m) of wheel <paramref name="index"/> (0=FL, 1=FR, 2=RL, 3=RR) on 07 v3.</summary>
        public Vector3 WheelPosition(int index)
        {
            if (_wheels == null || index < 0 || index >= _wheels.Length)
            {
                return Vector3.zero;
            }

            return _wheels[index];
        }

        internal void SetSockets(Vector3[] sockets)
        {
            _sockets = sockets ?? throw new ArgumentNullException(nameof(sockets));
        }

        internal void SetWheels(Vector3[] wheels)
        {
            _wheels = wheels ?? throw new ArgumentNullException(nameof(wheels));
        }

        /// <summary>Null when all 12 socket positions and 4 wheel centres are present and populated, otherwise the problem.</summary>
        public string Validate()
        {
            if (_sockets == null || _sockets.Length != SocketCount)
            {
                return $"needs {SocketCount} socket positions (has {_sockets?.Length ?? 0})";
            }

            if (_wheels == null || _wheels.Length != WheelCount)
            {
                return $"needs {WheelCount} wheel positions (has {_wheels?.Length ?? 0})";
            }

            bool anySocketNonZero = false;
            for (int i = 0; i < _sockets.Length; i++)
            {
                if (_sockets[i].sqrMagnitude > 1e-6f)
                {
                    anySocketNonZero = true;
                    break;
                }
            }

            if (!anySocketNonZero)
            {
                return "has uninitialised (all-zero) socket positions";
            }

            for (int i = 0; i < _wheels.Length; i++)
            {
                if (_wheels[i].sqrMagnitude <= 1e-6f)
                {
                    return $"has uninitialised wheel position at index {i} ({WheelNodeName(i)})";
                }
            }

            return null;
        }
    }
}
