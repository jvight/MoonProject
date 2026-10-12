namespace MoonProject.Audio
{
    /// <summary>What the radio may play yet: music is earned (docs/features/M3-24 "Owner decisions" 3).</summary>
    public enum RadioGateStage
    {
        /// <summary>A fresh game: static and the room tone only (the carrier once tower level 1 clears it).</summary>
        Static = 0,

        /// <summary>07's tape deck is fitted: the first tape, softly, on repeat.</summary>
        Deck = 1,

        /// <summary>Bell's dial is unlocked: the lofi station with its tracks and the dial's channels.</summary>
        Station = 2,
    }

    /// <summary>
    /// The radio's music gate (pure, EditMode-tested). The tape deck on 07 earns the first tape; Bell's dial earns
    /// the station, whether or not the deck came first; tower level 1 only turns the static into the Earth signal's
    /// carrier, never music. Every step only opens: nothing closes the gate again.
    /// </summary>
    public sealed class RadioGate
    {
        public bool DeckFitted { get; private set; }

        public bool DialUnlocked { get; private set; }

        /// <summary>True once tower level 1 has cleared the static into the carrier.</summary>
        public bool CarrierClear { get; private set; }

        public RadioGateStage Stage =>
            DialUnlocked ? RadioGateStage.Station : DeckFitted ? RadioGateStage.Deck : RadioGateStage.Static;

        /// <summary>True when only the carrier (no music) fills the set.</summary>
        public bool CarrierHeard => CarrierClear && Stage == RadioGateStage.Static;

        /// <summary>Fits the tape deck; returns true when that changed <see cref="Stage"/>.</summary>
        public bool FitDeck()
        {
            RadioGateStage before = Stage;
            DeckFitted = true;
            return Stage != before;
        }

        /// <summary>Follows Bell's dial; returns true when that changed <see cref="Stage"/>.</summary>
        public bool SetDialUnlocked(bool unlocked)
        {
            RadioGateStage before = Stage;
            DialUnlocked |= unlocked;
            return Stage != before;
        }

        /// <summary>Tower level 1 is lit; returns true the first time.</summary>
        public bool ClearCarrier()
        {
            bool changed = !CarrierClear;
            CarrierClear = true;
            return changed;
        }
    }
}
