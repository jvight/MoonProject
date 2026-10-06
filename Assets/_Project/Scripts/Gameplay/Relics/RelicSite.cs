using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>A planned burial spot: a point on the surface and the ground normal there.</summary>
    public readonly struct RelicSite
    {
        public RelicSite(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }

        public Vector3 Position { get; }

        public Vector3 Normal { get; }
    }
}
