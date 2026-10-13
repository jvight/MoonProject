namespace MoonProject.Gameplay
{
    /// <summary>How a cassette's spot is chosen. Serialized in content assets: append, never renumber.</summary>
    public enum CassetteSiteRule
    {
        /// <summary>At a world anchor (a canyon ledge, beside the crew log cache).</summary>
        Anchor = 0,

        /// <summary>
        /// On a derelict (0.5.5 §2): its container set on the <c>TapeCaseSocket</c> of the derelict standing on the
        /// named micro-site anchor (Dust &amp; Honey in Ro's case beside 06).
        /// </summary>
        Derelict = 2,
    }
}
