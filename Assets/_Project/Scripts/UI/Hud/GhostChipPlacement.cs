using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// Where the build ghost's chip (its cost and keys) stands on the screen (pure, EditMode-tested): never over the
    /// ghost, so the thing being placed is always fully seen, and never over the HUD's corner cards. It tries below the
    /// ghost, then above it, then to its right, then to its left, each kept inside the safe area; the first spot that
    /// meets neither the ghost, a card nor 07 wins. If every side meets 07, 07 gives way first: the first spot clear of
    /// the ghost and the cards wins. When none fits (a ghost that fills the view), the chip docks to a fixed slot low in
    /// the middle of the screen.
    /// </summary>
    internal static class GhostChipPlacement
    {
        /// <summary>The chip's top-left corner in panel coordinates (origin top-left).</summary>
        /// <param name="ghost">The ghost's bounds on the screen.</param>
        /// <param name="chip">The chip's size.</param>
        /// <param name="safe">The area the chip must stay inside (the panel less its margin).</param>
        /// <param name="gap">Space kept between the chip and the ghost or a card.</param>
        /// <param name="cards">HUD cards the chip must not cover.</param>
        /// <param name="rover">07 on the screen: kept clear when a side allows it (a zero-size rect when 07 is off it).</param>
        public static Vector2 Place(Rect ghost, Vector2 chip, Rect safe, float gap, IReadOnlyList<Rect> cards,
            Rect rover)
        {
            bool roverShown = rover.width > 0f && rover.height > 0f;
            if (roverShown && TrySides(ghost, chip, safe, gap, cards, rover, true, out Vector2 clearOf07))
            {
                return clearOf07;
            }

            if (TrySides(ghost, chip, safe, gap, cards, rover, false, out Vector2 at))
            {
                return at;
            }

            return Clamp(new Vector2(safe.center.x - chip.x * 0.5f, safe.yMax - chip.y), chip, safe);
        }

        private static bool TrySides(Rect ghost, Vector2 chip, Rect safe, float gap, IReadOnlyList<Rect> cards,
            Rect rover, bool avoidRover, out Vector2 at)
        {
            float centreX = ghost.center.x - chip.x * 0.5f;
            float centreY = ghost.center.y - chip.y * 0.5f;
            return TryAt(new Vector2(centreX, ghost.yMax + gap), ghost, chip, safe, gap, cards, rover, avoidRover,
                       out at) ||
                   TryAt(new Vector2(centreX, ghost.yMin - gap - chip.y), ghost, chip, safe, gap, cards, rover,
                       avoidRover, out at) ||
                   TryAt(new Vector2(ghost.xMax + gap, centreY), ghost, chip, safe, gap, cards, rover, avoidRover,
                       out at) ||
                   TryAt(new Vector2(ghost.xMin - gap - chip.x, centreY), ghost, chip, safe, gap, cards, rover,
                       avoidRover, out at);
        }

        /// <summary>The smallest screen rect around <paramref name="points"/> (the ghost's projected corners).</summary>
        public static Rect Around(IReadOnlyList<Vector2> points, int count)
        {
            Vector2 min = points[0];
            Vector2 max = points[0];
            for (int i = 1; i < count; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static bool TryAt(Vector2 wanted, Rect ghost, Vector2 chip, Rect safe, float gap,
            IReadOnlyList<Rect> cards, Rect rover, bool avoidRover, out Vector2 at)
        {
            at = Clamp(wanted, chip, safe);
            var rect = new Rect(at, chip);
            if (Meets(rect, ghost, gap) || (avoidRover && Meets(rect, rover, gap)))
            {
                return false;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                if (Meets(rect, cards[i], gap))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Meets(Rect a, Rect b, float gap)
        {
            return a.xMax > b.xMin - gap && a.xMin < b.xMax + gap && a.yMax > b.yMin - gap && a.yMin < b.yMax + gap;
        }

        private static Vector2 Clamp(Vector2 at, Vector2 chip, Rect safe)
        {
            return new Vector2(
                Mathf.Clamp(at.x, safe.xMin, Mathf.Max(safe.xMin, safe.xMax - chip.x)),
                Mathf.Clamp(at.y, safe.yMin, Mathf.Max(safe.yMin, safe.yMax - chip.y)));
        }
    }
}
