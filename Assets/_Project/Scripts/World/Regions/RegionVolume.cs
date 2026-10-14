using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Spatial volume for a world region: evaluates horizontal signed distance (negative inside the core volume,
    /// zero on the boundary, positive outside) without allocations.
    /// </summary>
    public sealed class RegionVolume
    {
        private readonly Vector2[] _from;
        private readonly Vector2[] _to;
        private readonly float _radius;

        private RegionVolume(
            string regionId,
            Vector3 centre,
            float outerRadius,
            float radius,
            float blendBand,
            int priority,
            bool isFallback,
            Vector2[] from,
            Vector2[] to)
        {
            if (string.IsNullOrEmpty(regionId))
            {
                throw new ArgumentException("Region id must not be empty.", nameof(regionId));
            }

            RegionId = regionId;
            Centre = centre;
            OuterRadius = outerRadius;
            _radius = radius;
            BlendBand = blendBand;
            Priority = priority;
            IsFallback = isFallback;
            _from = from;
            _to = to;
        }

        /// <summary>Stable region identifier (<see cref="RegionIds"/>).</summary>
        public string RegionId { get; }

        /// <summary>Representative world-space centre of the volume.</summary>
        public Vector3 Centre { get; }

        /// <summary>Bounding outer radius (m) around <see cref="Centre"/>.</summary>
        public float OuterRadius { get; }

        /// <summary>Width (m) of this volume's signed-distance blend band.</summary>
        public float BlendBand { get; }

        /// <summary>Ordering priority (higher overrides lower; fallback has priority 0).</summary>
        public int Priority { get; }

        /// <summary>True when this volume is the world fallback that absorbs remaining blend weight.</summary>
        public bool IsFallback { get; }

        /// <summary>Creates a horizontal disc volume centred at <paramref name="centre"/>.</summary>
        public static RegionVolume CreateDisc(string regionId, Vector3 centre, float outerRadius, float blendBand,
            int priority = 0, bool isFallback = false)
        {
            return new RegionVolume(regionId, centre, outerRadius, outerRadius, blendBand, priority, isFallback,
                null, null);
        }

        /// <summary>
        /// Creates Whispering Canyon's polyline capsule volume along its anchors
        /// (<c>canyon.mouth -&gt; lip -&gt; landing -&gt; ledge -&gt; alcoves -&gt; terminus</c> plus
        /// <c>landing -&gt; exit</c>).
        /// </summary>
        public static RegionVolume CreateCanyon(IWorldAnchors anchors, float halfWidth, float blendBand,
            int priority = 1)
        {
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            WorldAnchor mouthAnchor = RequireAnchor(anchors, WorldAnchorIds.CanyonMouth);
            Vector3 lip = RequireAnchor(anchors, WorldAnchorIds.CanyonLip).Position;
            Vector3 landing = RequireAnchor(anchors, WorldAnchorIds.CanyonLanding).Position;
            Vector3 ledge = RequireAnchor(anchors, WorldAnchorIds.CanyonLedge).Position;
            Vector3 terminus = RequireAnchor(anchors, WorldAnchorIds.CanyonTerminus).Position;
            Vector3 exit = RequireAnchor(anchors, WorldAnchorIds.CanyonExit).Position;

            // Inset the capsule start along the straight mouth approach so canyon.mouth and canyon.lip sit inside
            // halfWidth while relay.2 and micro.06 outside the mouth remain on the basin side of the boundary.
            float lipDistance = Vector3.Distance(mouthAnchor.Position, lip);
            Vector3 mouthStart = mouthAnchor.Position + mouthAnchor.Forward * Mathf.Max(lipDistance, halfWidth - 1f);

            int alcoves = 0;
            while (anchors.TryGet(WorldAnchorIds.CanyonAlcovePrefix + alcoves, out _))
            {
                alcoves++;
            }

            int trunkPoints = 4 + alcoves;
            var trunk = new Vector3[trunkPoints];
            trunk[0] = mouthStart;
            trunk[1] = landing;
            trunk[2] = ledge;
            for (int i = 0; i < alcoves; i++)
            {
                anchors.TryGet(WorldAnchorIds.CanyonAlcovePrefix + i, out WorldAnchor alcove);
                trunk[3 + i] = alcove.Position;
            }

            trunk[trunkPoints - 1] = terminus;

            int segmentCount = trunkPoints;
            var from = new Vector2[segmentCount];
            var to = new Vector2[segmentCount];
            Vector3 min = mouthAnchor.Position;
            Vector3 max = mouthAnchor.Position;
            for (int i = 0; i < trunkPoints - 1; i++)
            {
                from[i] = new Vector2(trunk[i].x, trunk[i].z);
                to[i] = new Vector2(trunk[i + 1].x, trunk[i + 1].z);
                min = Vector3.Min(min, trunk[i + 1]);
                max = Vector3.Max(max, trunk[i + 1]);
            }

            int branch = segmentCount - 1;
            from[branch] = new Vector2(landing.x, landing.z);
            to[branch] = new Vector2(exit.x, exit.z);
            min = Vector3.Min(min, exit);
            max = Vector3.Max(max, exit);

            Vector3 centre = (min + max) * 0.5f;
            float dx = max.x - min.x;
            float dz = max.z - min.z;
            float outerRadius = Mathf.Sqrt(dx * dx + dz * dz) * 0.5f + halfWidth;
            return new RegionVolume(RegionIds.Canyon, centre, outerRadius, halfWidth, blendBand, priority, false,
                from, to);
        }

        /// <summary>
        /// Signed horizontal distance in metres to the volume boundary (negative inside, zero on the boundary,
        /// positive outside).
        /// </summary>
        public float SignedDistance(Vector3 position)
        {
            if (_from == null)
            {
                float dx = position.x - Centre.x;
                float dz = position.z - Centre.z;
                return Mathf.Sqrt(dx * dx + dz * dz) - _radius;
            }

            float minSq = float.MaxValue;
            float px = position.x;
            float pz = position.z;
            for (int i = 0; i < _from.Length; i++)
            {
                Vector2 a = _from[i];
                Vector2 b = _to[i];
                float abx = b.x - a.x;
                float abz = b.y - a.y;
                float lenSq = abx * abx + abz * abz;
                float t = lenSq > 0f ? Mathf.Clamp01(((px - a.x) * abx + (pz - a.y) * abz) / lenSq) : 0f;
                float dx = px - (a.x + abx * t);
                float dz = pz - (a.y + abz * t);
                float sq = dx * dx + dz * dz;
                if (sq < minSq)
                {
                    minSq = sq;
                }
            }

            return Mathf.Sqrt(minSq) - _radius;
        }

        private static WorldAnchor RequireAnchor(IWorldAnchors anchors, string id)
        {
            if (!anchors.TryGet(id, out WorldAnchor anchor))
            {
                throw new InvalidOperationException($"Regions: required world anchor '{id}' is missing.");
            }

            return anchor;
        }
    }
}
