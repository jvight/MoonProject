using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Which bay of the socket rack 07 is looking at (pure, EditMode-tested; opening v3 4.3): of the bays showing an
    /// outline (a goal; a filled bay is done and an empty one says nothing), the one within reach of 07 and inside the
    /// view's margin whose screen point lies nearest the centre of the view. The bay read last stays read while it is
    /// still readable (sticky, like a scan target), and while E is held on it, it stays read as long as it is within
    /// reach, in view or not: a hold never switches bays.
    /// </summary>
    public static class RackAim
    {
        private static readonly Vector2 ViewCentre = new Vector2(0.5f, 0.5f);

        /// <param name="rack">What each bay shows.</param>
        /// <param name="bayPoints">Each bay's point in the world.</param>
        /// <param name="viewportPoints">Each bay's point through the view camera (viewport x, y; z = depth).</param>
        /// <param name="rover">07's position.</param>
        /// <param name="reach">Metres from 07 within which a bay can be read.</param>
        /// <param name="margin">A bay counts as in view this far in from each edge (a share of the view).</param>
        /// <returns>The bay looked at, or -1.</returns>
        public static int Pick(IRackStatus rack, Vector3[] bayPoints, Vector3[] viewportPoints, Vector3 rover,
            float reach, float margin)
        {
            if (rack == null)
            {
                throw new ArgumentNullException(nameof(rack));
            }

            if (bayPoints == null || viewportPoints == null || bayPoints.Length < rack.Count ||
                viewportPoints.Length < rack.Count)
            {
                throw new ArgumentException("One world point and one view point per bay.");
            }

            int best = -1;
            float bestDistance = float.MaxValue;
            float reachSqr = reach * reach;
            for (int i = 0; i < rack.Count; i++)
            {
                Vector3 view = viewportPoints[i];
                if (rack.StateOf(i) != RackBayState.Outline || (bayPoints[i] - rover).sqrMagnitude > reachSqr ||
                    !InView(view, margin))
                {
                    continue;
                }

                float distance = (new Vector2(view.x, view.y) - ViewCentre).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <param name="kept">The bay read last, or -1.</param>
        /// <param name="holding">E is held on <paramref name="kept"/>: it stays read while within reach.</param>
        public static int Pick(IRackStatus rack, Vector3[] bayPoints, Vector3[] viewportPoints, Vector3 rover,
            float reach, float margin, int kept, bool holding)
        {
            int picked = Pick(rack, bayPoints, viewportPoints, rover, reach, margin);
            if (kept < 0 || kept >= rack.Count || kept == picked || rack.StateOf(kept) != RackBayState.Outline ||
                (bayPoints[kept] - rover).sqrMagnitude > reach * reach)
            {
                return picked;
            }

            return holding || InView(viewportPoints[kept], margin) ? kept : picked;
        }

        private static bool InView(Vector3 view, float margin)
        {
            return view.z > 0f && view.x >= margin && view.x <= 1f - margin && view.y >= margin &&
                   view.y <= 1f - margin;
        }
    }
}
