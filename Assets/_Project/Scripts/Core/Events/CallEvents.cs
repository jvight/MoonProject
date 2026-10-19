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
}
