using UnityEngine;
namespace MoonProject.Core.Events
{
    /// <summary>The call moved to <see cref="State"/>; <see cref="Restored"/> is true when replayed from a save.</summary>
    public readonly struct CallStateChanged
    {
        public CallStateChanged(CallState state, bool restored)
        {
            State = state;
            Restored = restored;
        }

        /// <summary>The call's new state.</summary>
        public CallState State { get; }

        /// <summary>True when replayed at session start from a loaded save rather than reached just now.</summary>
        public bool Restored { get; }
    }

    /// <summary>
    /// More of the carrier's identity resolved on 07's face (opening-v3-purpose.md §2: `A?? ??` after the first
    /// charge, more at the answer, then one per dock until it reads in full).
    /// </summary>
    public readonly struct CallDecoded
    {
        public CallDecoded(int charactersDecoded, int charactersTotal, bool restored)
        {
            CharactersDecoded = charactersDecoded;
            CharactersTotal = charactersTotal;
            Restored = restored;
        }

        /// <summary>How many characters of the carrier's identity 07 has resolved so far.</summary>
        public int CharactersDecoded { get; }

        /// <summary>How many characters the identity has in full.</summary>
        public int CharactersTotal { get; }

        /// <summary>True when replayed at session start from a loaded save rather than decoded just now.</summary>
        public bool Restored { get; }
    }

    /// <summary>
    /// 07 tried to answer the call from the sky and could not (docs/design/opening-v3-purpose.md R7): at night, with the
    /// base night-ready and the call clear, once in a game. Rover lowers its head and drops the antenna lamp, Audio
    /// plays the failed query chirp (F sharp), UI shows <c>TX · NO RANGE</c> on 07's face. Published live only: a
    /// loaded game that already tried says nothing.
    /// </summary>
    public readonly struct CallReplyFailed
    {
    }

    /// <summary>
    /// A segment of the antenna mast locked home as it was raised (docs/design/opening-v3-plan.md 3.3): base, middle,
    /// top in order. Published live only, at the mast's <see cref="Position"/>, for Audio's clunks.
    /// </summary>
    public readonly struct MastSegmentRaised
    {
        public MastSegmentRaised(int index, int count, Vector3 position, bool restored)
        {
            Index = index;
            Count = count;
            Position = position;
            Restored = restored;
        }

        /// <summary>The segment that locked, 0 (base) up to <see cref="Count"/> - 1 (top).</summary>
        public int Index { get; }

        /// <summary>Segments in the mast.</summary>
        public int Count { get; }

        public Vector3 Position { get; }

        public bool Restored { get; }

        /// <summary>The top segment locked: the mast stands raised.</summary>
        public bool IsLast => Index == Count - 1;
    }
}
