using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Map;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// The crew map's place list (docs/features/M3-07, docs/design/text/crew-map.md) and how each place reads the rest
    /// of Gameplay: home, the salvage sites, the relay masts, the canyon's places, Kestrel-3's skid, the crew's antenna
    /// farm, each friend's site (a friend found at another place's spot belongs to that place, as Bell does to the
    /// canyon's terminus) and the two places beyond the rim. Home comes first, then every place by its distance from
    /// home, the order the chart pen inks them; the places beyond the rim come last.
    /// </summary>
    public static class MapPlaces
    {
        private static readonly string[] CanyonIds =
        {
            WorldAnchorIds.CanyonMouth, WorldAnchorIds.CanyonLip, WorldAnchorIds.CanyonLanding,
            WorldAnchorIds.CanyonLedge, WorldAnchorIds.CanyonTerminus, WorldAnchorIds.CanyonExit,
        };

        private static readonly string[] RegionIds = { WorldAnchorIds.RegionPeak, WorldAnchorIds.RegionTerraces };

        /// <summary>Home's place id (its text is the map's "map.place.lander.*").</summary>
        public const string HomeId = "place.lander";

        /// <summary>The places and their probes, in ink order.</summary>
        public static void Build(MapTuning tuning, IWorldAnchors anchors, StationReach reach,
            IReadOnlyList<SalvageSite> sites, IReadOnlyList<Friend> friends, IBlueprintBoard board,
            IReadOnlyList<BlueprintTube> tubes, List<MapPlace> places, List<MapPlaceProbe> probes)
        {
            if (tuning == null || anchors == null || reach == null || sites == null || friends == null
                || board == null || tubes == null || places == null || probes == null)
            {
                throw new ArgumentNullException(nameof(tuning), "The map's place list is missing an input.");
            }

            Vector3 home = reach.Position(StationReach.Home);
            var entries = new List<Entry>
            {
                new Entry(new MapPlace(HomeId, home, MapPlaceKind.Home, tuning.HomeRadius)) { Visited = Always },
            };

            foreach (SalvageSite site in sites)
            {
                entries.Add(SiteEntry(site, board, tubes));
            }

            for (int node = StationReach.Home + 1; node < reach.NodeCount; node++)
            {
                entries.Add(MastEntry(reach, node, anchors));
            }

            foreach (string id in CanyonIds)
            {
                entries.Add(AnchorEntry(anchors, id, MapPlaceKind.Canyon));
            }

            entries.Add(AnchorEntry(anchors, WorldAnchorIds.KestrelTrail, MapPlaceKind.Trail));
            entries.Add(AnchorEntry(anchors, WorldAnchorIds.PlaceAntennaFarm, MapPlaceKind.Landmark));
            foreach (Friend friend in friends)
            {
                AddFriend(entries, friend, tuning);
            }

            entries.Sort(1, entries.Count - 1, new ByDistance(home));
            foreach (string id in RegionIds)
            {
                Entry region = AnchorEntry(anchors, id, MapPlaceKind.Region);
                region.Visitable = false;
                entries.Add(region);
            }

            foreach (Entry entry in entries)
            {
                places.Add(entry.Place);
                probes.Add(new MapPlaceProbe(entry.Seen, entry.Visited, entry.Deeds, entry.StateLine,
                    entry.Visitable));
            }
        }

        private static bool Always()
        {
            return true;
        }

        private static Entry SiteEntry(SalvageSite site, IBlueprintBoard board, IReadOnlyList<BlueprintTube> tubes)
        {
            string kit = null;
            foreach (BlueprintTube tube in tubes)
            {
                kit = tube.SiteId == site.Id ? tube.KitId : kit;
            }

            bool TubeWaits() => kit != null && !board.IsPinned(kit);

            bool HeartWaits()
            {
                for (int i = 0; i < site.Relics.Count; i++)
                {
                    if (site.Relics[i].CanBeLifted)
                    {
                        return true;
                    }
                }

                return TubeWaits();
            }

            return new Entry(new MapPlace(site.Id, site.Position, MapPlaceKind.Site, site.Radius))
            {
                Seen = () => site.Discovered,
                Visited = () =>
                {
                    for (int i = 0; i < site.Relics.Count; i++)
                    {
                        if (site.Relics[i].State != RelicState.Buried)
                        {
                            return true;
                        }
                    }

                    return site.Remaining < site.Pieces.Count;
                },
                Deeds = () =>
                {
                    MapPlaceDeeds deeds = kit != null && board.IsPinned(kit) ? MapPlaceDeeds.Blueprint
                        : MapPlaceDeeds.None;
                    for (int i = 0; i < site.Relics.Count; i++)
                    {
                        RelicState state = site.Relics[i].State;
                        if (state == RelicState.Displayed || state == RelicState.Depositing)
                        {
                            deeds |= MapPlaceDeeds.Relic;
                        }
                    }

                    return deeds;
                },
                StateLine = () => HeartWaits() ? MapKeys.StateHeartWaiting
                    : site.IsPickedClean ? MapKeys.StatePickedClean
                    : null,
            };
        }

        private static Entry MastEntry(StationReach reach, int node, IWorldAnchors anchors)
        {
            string id = reach.Id(node);
            float radius = anchors.TryGet(id, out WorldAnchor anchor) ? anchor.Radius : 0f;
            return new Entry(new MapPlace(id, reach.Position(node), MapPlaceKind.Relay, radius))
            {
                Visited = () => reach.IsRestored(node),
                Deeds = () => reach.IsLit(node) ? MapPlaceDeeds.Mast : MapPlaceDeeds.None,
                StateLine = () => reach.IsLit(node) ? MapKeys.StateMastLit : MapKeys.StateMastDark,
            };
        }

        private static Entry AnchorEntry(IWorldAnchors anchors, string id, MapPlaceKind kind)
        {
            if (!anchors.TryGet(id, out WorldAnchor anchor))
            {
                throw new InvalidOperationException($"The crew's map needs world anchor '{id}', which is missing.");
            }

            return new Entry(new MapPlace(id, anchor.Position, kind, anchor.Radius));
        }

        /// <summary>
        /// A friend's site joins the place it lies at (seen, visited and its icon), or becomes its own place.
        /// </summary>
        private static void AddFriend(List<Entry> entries, Friend friend, MapTuning tuning)
        {
            FriendProgress progress = friend.Progress;
            Func<bool> seen = () => progress.Discovered;
            Func<bool> visited = () => progress.Collected > 0 || progress.State >= FriendState.Repairing;
            Func<MapPlaceDeeds> deeds = () => progress.State >= FriendState.Repairing ? MapPlaceDeeds.Friend
                : MapPlaceDeeds.None;
            Vector3 site = friend.Site.Position;
            foreach (Entry entry in entries)
            {
                if (entry.Place.Kind != MapPlaceKind.Home
                    && SurfaceRules.HorizontalDistance(entry.Place.Position, site) <= tuning.FriendMergeDistance)
                {
                    entry.Seen = Or(entry.Seen, seen);
                    entry.Visited = Or(entry.Visited, visited);
                    Func<MapPlaceDeeds> own = entry.Deeds;
                    entry.Deeds = own == null ? deeds : () => own() | deeds();
                    return;
                }
            }

            entries.Add(new Entry(new MapPlace(MapKeys.FriendPrefix + friend.Id, site, MapPlaceKind.Friend,
                tuning.FriendRadius))
            {
                Seen = seen,
                Visited = visited,
                Deeds = deeds,
            });
        }

        private static Func<bool> Or(Func<bool> a, Func<bool> b)
        {
            return a == null ? b : () => a() || b();
        }

        private sealed class Entry
        {
            public Entry(MapPlace place)
            {
                Place = place;
            }

            public MapPlace Place { get; }

            public Func<bool> Seen { get; set; }

            public Func<bool> Visited { get; set; }

            public Func<MapPlaceDeeds> Deeds { get; set; }

            public Func<string> StateLine { get; set; }

            public bool Visitable { get; set; } = true;
        }

        private sealed class ByDistance : IComparer<Entry>
        {
            private readonly Vector3 _home;

            public ByDistance(Vector3 home)
            {
                _home = home;
            }

            public int Compare(Entry a, Entry b)
            {
                float da = SurfaceRules.HorizontalDistanceSquared(a.Place.Position, _home);
                float db = SurfaceRules.HorizontalDistanceSquared(b.Place.Position, _home);
                int byDistance = da.CompareTo(db);
                return byDistance != 0 ? byDistance : string.CompareOrdinal(a.Place.Id, b.Place.Id);
            }
        }
    }
}
