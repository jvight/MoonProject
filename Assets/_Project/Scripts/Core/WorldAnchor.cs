using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// A named point of the generated world where content belongs (a canyon ledge, an alcove, a terminus chamber).
    /// Published by the World domain through <see cref="IWorldAnchors"/>; ids are listed in <see cref="WorldAnchorIds"/>.
    /// </summary>
    public readonly struct WorldAnchor
    {
        public WorldAnchor(string id, Vector3 position, Vector3 forward, float radius)
        {
            Id = id;
            Position = position;
            Forward = forward;
            Radius = radius;
        }

        public string Id { get; }

        /// <summary>On the surface (y = terrain height), so content can stand on it directly.</summary>
        public Vector3 Position { get; }

        /// <summary>
        /// Horizontal unit vector: the way 07 travels when arriving into the space (into the canyon, across the chasm,
        /// into an alcove or the terminus chamber, down the exit). Content meant to greet 07 faces -Forward.
        /// </summary>
        public Vector3 Forward { get; }

        /// <summary>Radius in metres of flat, drivable, uncluttered ground around <see cref="Position"/>.</summary>
        public float Radius { get; }
    }
}
