using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's battery as pure rules (docs/features/M3-20 §2-§3, VISION ruling 15): the battery is range, never a threat.
    /// <list type="bullet">
    /// <item>Driving draws one cell per tuned metres on the ground, eased by the slope, more while towing or boosting;
    /// beam work draws by the second; a leap is paid when it happens. The capacitor drums pay boosts and leaps first.
    /// Standing still never drains.</item>
    /// <item>A verb in progress always completes: at empty, beam work in progress runs on the reserve, and the crawl
    /// starts once it is done. The crawl caps the drive, never runs out, and ends on a charger or once the charge
    /// passes the exit margin. After a few seconds of crawl Tilly (once home) leads and the cap rises.</item>
    /// <item>Chargers fill the battery and the drums in a fixed time whatever the capacity; the repaired solar cell
    /// trickles a still 07 back up to one cell, never more.</item>
    /// <item>The Low and VeryLow warnings fire once per discharge and re-arm while a charger fills past them.</item>
    /// </list>
    /// Publishes <see cref="BatteryChanged"/> on cell boundaries only, <see cref="LowPowerChanged"/>,
    /// <see cref="ChargingChanged"/>, <see cref="BatteryWarned"/> and <see cref="BatteryArcRevealed"/>.
    /// Allocation-free per step.
    /// </summary>
    public sealed class BatteryModel : IRoverBattery
    {
        /// <summary>Flank lamp sockets on 07's stripe: the most cells a battery can hold (Cell Pack III's eight).</summary>
        public const int MaxCells = 8;

        /// <summary>Charge (cells) treated as a whole cell, so float sums never miss a boundary or the empty mark.</summary>
        private const float CellEpsilon = 1e-4f;

        private readonly BatteryTuning _tuning;
        private readonly EventBus _events;
        private float _slopeMultiplier = 1f;
        private float _stillTime;
        private float _crawlTime;
        private bool _lowArmed;
        private bool _veryLowArmed;
        private int _fullCells;
        private Vector3 _chargerPosition;

        public BatteryModel(BatteryTuning tuning, EventBus events)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            Cells = Mathf.Clamp(tuning.StartingCells, 1, MaxCells);
            Charge = Cells;
            DriveCap01 = 1f;
            _fullCells = FullCells;
            Rearm();
        }

        public int Cells { get; private set; }

        public float Charge { get; private set; }

        public float Charge01 => Charge / Cells;

        /// <summary>Whole cells of charge (what the flank lamps show lit).</summary>
        public int FullCells => Mathf.Min(Cells, Mathf.FloorToInt(Charge + CellEpsilon));

        public bool IsLowPower { get; private set; }

        public float DriveCap01 { get; private set; }

        public bool CanLeap => !IsLowPower && Drums + Charge >= _tuning.FullLeapCost - CellEpsilon;

        /// <summary>Cells held in the capacitor drums (0 while they are not fitted).</summary>
        public float Drums { get; private set; }

        public float Drums01 => DrumsFitted && _tuning.DrumCells > 0f ? Drums / _tuning.DrumCells : 0f;

        public bool DrumsFitted { get; private set; }

        public bool IsCharging { get; private set; }

        public ChargerKind Charger { get; private set; }

        /// <summary>True while Tilly leads the crawl home (the higher drive cap).</summary>
        public bool FriendLeads { get; private set; }

        /// <summary>Seconds of the current crawl (0 when not crawling).</summary>
        public float CrawlTime => _crawlTime;

        /// <summary>The charge is spent but a verb is still finishing on the reserve; the crawl starts after it.</summary>
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
            Charge = Mathf.Max(0f, Charge - PayFromDrums(cost));
        }

        /// <summary>
        /// A new cell pack raised the capacity to <paramref name="cells"/>: the new cells arrive full (the install
        /// moment's rising notes). A smaller or equal capacity changes nothing.
        /// </summary>
        public void GrowTo(int cells)
        {
            cells = Mathf.Clamp(cells, 1, MaxCells);
            if (cells <= Cells)
            {
                return;
            }

            Charge += cells - Cells;
            Cells = cells;
            _fullCells = FullCells;
            _events.Publish(new BatteryChanged(_fullCells, Cells, true, false));
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
            StepWarnings(step.NearestLitCharger);
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
            FriendLeads = false;
            _crawlTime = 0f;
            DriveCap01 = IsLowPower ? _tuning.CrawlCap : 1f;
            _fullCells = FullCells;
            _lowArmed = false;
            _veryLowArmed = false;
            Rearm();
        }

        /// <summary>Tells everyone the battery the game starts with (Restored = true: shown at once, silently).</summary>
        public void Announce()
        {
            _events.Publish(new BatteryChanged(_fullCells, Cells, false, true));
            _events.Publish(new LowPowerChanged(IsLowPower, true));
            if (ArcRevealed)
            {
                _events.Publish(new BatteryArcRevealed(true));
            }
        }

        /// <summary>The slope's cost multiplier eases toward the ground under the travel, held while 07 stands.</summary>
        private void StepSlope(in BatteryStep step, float deltaTime)
        {
            if (step.GroundDistance <= 0f)
            {
                return;
            }

            float share = Mathf.Clamp01(Mathf.Abs(step.SlopeDegrees) / _tuning.FullSlope);
            float target = step.SlopeDegrees >= 0f
                ? Mathf.Lerp(1f, _tuning.UphillMultiplier, share)
                : Mathf.Lerp(1f, _tuning.DownhillMultiplier, share);
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
            Charge = Mathf.Max(0f, Charge - drive - beam);
        }

        /// <summary>What the drums cannot pay of <paramref name="cost"/> (cells); the drums pay what they hold.</summary>
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
        /// A charger fills the cells and the drums in its fixed time; charging starts once 07 is on it with room to
        /// fill and stops when full or when 07 leaves.
        /// </summary>
        private void StepCharger(in BatteryStep step, float deltaTime)
        {
            if (step.OnCharger && !IsCharging && NeedsCharge())
            {
                IsCharging = true;
                Charger = step.Charger;
                _chargerPosition = step.ChargerPosition;
                _events.Publish(new ChargingChanged(true, Charger, _chargerPosition));
            }

            if (IsCharging && step.OnCharger)
            {
                float fill = deltaTime / (Charger == ChargerKind.Dock ? _tuning.DockFill : _tuning.MastFill);
                Charge = Mathf.Min(Cells, Charge + Cells * fill);
                Drums = DrumsFitted ? Mathf.Min(_tuning.DrumCells, Drums + _tuning.DrumCells * fill) : 0f;
                Rearm();
            }

            if (IsCharging && (!step.OnCharger || !NeedsCharge()))
            {
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
            bool working = step.BeamSeconds > 0f;
            if (!IsLowPower && Charge <= CellEpsilon && !working && !IsCharging)
            {
                IsLowPower = true;
                _crawlTime = 0f;
                _events.Publish(new LowPowerChanged(true, false));
            }
            else if (IsLowPower && Charge >= _tuning.ExitMargin)
            {
                IsLowPower = false;
                _events.Publish(new LowPowerChanged(false, false));
            }

            _crawlTime = IsLowPower ? _crawlTime + deltaTime : 0f;
            FriendLeads = IsLowPower && step.FriendCanLead && _crawlTime >= _tuning.LeadDelay;
            float cap = !IsLowPower ? 1f : FriendLeads ? _tuning.LeadCap : _tuning.CrawlCap;
            DriveCap01 = Damp.Toward(DriveCap01, cap, _tuning.CapEase, deltaTime);
        }

        private void StepWarnings(Vector3 nearestLitCharger)
        {
            float share = Charge01;
            if (_lowArmed && share < _tuning.LowThreshold)
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
