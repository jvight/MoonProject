namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening's objective line keys (docs/design/opening-v2.md §3), published through
    /// <see cref="Core.Events.NextStepChanged"/> one at a time; UI's objective line owns their en and vi text. Never
    /// rename a key: it is how the text is found.
    /// </summary>
    public static class OpeningKeys
    {
        /// <summary>B1/B2: "Drive to the lander."</summary>
        public const string DriveToLander = "next.v2.drive_lander";

        /// <summary>B3: "Scan the lander's cargo for a blueprint."</summary>
        public const string ScanLander = "next.v2.scan_lander";

        /// <summary>B4: "Collect the parts from the lander's open crate."</summary>
        public const string CollectParts = "next.v2.collect_parts";

        /// <summary>B5: "Build a charging pad beside the lander."</summary>
        public const string BuildPad = "next.v2.build_pad";

        /// <summary>B6: "Park on the pad to charge."</summary>
        public const string Charge = "next.v2.charge";

        /// <summary>B6, charging with the igniter in the hold: "Fit the igniter to the cutter."</summary>
        public const string FitIgniter = "next.v2.fit_igniter";

        /// <summary>B7: "Find a solar blueprint in the Lumen ruins."</summary>
        public const string FindSolar = "next.v2.find_solar";

        /// <summary>B8, while the hold does not cover the array: "Cut the ruins for optics."</summary>
        public const string CutOptics = "next.v2.cut_optics";

        /// <summary>B8: "Build a solar array near the pad."</summary>
        public const string BuildSolar = "next.v2.build_solar";

        /// <summary>B9: "Night is coming. Build a battery bank."</summary>
        public const string BuildBatteryBank = "next.v2.build_battery_bank";

        /// <summary>B11: "Scan the radio tower for an antenna."</summary>
        public const string ScanTower = "next.v2.scan_tower";

        /// <summary>B11: "Build an antenna mast."</summary>
        public const string BuildMast = "next.v2.build_mast";
    }
}
