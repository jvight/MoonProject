using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Unloading at the dock (docs/features/M3-25 §3.2): each time 07 comes to rest on the dock, with no input and
    /// inside the charge, the hatch opens (a Cargo Anticipate) and 07's slots ride the belt onto home's shelves one after
    /// another (an Act each, strength = how far it has come), ending on the last crate (Reward). When home fills, the
    /// belt stops (Cancel), the rest stays in 07 and the line says so once. Leaving the dock early keeps what moved.
    /// Allocation-free.
    /// </summary>
    public sealed class DockUnload
    {
        /// <summary>The line when home's shelves are full.</summary>
        public const string TickerHomeFull = "ticker.cargo.home_full";

        private readonly EventBus _events;
        private readonly MaterialStock _stock;
        private readonly CargoTuning _tuning;
        private readonly ISaveService _save;
        private bool _wasDocked;
        private bool _running;
        private float _next;
        private int _slots;
        private int _done;
        private int _moved;

        public DockUnload(EventBus events, MaterialStock stock, CargoTuning tuning, ISaveService save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>True from the hatch opening until the last crate or the belt stopping.</summary>
        public bool Running => _running;

        /// <summary>07 came to rest on the dock this frame (a trip ends).</summary>
        public bool DockedNow { get; private set; }

        /// <param name="docked">True while 07 rests on the dock (<see cref="ChargingDock.Docked"/>).</param>
        /// <param name="hatch">Where 07's hatch is (the beats' position).</param>
        public void Step(bool docked, Vector3 hatch, float now)
        {
            DockedNow = docked && !_wasDocked;
            bool left = !docked && _wasDocked;
            _wasDocked = docked;
            if (DockedNow && _stock.Hold.UsedSlots > 0)
            {
                _running = true;
                _slots = _stock.Hold.UsedSlots;
                _done = 0;
                _moved = 0;
                _next = now + _tuning.UnloadLeadSeconds;
                Publish(FeelPhase.Anticipate, hatch, 0f, _tuning.UnloadLeadSeconds);
            }

            if (!_running)
            {
                return;
            }

            if (left)
            {
                Finish(false);
                return;
            }

            while (_running && now >= _next)
            {
                MoveOne(hatch);
                _next += _tuning.UnloadStepSeconds;
            }
        }

        private void MoveOne(Vector3 hatch)
        {
            MaterialSlot slot = _stock.Hold.GetSlot(0);
            int moved = _stock.MoveHome(slot.Material, slot.Units);
            _moved += moved;
            if (moved < slot.Units)
            {
                Publish(FeelPhase.Cancel, hatch, Progress(), 0f);
                _events.Publish(new TickerLine(TickerHomeFull));
                Finish(true);
                return;
            }

            _done++;
            Publish(FeelPhase.Act, hatch, Progress(), 0f);
            if (_stock.Hold.UsedSlots == 0)
            {
                Publish(FeelPhase.Reward, hatch, 1f, 0f);
                Finish(false);
            }
        }

        private void Finish(bool homeFull)
        {
            _running = false;
            _events.Publish(new CargoUnloaded(_moved, _stock.Hold.TotalUnits, homeFull));
            _save.SaveNow();
        }

        private float Progress()
        {
            return Mathf.Clamp01((float)_done / Mathf.Max(1, _slots));
        }

        private void Publish(FeelPhase phase, Vector3 at, float strength, float seconds)
        {
            _events.Publish(new FeelBeat(FeelKind.Cargo, phase, at, Vector3.up, strength, seconds));
        }
    }
}
