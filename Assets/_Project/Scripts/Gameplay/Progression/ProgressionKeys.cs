namespace MoonProject.Gameplay
{
    /// <summary>
    /// The keys of the base's wake-up (docs/features/M3-16, M3-19 §7): place ids for
    /// <see cref="Core.Events.PlaceFirstApproached"/> (UI reads the id plus ".name" and ".job"), the "next" lines for
    /// <see cref="Core.Events.NextStepChanged"/> in step order, and the "not yet" lines of a dormant station
    /// (<see cref="Core.Events.StationNotYet"/>).
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

        /// <summary>Step 0: drive, look.</summary>
        public const string NextWake = "next.wake";

        /// <summary>Step 1: ping, cut the supply depot.</summary>
        public const string NextSalvage = "next.salvage";

        /// <summary>Step 2: feed and stitch the tower's first level.</summary>
        public const string NextTower = "next.tower";

        /// <summary>Step 3: follow the faint signal, stitch Tilly.</summary>
        public const string NextSignal = "next.signal";

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

        /// <summary>Step 5: power the bay (tower L2), park on the turntable, fit a kit.</summary>
        public const string NextBay = "next.bay";

        /// <summary>Step 6: leap the chasm, bring Bell home.</summary>
        public const string NextLeap = "next.leap";

        /// <summary>Step 7: restore a relay mast.</summary>
        public const string NextRelay = "next.relay";

        /// <summary>Step 8: the tower's third level wakes the lab's survey chart.</summary>
        public const string NextChart = "next.chart";

        /// <summary>Step 8: ride up and look at the survey chart.</summary>
        public const string NextChartLook = "next.chart.look";

        /// <summary>The tower before its first level: the only sleeper that answers then (M3-16 P4).</summary>
        public const string NotYetTower = "notyet.tower";

        public const string NotYetBay = "notyet.bay";

        /// <summary>The survey chart, looked at before the tower's third level.</summary>
        public const string NotYetChart = "notyet.chart";
    }
}
