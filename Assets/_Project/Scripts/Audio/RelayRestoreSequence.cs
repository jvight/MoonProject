using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The order of a relay mast coming back (M3-06): the creak as it straightens at once, its lamp warming a moment
    /// later, then home's link answer. A new restoration restarts the sequence. Allocation-free.
    /// </summary>
    public sealed class RelayRestoreSequence
    {
        private float _elapsed;
        private bool _running;
        private bool _lampDone;
        private bool _linkDone;

        public bool Running => _running;

        /// <summary>Starts the sequence (the creak is the caller's, now).</summary>
        public void Begin()
        {
            _running = true;
            _elapsed = 0f;
            _lampDone = false;
            _linkDone = false;
        }

        /// <summary>Advances time; <paramref name="lamp"/> and <paramref name="link"/> are true on the single step at
        /// which each is due.</summary>
        public void Step(float deltaTime, float lampDelay, float linkDelay, out bool lamp, out bool link)
        {
            lamp = false;
            link = false;
            if (!_running)
            {
                return;
            }

            _elapsed += Mathf.Max(0f, deltaTime);
            if (!_lampDone && _elapsed >= lampDelay)
            {
                _lampDone = true;
                lamp = true;
            }

            if (!_linkDone && _elapsed >= linkDelay)
            {
                _linkDone = true;
                link = true;
            }

            _running = !(_lampDone && _linkDone);
        }
    }
}
