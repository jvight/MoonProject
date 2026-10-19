using System;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// Rover's view of 07's battery (<see cref="IRoverBattery"/>, docs/features/M3-20). Gameplay registers the battery
    /// after Rover initialises, so it is resolved once, at first use. A scene without Gameplay (Rover's own test
    /// scenes) has no battery at all: there 07 drives, boosts and leaps as on a full battery.
    /// </summary>
    public sealed class BatteryLink
    {
        private readonly GameContext _context;
        private IRoverBattery _battery;
        private bool _resolved;

        public BatteryLink(GameContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>The battery, or null in a scene without Gameplay.</summary>
        public IRoverBattery Battery
        {
            get
            {
                if (!_resolved)
                {
                    _resolved = true;
                    _context.TryGet(out _battery);
                }

                return _battery;
            }
        }

        /// <summary>Share of the normal top speed allowed now (the crawl's cap; 1 without a battery).</summary>
        public float DriveCap01 => Battery != null ? Battery.DriveCap01 : 1f;

        /// <summary>The opening still runs on the reserve: the lamps are dimmed.</summary>
        public bool OnOpeningReserve => Battery != null && Battery.OnOpeningReserve;

        /// <summary>The battery can pay for a full leap now.</summary>
        public bool CanLeap => Battery == null || Battery.CanLeap;

        /// <summary>07 crawls on the reserve: the boost and the leap sleep.</summary>
        public bool IsLowPower => Battery != null && Battery.IsLowPower;

        /// <summary>Charge over capacity, 0..1 (1 without a battery).</summary>
        public float Charge01 => Battery != null ? Battery.Charge01 : 1f;

        /// <summary>The capacitor drums' charge, 0..1 (full without a battery).</summary>
        public float Drums01 => Battery != null ? Battery.Drums01 : 1f;
    }
}
