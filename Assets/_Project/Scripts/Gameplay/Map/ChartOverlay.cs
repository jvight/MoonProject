using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// <see cref="IChartOverlay"/> over the station's reach, the Link's chain and 07's battery (docs/features/M3-21
    /// §2): the dock and each lit mast are chargers; a charger's circle is the share of a full battery's flat-ground
    /// drive that takes 07 out and back with a margin (GDD v3 §3.7: about 140 m on four cells).
    /// </summary>
    public sealed class ChartOverlay : IChartOverlay
    {
        private readonly StationReach _reach;
        private readonly RelayField _relays;
        private readonly ChargingDock _dock;
        private readonly IRoverBattery _battery;
        private readonly float _metresPerCell;
        private readonly float _rangeShare;

        /// <param name="metresPerCell">Metres one cell drives on flat ground.</param>
        /// <param name="rangeShare">Share of a full battery's drive drawn as a charger's circle.</param>
        public ChartOverlay(StationReach reach, RelayField relays, ChargingDock dock, IRoverBattery battery,
            float metresPerCell, float rangeShare, Vector3 peak)
        {
            _reach = reach ?? throw new ArgumentNullException(nameof(reach));
            _relays = relays != null ? relays : throw new ArgumentNullException(nameof(relays));
            _dock = dock != null ? dock : throw new ArgumentNullException(nameof(dock));
            _battery = battery ?? throw new ArgumentNullException(nameof(battery));
            _metresPerCell = metresPerCell;
            _rangeShare = rangeShare;
            Peak = peak;
        }

        public int ChargerCount
        {
            get
            {
                int count = 1;
                for (int node = 0; node < _reach.NodeCount; node++)
                {
                    count += node != StationReach.Home && _reach.IsLit(node) ? 1 : 0;
                }

                return count;
            }
        }

        public float ChargerRadius => _battery.Cells * _metresPerCell * _rangeShare;

        public int LinkPointCount
        {
            get
            {
                int count = 1;
                for (int i = 0; i < _relays.ChainMastCount; i++)
                {
                    count += _relays.IsChainMastLit(i) ? 1 : 0;
                }

                return count;
            }
        }

        public Vector3 Peak { get; }

        public Vector3 ChargerPosition(int index)
        {
            if (index == 0)
            {
                return _dock.Position;
            }

            for (int node = 0; node < _reach.NodeCount; node++)
            {
                if (node != StationReach.Home && _reach.IsLit(node) && --index == 0)
                {
                    return _reach.Position(node);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public Vector3 LinkPoint(int index)
        {
            if (index == 0)
            {
                return _reach.Position(StationReach.Home);
            }

            for (int i = 0; i < _relays.ChainMastCount; i++)
            {
                if (_relays.IsChainMastLit(i) && --index == 0)
                {
                    return _relays.ChainMastPosition(i);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
