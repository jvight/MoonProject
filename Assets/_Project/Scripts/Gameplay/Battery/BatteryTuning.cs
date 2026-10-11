using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's battery (docs/features/M3-20 §3, VISION ruling 15): what draws charge, the crawl, the solar trickle, the
    /// capacitor drums, the chargers and the warnings. The battery is range, never a threat. Created by the
    /// Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class BatteryTuning : ScriptableObject
    {
        [Header("Capacity")]
        [Tooltip("Cells 07 starts with (four amber flank lamps). Cell Pack II raises it.")]
        [Range(1, BatteryModel.MaxCells)] [SerializeField] private int _startingCells = 4;

        [Tooltip("Cells once Cell Pack II's battery spine is fitted (the two new cells arrive full).")]
        [Range(1, BatteryModel.MaxCells)] [SerializeField] private int _cellPackCells = 6;

        [Header("Driving")]
        [Tooltip("Metres driven on the ground at cruise, on the flat, that one cell pays for. Airborne metres are " +
                 "free.")]
        [Range(30f, 400f)] [SerializeField] private float _metresPerCell = 120f;

        [Tooltip("Driving cost multiplier on an uphill at or steeper than the full slope.")]
        [Range(1f, 3f)] [SerializeField] private float _uphillMultiplier = 1.5f;

        [Tooltip("Driving cost multiplier on a downhill at or steeper than the full slope.")]
        [Range(0f, 1f)] [SerializeField] private float _downhillMultiplier = 0.5f;

        [Tooltip("Ground slope (degrees, along the travel) at which the uphill or downhill multiplier is full; " +
                 "gentler slopes blend from 1.")]
        [Range(2f, 40f)] [SerializeField] private float _fullSlope = 15f;

        [Tooltip("Seconds (time constant) the slope multiplier eases over, so bumps never flicker it.")]
        [Range(0f, 3f)] [SerializeField] private float _slopeEase = 0.5f;

        [Tooltip("Driving cost multiplier while a relic is towed (the tether itself costs nothing).")]
        [Range(1f, 3f)] [SerializeField] private float _towMultiplier = 1.25f;

        [Tooltip("Driving cost multiplier while the Boost Coils are engaged (the drums pay it first).")]
        [Range(1f, 4f)] [SerializeField] private float _boostMultiplier = 2f;

        [Header("Beam work")]
        [Tooltip("Seconds of active beam work (cut, dig, stitch, feed) one cell pays for. A site's work stays " +
                 "about 0.4-0.6 cell.")]
        [Range(10f, 300f)] [SerializeField] private float _beamSecondsPerCell = 60f;

        [Header("Hover-Jump")]
        [Tooltip("Cells a full-charge leap costs (paid at the leap; the drums pay first). The leap refuses at the " +
                 "lip when the drums and cells together can't pay this.")]
        [Range(0f, 1f)] [SerializeField] private float _fullLeapCost = 0.25f;

        [Tooltip("Cells a tap hop costs; a partial charge blends toward the full leap's cost.")]
        [Range(0f, 0.5f)] [SerializeField] private float _tapLeapCost = 0.05f;

        [Header("Capacitor drums (the Boost Coils)")]
        [Tooltip("Cells the twin drums hold. Boosts and leaps draw from them first.")]
        [Range(0f, 1f)] [SerializeField] private float _drumCells = 0.25f;

        [Tooltip("Downhill (degrees) at or steeper than this, rolled at speed, refills the drums.")]
        [Range(1f, 30f)] [SerializeField] private float _drumRollSlope = 5f;

        [Tooltip("Ground speed (m/s) a downhill roll needs to refill the drums.")]
        [Range(0f, 10f)] [SerializeField] private float _drumRollSpeed = 3.5f;

        [Tooltip("Cells a metre of downhill rolling puts back into the drums.")]
        [Range(0f, 0.1f)] [SerializeField] private float _drumRollRefill = 0.01f;

        [Header("The crawl (Kenji's sealed reserve cell)")]
        [Tooltip("Share of 07's top speed while crawling home on the reserve.")]
        [Range(0.1f, 1f)] [SerializeField] private float _crawlCap = 0.35f;

        [Tooltip("Share of 07's top speed while Tilly leads the crawl with her lamp.")]
        [Range(0.1f, 1f)] [SerializeField] private float _leadCap = 0.55f;

        [Tooltip("Seconds (time constant) the drive cap eases over; about three of these (~1.5 s) settle it.")]
        [Range(0.05f, 3f)] [SerializeField] private float _capEase = 0.5f;

        [Tooltip("Seconds of crawl before Tilly (once she is home) flies out by herself to lead.")]
        [Range(0f, 30f)] [SerializeField] private float _leadDelay = 5f;

        [Tooltip("The friend (its id) who flies out to lead a crawl once home, and whose gift is the solar cell.")]
        [SerializeField] private string _leadFriend = "tilly";

        [Tooltip("Cells of charge that end the crawl, so it never flickers on and off at empty.")]
        [Range(0.05f, 1f)] [SerializeField] private float _exitMargin = 0.25f;

        [Header("Solar trickle (Tilly's gift)")]
        [Tooltip("Seconds of standing still that trickle one cell back in.")]
        [Range(30f, 600f)] [SerializeField] private float _trickleSecondsPerCell = 180f;

        [Tooltip("The trickle never lifts the charge above this many cells: waiting is never a strategy.")]
        [Range(0f, 2f)] [SerializeField] private float _trickleCap = 1f;

        [Tooltip("Seconds 07 must stand still before the trickle starts.")]
        [Range(0f, 10f)] [SerializeField] private float _trickleStill = 3f;

        [Tooltip("Ground speed (m/s) at or under which 07 counts as standing still for the trickle.")]
        [Range(0f, 1f)] [SerializeField] private float _stillSpeed = 0.2f;

        [Header("Chargers")]
        [Tooltip("Seconds the dock takes to fill an empty battery, whatever its size.")]
        [Range(0.5f, 15f)] [SerializeField] private float _dockFill = 4f;

        [Tooltip("Seconds a lit mast's pad takes to fill an empty battery, whatever its size.")]
        [Range(0.5f, 15f)] [SerializeField] private float _mastFill = 3f;

        [Tooltip("Radius (m) of a mast's charge ring (the 4 m painted ring on the relay pad).")]
        [Range(0.5f, 3f)] [SerializeField] private float _ringRadius = 2f;

        [Tooltip("Walking pace (m/s): 07 charges on a ring at this speed or slower.")]
        [Range(0.1f, 4f)] [SerializeField] private float _ringMaxSpeed = 1.3f;

        [Header("Warnings (gentle, early, no red)")]
        [Tooltip("Charge (share of capacity) below which the Low warning plays once per discharge: the two-note, " +
                 "the eye dims, the nearest charger's pillar, a ticker line.")]
        [Range(0.05f, 0.9f)] [SerializeField] private float _lowThreshold = 0.3f;

        [Tooltip("Charge (share of capacity) below which 07 glances toward the nearest lit charger, once per " +
                 "discharge.")]
        [Range(0.01f, 0.5f)] [SerializeField] private float _veryLowThreshold = 0.15f;

        [Tooltip("The HUD arc reveals itself the first time the charge drops below this share of capacity.")]
        [Range(0.1f, 1f)] [SerializeField] private float _arcReveal = 0.7f;

        [Tooltip("Times the Low warning's ticker line is shown in a whole game; after that the sound, the eye and " +
                 "the pillar carry it.")]
        [Range(0, 10)] [SerializeField] private int _lowLines = 3;

        [Tooltip("Seconds the nearest charger's pillar stands after the Low warning.")]
        [Range(1f, 60f)] [SerializeField] private float _pillarSeconds = 20f;

        public int StartingCells => _startingCells;
        public int CellPackCells => _cellPackCells;
        public float MetresPerCell => _metresPerCell;
        public float UphillMultiplier => _uphillMultiplier;
        public float DownhillMultiplier => _downhillMultiplier;
        public float FullSlope => _fullSlope;
        public float SlopeEase => _slopeEase;
        public float TowMultiplier => _towMultiplier;
        public float BoostMultiplier => _boostMultiplier;
        public float BeamSecondsPerCell => _beamSecondsPerCell;
        public float FullLeapCost => _fullLeapCost;
        public float TapLeapCost => _tapLeapCost;
        public float DrumCells => _drumCells;
        public float DrumRollSlope => _drumRollSlope;
        public float DrumRollSpeed => _drumRollSpeed;
        public float DrumRollRefill => _drumRollRefill;
        public float CrawlCap => _crawlCap;
        public float LeadCap => _leadCap;
        public float CapEase => _capEase;
        public float LeadDelay => _leadDelay;
        public string LeadFriend => _leadFriend;
        public float ExitMargin => _exitMargin;
        public float TrickleSecondsPerCell => _trickleSecondsPerCell;
        public float TrickleCap => _trickleCap;
        public float TrickleStill => _trickleStill;
        public float StillSpeed => _stillSpeed;
        public float DockFill => _dockFill;
        public float MastFill => _mastFill;
        public float RingRadius => _ringRadius;
        public float RingMaxSpeed => _ringMaxSpeed;
        public float LowThreshold => _lowThreshold;
        public float VeryLowThreshold => _veryLowThreshold;
        public float ArcReveal => _arcReveal;
        public int LowLines => _lowLines;
        public float PillarSeconds => _pillarSeconds;
    }
}
