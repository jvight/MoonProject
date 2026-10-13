using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Tilly fetches what was held back (docs/features/M3-25 T3, 0.5.5 §2): once she is home, every so often she flies
    /// out to the nearest held stack inside the station's reach (the tower's circle or a lit relay's), unclips it from
    /// its wreck or its crate's mouth, hooks it under her body and brings it home at a calm speed, while 07 does
    /// anything else. Leading a crawl home comes first: she keeps the stack hanging under her and brings it home
    /// after; it never drops on the ground. One stack at a time; allocation-free.
    /// </summary>
    public sealed class TillyFetch
    {
        private readonly CargoBundles _bundles;
        private readonly IStationReach _reach;
        private readonly Friend _tilly;
        private readonly DroneBody _body;
        private readonly CargoTuning _tuning;
        private readonly Func<Vector3> _shelves;
        private Stage _stage;
        private int _bundleId = -1;
        private float _next;
        private bool _homing;

        public TillyFetch(CargoBundles bundles, IStationReach reach, Friend tilly, CargoTuning tuning,
            Func<Vector3> shelves)
        {
            _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
            _reach = reach ?? throw new ArgumentNullException(nameof(reach));
            _tilly = tilly ?? throw new ArgumentNullException(nameof(tilly));
            _body = tilly.Body as DroneBody ?? throw new ArgumentException(
                $"'{tilly.Id}' does not fly, so she cannot fetch bundles.", nameof(tilly));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _shelves = shelves ?? throw new ArgumentNullException(nameof(shelves));
        }

        private enum Stage
        {
            Resting,
            Out,
            Back,
        }

        /// <summary>True from her setting off until the stack is home.</summary>
        public bool Fetching => _stage != Stage.Resting;

        public void Step(float now)
        {
            bool free = _tilly.Progress.State == FriendState.Awake && !_body.Leading;
            switch (_stage)
            {
                case Stage.Resting:
                    if (free && now >= _next)
                    {
                        SetOff(now);
                    }

                    break;
                case Stage.Out:
                    StepOut(free, now);
                    break;
                case Stage.Back:
                    StepBack(free, now);
                    break;
            }
        }

        private void SetOff(float now)
        {
            _next = now + _tuning.FetchIntervalSeconds;
            int bundle = _bundles.NearestToFetch(_reach, _shelves());
            if (bundle < 0)
            {
                return;
            }

            _bundleId = _bundles.IdAt(bundle);
            _stage = Stage.Out;
            _body.Fetch(true, _bundles.RestPosition(bundle), _tuning.FetchSpeed, _tuning.FetchHeight);
        }

        private void StepOut(bool free, float now)
        {
            int bundle = _bundles.IndexOf(_bundleId);
            if (!free || bundle < 0 || !_bundles.IsWaiting(bundle))
            {
                StandDown(now);
                return;
            }

            Vector3 over = _bundles.RestPosition(bundle) + Vector3.up * _tuning.FetchHeight;
            if (SurfaceRules.HorizontalDistance(_tilly.Position, over) > _tuning.FetchArrive)
            {
                return;
            }

            _bundles.Hook(bundle);
            _stage = Stage.Back;
            _homing = true;
            _body.Fetch(true, _shelves(), _tuning.FetchSpeed, _tuning.FetchHeight);
        }

        private void StepBack(bool free, float now)
        {
            int bundle = _bundles.IndexOf(_bundleId);
            _bundles.Carry(bundle, _tilly.Position - Vector3.up * _tuning.FetchHang, now);
            if (!free)
            {
                _homing = false;
                return;
            }

            if (!_homing)
            {
                _homing = true;
                _body.Fetch(true, _shelves(), _tuning.FetchSpeed, _tuning.FetchHeight);
            }

            if (SurfaceRules.HorizontalDistance(_tilly.Position, _shelves()) > _tuning.FetchArrive)
            {
                return;
            }

            _bundles.Deliver(bundle);
            StandDown(now);
        }

        private void StandDown(float now)
        {
            _stage = Stage.Resting;
            _bundleId = -1;
            _next = now + _tuning.FetchIntervalSeconds;
            _body.Fetch(false, Vector3.zero, 0f, 0f);
        }
    }
}
