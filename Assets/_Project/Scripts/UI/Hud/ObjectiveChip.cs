using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The key chip that belongs to the objective line (pure, EditMode-tested). A step that needs a verb shows its key
    /// glyph and one word for as long as its line is up, bright or dim, until the verb is first used in that step;
    /// after that it comes back whenever the verb is on offer again (the next of two fragments to scan, standing or
    /// moving), since the context prompt for a learned verb no longer shows. The chip is part of the objective's one
    /// guidance element: it shows only while the line shows and never competes with it for the slot.
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
        public HintWord Word(bool lineShowing, bool canFit, bool faceOpen, InteractionKind offered)
        {
            if (!lineShowing || (_used && (offered == InteractionKind.None || offered != KindOf(_verb))))
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
