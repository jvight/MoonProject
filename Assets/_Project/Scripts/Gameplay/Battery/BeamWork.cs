namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's beam work as the battery sees it (docs/features/M3-20 §2-§3): every beam verb (cut, dig, stitch, feed)
    /// reports the seconds it works, and asks before it starts a new piece of work. While 07 crawls on the reserve,
    /// new work sleeps; work already under way always completes. One per game, shared through
    /// <see cref="GameplayServices.Beam"/>. Allocation-free.
    /// </summary>
    public sealed class BeamWork
    {
        private float _seconds;

        /// <summary>New beam work may begin (false while 07 crawls on the reserve cell).</summary>
        public bool MayStart { get; private set; } = true;

        /// <summary>A verb worked during the frame the battery last read (the HUD arc hides meanwhile).</summary>
        public bool Working { get; private set; }

        /// <summary>A beam verb worked for <paramref name="deltaTime"/> seconds this frame.</summary>
        public void Work(float deltaTime)
        {
            _seconds += deltaTime;
        }

        /// <summary>The seconds worked since the last call (the battery reads them once per frame).</summary>
        public float Take()
        {
            float seconds = _seconds;
            _seconds = 0f;
            Working = seconds > 0f;
            return seconds;
        }

        /// <summary>The battery puts new beam work to sleep during the crawl and wakes it after.</summary>
        public void SetAsleep(bool asleep)
        {
            MayStart = !asleep;
        }
    }
}
