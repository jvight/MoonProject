using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's battery as pure rules (docs/features/M3-20 §2-§3, VISION ruling 15): the battery is range, never a threat.
    /// <list type="bullet"> <item>Driving draws one cell per tuned metres on the ground, eased by the slope, more while
    /// towing or boosting; beam work, scanning and welding draw by the second (opening-v2 §10); a leap is paid when it happens.
    /// The capacitor drums pay boosts and leaps first. Standing still never drains.</item> <item>A verb in progress
    /// always completes: at empty, beam work in progress runs on the reserve, and the crawl starts once it is done. The
    /// crawl caps the drive (so does a tow, more gently), never runs out, and ends on a charger or once the charge passes the exit margin.</item>
    /// <item>A charger starts only once 07 has stopped on it for a moment, then fills cell by cell at its tuned seconds
    /// per cell (the drums at the same rate). Leaving is free at any moment and keeps the part of a cell already
    /// filled. A working solar wing trickles a still 07 back up to one cell, never more.</item> <item>The Low warning
    /// fires below its share, or once the range left is short of the way to the nearest lit charger, whichever comes
    /// first, but never near a lit charger, on a charger or while charging. Low and VeryLow fire once per discharge and
    /// re-arm while a charger fills past them.</item> </list> Publishes <see cref="BatteryChanged"/> on cell boundaries
    /// only, <see cref="LowPowerChanged"/>, <see cref="ChargingChanged"/>, <see cref="BatteryWarned"/> and
    /// <see cref="BatteryArcRevealed"/>. Allocation-free per step.
    /// </summary>
    public sealed class BatteryModel : IRoverBattery
    {
        /// <summary>
        /// Flank lamp sockets on 07's stripe: the most cells a battery can hold (Cell Pack III's eight).
        /// </summary>
        public const int MaxCells = 8;

        /// <summary>
        /// Charge (cells) treated as a whole cell, so float sums never miss a boundary or the empty mark.
        /// </summary>
        private const float CellEpsilon = 1e-4f;

        private readonly BatteryTuning _tuning;
        private readonly EventBus _events;
        private float _slopeMultiplier = 1f;
        private float _stillTime;
        private float _chargerStopTime;
        private bool _lowArmed;
        private bool _veryLowArmed;
        private int _fullCells;
        private Vector3 _chargerPosition;

        public BatteryModel(BatteryTuning tuning, EventBus events)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            Cells = Mathf.Clamp(tuning.StartingCells, 1, MaxCells);
            Charge = Cells * tuning.StartingCharge01;
            DriveCap01 = 1f;
            _fullCells = FullCells;
            Rearm();
        }

        public int Cells { get; private set; }

        public float Charge { get; private set; }

        public float Charge01 => Charge / Cells;

        /// <summary>Whole cells of charge (what the flank lamps show lit).</summary>
        /// <summary>07 has stood still on a charger long enough for it to charge, charging or not.</summary>
        public bool RestsOnCharger { get; private set; }

        /// <summary>Every cell is full (the drums apart).</summary>
        public bool CellsFull => Charge >= Cells - CellEpsilon;

        public int FullCells => Mathf.Min(Cells, Mathf.FloorToInt(Charge + CellEpsilon));

        public bool IsLowPower { get; private set; }

        public float DriveCap01 { get; private set; }

        /// <summary>The opening runs on the reserve: top speed is capped until the first charge.</summary>
        public bool OnOpeningReserve { get; private set; }

        /// <summary>The charge may not fall below <see cref="BatteryTuning.OpeningChargeFloor"/> (the opening).</summary>
        public bool ChargeFloorHeld { get; private set; }

        /// <summary>Starts or lifts the opening's reserve cap on top speed.</summary>
        public void SetOpeningReserve(bool on)
        {
            OnOpeningReserve = on;
        }

        /// <summary>Holds or releases the opening's charge floor.</summary>
        public void HoldChargeFloor(bool held)
        {
            ChargeFloorHeld = held;
            if (held)
            {
                Charge = Mathf.Max(Charge, _tuning.OpeningChargeFloor);
            }
        }

        public bool CanLeap => !IsLowPower && Drums + Charge >= _tuning.FullLeapCost - CellEpsilon;

        /// <summary>Cells held in the capacitor drums (0 while they are not fitted).</summary>
        public float Drums { get; private set; }

        public float Drums01 => DrumsFitted && _tuning.DrumCells > 0f ? Drums / _tuning.DrumCells : 0f;

        public bool DrumsFitted { get; private set; }

        public bool IsCharging { get; private set; }

        public ChargerKind Charger { get; private set; }

        /// <summary>
        /// The charge is spent but a verb is still finishing on the reserve; the crawl starts after it.
        /// </summary>
        public bool OnReserve => !IsLowPower && Charge <= CellEpsilon;

        /// <summary>The HUD arc has revealed itself in this game.</summary>
        public bool ArcRevealed { get; private set; }

        /// <summary>Times the Low warning's ticker line has been shown in this game.</summary>
        public int LowLinesShown { get; private set; }

        /// <summary>True (and counted) when the Low warning may still show its ticker line.</summary>
        public bool TakeLowLine()
        {
            if (LowLinesShown >= _tuning.LowLines)
            {
                return false;
            }

            LowLinesShown++;
            return true;
        }

        /// <summary>Pays a leap of <paramref name="strength"/> (0 a tap .. 1 a full charge): the drums first.</summary>
        public void PayLeap(float strength)
        {
            float cost = Mathf.Lerp(_tuning.TapLeapCost, _tuning.FullLeapCost, Mathf.Clamp01(strength));
            Charge = Mathf.Max(Floor, Charge - PayFromDrums(cost));
        }

        public void Step(in BatteryStep step, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            DrumsFitted = step.DrumsFitted;
            if (!DrumsFitted)
            {
                Drums = 0f;
            }

            StepSlope(step, deltaTime);
            if (!IsLowPower)
            {
                Drain(step);
            }

            RollIntoDrums(step);
            StepCharger(step, deltaTime);
            StepTrickle(step, deltaTime);
            StepCrawl(step, deltaTime);
            StepWarnings(step.OnCharger, step.Position, step.NearestLitCharger);
            PublishCells();
        }

        public BatterySaveData Capture()
        {
            return new BatterySaveData
            {
                cells = Cells,
                charge = Charge,
                drums = Drums,
                lowPower = IsLowPower,
                arcRevealed = ArcRevealed,
                lowLinesShown = LowLinesShown,
            };
        }

        /// <summary>Takes a loaded game's battery silently; <see cref="Announce"/> tells the others.</summary>
        public void Restore(BatterySaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Cells = Mathf.Clamp(data.cells > 0 ? data.cells : _tuning.StartingCells, 1, MaxCells);
            Charge = Mathf.Clamp(data.charge, 0f, Cells);
            Drums = Mathf.Clamp(data.drums, 0f, _tuning.DrumCells);
            IsLowPower = data.lowPower && Charge < _tuning.ExitMargin;
            ArcRevealed = data.arcRevealed;
            LowLinesShown = Mathf.Max(0, data.lowLinesShown);
            IsCharging = false;
            _chargerStopTime = 0f;
            DriveCap01 = IsLowPower ? _tuning.CrawlCap : 1f;
            _fullCells = FullCells;
            _lowArmed = false;
            _veryLowArmed = false;
            Rearm();
        }

        /// <summary>
        /// Tells everyone the battery the game starts with (Restored = true: shown at once, silently).
        /// </summary>
        public void Announce()
        {
            _events.Publish(new BatteryChanged(_fullCells, Cells, false, true));
            _events.Publish(new LowPowerChanged(IsLowPower, true));
            if (ArcRevealed)
            {
                _events.Publish(new BatteryArcRevealed(true));
            }
        }

        /// <summary>
        /// The slope's cost multiplier eases toward the ground under the travel, held while 07 stands.
        /// </summary>
        private void StepSlope(in BatteryStep step, float deltaTime)
        {
            if (step.GroundDistance <= 0f)
            {
                return;
            }

            float target = _tuning.SlopeMultiplier(step.SlopeDegrees);
            _slopeMultiplier = Damp.Toward(_slopeMultiplier, target, _tuning.SlopeEase, deltaTime);
        }

        private void Drain(in BatteryStep step)
        {
            float drive = step.GroundDistance / _tuning.MetresPerCell * _slopeMultiplier;
            if (step.Towing)
            {
                drive *= _tuning.TowMultiplier;
            }

            if (step.Boosting)
            {
                drive = PayFromDrums(drive * _tuning.BoostMultiplier);
            }

            float beam = step.BeamSeconds / _tuning.BeamSecondsPerCell;
            float scan = step.ScanSeconds / _tuning.ScanSecondsPerCell;
            float weld = _tuning.WeldCells(step.WeldSeconds);
            Charge = Mathf.Max(Floor, Charge - drive - beam - scan - weld);
        }

        private float Floor => ChargeFloorHeld ? _tuning.OpeningChargeFloor : 0f;

        /// <summary>
        /// What the drums cannot pay of <paramref name="cost"/> (cells); the drums pay what they hold.
        /// </summary>
        private float PayFromDrums(float cost)
        {
            float paid = Mathf.Min(Drums, cost);
            Drums -= paid;
            return cost - paid;
        }

        private void RollIntoDrums(in BatteryStep step)
        {
            if (DrumsFitted && step.SlopeDegrees <= -_tuning.DrumRollSlope && step.Speed >= _tuning.DrumRollSpeed)
            {
                Drums = Mathf.Min(_tuning.DrumCells, Drums + step.GroundDistance * _tuning.DrumRollRefill);
            }
        }

        private bool NeedsCharge()
        {
            return Charge < Cells - CellEpsilon || (DrumsFitted && Drums < _tuning.DrumCells - CellEpsilon);
        }

        /// <summary>
        /// A charger fills the cells, and the drums at the same rate, at its seconds per cell; charging starts once 07
        /// has stopped on it (at or under the stop speed for the stop hold) with room to fill, and ends when full or
        /// when 07 leaves, keeping whatever part of a cell is filled.
        /// </summary>
        private void StepCharger(in BatteryStep step, float deltaTime)
        {
            bool stopped = step.OnCharger && step.Speed <= _tuning.ChargeStopSpeed;
            _chargerStopTime = stopped ? _chargerStopTime + deltaTime : 0f;
            RestsOnCharger = stopped && _chargerStopTime >= _tuning.ChargeStopHold;
            if (stopped && !IsCharging && _chargerStopTime >= _tuning.ChargeStopHold && NeedsCharge())
            {
                IsCharging = true;
                Charger = step.Charger;
                _chargerPosition = step.ChargerPosition;
                _events.Publish(new ChargingChanged(true, Charger, _chargerPosition));
            }

            if (IsCharging && step.OnCharger)
            {
                float fill = deltaTime / _tuning.DockSecondsPerCell;
                Charge = Charge + fill >= Cells - CellEpsilon ? Cells : Charge + fill;
                Drums = DrumsFitted ? Mathf.Min(_tuning.DrumCells, Drums + fill) : 0f;
                Rearm();
            }

            if (IsCharging && (!step.OnCharger || !NeedsCharge()))
            {
                PublishCells();
                IsCharging = false;
                _events.Publish(new ChargingChanged(false, Charger, _chargerPosition));
            }
        }

        private void StepTrickle(in BatteryStep step, float deltaTime)
        {
            _stillTime = step.Still ? _stillTime + deltaTime : 0f;
            float cap = Mathf.Min(_tuning.TrickleCap, Cells);
            if (step.SolarFitted && !IsCharging && _stillTime >= _tuning.TrickleStill && Charge < cap)
            {
                Charge = Mathf.Min(cap, Charge + deltaTime / _tuning.TrickleSecondsPerCell);
            }
        }

        private void StepCrawl(in BatteryStep step, float deltaTime)
        {
            bool working = step.BeamSeconds > 0f || step.ScanSeconds > 0f || step.WeldSeconds > 0f;
            if (!IsLowPower && Charge <= CellEpsilon && !working && !IsCharging)
            {
                IsLowPower = true;
                    _events.Publish(new LowPowerChanged(true, false));
            }
            else if (IsLowPower && Charge >= _tuning.ExitMargin)
            {
                IsLowPower = false;
                _events.Publish(new LowPowerChanged(false, false));
            }

            float cap = IsLowPower ? _tuning.CrawlCap : OnOpeningReserve ? _tuning.OpeningReserveCap : 1f;
            if (step.Towing)
            {
                cap = Mathf.Min(cap, _tuning.TowCap * Mathf.Lerp(1f, _tuning.TowStrainFloor, step.TowStrain01));
            }

            DriveCap01 = Damp.Toward(DriveCap01, cap, _tuning.CapEase, deltaTime);
        }

        private void StepWarnings(bool onCharger, Vector3 position, Vector3 nearestLitCharger)
        {
            float share = Charge01;
            if (_lowArmed && !IsCharging && !onCharger && RunningLow(share, position, nearestLitCharger))
            {
                _lowArmed = false;
                _events.Publish(new BatteryWarned(BatteryWarning.Low, nearestLitCharger));
            }

            if (_veryLowArmed && share < _tuning.VeryLowThreshold)
            {
                _veryLowArmed = false;
                _events.Publish(new BatteryWarned(BatteryWarning.VeryLow, nearestLitCharger));
            }

            if (!ArcRevealed && share < _tuning.ArcReveal)
            {
                ArcRevealed = true;
                _events.Publish(new BatteryArcRevealed(false));
            }
        }

        /// <summary>
        /// Below the Low share, or with less range left (flat cruise metres) than the margin times the way to the
        /// nearest lit charger; never while that charger is near.
        /// </summary>
        private bool RunningLow(float share, Vector3 position, Vector3 nearestLitCharger)
        {
            float distance = Mathf.Sqrt(SurfaceRules.HorizontalDistanceSquared(position, nearestLitCharger));
            if (distance <= _tuning.LowChargerClearance)
            {
                return false;
            }

            float range = Charge * _tuning.MetresPerCell;
            float way = distance * _tuning.LowRouteFactor;
            return share < _tuning.LowThreshold || range < way * _tuning.LowRangeMargin;
        }

        /// <summary>Each warning is armed while the charge sits at or above its threshold.</summary>
        private void Rearm()
        {
            float share = Charge01;
            _lowArmed |= share >= _tuning.LowThreshold;
            _veryLowArmed |= share >= _tuning.VeryLowThreshold;
        }

        private void PublishCells()
        {
            int full = FullCells;
            if (full == _fullCells)
            {
                return;
            }

            bool rising = full > _fullCells;
            _fullCells = full;
            _events.Publish(new BatteryChanged(full, Cells, rising, false));
        }
    }
}
