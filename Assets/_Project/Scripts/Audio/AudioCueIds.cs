namespace MoonProject.Audio
{
    /// <summary>
    /// Cue ids of tools/audio/sfx_manifest.json, the single source of truth (a test checks every id exists there).
    /// </summary>
    public static class AudioCueIds
    {
        public const string FriendStitch = "friend_stitch";
        public const string FriendBoot = "friend_boot";
        public const string FriendPart = "friend_part";
        public const string FriendSpotPing = "friend_spot_ping";
        public const string JumpCharge = "jump_charge";
        public const string JumpLeap = "jump_leap";
        public const string CoilTwang = "coil_twang";
        public const string AirWind = "air_wind";
        public const string JumpLand = "jump_land";
        public const string CoilPop = "coil_pop";
        public const string WorkbenchUpgrade = "workbench_upgrade";
        public const string RoverHum = "rover_hum";
        public const string DustCrunch = "dust_crunch";
        public const string SuspensionCreak = "suspension_creak";
        public const string LandingThump = "landing_thump";
        public const string RadioStatic = "radio_static";
        public const string RadioTune = "radio_tune";
        public const string RadioDialClick = "radio_dial_click";
        public const string AmbienceBed = "ambience_bed";
        public const string SonarPing = "sonar_ping";
        public const string RelicAnswer = "relic_answer";
        public const string ScrapChime = "scrap_chime";
        public const string TetherAttach = "tether_attach";
        public const string TetherHum = "tether_hum";
        public const string TetherRelease = "tether_release";
        public const string TetherSnap = "tether_snap";
        public const string RelicPlaced = "relic_placed";
        public const string RecoveryLift = "recovery_lift";
        public const string RecoverySettle = "recovery_settle";
        public const string ExcavationRumble = "excavation_rumble";
        public const string SurfacingSparkle = "surfacing_sparkle";
        public const string UiConfirm = "ui_confirm";
        public const string UiBack = "ui_back";
        public const string UiMenuOpen = "ui_menu_open";
        public const string UiMenuClose = "ui_menu_close";
        public const string UiFocus = "ui_focus";
        public const string UiSlider = "ui_slider";
        public const string UiPrompt = "ui_prompt";
        public const string UiHoldFill = "ui_hold_fill";
        public const string UiHoldComplete = "ui_hold_complete";
        public const string UiCard = "ui_card";
        public const string UpgradeArpeggio = "upgrade_arpeggio";
        public const string CassettePickup = "cassette_pickup";
        public const string CrewLogFound = "crew_log_found";
        public const string BellSignalPick = "bell_signal_pick";
        public const string BellSignalFound = "bell_signal_found";
        public const string CanyonWhisper = "canyon_whisper";
        public const string CanyonTrough = "canyon_trough";

        /// <summary>A friend's chirp cue, <c>&lt;friendId&gt;_&lt;mood&gt;</c> (e.g. tilly_curious). Builds a string:
        /// call at initialisation only.</summary>
        public static string FriendChirp(string friendId, FriendMood mood)
        {
            return $"{friendId}_{MoodSuffix(mood)}";
        }

        /// <summary>A friend's own cue <c>&lt;friendId&gt;_&lt;part&gt;</c> for one of the
        /// <see cref="FriendCueParts"/>. Builds a string: initialisation only.</summary>
        public static string FriendCue(string friendId, string part)
        {
            return $"{friendId}_{part}";
        }

        private static string MoodSuffix(FriendMood mood)
        {
            switch (mood)
            {
                case FriendMood.Broken:
                    return "broken";
                case FriendMood.Curious:
                    return "curious";
                case FriendMood.Happy:
                    return "happy";
                case FriendMood.Sleepy:
                    return "sleepy";
                case FriendMood.Greeting:
                    return "greeting";
                case FriendMood.Excited:
                    return "excited";
                case FriendMood.Found:
                    return "found";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(mood), mood, "Unknown friend mood.");
            }
        }
    }
}
