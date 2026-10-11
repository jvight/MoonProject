using UnityEngine;

namespace MoonProject.Core.Map
{
    /// <summary>One place on the crew's map (docs/features/M3-07), read through <see cref="IMapKnowledge"/>.</summary>
    public readonly struct MapPlace
    {
        public MapPlace(string id, Vector3 position, MapPlaceKind kind, float radius)
        {
            Id = id;
            Position = position;
            Kind = kind;
            Radius = radius;
        }

        /// <summary>
        /// The place's anchor id ("site.depot", "relay.2", "canyon.mouth", "place.lander", "friend.tilly", ...); the
        /// map's text keys are <see cref="MapKeys"/>' name and note keys of it.
        /// </summary>
        public string Id { get; }

        /// <summary>World position on the surface.</summary>
        public Vector3 Position { get; }

        public MapPlaceKind Kind { get; }

        /// <summary>Metres around <see cref="Position"/> that count as being there (and that the outline rings).</summary>
        public float Radius { get; }
    }
}
