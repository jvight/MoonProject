using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>Something undiscovered Bell could point at, as she sees it right now.</summary>
    public readonly struct SignalCandidate
    {
        public SignalCandidate(BellSignalTarget kind, string id, Vector3 position, bool waiting, bool reachable)
        {
            Kind = kind;
            Id = id;
            Position = position;
            Waiting = waiting;
            Reachable = reachable;
        }

        public BellSignalTarget Kind { get; }

        /// <summary>Stable id within its kind (cassette, log or relic id): how a saved signal finds it again.</summary>
        public string Id { get; }

        /// <summary>Where it is (on the surface).</summary>
        public Vector3 Position { get; }

        /// <summary>Not found yet: a cassette not collected, a cache not opened, a relic not answered yet.</summary>
        public bool Waiting { get; }

        /// <summary>07's abilities can get there (Bell never points behind a gate).</summary>
        public bool Reachable { get; }
    }
}
