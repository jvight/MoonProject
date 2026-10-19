using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Ground shaped for the places content stands on: opening v2's ruins, the fallen array off them, lander and apron,
    /// and Kestrel-3's impact.
    /// Each shape blends into the ground around it, so the surface stays smooth. Positions are bearings and distances
    /// from the base.
    /// </summary>
    [Serializable]
    public sealed class SiteSettings
    {
        [Tooltip("Distance over which a shaped footprint blends back into the ground around it, metres.")]
        [Range(4f, 30f)]
        [SerializeField] private float _footprintBlend = 12f;

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

        [Tooltip("Where the fallen solar array lies, XZ metres from the ruins cluster's centre: out of The Peak's " +
            "shadow at the golden-hour hold, at the shadow's edge south-west of the cluster, so its wings catch the " +
            "new Sun (opening v3 R4). Its ruin placement must stand inside this footprint.")]
        [SerializeField] private Vector2 _fallenArrayOffset = new Vector2(-22f, -24f);

        [Tooltip("Radius of the fallen array's own flat footing, metres: its wings and its panels' crate beside them.")]
        [Range(2f, 10f)]
        [SerializeField] private float _fallenArrayRadius = 6f;

        [Tooltip("Radius round the fallen array kept clear of rocks, metres: its wings and the sources that stand " +
            "beside it (its junction box stands about 10 m out, on the footing's gentle blend).")]
        [Range(2f, 20f)]
        [SerializeField] private float _fallenArrayClearance = 11f;

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

        [Tooltip("The lander's blast-swept zone round its footprint: why rovers, and 07's old post, stand outside it.")]
        [SerializeField] private BlastZoneSettings _blastZone = new BlastZoneSettings();

        [Header("The Scout, the 1967 soft lander, and its small crater (basin dressing §2)")]
        [Tooltip("Bearing of the Scout from the build apron, degrees: SE, so on the first night Earth (338 degrees, " +
            "16 up) lights the side it shows the base, 25 degrees off the line of sight, and its pale shape reads.")]
        [Range(0f, 360f)]
        [SerializeField] private float _scoutBearing = 137.7f;

        [Tooltip("Distance of the Scout from the build apron, metres: far enough to stay a small \"what's that?\" at " +
            "night, near enough that its earthlit shape reads from the apron (measured: 80 m beats 120 m).")]
        [Range(40f, 300f)]
        [SerializeField] private float _scoutDistance = 80f;

        [Tooltip("Bearing from the Scout to its crater's centre, degrees: it came to rest on the crater's inner wall " +
            "on the base's side, where the wall cancels the plain's natural fall and keeps it near level.")]
        [Range(0f, 360f)]
        [SerializeField] private float _scoutCraterBearing = 317.7f;

        [Tooltip("How far down the wall from the Scout the crater's centre lies, metres: about 0.7 of the radius, " +
            "where the wall leans ~8 degrees.")]
        [Range(0f, 12f)]
        [SerializeField] private float _scoutCraterOffset = 7f;

        [Tooltip("Radius of the Scout's crater where its wall meets the rim crest, metres.")]
        [Range(4f, 16f)]
        [SerializeField] private float _scoutCraterRadius = 10f;

        [Tooltip("Depth of the Scout's crater floor below the plain, metres: shallow enough that its wall stays " +
            "drivable (under 12 degrees at its steepest).")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _scoutCraterDepth = 1.1f;

        [Tooltip("Height of the Scout crater's low rim crest above the plain, metres.")]
        [Range(0f, 1f)]
        [SerializeField] private float _scoutCraterRim = 0.3f;

        public float FootprintBlend => _footprintBlend;
        public Vector2 WakePost => _wakePost;
        public Vector2 LanderCenter => MoonSurface.BearingToDirection(_landerBearing) * _landerDistance;
        public float LanderRadius => _landerRadius;
        public Vector2 ApronCenter => MoonSurface.BearingToDirection(_apronBearing) * _apronDistance;
        public float ApronRadius => _apronRadius;
        public Vector2 MidwayCenter => MoonSurface.BearingToDirection(_midwayBearing) * _midwayDistance;
        public float MidwayRadius => _midwayRadius;
        public Vector2 RuinsCenter => MoonSurface.BearingToDirection(_ruinsBearing) * _ruinsDistance;
        public float RuinsRadius => _ruinsRadius;
        public float RuinsBlend => _ruinsBlend;
        public Vector2 FallenArrayCenter => RuinsCenter + _fallenArrayOffset;
        public float FallenArrayRadius => _fallenArrayRadius;
        public float FallenArrayClearance => _fallenArrayClearance;
        public KestrelSettings Kestrel => _kestrel;
        public BlastZoneSettings BlastZone => _blastZone;
        public Vector2 ScoutSpot => ApronCenter + MoonSurface.BearingToDirection(_scoutBearing) * _scoutDistance;
        public Vector2 ScoutCraterCenter =>
            ScoutSpot + MoonSurface.BearingToDirection(_scoutCraterBearing) * _scoutCraterOffset;
        public float ScoutCraterRadius => _scoutCraterRadius;
        public float ScoutCraterDepth => _scoutCraterDepth;
        public float ScoutCraterRim => _scoutCraterRim;

        /// <summary>Returns null when the settings are consistent, otherwise the problem.</summary>
        public string Validate()
        {
            if (_ruinsRadius + _footprintBlend > _ruinsDistance)
            {
                return "Sites: the ruins cluster reaches back to the base.";
            }

            if (_ruinsRadius + _fallenArrayRadius + _footprintBlend > _fallenArrayOffset.magnitude)
            {
                return "Sites: the fallen array's footprint reaches into the ruins' flat ground.";
            }

            if (_scoutCraterOffset >= _scoutCraterRadius)
            {
                return "Sites: the Scout stands outside its own crater.";
            }

            return _kestrel.Validate();
        }
    }
}
