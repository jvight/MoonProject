using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Ground shaped for the places content stands on (M3-06 relays, M3-13 salvage sites): flat, drivable footprints
    /// for the salvage sites on the basin floor and the low mound the teaching relay (relay.0) stands on. Each shape
    /// blends into the ground around it, so the surface stays smooth. Positions are bearings and distances from the
    /// base.
    /// </summary>
    [Serializable]
    public sealed class SiteSettings
    {
        [Tooltip("Distance over which a shaped footprint blends back into the ground around it, metres.")]
        [Range(4f, 30f)]
        [SerializeField] private float _footprintBlend = 12f;

        [Header("site.depot: the crew's supply depot, near the base")]
        [Tooltip("Bearing of the depot from the base, degrees.")]
        [Range(0f, 360f)]
        [SerializeField] private float _depotBearing = 315f;

        [Tooltip("Distance of the depot from the base, metres.")]
        [Range(30f, 120f)]
        [SerializeField] private float _depotDistance = 55f;

        [Tooltip("Radius of the depot's flat footprint, metres.")]
        [Range(4f, 20f)]
        [SerializeField] private float _depotRadius = 10f;

        [Header("site.drill: the tipped-over drill rig on a crater rim")]
        [Tooltip("Bearing of the drill rig from the base, degrees: on the outer flank of the far rim of the large " +
            "crater at bearing 118, so the rig stands silhouetted across the crater from home.")]
        [Range(0f, 360f)]
        [SerializeField] private float _drillBearing = 113f;

        [Tooltip("Distance of the drill rig from the base, metres.")]
        [Range(60f, 280f)]
        [SerializeField] private float _drillDistance = 148.3f;

        [Tooltip("Radius of the drill rig's flat footprint, metres.")]
        [Range(4f, 20f)]
        [SerializeField] private float _drillRadius = 9f;

        [Header("site.garage: Kenji's half-buried rover garage")]
        [Tooltip("Bearing of the garage from the base, degrees.")]
        [Range(0f, 360f)]
        [SerializeField] private float _garageBearing = 175f;

        [Tooltip("Distance of the garage from the base, metres.")]
        [Range(60f, 280f)]
        [SerializeField] private float _garageDistance = 125f;

        [Tooltip("Radius of the garage's flat footprint, metres.")]
        [Range(4f, 20f)]
        [SerializeField] private float _garageRadius = 10f;

        [Header("relay.0's mound: the teaching mast in the spawn first frame")]
        [Tooltip("Bearing of the mound from the base, degrees: inside the spawn view and at least 15 degrees " +
            "right of The Peak; past the play ramp at bearing 38 so no jump lands on the mast.")]
        [Range(0f, 360f)]
        [SerializeField] private float _moundBearing = 28.5f;

        [Tooltip("Distance of the mound from the base, metres (beyond the ramp's landing ground).")]
        [Range(60f, 250f)]
        [SerializeField] private float _moundDistance = 150f;

        [Tooltip("Radius of the mound's flat top, metres (the mast's pad needs 3).")]
        [Range(3f, 10f)]
        [SerializeField] private float _moundRadius = 4f;

        [Tooltip("How far the mound's top rises above the ground around it, metres: low, but a high point.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _moundRise = 1.6f;

        [Tooltip("Length of the mound's gentle sides, metres.")]
        [Range(6f, 30f)]
        [SerializeField] private float _moundRun = 16f;

        [Header("site.kestrel: the fallen relay satellite")]
        [Tooltip("Kestrel-3's impact crater and the furrow trailing back along its fall line.")]
        [SerializeField] private KestrelSettings _kestrel = new KestrelSettings();

        public float FootprintBlend => _footprintBlend;
        public Vector2 DepotCenter => MoonSurface.BearingToDirection(_depotBearing) * _depotDistance;
        public Vector2 DrillCenter => MoonSurface.BearingToDirection(_drillBearing) * _drillDistance;
        public Vector2 GarageCenter => MoonSurface.BearingToDirection(_garageBearing) * _garageDistance;
        public Vector2 MoundCenter => MoonSurface.BearingToDirection(_moundBearing) * _moundDistance;
        public float DepotRadius => _depotRadius;
        public float DrillRadius => _drillRadius;
        public float GarageRadius => _garageRadius;
        public float MoundRadius => _moundRadius;
        public float MoundRise => _moundRise;
        public float MoundRun => _moundRun;
        public KestrelSettings Kestrel => _kestrel;

        /// <summary>Returns null when the settings are consistent, otherwise the problem.</summary>
        public string Validate()
        {
            if (_moundRadius + _moundRun > _moundDistance)
            {
                return "Sites: relay.0's mound reaches back to the base.";
            }

            return _kestrel.Validate();
        }
    }
}
