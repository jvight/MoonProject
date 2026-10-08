using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Test-only staging of what Gameplay's cargo cradle does with a relic: it rides at the seat every LateUpdate,
    /// resting on the basket floor (its centre-of-mass pivot lifted by half its height).
    /// </summary>
    public sealed class SeatRider : MonoBehaviour
    {
        private IRoverCargoSeat _seat;
        private float _rest;

        public void Ride(IRoverCargoSeat seat, float restHeight)
        {
            _seat = seat;
            _rest = restHeight;
        }

        private void LateUpdate()
        {
            if (_seat == null)
            {
                return;
            }

            Quaternion rotation = _seat.Rotation;
            transform.SetPositionAndRotation(_seat.Position + rotation * Vector3.up * _rest, rotation);
        }
    }
}
