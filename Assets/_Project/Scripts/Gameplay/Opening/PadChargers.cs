using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The charging pads 07 built (docs/design/opening-v2.md B5–B9): a built pad charges 07 stopped on it while the
    /// base powers it (a connected solar array or battery bank) or, before that, while it stands on the lander's port
    /// cable and the lander's reserve has charge left (<see cref="LanderPort"/>). Also reads the opening's power
    /// milestones from the base: Power on (a built solar array connected to a built pad) and night-ready (a built
    /// battery bank connected to it as well). Allocation-free.
    /// </summary>
    public sealed class PadChargers
    {
        private const float PoweredSupply = 0.01f;

        private readonly PlacedBase _base;
        private readonly LanderPort _port;
        private readonly Vector3 _cableEnd;
        private readonly OpeningTuning _tuning;

        /// <param name="cableEnd">Where the lander's port cable ends: the apron's centre.</param>
        public PadChargers(PlacedBase placed, LanderPort port, Vector3 cableEnd, OpeningTuning tuning)
        {
            _base = placed ?? throw new ArgumentNullException(nameof(placed));
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _cableEnd = cableEnd;
        }

        /// <summary>A built pad stands on the lander's port cable.</summary>
        public bool OnCable(PlacedStructure pad)
        {
            return SurfaceRules.HorizontalDistance(pad.Pose.Position, _cableEnd) <= _tuning.PortCableReach;
        }

        /// <summary>The base powers <paramref name="pad"/> (its supply from a connected source or store).</summary>
        public static bool Powered(PlacedStructure pad)
        {
            return pad.SupplyRatio > PoweredSupply;
        }

        /// <summary>
        /// The built pad under <paramref name="rover"/> that can charge it now, if any; <paramref name="fromReserve"/>
        /// is true when the lander's reserve feeds it (the base does not power it yet).
        /// </summary>
        public bool TryFindUnder(Vector3 rover, out Vector3 position, out bool fromReserve)
        {
            float radiusSq = _tuning.PadChargeRadius * _tuning.PadChargeRadius;
            for (int i = 0; i < _base.Count; i++)
            {
                PlacedStructure pad = _base.StructureAt(i);
                if (!pad.IsBuilt || pad.Definition.Id != StructureIds.ChargingPad ||
                    SurfaceRules.HorizontalDistanceSquared(rover, pad.Pose.Position) > radiusSq)
                {
                    continue;
                }

                bool powered = Powered(pad);
                if (powered || (OnCable(pad) && !_port.Spent))
                {
                    position = pad.Pose.Position;
                    fromReserve = !powered;
                    return true;
                }
            }

            position = Vector3.zero;
            fromReserve = false;
            return false;
        }

        /// <summary>A built solar array is connected to a built pad.</summary>
        public bool PowerOn => AnyConnectedToPad(StructureIds.SolarArray);

        /// <summary>Power is on and a built battery bank is connected to a built pad as well.</summary>
        public bool NightReady => PowerOn && AnyConnectedToPad(StructureIds.BatteryBank);

        /// <summary>At least one <paramref name="structureId"/> stands built.</summary>
        public bool Built(string structureId)
        {
            for (int i = 0; i < _base.Count; i++)
            {
                PlacedStructure structure = _base.StructureAt(i);
                if (structure.IsBuilt && structure.Definition.Id == structureId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool AnyConnectedToPad(string structureId)
        {
            for (int i = 0; i < _base.Count; i++)
            {
                PlacedStructure source = _base.StructureAt(i);
                if (!source.IsBuilt || source.Definition.Id != structureId)
                {
                    continue;
                }

                for (int j = 0; j < _base.Count; j++)
                {
                    PlacedStructure pad = _base.StructureAt(j);
                    if (pad.IsBuilt && pad.Definition.Id == StructureIds.ChargingPad &&
                        _base.PowerGrid.AreConnected(source.InstanceId, pad.InstanceId))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
