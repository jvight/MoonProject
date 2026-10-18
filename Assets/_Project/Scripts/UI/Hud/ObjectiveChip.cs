using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The key chip that belongs to the objective line (pure, EditMode-tested). A step that needs a verb shows its key
    /// glyph and one word for as long as its line is up, bright or dim, until the verb is first used in that step;
    /// after that it comes back whenever the verb is on offer again (the next of two fragments to scan, standing or
    /// moving), since the context prompt for a learned verb no longer shows. Whatever the step asks, the chip never
    /// names a key that does something else right now: while E is held it names what E is doing, and with an E target
    /// in reach it names what E would do there (a crate seal reads Cut even on a scan step). The chip is part of the
    /// objective's one guidance element: it shows only while the line shows and never competes with it for the slot.
    /// </summary>
    internal sealed class ObjectiveChip
    {
        private ObjectiveVerb _verb;
        private bool _used;

        /// <summary>A new step began: its verb is wanted again, whether or not it was used in an earlier step.</summary>
        public void SetKey(string objectiveKey)
        {
            _verb = ObjectiveVerbs.Of(objectiveKey);
            _used = false;
        }

        /// <summary>The player did <paramref name="verb"/>; the chip retires if it was the step's verb.</summary>
        public void Used(ObjectiveVerb verb)
        {
            if (verb == _verb)
            {
                _used = true;
            }
        }

        /// <summary>The word on the chip now (None while it shows nothing).</summary>
        /// <param name="lineShowing">The objective line is on screen.</param>
        /// <param name="canFit">The igniter can be fitted now (docked, igniter in the hold).</param>
        /// <param name="faceOpen">07's face screen is open: Tab is already answered.</param>
        /// <param name="offered">The gameplay's ready action now (None when nothing is in reach).</param>
        /// <param name="held">What a held E is doing now (None while E is not at work): it outranks the offer.</param>
        public HintWord Word(bool lineShowing, bool canFit, bool faceOpen, InteractionKind offered,
            InteractionKind held)
        {
            if (!lineShowing)
            {
                return HintWord.None;
            }

            HintWord excavate = ExcavateWord(held != InteractionKind.None ? held : offered);
            if (excavate != HintWord.None)
            {
                return excavate;
            }

            if (_used && (offered == InteractionKind.None || offered != KindOf(_verb)))
            {
                return HintWord.None;
            }

            switch (_verb)
            {
                case ObjectiveVerb.Scan:
                    return HintWord.Scan;
                case ObjectiveVerb.Cut:
                    return HintWord.Cut;
                case ObjectiveVerb.Build:
                    return HintWord.Build;
                case ObjectiveVerb.Fit:
                    return canFit && !faceOpen ? HintWord.Fit : HintWord.None;
                default:
                    return HintWord.None;
            }
        }

        /// <summary>
        /// True while the step's verb has its target in front of 07 (<paramref name="primary"/> is that verb's kind,
        /// ready or not): the line and this chip come without waiting for 07 to settle.
        /// </summary>
        public bool AtHand(InteractionKind primary)
        {
            return primary != InteractionKind.None && primary == KindOf(_verb);
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
