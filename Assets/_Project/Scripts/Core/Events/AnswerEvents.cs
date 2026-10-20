using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>
    /// The answer moved to <see cref="Phase"/> (<see cref="IAnswerStatus"/>). Ready: UI puts <c>TX · READY</c> on 07's
    /// face. Answered: relay.1 has answered. <see cref="Restored"/> is true for a phase found already reached at a
    /// session start (nothing plays for it).
    /// </summary>
    public readonly struct CallAnswerPhaseChanged
    {
        public CallAnswerPhaseChanged(AnswerPhase phase, bool restored)
        {
            Phase = phase;
            Restored = restored;
        }

        public AnswerPhase Phase { get; }

        public bool Restored { get; }
    }

    /// <summary>07 stepped up to the mast while it may transmit: UI shows <c>TX · READY</c> again, for a moment.</summary>
    public readonly struct TransmitPrompted
    {
    }

    /// <summary>
    /// A pulse of the old network runs from <see cref="From"/> to <see cref="To"/> over <see cref="Seconds"/>: first up
    /// the mast and out to relay.1 on the west rim (leg 0), then on from relay.1 to the Peak's dish (leg 1).
    /// </summary>
    public readonly struct NetworkPulseLaunched
    {
        public NetworkPulseLaunched(Vector3 from, Vector3 to, float seconds, int leg)
        {
            From = from;
            To = to;
            Seconds = seconds;
            Leg = leg;
        }

        public Vector3 From { get; }

        public Vector3 To { get; }

        public float Seconds { get; }

        public int Leg { get; }
    }

    /// <summary>Relay.1 answered 07's transmission: its lamp lights and keys the pattern back; the CRT reads the route.</summary>
    public readonly struct RelayAcked
    {
        public RelayAcked(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Position { get; }
    }

    /// <summary>The pulse reached the Peak's dark dish and glinted on it.</summary>
    public readonly struct PeakDishGlinted
    {
        public PeakDishGlinted(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Position { get; }
    }
}
