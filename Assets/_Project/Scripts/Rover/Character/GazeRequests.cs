using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>One gaze target slot per <see cref="GazePriority"/>; the highest occupied slot wins.</summary>
    public sealed class GazeRequests
    {
        private const int SlotCount = (int)GazePriority.Focus + 1;

        private readonly Vector3[] _points = new Vector3[SlotCount];
        private readonly bool[] _active = new bool[SlotCount];

        public void Set(GazePriority priority, Vector3? worldPoint)
        {
            int slot = (int)priority;
            _active[slot] = worldPoint.HasValue;
            _points[slot] = worldPoint.GetValueOrDefault();
        }

        public void Clear()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                _active[i] = false;
            }
        }

        /// <summary>The highest-priority active target, if any.</summary>
        public bool TryGetTop(out Vector3 worldPoint, out GazePriority priority)
        {
            for (int i = SlotCount - 1; i >= 0; i--)
            {
                if (_active[i])
                {
                    worldPoint = _points[i];
                    priority = (GazePriority)i;
                    return true;
                }
            }

            worldPoint = Vector3.zero;
            priority = GazePriority.Glance;
            return false;
        }
    }
}
