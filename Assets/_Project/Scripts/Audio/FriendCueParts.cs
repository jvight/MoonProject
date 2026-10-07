namespace MoonProject.Audio
{
    /// <summary>
    /// Optional parts of a friend's voice, rendered as cues named <c>&lt;friendId&gt;_&lt;part&gt;</c> (see
    /// <see cref="AudioCueIds.FriendCue"/>). A friend without a part simply does not make that sound.
    /// </summary>
    public static class FriendCueParts
    {
        /// <summary>Rotor/motor loop following the friend's effort (Tilly).</summary>
        public const string Rotor = "rotor";

        /// <summary>Foot taps while it walks (Bell).</summary>
        public const string Step = "step";

        /// <summary>Loop while it naps (Bell's ember hum).</summary>
        public const string Doze = "doze";

        /// <summary>One-shot as it wakes from a nap.</summary>
        public const string Wake = "wake";

        /// <summary>One-shot when the radio dial turns to another station (Bell's crackle).</summary>
        public const string Tune = "tune";

        /// <summary>Music-box jingle when it stands up repaired, instead of the happy chirp (Bell).</summary>
        public const string JingleShort = "jingle_short";

        /// <summary>Music-box jingle for its homecoming greeting, instead of the greeting chirp (Bell).</summary>
        public const string JingleFull = "jingle_full";
    }
}
