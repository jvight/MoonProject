namespace MoonProject.Gameplay
{
    /// <summary>
    /// The keys of the base's wake-up (docs/features/M3-16, M3-19 §7, M3-21 §3): place ids for
    /// <see cref="Core.Events.PlaceFirstApproached"/> (UI reads the id plus ".name" and ".job"), the "next" lines for
    /// <see cref="Core.Events.NextStepChanged"/> (the teaching steps in order, then the recovery targets' needs), and
    /// the "not yet" lines of a dormant station (<see cref="Core.Events.StationNotYet"/>).
    /// </summary>
    public static class ProgressionKeys
    {
        public const string PlaceDock = "place.dock";
        public const string PlaceLander = "place.lander";
        public const string PlaceTower = "place.tower";
        public const string PlaceBay = "place.bay";
        public const string PlaceLadder = "place.ladder";
        public const string PlaceLift = "place.lift";
        public const string PlaceLab = "place.lab";
        public const string PlaceRack = "place.rack";

        /// <summary>Kenji's garage service port (first-hour v2 §3): it fits 07's spare parts.</summary>
        public const string PlaceGaragePort = "place.garage_port";

        /// <summary>The ticker line as the port fits the tether ring.</summary>
        public const string TickerTetherBack = "ticker.tether.back";

        /// <summary>Before the ring, said once near a loose keepsake: the tether is gone, Kenji kept a spare.</summary>
        public const string NotYetTether = "notyet.tether";

        /// <summary>Step 0: drive, look.</summary>
        public const string NextWake = "next.wake";

        /// <summary>Step 1: ping, cut the supply depot.</summary>
        public const string NextSalvage = "next.salvage";

        /// <summary>Step 2: feed and stitch the tower's first level.</summary>
        public const string NextTower = "next.tower";

        /// <summary>Step 3: follow the faint signal, stitch Tilly.</summary>
        public const string NextSignal = "next.signal";

        /// <summary>Step 3b: Tilly's last part lies under the dust; dig it.</summary>
        public const string NextDig = "next.dig";

        /// <summary>Step 3c: all her parts are here; repair her.</summary>
        public const string NextFriendRepair = "next.friend.repair";

        /// <summary>Step 3d: the tower's sweep named Kenji's garage; something of 07's is there.</summary>
        public const string NextGarage = "next.garage";

        /// <summary>Step 3e: the garage's port is open; park on its pad for the spare.</summary>
        public const string NextPort = "next.port";

        /// <summary>Step 4: dig the first relic and tow it home.</summary>
        public const string NextRelic = "next.relic";

        /// <summary>Step 4: the belly lift is stuck; stitch its cable.</summary>
        public const string NextLiftStitch = "next.lift.stitch";

        /// <summary>Step 4: ride the lift up and set the relic on the keepsake shelf.</summary>
        public const string NextLiftRide = "next.lift.ride";

        /// <summary>The station's automated weather loop, once, as tower level 1 clears the radio.</summary>
        public const string RadioWeather = "ticker.radio.weather";

        /// <summary>The masts' letters in time order: letter.mast_1, letter.mast_2...</summary>
        public const string LetterPrefix = "letter.mast_";

        /// <summary>Step 4b (P2): Tilly spots Bell's light across the chasm; drive there and look across.</summary>
        public const string NextChasm = "next.chasm";

        /// <summary>Step 5: the bay sleeps until the tower's second level wakes it.</summary>
        public const string NextBayWake = "next.bay.wake";

        /// <summary>Step 5, the last teaching step: the bay has power; park on the turntable, fit a kit.</summary>
        public const string NextBay = "next.bay";

        /// <summary>Power: the tower's next level waits for its component, not found yet.</summary>
        public const string NextPowerPart = "next.power.part";

        /// <summary>Power: 07 carries the tower's component; feed it to the port.</summary>
        public const string NextPowerCarried = "next.power.carried";

        /// <summary>Power: the component is in; the tower's level waits for its materials.</summary>
        public const string NextPowerBuild = "next.power.build";

        /// <summary>Link: a seen chain mast still dark, by its id (next.link.relay.0...).</summary>
        public const string NextLinkMastPrefix = "next.link.";

        /// <summary>Link: the chain still has dark masts, none of them seen yet.</summary>
        public const string NextLink = "next.link";

        /// <summary>The tower before its first level: the only sleeper that answers then (M3-16 P4).</summary>
        public const string NotYetTower = "notyet.tower";

        public const string NotYetBay = "notyet.bay";

        /// <summary>The survey chart, looked at before the tower's third level.</summary>
        public const string NotYetChart = "notyet.chart";

        /// <summary>The ticker line of 07's first listen ever (first-hour v2 §3), once per save.</summary>
        public const string FirstListen = "ticker.listen.first";

        /// <summary>
        /// True for an empty line or one about a recovery target (Power or Link), the only lines after the teaching
        /// steps (M3-21 §3).
        /// </summary>
        public static bool SpeaksOfTargets(string lineKey)
        {
            return string.IsNullOrEmpty(lineKey) || lineKey == NextPowerPart || lineKey == NextPowerCarried ||
                   lineKey == NextPowerBuild || lineKey == NextLink ||
                   lineKey.StartsWith(NextLinkMastPrefix, System.StringComparison.Ordinal);
        }
    }
}
