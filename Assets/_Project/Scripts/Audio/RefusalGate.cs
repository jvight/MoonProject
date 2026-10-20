using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// Holds a refused cut's fault tick until the frame's events are in, so the one-time sputter (which comes with the
    /// first refusal, in either order) speaks alone: a refusal in a frame that also sputtered is dropped. Plain logic,
    /// allocation-free; <see cref="SalvageAudio"/> asks it once per frame after the events.
    /// </summary>
    public sealed class RefusalGate
    {
        private bool _pending;
        private int _pendingFrame = -1;
        private int _sputterFrame = -1;
        private Vector3 _point;

        /// <summary>A cut was refused at <paramref name="point"/> in frame <paramref name="frame"/>.</summary>
        public void Refuse(int frame, Vector3 point)
        {
            _pending = true;
            _pendingFrame = frame;
            _point = point;
        }

        /// <summary>The cutter sputtered in frame <paramref name="frame"/>: that frame's refusal stays quiet.</summary>
        public void Sputter(int frame)
        {
            _sputterFrame = frame;
        }

        /// <summary>
        /// Once the frame's events are in: true with the refusal's <paramref name="point"/> when its tick should play
        /// (one was pending and no sputter came in its frame). Clears the pending refusal either way.
        /// </summary>
        public bool TryTakeRefusal(out Vector3 point)
        {
            point = _point;
            if (!_pending)
            {
                return false;
            }

            _pending = false;
            return _pendingFrame != _sputterFrame;
        }
    }
}
