namespace MoonProject.Core.Map
{
    /// <summary>
    /// The crew's map's text keys and place-id prefixes (docs/features/M3-07, docs/design/text/crew-map.md). Build
    /// keys once at load, never per frame.
    /// </summary>
    public static class MapKeys
    {
        /// <summary>Every map key starts with it: "map." + place id + <see cref="NameSuffix"/>.</summary>
        public const string Prefix = "map.";

        /// <summary>The place's name, written in Ines's hand once the chart pen inks it.</summary>
        public const string NameSuffix = ".name";

        /// <summary>Ines's pencil margin note for the place.</summary>
        public const string NoteSuffix = ".note";

        /// <summary>A friend's site as a place: "friend." + the friend's id.</summary>
        public const string FriendPrefix = "friend.";

        public const string SheetTitle = "map.sheet.title";
        public const string TallyLabel = "map.tally.label";
        public const string CompleteNote = "map.complete.note";

        /// <summary>A site whose heart still holds something.</summary>
        public const string StateHeartWaiting = "map.state.heart_waiting";

        /// <summary>A site with nothing left to take.</summary>
        public const string StatePickedClean = "map.state.picked_clean";

        /// <summary>A relay mast not lit yet.</summary>
        public const string StateMastDark = "map.state.mast_dark";

        /// <summary>A lit relay mast.</summary>
        public const string StateMastLit = "map.state.mast_lit";
    }
}
