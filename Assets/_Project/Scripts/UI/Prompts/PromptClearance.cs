using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// Keeps a prompt off the cards (pure, EditMode-tested): a prompt floats where its world point is, and that point
    /// can sit under the recipe card or the build menu; the prompt then rests on top of the card, or under it when
    /// there is no room above, never over its words.
    /// </summary>
    internal static class PromptClearance
    {
        /// <summary>
        /// Pixels to raise the prompt chip (negative: lower it) so it clears <paramref name="card"/> by
        /// <paramref name="gap"/>; 0 when they do not meet. <paramref name="chip"/> is where the chip would stand with
        /// no lift, <paramref name="screen"/> the panel; the chip stays <paramref name="margin"/> inside its top edge.
        /// </summary>
        public static float Lift(Rect chip, Rect card, float gap, Rect screen, float margin)
        {
            bool across = chip.xMax > card.xMin - gap && chip.xMin < card.xMax + gap;
            bool down = chip.yMax > card.yMin - gap && chip.yMin < card.yMax + gap;
            if (!across || !down)
            {
                return 0f;
            }

            float above = chip.yMax - (card.yMin - gap);
            if (chip.yMin - above >= screen.yMin + margin)
            {
                return above;
            }

            return -((card.yMax + gap) - chip.yMin);
        }
    }
}
