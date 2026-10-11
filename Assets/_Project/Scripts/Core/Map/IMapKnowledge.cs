namespace MoonProject.Core.Map
{
    /// <summary>
    /// What the station remembers of the basin for the crew's map (docs/features/M3-07), registered in the
    /// <see cref="GameContext"/> by the Gameplay domain. The place list is fixed after initialisation; per-place
    /// queries are dictionary lookups that never allocate. "The map never lies": a place is inked only while the save
    /// says <see cref="MapPlaceState.Visited"/> and the station says it is in reach.
    /// </summary>
    public interface IMapKnowledge
    {
        /// <summary>Every place on the paper, home first, then in the order the chart pen inks them.</summary>
        int PlaceCount { get; }

        MapPlace GetPlace(int index);

        /// <summary><see cref="MapPlaceState.Sketched"/> for an id that is not on the map.</summary>
        MapPlaceState GetState(string placeId);

        /// <summary>Visited and inside the station's reach right now.</summary>
        bool IsInked(string placeId);

        /// <summary>
        /// The chart pen has drawn this place's ink: by an earlier look, or by its <see cref="Events.MapInkCued"/> in
        /// this one. An inked place that is not drawn yet is drawn at the next look.
        /// </summary>
        bool IsInkDrawn(string placeId);

        /// <summary>Places inked but not drawn yet: what the chart pen draws first at the next look.</summary>
        int InkedSinceLastLook { get; }

        /// <summary>What 07 did there, for the tiny icons beside an inked name.</summary>
        MapPlaceDeeds GetDeeds(string placeId);

        /// <summary>
        /// The card's neutral "what is still there" line (a <see cref="MapKeys"/> state key), or null when there is
        /// nothing to say.
        /// </summary>
        string GetStateLineKey(string placeId);
    }
}
