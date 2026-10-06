using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Where a friend lies and where its parts are scattered (see <see cref="FriendSitePlanner"/>).</summary>
    [Serializable]
    public sealed class FriendPlacement
    {
        [Tooltip("Seed of this friend's placement: the same seed and world always give the same site and parts.")]
        [SerializeField] private int _seed = 41;

        [Tooltip("Distance range (m) of the site from the base centre.")]
        [SerializeField] private Vector2 _siteDistance = new Vector2(60f, 110f);

        [Tooltip("Bearing (degrees from +Z toward +X) the site is searched around.")]
        [Range(-180f, 180f)] [SerializeField] private float _siteBearing = 90f;

        [Tooltip("Half-width (degrees) of the fan searched around that bearing.")]
        [Range(5f, 180f)] [SerializeField] private float _siteBearingSpread = 55f;

        [Tooltip("The site lies in a crater: how much lower (m) its floor must be than the ring around it (min, max).")]
        [SerializeField] private Vector2 _craterDepth = new Vector2(0.3f, 2.5f);

        [Tooltip("Radius (m) of that ring.")]
        [Range(3f, 40f)] [SerializeField] private float _craterRadius = 10f;

        [Tooltip("The site must be in clear view (a silhouette on the dust) from the edge of the base pad.")]
        [SerializeField] private bool _visibleFromBase = true;

        [Tooltip("Distance range (m) of the parts from the site.")]
        [SerializeField] private Vector2 _partDistance = new Vector2(30f, 60f);

        public FriendPlacement(int seed, Vector2 siteDistance, float siteBearing, float siteBearingSpread,
            Vector2 craterDepth, float craterRadius, bool visibleFromBase, Vector2 partDistance)
        {
            _seed = seed;
            _siteDistance = siteDistance;
            _siteBearing = siteBearing;
            _siteBearingSpread = siteBearingSpread;
            _craterDepth = craterDepth;
            _craterRadius = craterRadius;
            _visibleFromBase = visibleFromBase;
            _partDistance = partDistance;
        }

        public int Seed => _seed;
        public Vector2 SiteDistance => Ordered(_siteDistance);
        public float SiteBearing => _siteBearing;
        public float SiteBearingSpread => _siteBearingSpread;
        public Vector2 CraterDepth => Ordered(_craterDepth);
        public float CraterRadius => _craterRadius;
        public bool VisibleFromBase => _visibleFromBase;
        public Vector2 PartDistance => Ordered(_partDistance);

        private static Vector2 Ordered(Vector2 range)
        {
            return range.x <= range.y ? range : new Vector2(range.y, range.x);
        }
    }
}
