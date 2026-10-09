namespace MoonProject.Rover
{
    /// <summary>
    /// When a friend's gift shows on 07 (docs/features/M3-11): a friend already repaired when the game loads gave it
    /// long ago (<see cref="GiftCue.ShowSilently"/> on the first step); one repaired during play gives it the next time
    /// 07 is home (<see cref="GiftCue.Present"/>, the soft moment). Once given, it stays.
    /// </summary>
    public sealed class FriendGift
    {
        private bool _started;
        private bool _pending;

        /// <summary>The gift is on 07.</summary>
        public bool Given { get; private set; }

        /// <param name="friendAwake">The friend is repaired and about (not dormant, not mid-repair).</param>
        /// <param name="roverHome">07 is home and settling in.</param>
        public GiftCue Step(bool friendAwake, bool roverHome)
        {
            bool first = !_started;
            _started = true;
            if (Given || !friendAwake && !_pending)
            {
                return GiftCue.None;
            }

            if (first)
            {
                Given = true;
                return GiftCue.ShowSilently;
            }

            _pending = true;
            if (!roverHome)
            {
                return GiftCue.None;
            }

            Given = true;
            _pending = false;
            return GiftCue.Present;
        }
    }
}
