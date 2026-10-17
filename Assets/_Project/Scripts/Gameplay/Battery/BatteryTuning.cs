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

        [Tooltip("How full 07's cells are when a new game starts (opening-v2 B1: half empty, so it needs a charger).")]
        [Range(0.05f, 1f)] [SerializeField] private float _startingCharge01 = 0.5f;

        [Tooltip("Cells once Cell Pack II's battery spine is fitted (the two new cells arrive full).")]
        [Range(1, BatteryModel.MaxCells)] [SerializeField] private int _cellPackCells = 6;

        [Header("Driving")]
        [Tooltip("Metres driven on the ground at cruise, on the flat, that one cell pays for. Airborne metres are " +
                 "free.")]
        [Range(30f, 600f)] [SerializeField] private float _metresPerCell = 350f;

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

        [Header("Scanning and welding (opening-v2 §10: every function costs battery; lamps are free)")]
        [Tooltip("Seconds of active scan (07's lens open on a target) one cell pays for. The lander cargo bay's two " +
                 "scans cost about a tenth of a cell.")]
        [Range(10f, 600f)] [SerializeField] private float _scanSecondsPerCell = 50f;

        [Tooltip("Seconds of welding one cell pays for: a structure's energy cost is its build seconds over this, so " +
                 "the charging pad's 3 s weld costs about a fifth of a cell.")]
        [Range(2f, 120f)] [SerializeField] private float _weldSecondsPerCell = 15f;

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
        [Tooltip("Seconds the dock takes to fill one cell (4 cells from empty: 4x this). Each cell is one lamp and " +
                 "one note, so the fill reads cell by cell.")]
        [Range(0.5f, 15f)] [SerializeField] private float _dockSecondsPerCell = 4f;

        [Tooltip("Seconds a lit mast's ring takes to fill one cell; slower than the dock, so home is the best " +
                 "charger.")]
        [Range(0.5f, 15f)] [SerializeField] private float _mastSecondsPerCell = 5f;

        [Tooltip("Radius (m) of a mast's charge ring (the 4 m painted ring on the relay pad).")]
        [Range(0.5f, 3f)] [SerializeField] private float _ringRadius = 2f;

        [Tooltip("Ground speed (m/s) at or under which 07 counts as stopped on a charger: charging reads as a stop, " +
                 "never a drive-through.")]
        [Range(0f, 1f)] [SerializeField] private float _chargeStopSpeed = 0.3f;

        [Tooltip("Seconds 07 must stay stopped on the dock or a ring before charging starts.")]
        [Range(0f, 3f)] [SerializeField] private float _chargeStopHold = 0.5f;

        [Header("Warnings (gentle, early, no red)")]
        [Tooltip("Charge (share of capacity) below which the Low warning plays once per discharge: the two-note, " +
                 "the eye dims, a ticker line.")]
        [Range(0.05f, 0.9f)] [SerializeField] private float _lowThreshold = 0.3f;

        [Tooltip("The Low warning also plays when the range left is under this many times the way to the nearest " +
                 "lit charger, whichever comes first: a percentage doesn't know where home is.")]
        [Range(1f, 3f)] [SerializeField] private float _lowRangeMargin = 1.5f;

        [Tooltip("The way to the nearest lit charger is its straight-line distance times this (the ground winds).")]
        [Range(1f, 2f)] [SerializeField] private float _lowRouteFactor = 1.2f;

        [Tooltip("Metres from a lit charger inside which the Low warning never plays (it waits until 07 is farther).")]
        [Range(0f, 150f)] [SerializeField] private float _lowChargerClearance = 40f;

        [Tooltip("Charge (share of capacity) below which 07 glances toward the nearest lit charger, once per " +
                 "discharge.")]
        [Range(0.01f, 0.5f)] [SerializeField] private float _veryLowThreshold = 0.15f;

        [Tooltip("The HUD arc reveals itself the first time the charge drops below this share of capacity.")]
        [Range(0.1f, 1f)] [SerializeField] private float _arcReveal = 0.7f;

        [Tooltip("Times the Low warning's ticker line is shown in a whole game; after that the sound and the eye " +
                 "carry it.")]
        [Range(0, 10)] [SerializeField] private int _lowLines = 3;

        public int StartingCells => _startingCells;
        public float StartingCharge01 => _startingCharge01;
        public int CellPackCells => _cellPackCells;
        public float MetresPerCell => _metresPerCell;
        public float UphillMultiplier => _uphillMultiplier;
        public float DownhillMultiplier => _downhillMultiplier;
        public float FullSlope => _fullSlope;
        public float SlopeEase => _slopeEase;
        public float TowMultiplier => _towMultiplier;
        public float BoostMultiplier => _boostMultiplier;
        public float BeamSecondsPerCell => _beamSecondsPerCell;
        public float ScanSecondsPerCell => _scanSecondsPerCell;
        public float WeldSecondsPerCell => _weldSecondsPerCell;

        /// <summary>Cells <paramref name="weldSeconds"/> of welding cost (a structure's: its build seconds).</summary>
        public float WeldCells(float weldSeconds)
        {
            return weldSeconds / _weldSecondsPerCell;
        }
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
        public float DockSecondsPerCell => _dockSecondsPerCell;
        public float MastSecondsPerCell => _mastSecondsPerCell;
        public float RingRadius => _ringRadius;
        public float ChargeStopSpeed => _chargeStopSpeed;
        public float ChargeStopHold => _chargeStopHold;
        public float LowThreshold => _lowThreshold;
        public float LowRangeMargin => _lowRangeMargin;
        public float LowRouteFactor => _lowRouteFactor;
        public float LowChargerClearance => _lowChargerClearance;
        public float VeryLowThreshold => _veryLowThreshold;
        public float ArcReveal => _arcReveal;
        public int LowLines => _lowLines;
    }
}
