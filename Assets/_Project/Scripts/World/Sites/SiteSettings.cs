using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Ground shaped for the places content stands on: opening v2's ruins, lander and apron, and the flat footprints
    /// and low mound the cut salvage sites and teaching relay (relay.0) stood on, kept as ground. Each shape
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
        [SerializeField] private float _garageBearing = 232f;

        [Tooltip("Distance of the garage from the base, metres: near enough for one trip from a full battery " +
            "(first-hour v2 §4).")]
        [Range(60f, 280f)]
        [SerializeField] private float _garageDistance = 115f;

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

        [Header("Lumen station ruins cluster, east of the lander (opening v2 §4)")]
        [Tooltip("Bearing of the Lumen station ruins cluster from the wake point, degrees clockwise from +Z: seen to " +
            "the right from the build apron.")]
        [Range(0f, 360f)]
        [SerializeField] private float _ruinsBearing = 27.2f;

        [Tooltip("Distance of the Lumen station ruins cluster from the wake point, metres (~65 m east of the lander).")]
        [Range(35f, 300f)]
        [SerializeField] private float _ruinsDistance = 208f;

        [Tooltip("Radius of the ruins cluster's flat footprint, metres.")]
        [Range(8f, 24f)]
        [SerializeField] private float _ruinsRadius = 14f;

        [Tooltip("Distance over which the ruins' flat footprint blends back into the ground, metres: longer than the " +
            "other sites', because the cluster stands on a low rise about 3 m above the hollow toward the lander and " +
            "the way down must stay drivable.")]
        [Range(4f, 40f)]
        [SerializeField] private float _ruinsBlend = 24f;

        [Header("Opening v2 layout (docs/design/opening-v2.md §4)")]
        [Tooltip("Where 07's dead charging post stands on the old pad (world XZ, metres): beside 07's wake spot.")]
        [SerializeField] private Vector2 _wakePost = new Vector2(-2.4f, -4f);

        [Tooltip("Bearing of the landed cargo lander from the wake point, degrees: in line with The Peak, so the " +
            "first frame shows the lander's beacon in front of it.")]
        [Range(0f, 360f)]
        [SerializeField] private float _landerBearing = 12.3f;

        [Tooltip("Distance of the lander from the wake point, metres (the first drive, ~30 s).")]
        [Range(60f, 300f)]
        [SerializeField] private float _landerDistance = 164f;

        [Tooltip("Radius of the lander's flat footprint, metres (the R1 CargoLander is 13.1 x 5.2 m).")]
        [Range(6f, 20f)]
        [SerializeField] private float _landerRadius = 10f;

        [Tooltip("Bearing of the build apron (the lander's old unloading apron) from the wake point, degrees.")]
        [Range(0f, 360f)]
        [SerializeField] private float _apronBearing = 6.1f;

        [Tooltip("Distance of the build apron from the wake point, metres: about 25 m short of the lander.")]
        [Range(60f, 300f)]
        [SerializeField] private float _apronDistance = 141f;

        [Tooltip("Radius of the build apron, metres: flat and clear of rocks, room for the pad, the solar array, the " +
            "battery bank and the mast.")]
        [Range(8f, 24f)]
        [SerializeField] private float _apronRadius = 14f;

        [Tooltip("Bearing of the first drive's midway point of interest (in the Kestrel furrow) from the wake point.")]
        [Range(0f, 360f)]
        [SerializeField] private float _midwayBearing = 336f;

        [Tooltip("Distance of the midway point of interest from the wake point, metres (12-15 s into the drive).")]
        [Range(30f, 200f)]
        [SerializeField] private float _midwayDistance = 98.5f;

        [Tooltip("Radius of the midway point, metres: room for its debris and a crate in the furrow (not shaped: " +
            "the debris lies in Kestrel-3's furrow as it is).")]
        [Range(3f, 12f)]
        [SerializeField] private float _midwayRadius = 6f;

        public float FootprintBlend => _footprintBlend;
        public Vector2 WakePost => _wakePost;
        public Vector2 LanderCenter => MoonSurface.BearingToDirection(_landerBearing) * _landerDistance;
        public float LanderRadius => _landerRadius;
        public Vector2 ApronCenter => MoonSurface.BearingToDirection(_apronBearing) * _apronDistance;
        public float ApronRadius => _apronRadius;
        public Vector2 MidwayCenter => MoonSurface.BearingToDirection(_midwayBearing) * _midwayDistance;
        public float MidwayRadius => _midwayRadius;
        public Vector2 DepotCenter => MoonSurface.BearingToDirection(_depotBearing) * _depotDistance;
        public Vector2 DrillCenter => MoonSurface.BearingToDirection(_drillBearing) * _drillDistance;
        public Vector2 GarageCenter => MoonSurface.BearingToDirection(_garageBearing) * _garageDistance;
        public Vector2 MoundCenter => MoonSurface.BearingToDirection(_moundBearing) * _moundDistance;
        public Vector2 RuinsCenter => MoonSurface.BearingToDirection(_ruinsBearing) * _ruinsDistance;
        public float DepotRadius => _depotRadius;
        public float DrillRadius => _drillRadius;
        public float GarageRadius => _garageRadius;
        public float MoundRadius => _moundRadius;
        public float MoundRise => _moundRise;
        public float MoundRun => _moundRun;
        public float RuinsRadius => _ruinsRadius;
        public float RuinsBlend => _ruinsBlend;
        public KestrelSettings Kestrel => _kestrel;

        /// <summary>Returns null when the settings are consistent, otherwise the problem.</summary>
        public string Validate()
        {
            if (_moundRadius + _moundRun > _moundDistance)
            {
                return "Sites: relay.0's mound reaches back to the base.";
            }

            if (_ruinsRadius + _footprintBlend > _ruinsDistance)
            {
                return "Sites: the ruins cluster reaches back to the base.";
            }

            return _kestrel.Validate();
        }
    }
}
