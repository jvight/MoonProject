namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's working functions as the battery sees them (docs/features/M3-20 §2-§3, opening-v2 §10: every function
    /// costs battery): every beam verb (cut, dig, stitch, feed) reports the seconds it works, a scan the seconds its
    /// lens is open, a weld the seconds its torch burns; beam verbs ask before they start a new piece of work. While
    /// 07 crawls on the reserve, new beam work sleeps; work already under way always completes. The battery prices
    /// each kind with its own rate. One per game, shared through <see cref="GameplayServices.Beam"/>. Allocation-free.
    /// </summary>
    public sealed class BeamWork : IBeamStatus
    {
        private float _seconds;
        private float _scanSeconds;
        private float _weldSeconds;

        /// <summary>New beam work may begin (false while 07 crawls on the reserve cell).</summary>
        public bool MayStart { get; private set; } = true;

        /// <summary>A function worked during the frame the battery last read (the HUD arc hides meanwhile).</summary>
        public bool Working { get; private set; }

        /// <summary>A beam verb worked for <paramref name="deltaTime"/> seconds this frame.</summary>
        public void Work(float deltaTime)
        {
            _seconds += deltaTime;
        }

        /// <summary>07's lens scanned for <paramref name="deltaTime"/> seconds this frame.</summary>
        public void Scan(float deltaTime)
        {
            _scanSeconds += deltaTime;
        }

        /// <summary>07 welded a structure for <paramref name="deltaTime"/> seconds this frame.</summary>
        public void Weld(float deltaTime)
        {
            _weldSeconds += deltaTime;
        }

        /// <summary>
        /// The beam, scan and weld seconds worked since the last call (the battery reads them once per frame).
        /// </summary>
        public float Take(out float scanSeconds, out float weldSeconds)
        {
            float seconds = _seconds;
            scanSeconds = _scanSeconds;
            weldSeconds = _weldSeconds;
            _seconds = 0f;
            _scanSeconds = 0f;
            _weldSeconds = 0f;
            Working = seconds > 0f || scanSeconds > 0f || weldSeconds > 0f;
            return seconds;
        }

        /// <summary>The battery puts new beam work to sleep during the crawl and wakes it after.</summary>
        public void SetAsleep(bool asleep)
        {
            MayStart = !asleep;
        }
    }
}
