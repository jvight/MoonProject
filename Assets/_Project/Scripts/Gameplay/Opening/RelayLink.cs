using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// relay.1 as a node of 07's network (opening-v3-payoff-audit §2): once it has answered the call it shows linked to
    /// the antenna mast, the hub, needing a part; fitted and lit, it is a charge point and the link is whole.
    /// </summary>
    public sealed class RelayLink : IRingLink
    {
        private readonly IAnswerStatus _answer;
        private readonly RelayRepair _relay;

        public RelayLink(IAnswerStatus answer, RelayRepair relay)
        {
            _answer = answer ?? throw new ArgumentNullException(nameof(answer));
            _relay = relay ?? throw new ArgumentNullException(nameof(relay));
        }

        public bool TryGetLink(out Vector3 hub, out Vector3 node, out bool needsPart)
        {
            hub = _answer.MastPoint;
            node = _relay.PadPoint;
            needsPart = !_relay.Lit;
            return _relay.Open;
        }
    }
}
