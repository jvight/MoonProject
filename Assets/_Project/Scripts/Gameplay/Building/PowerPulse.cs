using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The power-flow pulse on the base cables (base-home-and-crater-exits §3): "the solar array feeds my charger" read
    /// with no text. While the Sun is up a pale gold glow runs inside the solar array's cable, lighting the rubber lead
    /// from within (no floating light), from its port to the hub it feeds; from dusk an amber pulse runs from the battery bank instead (it keeps the base lit through the night),
    /// and the array's stops. Every other cable stays still. Each pulse that reaches the far end raises <see cref="PowerPulseArrived"/>
    /// (Audio's soft tick). Day and dusk follow the base lamps' own dusk, so the lamps and the pulse turn together.
    /// Allocation-free per frame once the base stands.
    /// </summary>
    public sealed class PowerPulse
    {
        private readonly EventBus _events;
        private readonly BaseTuning _tuning;
        private readonly BuildField _building;
        private readonly BaseLamps _lamps;
        private readonly List<PowerPulseTrain> _trains = new List<PowerPulseTrain>();
        private int _version = -1;

        public PowerPulse(EventBus events, BaseTuning tuning, BuildField building, BaseLamps lamps)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _building = building != null ? building : throw new ArgumentNullException(nameof(building));
            _lamps = lamps ?? throw new ArgumentNullException(nameof(lamps));
        }

        /// <summary>Pulses on the cables now (tests and debugging views).</summary>
        public int InFlight { get; private set; }

        /// <summary>Seconds a pulse takes along its cable.</summary>
        public float TravelSeconds => _tuning.PulseTravelSeconds;

        /// <summary>Seconds from one pulse leaving a cable to the next.</summary>
        public float IntervalSeconds => _tuning.PulseIntervalSeconds;

        /// <summary>
        /// True when power flows from the piece <paramref name="structureId"/> along its cable: the array's while it
        /// is not dusk, the bank's from dusk.
        /// </summary>
        public static bool Flows(string structureId, bool dusk)
        {
            if (string.Equals(structureId, StructureIds.SolarArray, StringComparison.Ordinal))
            {
                return !dusk;
            }

            return dusk && string.Equals(structureId, StructureIds.BatteryBank, StringComparison.Ordinal);
        }

        public void Step(float deltaTime)
        {
            BaseCables cables = _building.Cables;
            if (cables.Version != _version)
            {
                _version = cables.Version;
                while (_trains.Count < cables.Count)
                {
                    _trains.Add(new PowerPulseTrain());
                }

                for (int i = 0; i < _trains.Count; i++)
                {
                    _trains[i].Reset();
                }
            }

            bool dusk = _lamps.Dusk;
            InFlight = 0;
            for (int i = 0; i < cables.Count; i++)
            {
                string piece = cables.PieceOf(i);
                bool flowing = Flows(piece, dusk);
                PowerPulseTrain train = _trains[i];
                if (train.Step(deltaTime, flowing, _tuning.PulseIntervalSeconds, _tuning.PulseTravelSeconds))
                {
                    _events.Publish(new PowerPulseArrived(piece, cables.Along(i, 1f)));
                }

                if (!train.InFlight)
                {
                    cables.SetPulse(i, 0f, _tuning.PulseWidthMetres, Color.black);
                    continue;
                }

                InFlight++;
                bool sun = string.Equals(piece, StructureIds.SolarArray, StringComparison.Ordinal);
                Color color = (sun ? _tuning.SunPulseColor : _tuning.BankPulseColor) * train.Glow;
                cables.SetPulse(i, train.Along, _tuning.PulseWidthMetres, color);
            }
        }
    }
}
