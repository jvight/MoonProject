using System;

namespace MoonProject.UI
{
    /// <summary>The kinds of guidance, highest priority first (docs/design/opening-v2.md §5, §9).</summary>
    internal enum GuidanceKind
    {
        /// <summary>
        /// A line 07 puts on its own CRT face at a story moment (the failed reply's <c>TX · NO RANGE</c>): the moment is
        /// the whole screen's, so it takes the slot from everything else, which fades first and returns after it.
        /// </summary>
        CallLine = 0,

        /// <summary>
        /// The build step while 07 places or welds (<see cref="BuildSteps"/>): the player is in the middle of a task,
        /// so its one line waits for nothing and nothing interrupts it.
        /// </summary>
        BuildStep = 1,

        /// <summary>
        /// A chip or a card: the teaching prompt (glyph and one verb), the look/drive/hold chips, the recipe card, the
        /// blueprint-learned card.
        /// </summary>
        Prompt = 2,

        /// <summary>The low-battery hint: once per charge cycle, on the objective line's own line.</summary>
        Battery = 3,

        /// <summary>The objective line: what to do next.</summary>
        Objective = 4,
    }

    /// <summary>
    /// The one guidance slot (pure, EditMode-tested): of everything that wants to guide the player, one element shows at
    /// a time, the highest priority first. A lower element waits (never stacks) and an element that holds the slot
    /// gives it up, by fading, when a higher one wants it; the slot passes on only once the screen is clear. One
    /// exception keeps the screen from ever going blank: a prompt that takes over from the objective line starts at
    /// once, and the line keeps its place until the prompt has been fading in for the hand-over time, so the two
    /// cross-fade instead of leaving a gap.
    /// </summary>
    internal sealed class GuidanceSlot
    {
        public const int KindCount = 5;

        private readonly float _handOverSeconds;
        private float _promptUp;

        /// <param name="handOverSeconds">Seconds the objective line stays while a prompt that replaces it fades in.</param>
        public GuidanceSlot(float handOverSeconds)
        {
            if (handOverSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(handOverSeconds));
            }

            _handOverSeconds = handOverSeconds;
        }

        /// <summary>The element that may show now, or null while the slot is held back for a fade.</summary>
        public GuidanceKind? Granted { get; private set; }

        /// <summary>True while the objective line keeps its place under a prompt that is fading in over it.</summary>
        public bool HandingOver { get; private set; }

        /// <summary>True when <paramref name="kind"/> may show now.</summary>
        public bool Allows(GuidanceKind kind)
        {
            return Granted == kind || (kind == GuidanceKind.Objective && HandingOver);
        }

        /// <param name="wants">Per kind: it wants to show now.</param>
        /// <param name="visible">Per kind: it is on screen (also while it fades).</param>
        /// <param name="deltaTime">Unscaled seconds since the last step (0 while paused).</param>
        public void Step(bool[] wants, bool[] visible, float deltaTime)
        {
            if (wants.Length != KindCount || visible.Length != KindCount)
            {
                throw new ArgumentException("One flag per guidance kind.");
            }

            int winner = -1;
            for (int i = 0; i < KindCount; i++)
            {
                if (wants[i])
                {
                    winner = i;
                    break;
                }
            }

            if (winner < 0)
            {
                Granted = null;
                HandingOver = false;
                _promptUp = 0f;
                return;
            }

            // The winner starts only on a clear screen; one already up keeps the slot; every other visible element
            // is not granted, so it fades out first. A prompt may start over the objective line (the hand-over).
            bool overLine = winner == (int)GuidanceKind.Prompt && visible[(int)GuidanceKind.Objective];
            bool othersUp = false;
            for (int i = 0; i < KindCount; i++)
            {
                othersUp |= i != winner && visible[i] && !(overLine && i == (int)GuidanceKind.Objective);
            }

            Granted = othersUp ? (GuidanceKind?)null : (GuidanceKind)winner;
            bool handOver = overLine && Granted == GuidanceKind.Prompt;
            _promptUp = handOver && visible[(int)GuidanceKind.Prompt] ? _promptUp + deltaTime : 0f;
            HandingOver = handOver && _promptUp < _handOverSeconds;
        }
    }
}
