using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>
    /// The network's handshake reached a node (north-star §4a beat 7): when a mast lights, World's network pulse runs
    /// lamp to lamp out from it, and publishes this as each lamp flashes, starting with the new mast (hop 0). Audio
    /// can step up the pentatonic with each hop; home lights its cables when the pulse reaches it.
    /// </summary>
    public readonly struct NetworkPulseReached
    {
        public NetworkPulseReached(string nodeId, Vector3 position, int hop, int hops)
        {
            NodeId = nodeId;
            Position = position;
            Hop = hop;
            Hops = hops;
        }

        /// <summary>The node's id: "home" for the radio tower, otherwise the mast's ("relay.0", ...).</summary>
        public string NodeId { get; }

        /// <summary>World position of the node's lamp, where it flashes.</summary>
        public Vector3 Position { get; }

        /// <summary>Hops from the newly lit mast: 0 is the mast itself.</summary>
        public int Hop { get; }

        /// <summary>Hops to the farthest node this handshake reaches.</summary>
        public int Hops { get; }
    }

    /// <summary>
    /// Phase of the lunar sky cycle (0.5.7 SKY-CYCLE): the opening eclipse (or any solar eclipse behind Earth),
    /// sunrise as the Sun breaks out or climbs past the horizon, the low-sun day, sunset as it sinks, and the
    /// earthlit night while the Sun is below the horizon.
    /// </summary>
    public enum SkyPhase
    {
        /// <summary>The Sun is hidden (or partially hidden) behind Earth's disc.</summary>
        Eclipse = 0,

        /// <summary>The Sun has just cleared the opening eclipse or is rising across the horizon band.</summary>
        Sunrise = 1,

        /// <summary>The low Sun is above the horizon band and clear of Earth (look C "Low sun").</summary>
        Sunlit = 2,

        /// <summary>The low Sun is sinking toward the horizon band.</summary>
        Sunset = 3,

        /// <summary>The Sun is below the horizon; only cool earthshine lights the basin (look A "Moon grey").</summary>
        Earthlit = 4,
    }

    /// <summary>
    /// Published by World's sky cycle only when <see cref="Phase"/> changes. Consumers are audio, the awe beats and
    /// the power grid.
    /// </summary>
    public readonly struct SkyPhaseChanged
    {
        public SkyPhaseChanged(SkyPhase phase, float eclipseFactor)
        {
            Phase = phase;
            EclipseFactor = Mathf.Clamp01(eclipseFactor);
        }

        /// <summary>The new phase of the lunar sky.</summary>
        public SkyPhase Phase { get; }

        /// <summary>Fraction of the Sun's disc hidden by Earth's disc at this moment, 0..1.</summary>
        public float EclipseFactor { get; }
    }
}

