using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// The crew's lab as 07 sees it (docs/features/M3-19 §5), the <see cref="ILabInterior"/> the belly lift registers:
    /// inside or not, the station the turntable faces or turns to, and whether 07 looks at it. Each change is
    /// announced once (<see cref="LabEntered"/>, <see cref="LabStationFaced"/>, <see cref="LabStationLooked"/>,
    /// <see cref="LabLeft"/>); nothing allocates.
    /// </summary>
    public sealed class LabInterior : ILabInterior
    {
        /// <summary>The stations round the turntable.</summary>
        public const int StationCount = 4;

        private readonly EventBus _events;

        public LabInterior(EventBus events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public bool IsInside { get; private set; }

        public LabStation Facing { get; private set; }

        /// <summary>07 looks at the station it faces (the camera eased in; at the chart, the map is open).</summary>
        public bool IsLooking { get; private set; }

        /// <summary>The next station clockwise (<paramref name="direction"/> 1) or counter-clockwise (-1).</summary>
        public static LabStation Next(LabStation station, int direction)
        {
            int next = ((int)station + direction) % StationCount;
            return (LabStation)(next < 0 ? next + StationCount : next);
        }

        /// <summary>The platform settled in the lab facing <paramref name="facing"/>.</summary>
        public void Enter(LabStation facing)
        {
            IsInside = true;
            IsLooking = false;
            Facing = facing;
            _events.Publish(new LabEntered(facing));
        }

        /// <summary>The turntable starts toward <paramref name="station"/>; a look ends first.</summary>
        public void TurnTo(LabStation station)
        {
            Look(false);
            Facing = station;
        }

        /// <summary>The turntable came to rest facing <see cref="Facing"/>.</summary>
        public void Faced()
        {
            _events.Publish(new LabStationFaced(Facing));
        }

        public void Look(bool looking)
        {
            if (!IsInside || IsLooking == looking)
            {
                return;
            }

            IsLooking = looking;
            _events.Publish(new LabStationLooked(Facing, looking));
        }

        /// <summary>The platform starts down: a look ends, then 07 has left.</summary>
        public void Leave()
        {
            if (!IsInside)
            {
                return;
            }

            Look(false);
            IsInside = false;
            _events.Publish(new LabLeft());
        }
    }
}
