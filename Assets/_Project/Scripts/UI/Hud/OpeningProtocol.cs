using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>The protocol line 07's readout shows (docs/design/opening-v2.md §2 B1, B8, B11).</summary>
    internal enum ProtocolLine
    {
        /// <summary>Before the base has power: <c>POWER · OFFLINE</c>.</summary>
        PowerOffline = 0,

        /// <summary>A built solar array powers the pad: <c>POWER · ON</c>.</summary>
        PowerOn = 1,

        /// <summary>The antenna mast heard the west signal: <c>LINK · 0/1 · SIGNAL WEST</c>.</summary>
        LinkSignalWest = 2,
    }

    /// <summary>
    /// Which protocol line stands, kept from the opening's milestones (pure, EditMode-tested). Replayed milestones set
    /// the state; the caller animates only a milestone reached now.
    /// </summary>
    internal sealed class OpeningProtocol
    {
        private bool _powerOn;
        private bool _signalWest;

        /// <summary>The line the readout shows now.</summary>
        public ProtocolLine Line => _signalWest ? ProtocolLine.LinkSignalWest
            : _powerOn ? ProtocolLine.PowerOn : ProtocolLine.PowerOffline;

        /// <summary>True from the moment the base has power on (the Sun's arc is of use from then).</summary>
        public bool IsPowerOn => _powerOn || _signalWest;

        /// <summary>Takes <paramref name="milestone"/> in; true when it changed <see cref="Line"/>.</summary>
        public bool Reach(OpeningMilestone milestone)
        {
            ProtocolLine before = Line;
            if (milestone == OpeningMilestone.PowerOn)
            {
                _powerOn = true;
            }
            else if (milestone == OpeningMilestone.SignalWest)
            {
                _signalWest = true;
            }

            return Line != before;
        }

        /// <summary>"ui.readout.power_on": the words of <paramref name="line"/>.</summary>
        public static string KeyOf(ProtocolLine line)
        {
            switch (line)
            {
                case ProtocolLine.PowerOn:
                    return "ui.readout.power_on";
                case ProtocolLine.LinkSignalWest:
                    return "ui.readout.link_west";
                default:
                    return "ui.readout.power_off";
            }
        }
    }
}
