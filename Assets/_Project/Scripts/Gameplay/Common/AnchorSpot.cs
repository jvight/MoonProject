using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A spot near one of the World's named anchors (docs/ARCHITECTURE.md "Contract: world anchors"): the anchor's id
    /// and an offset in the anchor's own frame (x to its right, y along its forward). Content that sits at an anchor
    /// faces back the way 07 arrives (against the anchor's forward).
    /// </summary>
    [Serializable]
    public sealed class AnchorSpot
    {
        [Tooltip("World anchor id (Core WorldAnchorIds), e.g. canyon.terminus.")]
        [SerializeField] private string _anchorId = string.Empty;

        [Tooltip("Offset (m) from the anchor: x to its right, y along its forward.")]
        [SerializeField] private Vector2 _offset;

        public AnchorSpot(string anchorId, Vector2 offset)
        {
            _anchorId = anchorId;
            _offset = offset;
        }

        public string AnchorId => _anchorId;

        public Vector2 Offset => _offset;

        /// <summary>
        /// The spot on the surface and the horizontal way content there faces (toward 07 arriving along the
        /// anchor's forward). False when the world has no such anchor: a wiring bug the caller reports.
        /// </summary>
        public bool TryResolve(IWorldAnchors anchors, ITerrainQuery terrain, out Vector3 position,
            out Vector3 facing)
        {
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            if (!anchors.TryGet(_anchorId, out WorldAnchor anchor))
            {
                position = Vector3.zero;
                facing = Vector3.forward;
                return false;
            }

            Vector3 forward = anchor.Forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            var right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 point = anchor.Position + right * _offset.x + forward * _offset.y;
            position = SurfaceRules.OnSurface(terrain, point.x, point.z);
            facing = -forward;
            return true;
        }
    }
}
