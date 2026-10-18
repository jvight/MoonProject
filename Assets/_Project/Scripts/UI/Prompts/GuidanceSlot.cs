using System;

namespace MoonProject.UI
{
    /// <summary>The kinds of guidance, highest priority first (docs/design/opening-v2.md §5, §9).</summary>
    internal enum GuidanceKind
    {
        /// <summary>
        /// The build step while 07 places or welds (<see cref="BuildSteps"/>): the player is in the middle of a task,
        /// so its one line waits for nothing and nothing interrupts it.
        /// </summary>
        BuildStep = 0,

        /// <summary>
        /// A chip or a card: the teaching prompt (glyph and one verb), the look/drive/hold chips, the recipe card, the
        /// blueprint-learned card.
        /// </summary>
        Prompt = 1,

        /// <summary>The low-battery hint: once per charge cycle, on the objective line's own line.</summary>
        Battery = 2,

        /// <summary>The objective line: what to do next.</summary>
        Objective = 3,
    }

    /// <summary>
    /// The one guidance slot (pure, EditMode-tested): of everything that wants to guide the player, one element shows at
    /// a time, the highest priority first. A lower element waits (never stacks) and an element that holds the slot
    /// gives it up, by fading, when a higher one wants it; the slot passes on only once the screen is clear, so no two
    /// guidance elements are ever visible together, not even while one fades.
    /// </summary>
    internal sealed class GuidanceSlot
    {
        public const int KindCount = 4;

        /// <summary>The element that may show now, or null while the slot is held back for a fade.</summary>
        public GuidanceKind? Granted { get; private set; }

        /// <summary>True when <paramref name="kind"/> holds the slot now.</summary>
        public bool Allows(GuidanceKind kind)
        {
            return Granted == kind;
        }

        /// <summary>
        /// Decides the slot for this frame. <paramref name="wants"/> says which kinds want to show (independent of
        /// the slot); <paramref name="visible"/> which are on screen now, fading included. Both are indexed by
        /// <see cref="GuidanceKind"/>.
        /// </summary>
        public void Step(bool[] wants, bool[] visible)
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
                return;
            }

            // The winner starts only on a clear screen; one already up keeps the slot; every other visible element
            // is not granted, so it fades out first.
            bool othersUp = false;
            for (int i = 0; i < KindCount; i++)
            {
                othersUp |= i != winner && visible[i];
            }

            Granted = othersUp ? (GuidanceKind?)null : (GuidanceKind)winner;
        }
    }
}
