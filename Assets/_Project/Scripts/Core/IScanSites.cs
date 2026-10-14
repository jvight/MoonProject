using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// The places that hold scan fragments (docs/design/0.5.5-owner-feedback.md §1.4), registered by Gameplay: the
    /// salvage sites (a derelict standing inside a wreck counts as its wreck), the derelicts on the routes and the
    /// chasm's near lip. Each place id is a crew map place id (<see cref="Map.MapPlace.Id"/>: "site.garage",
    /// "micro.06", "canyon.lip"). The list is fixed after initialisation, in a stable order; queries never allocate.
    /// <see cref="Events.ScanSiteReached"/> and <see cref="Events.ScanSiteEmptied"/> tell the moments; a loaded game
    /// is read from here.
    /// </summary>
    public interface IScanSites
    {
        int Count { get; }

        /// <summary>The place's map id.</summary>
        string PlaceId(int index);

        /// <summary>Where the place is on the surface (its map place's position).</summary>
        Vector3 Position(int index);

        /// <summary>How many fragments lie at the place in all.</summary>
        int FragmentCount(int index);

        /// <summary>How many of them are still to scan; 0 once the place is emptied.</summary>
        int Left(int index);

        /// <summary>
        /// 07 knows the place is there: it answered a listen or a spotter showed it (a site or a derelict), or 07 has
        /// come within it (<see cref="Events.ScanSiteReached"/>). Saved through the places' own state.
        /// </summary>
        bool IsKnown(int index);

        /// <summary>The index of <paramref name="placeId"/>, or -1 when no fragment lies there.</summary>
        int IndexOf(string placeId);

        /// <summary>
        /// How many fragments of <paramref name="blueprintId"/>'s set (a kit id, e.g. "rover.cargo_cradle") are
        /// scanned, wherever they lay; 0 for an id with no set.
        /// </summary>
        int ScannedOf(string blueprintId);

        /// <summary>How many fragments <paramref name="blueprintId"/>'s set has; 0 for an id with no set.</summary>
        int SetSize(string blueprintId);
    }
}
