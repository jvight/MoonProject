namespace MoonProject.Core
{
    /// <summary>Physics layer indices. Must match ProjectSettings/TagManager.asset (see docs/ARCHITECTURE.md).</summary>
    public static class Layers
    {
        public const int Ground = 6;
        public const int Rover = 7;
        public const int Relic = 8;
        public const int Pickup = 9;
        public const int Prop = 10;
        public const int Trigger = 11;

        public const int GroundMask = 1 << Ground;
        public const int RoverMask = 1 << Rover;
        public const int RelicMask = 1 << Relic;
        public const int PickupMask = 1 << Pickup;
        public const int PropMask = 1 << Prop;
        public const int TriggerMask = 1 << Trigger;

        /// <summary>Everything a wheel or the rover can stand on.</summary>
        public const int DriveableMask = GroundMask | PropMask;
    }
}
