using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A friend's gift that is a part (docs/design/0.5.5-owner-feedback.md §3.5, Tilly's solar cell for the wing): a
    /// friend already repaired when an older save loads gave it long ago, so it loads fitted; one repaired during play
    /// gives it the next time 07 is home and settling in, when it fits itself (the soft moment,
    /// <see cref="PartFitted.Gift"/>). Once on, it stays. Allocation-free.
    /// </summary>
    public sealed class GiftFitting
    {
        private readonly RoverEquipment _equipment;
        private readonly RoverKitPiece _gift;
        private readonly IFriendState _friend;
        private readonly IRoverState _rover;
        private readonly Vector3 _home;
        private readonly FriendTuning _tuning;
        private bool _started;

        public GiftFitting(RoverEquipment equipment, RoverKitPiece gift, IFriendState friend, IRoverState rover,
            Vector3 home, FriendTuning tuning)
        {
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            _friend = friend ?? throw new ArgumentNullException(nameof(friend));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _gift = gift;
            _home = home;
        }

        public void Step()
        {
            bool awake = _friend.Activity != FriendActivity.Dormant && _friend.Activity != FriendActivity.Repairing;
            if (!_started)
            {
                _started = true;
                if (awake)
                {
                    _equipment.RestoreOwned(_gift);
                }
            }

            if (awake && !_equipment.IsFitted(_gift) && Home())
            {
                _equipment.FitItself(_gift);
            }
        }

        private bool Home()
        {
            return SurfaceRules.HorizontalDistance(_rover.Position, _home) <= _tuning.GiftHomeRadius &&
                   _rover.Speed <= _tuning.GiftMaxSpeed;
        }
    }
}
