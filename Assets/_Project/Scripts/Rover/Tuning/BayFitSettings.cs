using System;
using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// The install moment in Kenji's Rover Bay (docs/features/M3-14, VISION ruling 14: 07 has no hands): the bay's
    /// guides centre 07 on the turntable, the turntable turns a socket toward an arm if it must, the arm takes the
    /// piece from the rack under the roof, brings it down over its socket and sets it on with a weld, then folds away
    /// while the turntable turns 07 to show the piece.
    /// </summary>
    [Serializable]
    public sealed class BayFitSettings
    {
        [Tooltip("Seconds the bay's guides take to ease 07 onto the turntable's centre and facing (and the turntable "
            + "back from the last fitting's show turn).")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _centreSeconds = 0.5f;

        [Tooltip("Seconds the turntable takes to turn half round (shorter turns take proportionally less).")]
        [Range(0.3f, 5f)]
        [SerializeField] private float _halfTurnSeconds = 1.2f;

        [Tooltip("Seconds an arm takes to swing from its folded rest to just above the socket, piece in hand.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _reachSeconds = 0.9f;

        [Tooltip("Height (m) above the socket the arm brings the piece before lowering it straight on.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _hover = 0.4f;

        [Tooltip("Seconds the arm takes to lower the piece from above the socket onto it.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _descendSeconds = 0.6f;

        [Tooltip("Seconds the arm holds the piece on its socket while the weld sparks fly.")]
        [Range(0f, 3f)]
        [SerializeField] private float _weldSeconds = 0.4f;

        [Tooltip("Seconds the arm takes to rise off the piece and fold back to rest (the show turn starts halfway).")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _liftSeconds = 0.8f;

        [Tooltip("Scale the piece shows at as the arm takes it from the rack, growing to full size as it swings down.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _pickScale = 0.6f;

        [Tooltip("How far (m) a belly socket may sit off the floor arm's lift axis and still be reached.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _floorAlign = 0.25f;

        [Tooltip("Chassis bob (m/s, downward) as the arm presses the piece home, so the fit lands with a soft clunk.")]
        [Range(0f, 2f)]
        [SerializeField] private float _seatHeaveKick = 0.35f;

        [Tooltip("Antenna wiggle (deg/s) as the piece is pressed home.")]
        [Range(0f, 300f)]
        [SerializeField] private float _seatAntennaKick = 60f;

        [Tooltip("Gaze priority of 07 watching the piece come in on the arm (as gameplay's: 0 glance, 1 interest, "
            + "2 focus).")]
        [Range(0, 4)]
        [SerializeField] private int _watchPriority = 2;

        [Header("Showing the piece (turntable angle from 07's arrival facing, deg)")]
        [Tooltip("The lamp bar: turned to face the open front.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _showLampBar = 180f;

        [Tooltip("The capacitor drums: a flank to the open front.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _showDrums = 90f;

        [Tooltip("The cargo rack: its back to the open front.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _showRack;

        [Tooltip("The Hover-Jump coils: a flank to the open front.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _showCoils = 90f;

        public float CentreSeconds => _centreSeconds;

        public float HalfTurnSeconds => _halfTurnSeconds;

        public float ReachSeconds => _reachSeconds;

        public float Hover => _hover;

        public float DescendSeconds => _descendSeconds;

        public float WeldSeconds => _weldSeconds;

        public float LiftSeconds => _liftSeconds;

        public float PickScale => _pickScale;

        public float FloorAlign => _floorAlign;

        public int WatchPriority => _watchPriority;

        public float SeatHeaveKick => _seatHeaveKick;

        public float SeatAntennaKick => _seatAntennaKick;

        /// <summary>Seconds the turntable takes to turn <paramref name="degrees"/>.</summary>
        public float TurnSeconds(float degrees)
        {
            return Mathf.Abs(degrees) / 180f * _halfTurnSeconds;
        }

        /// <summary>The turntable angle (from arrival) that shows <paramref name="piece"/> to the open front.</summary>
        public float Show(RoverKitPiece piece)
        {
            switch (piece)
            {
                case RoverKitPiece.LampBar:
                    return _showLampBar;
                case RoverKitPiece.CapacitorDrums:
                    return _showDrums;
                case RoverKitPiece.CargoRack:
                    return _showRack;
                case RoverKitPiece.HoverCoils:
                    return _showCoils;
                default:
                    throw new ArgumentOutOfRangeException(nameof(piece), piece,
                        "Only crafted kit is fitted in the bay.");
            }
        }
    }
}
