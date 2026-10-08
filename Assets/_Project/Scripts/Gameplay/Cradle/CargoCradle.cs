using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The Cargo Cradle's relic carry (docs/features/M3-11). While the cradle is fitted on 07 (Core's
    /// <see cref="IRoverCargoSeat"/>, from the Rover domain) and empty, the press that would latch the tether onto an
    /// aimed loose relic lifts it gently into the rear rack instead: it floats along a soft arc to the seat, pauses
    /// just above it and settles in with a small overshoot (<see cref="RelicStowed"/>, then a save). It then rides at
    /// the seat every frame, never fighting physics, through bumps, leaps, recoveries and radio-hops alike. At the
    /// museum shelf a press of the same button sets it down onto the shelf through the usual deposit. One relic at a
    /// time: further relics and the big salvage drag pieces still go on the tether. A relic saved in the rack comes
    /// back in it, and if the cradle is ever off 07 the relic is set down loose beside it (VISION ruling 1: nothing is
    /// lost).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CargoCradle : MonoBehaviour
    {
        private EventBus _events;
        private ISaveService _save;
        private IRoverCargoSeat _seat;
        private RelicField _relics;
        private HomeBase _home;
        private Relic _carried;
        private bool _settled;
        private float _time;
        private float _duration;
        private Vector3 _start;
        private Quaternion _startRotation;
        private bool _initialized;

        /// <summary>The relic in (or lifting into) the rack, or null.</summary>
        public Relic Carried => _carried;

        /// <summary>The carried relic has settled into its seat.</summary>
        public bool IsSettled => _carried != null && _settled;

        /// <summary>A press would lift an aimed relic into the rack: the cradle is on 07 and empty.</summary>
        public bool CanStow => _initialized && _carried == null && _seat.IsFitted;

        /// <summary>A press would set the carried relic down on the museum shelf: settled, and in reach.</summary>
        public bool CanUnload => IsSettled && _home.InDepositZone(_carried.transform.position);

        internal bool Initialize(GameplayServices services, IRoverCargoSeat seat, RelicField relics, HomeBase home)
        {
            _events = services.Events;
            _save = services.Save;
            _seat = seat ?? throw new ArgumentNullException(nameof(seat));
            _relics = relics != null ? relics : throw new ArgumentNullException(nameof(relics));
            _home = home != null ? home : throw new ArgumentNullException(nameof(home));
            _initialized = true;
            return true;
        }

        /// <summary>
        /// The tether is about to latch onto <paramref name="towable"/>: true when the cradle takes it instead (fitted,
        /// empty, and it is a loose relic). It starts lifting into the rack.
        /// </summary>
        internal bool TryStow(ITowable towable)
        {
            if (!CanStow || !(towable is Relic relic) || !relic.IsTetherable)
            {
                return false;
            }

            _start = relic.transform.position;
            _startRotation = relic.transform.rotation;
            relic.BeginCarry(RelicState.Cradled, -1);
            _carried = relic;
            _settled = false;
            _time = 0f;
            RelicTuning tuning = _relics.Tuning;
            _duration = tuning.StowDuration + Vector3.Distance(_start, SeatPose(relic)) * tuning.StowPerMetre;
            return true;
        }

        /// <summary>A press at the shelf: true when the carried relic was set down onto it.</summary>
        internal bool TryUnload()
        {
            if (!CanUnload || !_home.TakeFromCradle(_carried))
            {
                return false;
            }

            _carried = null;
            return true;
        }

        /// <summary>After a load: a relic saved in the rack rides in it again (any other is set down loose).</summary>
        internal void AdoptRestored()
        {
            _carried = null;
            for (int i = 0; i < _relics.Relics.Count; i++)
            {
                Relic relic = _relics.Relics[i];
                if (relic.State != RelicState.Cradled)
                {
                    continue;
                }

                if (_carried == null)
                {
                    _carried = relic;
                    _settled = true;
                }
                else
                {
                    relic.Release();
                }
            }
        }

        // After 07 has moved this frame (physics, interpolation, a hop), so the relic sits exactly in the rack.
        private void LateUpdate()
        {
            if (!_initialized || _carried == null)
            {
                return;
            }

            if (_carried.State != RelicState.Cradled)
            {
                _carried = null;
                return;
            }

            if (!_seat.IsFitted)
            {
                // Nothing is ever lost: without the cradle it is set down loose, where relics are looked after.
                _carried.Release();
                _carried = null;
                return;
            }

            Vector3 seat = SeatPose(_carried);
            Quaternion rotation = _seat.Rotation;
            if (_settled)
            {
                _carried.SetCarryPose(seat, rotation);
                return;
            }

            RelicTuning tuning = _relics.Tuning;
            _time += Time.deltaTime;
            float progress = _time / _duration;
            _carried.SetCarryPose(
                GlidePath.Position(_start, seat, progress, tuning.StowLift, tuning.StowHover, tuning.StowOvershoot),
                GlidePath.Rotation(_startRotation, rotation, progress));
            if (progress < 1f)
            {
                return;
            }

            _settled = true;
            _carried.SetCarryPose(seat, rotation);
            _events.Publish(new RelicStowed(_carried.Definition.Id, seat));
            _save.SaveNow();
        }

        /// <summary>Where the relic's pivot rests in the rack: its base on the seat.</summary>
        private Vector3 SeatPose(Relic relic)
        {
            return _seat.Position + _seat.Rotation * Vector3.up * relic.RestHeight;
        }
    }
}
