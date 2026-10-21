using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The key chip that belongs to a reminder line (pure, EditMode-tested; docs/design/opening-v3-guidance-audit.md
    /// G2). It never stands beside a line: the line draws its own key's glyph when the key is new, and the chip takes
    /// the line's place (the guidance slot's cross-fade) only once the step's object is in reach, so the one element on
    /// screen is the thing to press now (the next of two fragments to scan, standing or moving). It never names a key
    /// that does something else right now: while E is held it names what E is doing, and with an E target in reach it
    /// names what E would do there (a crate seal reads Cut even on a scan step).
    /// </summary>
    internal sealed class ObjectiveChip
    {
        private ObjectiveVerb _verb;

        /// <summary>A new step began: its verb is the one the chip may name.</summary>
        public void SetKey(string objectiveKey)
        {
            _verb = ObjectiveVerbs.Of(objectiveKey);
        }

        /// <summary>The word on the chip now (None while it shows nothing).</summary>
        /// <param name="reminderUp">A reminder line wants the screen (a next line carries its own key).</param>
        /// <param name="offered">The gameplay's ready action now (None when nothing is in reach and ready).</param>
        /// <param name="held">What a held E is doing now (None while E is not at work): it outranks the offer.</param>
        public HintWord Word(bool reminderUp, InteractionKind offered, InteractionKind held)
        {
            if (!reminderUp)
            {
                return HintWord.None;
            }

            HintWord excavate = ExcavateWord(held != InteractionKind.None ? held : offered);
            if (excavate != HintWord.None)
            {
                return excavate;
            }

            return offered != InteractionKind.None && offered == KindOf(_verb) ? VerbWord(_verb) : HintWord.None;
        }

        private static HintWord VerbWord(ObjectiveVerb verb)
        {
            switch (verb)
            {
                case ObjectiveVerb.Scan:
                    return HintWord.Scan;
                case ObjectiveVerb.Cut:
                    return HintWord.Cut;
                case ObjectiveVerb.Build:
                    return HintWord.Build;
                default:
                    return HintWord.None;
            }
        }

        /// <summary>The word of what E does on <paramref name="kind"/> (None when E does not do it).</summary>
        private static HintWord ExcavateWord(InteractionKind kind)
        {
            switch (kind)
            {
                case InteractionKind.Salvage:
                    return HintWord.Cut;
                case InteractionKind.Scan:
                    return HintWord.Scan;
                default:
                    return HintWord.None;
            }
        }

        /// <summary>The interaction that offers <paramref name="verb"/> in the world (None for a verb on 07's screen).</summary>
        private static InteractionKind KindOf(ObjectiveVerb verb)
        {
            switch (verb)
            {
                case ObjectiveVerb.Scan:
                    return InteractionKind.Scan;
                case ObjectiveVerb.Cut:
                    return InteractionKind.Salvage;
                case ObjectiveVerb.Build:
                    return InteractionKind.Build;
                default:
                    return InteractionKind.None;
            }
        }
    }
}
