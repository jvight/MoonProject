namespace MoonProject.Gameplay
{
    /// <summary>Where on the crater floor a relic is buried (see <see cref="RelicSitePlanner"/>).</summary>
    public enum RelicPlacementBand
    {
        /// <summary>Close to home, roughly ahead of 07's spawn heading: the first pings answer from here.</summary>
        Onboarding = 0,

        /// <summary>Spread around the basin at mid range, in different directions.</summary>
        Wanderer = 1,

        /// <summary>Near the playable edge toward The Peak, on a spot with a clear view of it.</summary>
        RimView = 2,
    }
}
