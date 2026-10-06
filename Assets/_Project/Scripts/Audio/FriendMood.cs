namespace MoonProject.Audio
{
    /// <summary>
    /// A friend's chirp vocabulary. Each mood is a cue named <c>&lt;friendId&gt;_&lt;mood&gt;</c> (e.g. tilly_curious)
    /// rendered by tools/audio with a few variants.
    /// </summary>
    public enum FriendMood
    {
        /// <summary>Dormant and damaged, answering a ping: faint, glitchy, hopeful.</summary>
        Broken = 0,

        /// <summary>Looking around while following or spotting.</summary>
        Curious = 1,

        /// <summary>Content at home, or just repaired.</summary>
        Happy = 2,

        /// <summary>Napping on its perch.</summary>
        Sleepy = 3,

        /// <summary>Flying out to meet 07 when it comes home.</summary>
        Greeting = 4,

        /// <summary>A relic was brought home.</summary>
        Excited = 5,

        /// <summary>Spotted something worth finding ("found it!").</summary>
        Found = 6,
    }
}
