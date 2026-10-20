using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>Where the answer to the call from the sky stands (docs/design/opening-v3-purpose.md R8).</summary>
    public enum AnswerPhase
    {
        /// <summary>Not yet possible: the mast, the night or the failed reply has not come.</summary>
        NotReady = 0,

        /// <summary>07 can answer: hold at the mast to transmit.</summary>
        Ready = 1,

        /// <summary>07 transmitted: the network pulse runs from the mast out to relay.1.</summary>
        Transmitting = 2,

        /// <summary>Relay.1 answered and the route is read out; the pulse runs on to the Peak's dish.</summary>
        Answered = 3,

        /// <summary>The answer is over (or was made in an earlier session).</summary>
        Done = 4,
    }

    /// <summary>
    /// The answer to the call (plan slice 4.1), registered in the <see cref="GameContext"/> by Gameplay: whether 07 may
    /// transmit, how far its hold at the mast has come (the ring UI draws) and where the mast stands.
    /// </summary>
    public interface IAnswerStatus
    {
        /// <summary>Where the answer stands.</summary>
        AnswerPhase Phase { get; }

        /// <summary>How far 07's hold to transmit has come, 0..1 (0 when it is not holding).</summary>
        float HoldProgress01 { get; }

        /// <summary>07 is at the mast, still, and may hold to transmit now.</summary>
        bool AtTheMast { get; }

        /// <summary>The antenna mast's foot, where the ring is drawn; meaningful once the mast has stood.</summary>
        Vector3 MastPoint { get; }
    }
}
