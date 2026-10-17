using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>
    /// The number of full cells or the capacity changed (docs/features/M3-20): published by Gameplay on cell boundaries
    /// only, never per frame. Rover steps the flank lamps, Audio plays a rising cell note while charging (D E F# A B …
    /// by <see cref="FullCells"/>), UI steps the HUD arc. Restored is true once after a save is applied: show the state
    /// at once, with no sound.
    /// </summary>
    public readonly struct BatteryChanged
    {
        public BatteryChanged(int fullCells, int cells, bool rising, bool restored)
        {
            FullCells = fullCells;
            Cells = cells;
            Rising = rising;
            Restored = restored;
        }

        /// <summary>Whole cells of charge, 0..<see cref="Cells"/>.</summary>
        public int FullCells { get; }

        /// <summary>Capacity in cells (it grows with Cell Pack II).</summary>
        public int Cells { get; }

        /// <summary>True when a charger, the solar trickle or a new cell pack raised the count.</summary>
        public bool Rising { get; }

        public bool Restored { get; }
    }

    /// <summary>
    /// 07 dropped into the low-power crawl (<see cref="LowPower"/> true) or left it at a charger (false), see
    /// <see cref="IRoverBattery.IsLowPower"/>. Rover eases into the calm crawl (wider camera, dimmed lamp, the eye and
    /// wing low), Audio plays the power-down sigh and thins the radio, Gameplay sends Tilly out to lead once she is
    /// home.
    /// </summary>
    public readonly struct LowPowerChanged
    {
        public LowPowerChanged(bool lowPower, bool restored)
        {
            LowPower = lowPower;
            Restored = restored;
        }

        public bool LowPower { get; }

        public bool Restored { get; }
    }

    /// <summary>
    /// The landed lander's old cells changed (opening-v2 B6): <see cref="Reserve01"/> of the one charge they held is
    /// left, drawn through a built pad on the lander's port cable. The CRT reads <c>LANDER RESERVE · n%</c>; at 0 the
    /// base needs its own power. <see cref="Restored"/> is true when a loaded game states it at session start.
    /// </summary>
    public readonly struct LanderReserveChanged
    {
        public LanderReserveChanged(float reserve01, bool restored)
        {
            Reserve01 = reserve01;
            Restored = restored;
        }

        public float Reserve01 { get; }

        public bool Restored { get; }
    }

    /// <summary>
    /// A charger started filling 07's cells (<see cref="Charging"/> true) or stopped (false: full, or 07 drove off).
    /// Rover perks 07 up when it starts (head lift, brighter eye, an antenna wiggle); World warms the charger's pool.
    /// </summary>
    public readonly struct ChargingChanged
    {
        public ChargingChanged(bool charging, ChargerKind charger, Vector3 position)
        {
            Charging = charging;
            Charger = charger;
            Position = position;
        }

        public bool Charging { get; }

        public ChargerKind Charger { get; }

        /// <summary>The charger's pad centre.</summary>
        public Vector3 Position { get; }
    }

    /// <summary>
    /// The charge fell past a warning threshold on this discharge (docs/features/M3-20 "Warnings"); each fires at most
    /// once until 07 charges again. Audio plays the falling two-note on <see cref="BatteryWarning.Low"/>; Rover dims the
    /// eye and, on <see cref="BatteryWarning.VeryLow"/>, glances toward <see cref="NearestLitCharger"/>.
    /// </summary>
    public readonly struct BatteryWarned
    {
        public BatteryWarned(BatteryWarning warning, Vector3 nearestLitCharger)
        {
            Warning = warning;
            NearestLitCharger = nearestLitCharger;
        }

        public BatteryWarning Warning { get; }

        /// <summary>The pad of the nearest charger that works now: the dock or a lit mast.</summary>
        public Vector3 NearestLitCharger { get; }
    }

    /// <summary>
    /// The charge dropped below the reveal share for the first time in this game (docs/features/M3-20 "How 07 shows
    /// it"): UI's HUD arc around the compass exists from now on (it still hides while the cells are full and while 07
    /// works the beam). Published by Gameplay once, and once more at start with <see cref="Restored"/> true when a
    /// loaded game had already revealed it.
    /// </summary>
    public readonly struct BatteryArcRevealed
    {
        public BatteryArcRevealed(bool restored)
        {
            Restored = restored;
        }

        public bool Restored { get; }
    }

    /// <summary>
    /// One of 07's flank lamps lit as it wakes in the opening (docs/features/M3-20 "How 07 shows it": after O5's
    /// flicker the cells' lamps light one by one). Published by Rover; Audio plays a quiet tick at
    /// <see cref="Position"/>.
    /// </summary>
    public readonly struct FlankLampLit
    {
        public FlankLampLit(int index, Vector3 position)
        {
            Index = index;
            Position = position;
        }

        /// <summary>The lamp, 0 for the front one.</summary>
        public int Index { get; }

        public Vector3 Position { get; }
    }
}
