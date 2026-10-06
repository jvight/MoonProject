namespace MoonProject.Gameplay
{
    /// <summary>Things the player can do, for context prompts (design ruling 6: a glyph and one word, near the thing).</summary>
    public enum InteractionKind
    {
        None = 0,

        /// <summary>Send a sonar ping (Ready while the sonar is not cooling down).</summary>
        Ping = 1,

        /// <summary>Hold to lift the buried relic 07 is next to.</summary>
        Excavate = 2,

        /// <summary>Hold to latch the tether onto the highlighted relic.</summary>
        Tether = 3,

        /// <summary>Let go here: the towed relic will float onto the museum shelf.</summary>
        Deposit = 4,

        /// <summary>Parked on the tower pad: buy the next level (Ready when affordable).</summary>
        Upgrade = 5,

        /// <summary>Towing: the winch reels the relic in or out.</summary>
        Reel = 6,
    }
}
