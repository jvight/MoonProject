using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Footsteps from distance walked: one step per stride of horizontal travel, so a slow waddle taps slowly and a
    /// hurried one quickly, and a little dance in place taps a foot now and then. A jump larger than the teleport
    /// distance in one frame (being placed at home unseen) is ignored rather than becoming a burst of steps.
    /// </summary>
    public sealed class FootstepCadence
    {
        private Vector3 _last;
        private bool _hasLast;
        private float _travelled;

        /// <summary>Feeds the walker's position; returns true when a step lands.</summary>
        public bool Step(Vector3 position, float stride, float teleportDistance)
        {
            if (!_hasLast)
            {
                _last = position;
                _hasLast = true;
                return false;
            }

            float dx = position.x - _last.x;
            float dz = position.z - _last.z;
            _last = position;
            float moved = Mathf.Sqrt(dx * dx + dz * dz);
            if (moved > teleportDistance)
            {
                _travelled = 0f;
                return false;
            }

            _travelled += moved;
            if (stride <= 0f || _travelled < stride)
            {
                return false;
            }

            _travelled = Mathf.Min(_travelled - stride, stride);
            return true;
        }
    }
}
