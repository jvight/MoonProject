using MoonProject.Core.Input;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening's objective line keys (docs/design/opening-v2.md §3, opening-v3 guidance audit), published through
    /// <see cref="Core.Events.NextStepChanged"/> one at a time; their en and vi text live in the localization tables.
    /// Never rename a key: it is how the text is found.
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

        /// <summary>B7, a part in the hold that needs the pad and 07 off its plate: "Park on the charging pad."</summary>
        public const string ParkOnPad = "next.v2.park_on_pad";

        /// <summary>B7, the igniter in the hold (anywhere, as it is a field swap): "Fit the igniter to the cutter."</summary>
        public const string FitIgniter = "next.v2.fit_igniter";

        /// <summary>B7: "Find a solar blueprint in the Lumen ruins."</summary>
        public const string FindSolar = "next.v2.find_solar";

        /// <summary>
        /// B7, once the faulted cutter has sputtered at the array's crates: "Take the spare igniter from the rack by the
        /// array." (Kenji's CUTTER SPARES rack).
        /// </summary>
        public const string TakeIgniter = "next.v2.take_igniter";

        /// <summary>B8, while the hold does not cover the array: "Cut the ruins for optics."</summary>
        public const string CutOptics = "next.v2.cut_optics";

        /// <summary>B8: "Build a solar array near the pad."</summary>
        public const string BuildSolar = "next.v2.build_solar";

        /// <summary>B9: "Night is coming. Build a battery bank."</summary>
        public const string BuildBatteryBank = "next.v2.build_battery_bank";

        /// <summary>B9, a battery bank built but out of the grid's reach: "Move the battery bank closer to the pad."</summary>
        public const string MoveBatteryBank = "next.v2.move_battery_bank";

        /// <summary>B11: "Scan the radio tower for an antenna."</summary>
        public const string ScanTower = "next.v2.scan_tower";

        /// <summary>B11: "Build the antenna mast."</summary>
        public const string BuildMast = "next.v2.build_mast";

        /// <summary>The first dusk, the battery bank not yet learned: "Night is coming. Scan the habitat's power module."</summary>
        public const string ScanModule = "next.v3.scan_module";

        /// <summary>
        /// B9, the battery bank learned, the power module not home: "The power module is the bank's frame. Tow it home
        /// to the apron."
        /// </summary>
        public const string TowModuleHome = "next.v3.tow_module";

        /// <summary>R5, a near site visited: "Park on the pad to end the day." (the dock begins the dusk).</summary>
        public const string DockForDusk = "next.v3.dock_for_dusk";

        /// <summary>R8, the mast up: "Transmit from the antenna mast."</summary>
        public const string Transmit = "next.v3.transmit";

        /// <summary>R5, power on: "Power is on. Visit the seismic station or the habitat before dusk."</summary>
        public const string VisitSite = "next.v3.visit_site";

        /// <summary>B9 to B10, the base night-ready: "The base is ready for the night."</summary>
        public const string AwaitNight = "next.v3.await_night";

        /// <summary>R9, relay.1 answered: "relay.1 answered from the west rim. Drive to it."</summary>
        public const string GoToRelay = "next.v3.go_relay";

        /// <summary>5.2, relay.1 lit: "06 is slumped at relay.2 by the canyon mouth. Drive there and scan it."</summary>
        public const string ScanElder06 = "next.v3.scan_elder06";

        /// <summary>5.2, the SSR-3 headlamp unlocked: "Assemble the SSR-3 headlamp at the rack by the pad."</summary>
        public const string AssembleHeadlamp = "next.v3.assemble_headlamp";

        /// <summary>5.2, the headlamp unlocked but the rack not out yet: "Park on the pad to bring out the rack."</summary>
        public const string DockForRack = "next.v3.dock_for_rack";

        /// <summary>
        /// The key a line's action takes, for its glyph (guidance audit G2); 0 when it takes none of 07's action keys
        /// (a drive, a dock, the Tab screen).
        /// </summary>
        public static RoverAction KeyOf(string lineKey)
        {
            switch (lineKey)
            {
                case ScanLander:
                case CollectParts:
                case TakeIgniter:
                case CutOptics:
                case ScanModule:
                case ScanTower:
                case Transmit:
                case ScanElder06:
                case AssembleHeadlamp:
                    return RoverAction.Excavate;
                case BuildPad:
                case BuildSolar:
                case BuildMast:
                case BuildBatteryBank:
                    return RoverAction.Build;
                case TowModuleHome:
                    return RoverAction.Tether;
                default:
                    return default;
            }
        }
    }
}
