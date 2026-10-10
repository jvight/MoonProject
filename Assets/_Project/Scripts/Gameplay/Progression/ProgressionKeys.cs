namespace MoonProject.Gameplay
{
    /// <summary>
    /// The keys of the base's wake-up (docs/features/M3-16): place ids for
    /// <see cref="Core.Events.PlaceFirstApproached"/> (UI reads the id plus ".name" and ".job"), the "next" lines for
    /// <see cref="Core.Events.NextStepChanged"/> in step order, and the "not yet" lines of a dormant station's sonar
    /// answer (<see cref="Core.Events.StationNotYet"/>).
    /// </summary>
    public static class ProgressionKeys
    {
        public const string PlaceDock = "place.dock";
        public const string PlaceLander = "place.lander";
        public const string PlaceShelf = "place.shelf";
        public const string PlaceTower = "place.tower";
        public const string PlaceBay = "place.bay";
        public const string PlaceBoard = "place.board";
        public const string PlaceLadder = "place.ladder";
        public const string PlaceLift = "place.lift";
        public const string PlaceRack = "place.rack";

        /// <summary>Step 0: drive, look.</summary>
        public const string NextWake = "next.wake";

        /// <summary>Step 1: ping, cut the supply depot.</summary>
        public const string NextSalvage = "next.salvage";

        /// <summary>Step 2: feed and stitch the tower's first level.</summary>
        public const string NextTower = "next.tower";

        /// <summary>Step 3: follow the faint signal, stitch Tilly.</summary>
        public const string NextSignal = "next.signal";

        /// <summary>Step 4: dig, tow and place the first relic.</summary>
        public const string NextRelic = "next.relic";

        /// <summary>Step 5: power the bay (tower L2), park on the turntable, fit a kit.</summary>
        public const string NextBay = "next.bay";

        /// <summary>Step 6: leap the chasm, bring Bell home.</summary>
        public const string NextLeap = "next.leap";

        /// <summary>Step 7: restore a relay mast.</summary>
        public const string NextRelay = "next.relay";

        /// <summary>Step 8: power the crew's lift (tower L3).</summary>
        public const string NextLift = "next.lift";

        public const string NotYetBay = "notyet.bay";
        public const string NotYetLift = "notyet.lift";
        public const string NotYetShelf = "notyet.shelf";
    }
}
