namespace MoonProject.Gameplay
{
    /// <summary>What an awake friend's frame produced that the friend field turns into events and saves.</summary>
    internal readonly struct FriendBeat
    {
        public FriendBeat(bool greeted, bool spotted, SpotTarget spot)
        {
            Greeted = greeted;
            Spotted = spotted;
            Spot = spot;
        }

        /// <summary>It began greeting 07 coming home.</summary>
        public bool Greeted { get; }

        /// <summary>Its spotter gift found <see cref="Spot"/>.</summary>
        public bool Spotted { get; }

        public SpotTarget Spot { get; }
    }
}
