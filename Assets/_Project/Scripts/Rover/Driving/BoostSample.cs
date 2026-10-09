namespace MoonProject.Rover
{
    /// <summary>One physics step of what <see cref="BoostDrive"/> needs to know.</summary>
    public readonly struct BoostSample
    {
        /// <param name="owned">07 has the Boost Coils.</param>
        /// <param name="throttle">Eased throttle, -1..1.</param>
        /// <param name="steer">Eased steering, -1..1.</param>
        /// <param name="forwardSpeed">Signed speed along the heading (m/s).</param>
        /// <param name="topSpeed">The normal top speed (m/s).</param>
        /// <param name="slope">Angle (deg) of the ground under 07.</param>
        /// <param name="free">
        /// On the ground with contact and nothing else in charge: not held still, not leaping, not being lifted.
        /// </param>
        public BoostSample(bool owned, float throttle, float steer, float forwardSpeed, float topSpeed, float slope,
            bool free)
        {
            Owned = owned;
            Throttle = throttle;
            Steer = steer;
            ForwardSpeed = forwardSpeed;
            TopSpeed = topSpeed;
            Slope = slope;
            Free = free;
        }

        public bool Owned { get; }

        public float Throttle { get; }

        public float Steer { get; }

        public float ForwardSpeed { get; }

        public float TopSpeed { get; }

        public float Slope { get; }

        public bool Free { get; }
    }
}
