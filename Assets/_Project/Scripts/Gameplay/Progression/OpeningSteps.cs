namespace MoonProject.Gameplay
{
    /// <summary>
    /// The opening flow's steps by position (<see cref="OpeningFlow.CurrentIndex"/> is the first step not yet done):
    /// what a bot or a test reads to know where the game stands without reading any line of text. The order is the
    /// order <see cref="OpeningFlow"/> adds them in; a test keeps the two together.
    /// </summary>
    public static class OpeningSteps
    {
        public const int DriveToLander = 0;
        public const int ScanLander = 1;
        public const int CollectParts = 2;
        public const int BuildPad = 3;
        public const int Charge = 4;
        public const int FindSolar = 5;

        /// <summary>B7, the spare igniter: the cut line, the rack line once the cutter sputters, the fit line.</summary>
        public const int FitIgniter = 6;
        public const int BuildSolar = 7;
        public const int BuildBatteryBank = 8;
        public const int ScanTower = 9;
        public const int BuildMast = 10;

        /// <summary>R8: transmit from the mast until relay.1 answers.</summary>
        public const int Transmit = 11;

        /// <summary>R9 to R10: drive to relay.1 until its spare is fitted and it is lit.</summary>
        public const int GoToRelay = 12;

        /// <summary>5.2: drive to 06 at relay.2 and scan it, until Kessler–Arai understanding unlocks the headlamp.</summary>
        public const int ScanElder06 = 13;

        /// <summary>5.2: assemble the SSR-3 headlamp at the rack until its bay is filled.</summary>
        public const int AssembleHeadlamp = 14;

        /// <summary>The number of steps; the index once the opening is over.</summary>
        public const int Count = 15;
    }
}
