using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// Where Whispering Canyon is, for sound: a corridor along the World's canyon anchors, mouth -> lip -> landing ->
    /// alcoves (in order) -> terminus, with the exit corridor branching off the landing. <see cref="Inside"/> is 1
    /// within the corridor's half width once past the entry ramp from the mouth (or the exit), easing to 0 at its
    /// soft edge; <see cref="Trough"/> is how far 07 has sunk into the chasm between the lip and the landing.
    /// Horizontal distances only, so a leap over the chasm stays "inside". Built once from
    /// <see cref="IWorldAnchors"/>; queries do not allocate.
    /// </summary>
    public sealed class CanyonField
    {
        private readonly Vector3[] _from;
        private readonly Vector3[] _to;

        // Distance along the corridor (from the mouth, or from the exit for the exit branch) at each segment's ends.
        private readonly float[] _depthFrom;
        private readonly float[] _depthTo;
        private readonly int _troughSegment;

        private CanyonField(Vector3[] from, Vector3[] to, float[] depthFrom, float[] depthTo, int troughSegment)
        {
            _from = from;
            _to = to;
            _depthFrom = depthFrom;
            _depthTo = depthTo;
            _troughSegment = troughSegment;
            Vector3 min = from[0];
            Vector3 max = from[0];
            for (int i = 0; i < from.Length; i++)
            {
                min = Vector3.Min(min, Vector3.Min(from[i], to[i]));
                max = Vector3.Max(max, Vector3.Max(from[i], to[i]));
            }

            Centre = (min + max) * 0.5f;
            Extent = Vector3.Distance(min, max) * 0.5f;
        }

        /// <summary>Middle of the anchors' bounding box (where a canyon-wide effect can sit).</summary>
        public Vector3 Centre { get; }

        /// <summary>Distance from <see cref="Centre"/> to the farthest anchor corner (metres).</summary>
        public float Extent { get; }

        /// <summary>
        /// Builds the corridor from the World's anchors, or returns null with <paramref name="problem"/> naming the
        /// first required anchor (mouth, lip, landing, terminus, exit) that is missing. Initialisation only.
        /// </summary>
        public static CanyonField FromAnchors(IWorldAnchors anchors, out string problem)
        {
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            problem = null;
            if (!TryGet(anchors, WorldAnchorIds.CanyonMouth, out Vector3 mouth, ref problem) |
                !TryGet(anchors, WorldAnchorIds.CanyonLip, out Vector3 lip, ref problem) |
                !TryGet(anchors, WorldAnchorIds.CanyonLanding, out Vector3 landing, ref problem) |
                !TryGet(anchors, WorldAnchorIds.CanyonTerminus, out Vector3 terminus, ref problem) |
                !TryGet(anchors, WorldAnchorIds.CanyonExit, out Vector3 exit, ref problem))
            {
                return null;
            }

            int alcoves = 0;
            while (anchors.TryGet(WorldAnchorIds.CanyonAlcovePrefix + alcoves, out _))
            {
                alcoves++;
            }

            // The main path: mouth, lip, landing, each alcove, terminus; then the exit branch off the landing.
            var path = new Vector3[4 + alcoves];
            path[0] = mouth;
            path[1] = lip;
            path[2] = landing;
            for (int i = 0; i < alcoves; i++)
            {
                anchors.TryGet(WorldAnchorIds.CanyonAlcovePrefix + i, out WorldAnchor alcove);
                path[3 + i] = alcove.Position;
            }

            path[path.Length - 1] = terminus;
            int count = path.Length;
            var from = new Vector3[count];
            var to = new Vector3[count];
            var depthFrom = new float[count];
            var depthTo = new float[count];
            float depth = 0f;
            for (int i = 0; i < count - 1; i++)
            {
                from[i] = path[i];
                to[i] = path[i + 1];
                depthFrom[i] = depth;
                depth += HorizontalDistance(path[i], path[i + 1]);
                depthTo[i] = depth;
            }

            // The exit corridor counts its depth from its own open end, so leaving by it fades out like the mouth.
            int last = count - 1;
            from[last] = landing;
            to[last] = exit;
            depthFrom[last] = HorizontalDistance(landing, exit);
            depthTo[last] = 0f;
            return new CanyonField(from, to, depthFrom, depthTo, 1);
        }

        /// <summary>
        /// 0 outside .. 1 deep inside: 1 within <paramref name="halfWidth"/> of the corridor, easing to 0 over
        /// <paramref name="edge"/> metres beyond it, and ramping in over the first <paramref name="entry"/> metres
        /// from the mouth or the exit.
        /// </summary>
        public float Inside(Vector3 position, float halfWidth, float edge, float entry)
        {
            float best = 0f;
            for (int i = 0; i < _from.Length; i++)
            {
                Project(position, i, out float lateral, out float run);

                // Depth carries on past a segment's ends, so near a joint the next segment never counts the point
                // as deeper than it is (at the mouth, the chasm segment's start is still 10 m ahead).
                float depth = _depthFrom[i] + (_depthTo[i] - _depthFrom[i]) * run;
                float across = 1f - Smooth(halfWidth, halfWidth + edge, lateral);
                best = Mathf.Max(best, across * Smooth(0f, entry, depth));
            }

            return best;
        }

        /// <summary>
        /// 0 .. 1: how deep 07 is in the chasm's trough, between the lip and the landing: 0 at or above the line
        /// joining them (driving up to the lip, mid-leap), 1 from <paramref name="depthStart"/> +
        /// <paramref name="depthRange"/> metres below it, within the corridor's width.
        /// </summary>
        public float Trough(Vector3 position, float halfWidth, float edge, float depthStart, float depthRange)
        {
            float t = Project(position, _troughSegment, out float lateral, out _);
            float across = 1f - Smooth(halfWidth, halfWidth + edge, lateral);
            float line = Mathf.Lerp(_from[_troughSegment].y, _to[_troughSegment].y, t);
            return across * Smooth(depthStart, depthStart + depthRange, line - position.y);
        }

        /// <summary>Clamped 0..1 position along the segment (returned), the horizontal distance to it and the
        /// unclamped position <paramref name="run"/> (below 0 before its start, above 1 past its end).</summary>
        private float Project(Vector3 position, int segment, out float lateral, out float run)
        {
            Vector3 a = _from[segment];
            Vector3 b = _to[segment];
            float abx = b.x - a.x;
            float abz = b.z - a.z;
            float lengthSq = abx * abx + abz * abz;
            run = lengthSq > 0f ? ((position.x - a.x) * abx + (position.z - a.z) * abz) / lengthSq : 0f;
            float t = Mathf.Clamp01(run);
            float dx = position.x - (a.x + abx * t);
            float dz = position.z - (a.z + abz * t);
            lateral = Mathf.Sqrt(dx * dx + dz * dz);
            return t;
        }

        private static bool TryGet(IWorldAnchors anchors, string id, out Vector3 position, ref string problem)
        {
            if (anchors.TryGet(id, out WorldAnchor anchor))
            {
                position = anchor.Position;
                return true;
            }

            position = default;
            problem = problem ?? $"no '{id}' world anchor";
            return false;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = b.x - a.x;
            float dz = b.z - a.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static float Smooth(float edge0, float edge1, float x)
        {
            if (edge1 <= edge0)
            {
                return x >= edge1 ? 1f : 0f;
            }

            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
