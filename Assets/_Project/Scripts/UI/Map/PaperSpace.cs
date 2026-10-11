using UnityEngine;

namespace MoonProject.UI.Map
{
    /// <summary>
    /// Where the basin lies on the crew's paper: world XZ in metres to paper units, north up (UI Toolkit's y grows
    /// down the page).
    /// </summary>
    internal readonly struct PaperSpace
    {
        private readonly Vector2 _centre;
        private readonly float _unitsPerMetre;

        public PaperSpace(Vector2 centre, float radius, float size)
        {
            _centre = centre;
            Size = size;
            _unitsPerMetre = size / (2f * Mathf.Max(radius, Mathf.Epsilon));
        }

        /// <summary>Paper units across the sheet.</summary>
        public float Size { get; }

        /// <summary>Paper units per metre of basin.</summary>
        public float UnitsPerMetre => _unitsPerMetre;

        /// <summary>A point of the basin (world XZ) on the paper.</summary>
        public Vector2 ToPaper(Vector2 basin)
        {
            return new Vector2(Size * 0.5f + (basin.x - _centre.x) * _unitsPerMetre,
                Size * 0.5f - (basin.y - _centre.y) * _unitsPerMetre);
        }

        /// <summary>A world position on the paper (its height ignored).</summary>
        public Vector2 ToPaper(Vector3 world)
        {
            return ToPaper(new Vector2(world.x, world.z));
        }
    }
}
