using System;
using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Where the install moment looks from for each kit piece or gift, so the new piece faces the camera: the
    /// direction the camera looks along, in degrees from 07's heading (0 = the normal chase view from behind, 180 =
    /// looking back at 07's face, negative = turning left, so the camera stands off 07's right side).
    /// </summary>
    [Serializable]
    public sealed class KitViewSettings
    {
        [Tooltip("View of the Hover-Jump coils: from the side, low, so the belly shows.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _hoverCoils = -95f;

        [Tooltip("View of the lamp bar: a front three-quarter (Kenji's bench behind 07).")]
        [Range(-180f, 180f)]
        [SerializeField] private float _lampBar = -160f;

        [Tooltip("View of the capacitor drums: from the side, a little ahead.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _capacitorDrums = -105f;

        [Tooltip("View of the cargo rack: a rear three-quarter.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _cargoRack = -40f;

        [Tooltip("View of Tilly's cell on the solar wing: a rear three-quarter.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _solarCell = -35f;

        [Tooltip("View of Bell's fresh paint and pennant: from the side.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _freshPaint = -90f;

        [Tooltip("Distance (m) of the point the moment turns toward; only its bearing matters, within focus range.")]
        [Range(1f, 30f)]
        [SerializeField] private float _subjectDistance = 6f;

        public float SubjectDistance => _subjectDistance;

        /// <summary>The look direction (deg from 07's heading) of the moment for <paramref name="piece"/>.</summary>
        public float Bearing(RoverKitPiece piece)
        {
            switch (piece)
            {
                case RoverKitPiece.HoverCoils:
                    return _hoverCoils;
                case RoverKitPiece.LampBar:
                    return _lampBar;
                case RoverKitPiece.CapacitorDrums:
                    return _capacitorDrums;
                case RoverKitPiece.CargoRack:
                    return _cargoRack;
                case RoverKitPiece.SolarCell:
                    return _solarCell;
                case RoverKitPiece.FreshPaint:
                    return _freshPaint;
                default:
                    throw new ArgumentOutOfRangeException(nameof(piece), piece, "Unknown kit piece.");
            }
        }
    }
}
