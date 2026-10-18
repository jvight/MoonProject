using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What one hold of E is on: the target the verb offered when the key went down stays the target until the key
    /// comes up, whatever the view or the other verbs do meanwhile. A hold that began with the verb yielding to another
    /// stays silent, so the two never trade the hold mid-way. Pure logic.
    /// </summary>
    /// <typeparam name="T">The kind of target.</typeparam>
    public sealed class HoldLock<T> where T : class
    {
        private bool _wasHeld;
        private bool _silenced;
        private T _locked;

        /// <summary>The target the current hold began on, or null.</summary>
        public T Locked => _locked;

        /// <summary>
        /// The target for this frame. <paramref name="offered"/> is what the verb would pick now,
        /// <paramref name="yielded"/> that it picked nothing because another verb came first, and
        /// <paramref name="stillValid"/> says whether the locked target can still be worked.
        /// </summary>
        public T Resolve(bool held, T offered, bool yielded, Func<T, bool> stillValid)
        {
            if (stillValid == null)
            {
                throw new ArgumentNullException(nameof(stillValid));
            }

            if (!held)
            {
                _wasHeld = false;
                _silenced = false;
                _locked = null;
                return offered;
            }

            if (!_wasHeld)
            {
                _wasHeld = true;
                _locked = offered;
                _silenced = offered == null && yielded;
            }

            if (_locked != null && !stillValid(_locked))
            {
                _locked = null;
                _silenced = true;
            }

            if (_locked != null)
            {
                return _locked;
            }

            return _silenced ? null : offered;
        }
    }
}
