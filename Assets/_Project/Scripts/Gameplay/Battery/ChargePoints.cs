using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Every place that can recharge 07 now, for the range ring: the built pads and the lit relays (opening v3 R10:
    /// relay.1 once its feed unit is fitted). Allocation-free per call.
    /// </summary>
    public sealed class ChargePoints : IChargePointSource
    {
        private readonly IChargePointSource _pads;
        private readonly IChargePointSource _relays;

        public ChargePoints(IChargePointSource pads, IChargePointSource relays)
        {
            _pads = pads ?? throw new ArgumentNullException(nameof(pads));
            _relays = relays ?? throw new ArgumentNullException(nameof(relays));
        }

        public void CopyChargePoints(List<Vector3> points)
        {
            _pads.CopyChargePoints(points);
            _relays.CopyChargePoints(points);
        }
    }
}
