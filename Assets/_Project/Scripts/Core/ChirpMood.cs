namespace MoonProject.Core
{
    /// <summary>
    /// What 07's machine chirp says (docs/design/feel-pass.md §7.2): no words, only a melody in D major pentatonic
    /// whose contour carries the mood. Never saved, but sound banks key on the value: append, never renumber.
    /// </summary>
    public enum ChirpMood
    {
        /// <summary>A short "yes": the dock locks, a kit piece seats, a recipe completes.</summary>
        Affirm = 0,

        /// <summary>The battery is full at the dock; a friend is repaired.</summary>
        Happy = 1,

        /// <summary>A kit piece powers on.</summary>
        Proud = 2,

        /// <summary>A first answer of its kind while listening; a new place found.</summary>
        Curious = 3,

        /// <summary>The battery reaches Low; 07 is stuck or being recovered.</summary>
        Worried = 4,

        /// <summary>The battery is empty and the crawl begins.</summary>
        Tired = 5,

        /// <summary>Charging after a very low or empty battery; Tilly arrives to lead.</summary>
        Relieved = 6,

        /// <summary>A friend meets 07 at home.</summary>
        Greeting = 7,

        /// <summary>07 asks the sky and nothing answers: the reply to the call failed (CallReplyFailed).</summary>
        Ask = 8,
    }
}
