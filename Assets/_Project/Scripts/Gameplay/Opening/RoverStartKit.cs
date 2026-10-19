using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's start kit (docs/design/opening-v2.md §10): drive with lamps, scan and build work from the wake; the cutter
    /// is FAULT (no igniter) until the spare igniter from the lander's open crate is fitted while 07 stands on the pad
    /// it built (B6); the solar wing stays JAMMED (no stand-still trickle), the antenna RECEIVE ONLY and sockets 1-2
    /// EMPTY through the opening. Saved as "gameplay.startkit": whether the igniter is fitted, and whether 07 carries
    /// it. Plain state; allocation-free.
    /// </summary>
    public sealed class RoverStartKit : IRoverSystems
    {
        private static readonly RoverSystem[] Systems =
        {
            RoverSystem.Wheels, RoverSystem.Lamps, RoverSystem.Scan, RoverSystem.Assembly, RoverSystem.Cutter,
            RoverSystem.SolarWing, RoverSystem.Antenna, RoverSystem.Sockets,
        };

        private readonly EventBus _events;
        private readonly CargoHold _hold;
        private readonly Func<bool> _docked;
        private readonly string _igniter;
        private readonly bool _fieldFit;
        private bool _carriedOnLoad;

        /// <param name="docked">07 stands on a pad it built (the cutter socket is reached there, B6).</param>
        /// <param name="igniter">The spare igniter's story-part id in 07's hold.</param>
        /// <param name="fieldFit">The igniter is a small field swap: it fits anywhere once it is in the hold.</param>
        public RoverStartKit(EventBus events, CargoHold hold, Func<bool> docked, string igniter, bool fieldFit)
        {
            _fieldFit = fieldFit;
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _hold = hold ?? throw new ArgumentNullException(nameof(hold));
            _docked = docked ?? throw new ArgumentNullException(nameof(docked));
            _igniter = !string.IsNullOrEmpty(igniter)
                ? igniter
                : throw new ArgumentException("The spare igniter needs its part id.", nameof(igniter));
        }

        /// <summary>The cutter has its igniter: it cuts.</summary>
        public bool IgniterFitted { get; private set; }

        public string IgniterPartId => _igniter;

        /// <summary>The spare igniter is in the hold.</summary>
        public bool CarriesIgniter => _hold.HasPart(_igniter);

        /// <summary>07 stands on a pad it built.</summary>
        public bool OnPad => _docked();

        public bool CanFitIgniter => !IgniterFitted && CarriesIgniter && (_fieldFit || _docked());

        public RoverSystemState StateOf(RoverSystem system)
        {
            switch (system)
            {
                case RoverSystem.Cutter:
                    return IgniterFitted ? RoverSystemState.Ok : RoverSystemState.Fault;
                case RoverSystem.SolarWing:
                    return RoverSystemState.Jammed;
                case RoverSystem.Antenna:
                    return RoverSystemState.ReceiveOnly;
                case RoverSystem.Sockets:
                    return RoverSystemState.Empty;
                default:
                    return RoverSystemState.Ok;
            }
        }

        public bool TryFitIgniter()
        {
            if (!CanFitIgniter || !_hold.RemovePart(_igniter))
            {
                return false;
            }

            IgniterFitted = true;
            _events.Publish(new RoverSystemChanged(RoverSystem.Cutter, RoverSystemState.Ok, false));
            return true;
        }

        /// <summary>
        /// The session starts (after every save section is applied): a carried igniter is laid back into the hold, and
        /// every line of the readout is stated once (Restored).
        /// </summary>
        public void Announce()
        {
            if (_carriedOnLoad && !IgniterFitted)
            {
                _hold.TryAddPart(_igniter);
            }

            _carriedOnLoad = false;
            for (int i = 0; i < Systems.Length; i++)
            {
                _events.Publish(new RoverSystemChanged(Systems[i], StateOf(Systems[i]), true));
            }
        }

        public StartKitSaveData Capture()
        {
            return new StartKitSaveData { igniterFitted = IgniterFitted, igniterCarried = _hold.HasPart(_igniter) };
        }

        public void Restore(StartKitSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            IgniterFitted = data.igniterFitted;
            _carriedOnLoad = data.igniterCarried;
        }
    }
}
