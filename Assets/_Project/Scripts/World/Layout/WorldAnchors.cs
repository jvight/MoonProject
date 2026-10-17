using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The named content anchors of the generated world (registered as <see cref="IWorldAnchors"/>), built
    /// deterministically from the surface: today Whispering Canyon's mouth, lip, landing apron, glinting ledge,
    /// alcoves, terminus and the top of its one-way exit, the four relay masts of the station-reach network
    /// (M3-06: relay.0..3, whose Forward points home; relay.0 stands in the spawn first frame) and the five salvage
    /// sites (M3-13: site.depot, site.kestrel, site.drill, site.garage, site.lander) with trail.kestrel, the
    /// home-side end of Kestrel-3's debris furrow, the crew's antenna farm, and the crew map's two places beyond
    /// the rim (M3-07: region.peak on The Peak's summit plateau, region.terraces at the foot of the rim terraces).
    /// Every radius is flat, drivable, uncluttered ground, except The Peak's (its summit plateau).
    /// <para>
    /// Forward is the way 07 travels when arriving into the space, so content faces -Forward to greet it: into the
    /// canyon at the mouth and the landing, across the chasm at the lip, into the bay at the ledge and into each
    /// alcove from the corridor, into the chamber at the terminus, and down the step, out to the basin, at the exit.
    /// </para>
    /// <para>
    /// The salvage sites on the basin floor face on from home, the way 07 drives out to them; Kestrel-3's crater
    /// faces along its fall line, the way its debris trail leads 07 in, and so does trail.kestrel at the furrow's
    /// home-side end (its radius is the furrow's half-width there); the crashed lander faces into its bay off the
    /// canyon apron.
    /// </para>
    /// <para>
    /// The landing is the touchdown zone just past the far face (where charged leaps come down), not the apron's
    /// middle. The terminus stands just in front of the chamber's back wall: past the edge of its radius along
    /// Forward lies a short strip of flat floor and then the sheer wall, so something can lean against it.
    /// </para>
    /// </summary>
    public sealed class WorldAnchors : IWorldAnchors
    {
        private const float MouthRadius = 4f;
        private const float LipRadius = 1.5f;
        private const float LandingRadius = 6f;
        private const float LandingSetback = 1f;
        private const float LedgeRadius = 2f;
        private const float AlcoveRadius = 2f;

        // The terminus space, and the flat floor left between its edge and the foot of the chamber's back wall.
        private const float TerminusSpace = 4f;
        private const float TerminusWallGap = 1.25f;
        private const float ExitRadius = 2.5f;
        private const float ExitTopSetback = 4f;

        // The crashed lander's footprint in its bay, and the floor left between its edge and the bay's back wall.
        private const float LanderRadius = 9f;
        private const float LanderWallGap = 3f;

        private const string Depot = WorldAnchorIds.SitePrefix + "depot";
        private const string Kestrel = WorldAnchorIds.SitePrefix + "kestrel";
        private const string Drill = WorldAnchorIds.SitePrefix + "drill";
        private const string Garage = WorldAnchorIds.SitePrefix + "garage";
        private const string Lander = WorldAnchorIds.SitePrefix + "lander";

        // The antenna farm stands at about 270 degrees, 120 m out (the level review's L7), on a small clear patch.
        private const float AntennaFarmBearing = 270f;
        private const float AntennaFarmDistance = 120f;
        private const float AntennaFarmRadius = 6f;

        // The rim terraces (0.6's region, docs/features/M3-07 step 3) rise in the empty south-west quadrant; their
        // anchor is the last drivable floor at their foot, where the dock's evening raises 0.5's last pillar.
        private const float TerracesBearing = 225f;
        private const float TerracesRadius = 6f;
        private const float TerracesSearchStep = 1f;

        // The old vent's orange bead field (M3-27) on the open floor east-south-east of home.
        private const float VentBearing = 140f;
        private const float VentDistance = 150f;
        private const float VentRadius = 8f;

        // The vent stands on the first level disc found walking out and round from that bearing (a fixed lattice).
        private const float VentSearchBearing = 40f;
        private const float VentSearchDistance = 60f;
        private const float VentSearchStep = 5f;
        private const float VentMaxRise = 0.6f;
        private const int VentRingSamples = 12;

        // Open floor between the vent's field and any other anchored place (a site's reach runs past its radius).
        private const float VentClearance = 30f;

        // The ilmenite band (0.6) lies on the same terraces a little further round; the shadowed crater (0.7) is the
        // basin's biggest real crater, its anchor a small disc on its floor.
        private const float IlmeniteBearing = 238f;
        private const float ShadowedFloorRadius = 4f;

        // The micro-sites (0.5.5 §1.7). 06 and the hut stand beside relay.2's pad, to its right seen from home.
        private const float Micro06Side = 7f;
        private const float Micro06Radius = 4f;

        // Mule lies left of the canyon road (the route posts stand on its right) at about 160 m on its bearing.
        private const float MuleBearing = 72f;
        private const float MuleDistance = 160f;
        private const float MuleSide = 4.5f;
        private const float MuleRadius = 4f;

        // The drone hangs in relay.0's back-left guy wire (art's RelayMeshes.GuyStake 0, in the relay's frame), whose
        // wire runs up toward the mast's footing.
        private static readonly Vector2 GuyStake = new Vector2(-2.6f, -3.8f);
        private static readonly Vector2 MastFooting = new Vector2(0f, -2.2f);
        private const float DroneRadius = 2f;

        // The dead charging post's own footprint on the wake pad, kept clear of rocks and props.
        private const float WakeRadius = 1.5f;

        private readonly WorldAnchor[] _anchors;
        private readonly Dictionary<string, int> _byId = new Dictionary<string, int>(StringComparer.Ordinal);

        public WorldAnchors(MoonSurface surface, CanyonSettings settings, RelaySettings relays)
        {
            if (relays == null)
            {
                throw new ArgumentNullException(nameof(relays));
            }

            if (surface == null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            Canyon canyon = surface.Canyon;
            CanyonPath main = canyon.MainPath;
            CanyonPath exit = canyon.ExitPath;
            var anchors = new List<WorldAnchor>
            {
                Make(surface, WorldAnchorIds.CanyonMouth, main.PointAt(0f), main.TangentAt(0f), MouthRadius),
                Make(surface, WorldAnchorIds.CanyonLip, main.PointAt(canyon.LipArc), main.TangentAt(canyon.LipArc),
                    LipRadius),
            };

            float landing = canyon.FarFaceArc + Canyon.SlabHalfDepth + LandingSetback + LandingRadius;
            anchors.Add(Make(surface, WorldAnchorIds.CanyonLanding, main.PointAt(landing), main.TangentAt(landing),
                LandingRadius));
            anchors.Add(Make(surface, WorldAnchorIds.CanyonLedge, canyon.LedgeCenter,
                main.RightAt(canyon.LedgeArc) * canyon.LedgeSide, LedgeRadius));
            for (int i = 0; i < canyon.AlcoveArcs.Count; i++)
            {
                float arc = canyon.AlcoveArcs[i];
                float side = canyon.AlcoveSides[i];
                Vector2 outward = main.RightAt(arc) * side;
                Vector2 centre = main.PointAt(arc) + outward * (settings.CanyonHalfWidth + settings.AlcoveDepth * 0.5f);
                anchors.Add(Make(surface, WorldAnchorIds.CanyonAlcovePrefix + i, centre, outward, AlcoveRadius));
            }

            // The chamber is the corridor's round end cap, its wall foot TerminusRadius beyond the line's end.
            Vector2 intoChamber = main.TangentAt(main.EndArc);
            Vector2 terminus = main.PointAt(main.EndArc)
                + intoChamber * (settings.TerminusRadius - TerminusWallGap - TerminusSpace);
            anchors.Add(Make(surface, WorldAnchorIds.CanyonTerminus, terminus, intoChamber, TerminusSpace));
            float exitTop = canyon.ExitStepArc + Canyon.SlabHalfDepth + ExitTopSetback;
            anchors.Add(Make(surface, WorldAnchorIds.CanyonExit, exit.PointAt(exitTop), -exit.TangentAt(exitTop),
                ExitRadius));

            Vector2 mound = surface.Shaping.RelayMound.Center;
            Vector2 shoulder = RelaySiteFinder.Find(surface, relays.ShoulderBearing, relays.ShoulderBearingSpread,
                relays.ShoulderDistance, relays.ShoulderDistanceSpread, relays.PadRadius);
            Vector2 mouth = main.PointAt(relays.MouthArc) + main.RightAt(relays.MouthArc) * relays.MouthLateral;
            Vector2[] masts = { mound, shoulder, mouth, canyon.RelayLedgeCenter };
            for (int i = 0; i < masts.Length; i++)
            {
                anchors.Add(Make(surface, WorldAnchorIds.RelayPrefix + i, masts[i], -masts[i], relays.PadRadius));
            }

            GroundShaping shaping = surface.Shaping;
            KestrelImpact kestrel = surface.Kestrel;
            anchors.Add(Footprint(surface, Depot, shaping.Depot));
            anchors.Add(Make(surface, Kestrel, kestrel.Center, kestrel.Fall, kestrel.FloorRadius));
            anchors.Add(Make(surface, WorldAnchorIds.KestrelTrail, kestrel.TrailEnd, kestrel.Fall,
                kestrel.TailHalfWidth));
            anchors.Add(Footprint(surface, Drill, shaping.Drill));
            anchors.Add(Footprint(surface, Garage, shaping.Garage));
            float bayArc = settings.LanderBayArc;
            Vector2 intoBay = main.RightAt(bayArc) * canyon.LanderSide;
            float bayLateral = settings.ChasmHalfWidth + settings.LanderBayDepth - LanderRadius - LanderWallGap;
            anchors.Add(Make(surface, Lander, main.PointAt(bayArc) + intoBay * bayLateral, intoBay, LanderRadius));
            Vector2 farm = MoonSurface.BearingToDirection(AntennaFarmBearing) * AntennaFarmDistance;
            anchors.Add(Make(surface, WorldAnchorIds.PlaceAntennaFarm, farm, farm, AntennaFarmRadius));
            var summit = new Vector2(surface.PeakSummit.x, surface.PeakSummit.z);
            anchors.Add(Make(surface, WorldAnchorIds.RegionPeak, summit, summit, surface.SummitRadius));
            Vector2 up = MoonSurface.BearingToDirection(TerracesBearing);
            anchors.Add(Make(surface, WorldAnchorIds.RegionTerraces, TerracesFoot(surface, up), up, TerracesRadius));
            Vector2 vent = VentSite(surface, anchors);
            anchors.Add(Make(surface, WorldAnchorIds.PlaceOrangeVent, vent, vent, VentRadius));
            Vector2 band = MoonSurface.BearingToDirection(IlmeniteBearing);
            anchors.Add(Make(surface, WorldAnchorIds.PlaceIlmeniteBand, TerracesFoot(surface, band), band,
                TerracesRadius));
            AddMicroSites(surface, anchors);
            Vector2 shadowed = LargestCrater(surface);
            anchors.Add(Make(surface, WorldAnchorIds.PlaceShadowedCrater, shadowed, shadowed, ShadowedFloorRadius));
            AddOpening(surface, anchors);

            _anchors = anchors.ToArray();
            for (int i = 0; i < _anchors.Length; i++)
            {
                _byId.Add(_anchors[i].Id, i);
            }

            Vector2 foot = canyon.ExitFoot;
            ExitFoot = new Vector3(foot.x, surface.SampleHeight(foot.x, foot.y), foot.y);
        }

        public int Count => _anchors.Length;

        /// <summary>
        /// The foot of the exit's step on the basin side (not an anchor id yet): where a crew trail marker can stand.
        /// </summary>
        public Vector3 ExitFoot { get; }

        public WorldAnchor Get(int index)
        {
            return _anchors[index];
        }

        public bool TryGet(string id, out WorldAnchor anchor)
        {
            if (id != null && _byId.TryGetValue(id, out int index))
            {
                anchor = _anchors[index];
                return true;
            }

            anchor = default;
            return false;
        }

        /// <summary>
        /// The centre of a <see cref="TerracesRadius"/> disc of floor at the foot of the rim wall along
        /// <paramref name="up"/>: the last drivable point walking out from the basin floor, pulled back by the radius.
        /// </summary>
        private static Vector2 TerracesFoot(MoonSurface surface, Vector2 up)
        {
            float last = 0f;
            for (float r = 0f; r <= surface.CrestRadius; r += TerracesSearchStep)
            {
                Vector2 point = up * r;
                if (!surface.IsDrivable(point.x, point.y))
                {
                    break;
                }

                last = r;
            }

            return up * Mathf.Max(0f, last - TerracesRadius);
        }

        /// <summary>
        /// The old vent's site: the first spot from <see cref="VentBearing"/> / <see cref="VentDistance"/> outward and
        /// round whose whole bead field is drivable and level.
        /// </summary>
        private static Vector2 VentSite(MoonSurface surface, List<WorldAnchor> placed)
        {
            for (float reach = 0f; reach <= VentSearchDistance; reach += VentSearchStep)
            {
                for (float turn = 0f; turn <= VentSearchBearing; turn += VentSearchStep)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 spot = MoonSurface.BearingToDirection(VentBearing + turn * side)
                            * (VentDistance + reach);
                        if (IsClear(spot, placed) && IsLevelDisc(surface, spot, VentRadius))
                        {
                            return spot;
                        }
                    }
                }
            }

            throw new InvalidOperationException("No level, drivable spot for the orange vent's bead field.");
        }

        private static bool IsClear(Vector2 spot, List<WorldAnchor> placed)
        {
            foreach (WorldAnchor anchor in placed)
            {
                var at = new Vector2(anchor.Position.x, anchor.Position.z);
                if (Vector2.Distance(spot, at) < anchor.Radius + VentRadius + VentClearance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLevelDisc(MoonSurface surface, Vector2 centre, float radius)
        {
            float height = surface.SampleHeight(centre.x, centre.y);
            for (int i = 0; i <= VentRingSamples; i++)
            {
                float angle = i * Mathf.PI * 2f / VentRingSamples;
                float reach = i == VentRingSamples ? radius * 0.5f : radius;
                float x = centre.x + Mathf.Cos(angle) * reach;
                float z = centre.y + Mathf.Sin(angle) * reach;
                if (!surface.IsDrivable(x, z) || Mathf.Abs(surface.SampleHeight(x, z) - height) > VentMaxRise)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The mixed micro-sites on the basin's routes (0.5.5 §1.7 M1-M3), seated on the relays and the road.</summary>
        private static void AddMicroSites(MoonSurface surface, List<WorldAnchor> anchors)
        {
            WorldAnchor mouth = Find(anchors, WorldAnchorIds.RelayPrefix + 2);
            var mouthForward = new Vector2(mouth.Forward.x, mouth.Forward.z);
            var mouthRight = new Vector2(mouthForward.y, -mouthForward.x);
            var mouthCentre = new Vector2(mouth.Position.x, mouth.Position.z);
            anchors.Add(Make(surface, WorldAnchorIds.Micro06,
                mouthCentre + mouthRight * (mouth.Radius + Micro06Side), mouthForward, Micro06Radius));

            Vector2 road = MoonSurface.BearingToDirection(MuleBearing);
            var roadLeft = new Vector2(-road.y, road.x);
            anchors.Add(Make(surface, WorldAnchorIds.MicroMule, road * MuleDistance + roadLeft * MuleSide, road,
                MuleRadius));

            WorldAnchor mound = Find(anchors, WorldAnchorIds.RelayPrefix + 0);
            var forward = new Vector2(mound.Forward.x, mound.Forward.z);
            var right = new Vector2(forward.y, -forward.x);
            var centre = new Vector2(mound.Position.x, mound.Position.z);
            Vector2 stake = centre + right * GuyStake.x + forward * GuyStake.y;
            Vector2 footing = centre + right * MastFooting.x + forward * MastFooting.y;
            anchors.Add(Make(surface, WorldAnchorIds.MicroDrone, stake, footing - stake, DroneRadius));
        }

        private static WorldAnchor Find(List<WorldAnchor> anchors, string id)
        {
            foreach (WorldAnchor anchor in anchors)
            {
                if (anchor.Id == id)
                {
                    return anchor;
                }
            }

            throw new InvalidOperationException($"The micro-sites need the world anchor '{id}' first.");
        }

        /// <summary>The centre of the basin's largest crater that is not a play bowl.</summary>
        private static Vector2 LargestCrater(MoonSurface surface)
        {
            Vector2 centre = Vector2.zero;
            float largest = 0f;
            foreach (Crater crater in surface.Craters)
            {
                if (!crater.IsPlayBowl && crater.Radius > largest)
                {
                    largest = crater.Radius;
                    centre = crater.Center;
                }
            }

            return centre;
        }

        /// <summary>
        /// Opening v2's places (docs/design/opening-v2.md §4): the wake post on the old pad, the landed lander facing
        /// its apron, the apron facing the lander, the ruins and the midway point facing on along the way out.
        /// </summary>
        private static void AddOpening(MoonSurface surface, List<WorldAnchor> anchors)
        {
            GroundShaping shaping = surface.Shaping;
            Vector2 wake = shaping.WakePost;
            Vector2 lander = shaping.Lander.Center;
            Vector2 apron = shaping.Apron.Center;
            anchors.Add(Make(surface, WorldAnchorIds.PlaceWake, wake, lander - wake, WakeRadius));
            anchors.Add(Make(surface, WorldAnchorIds.PlaceLander, lander, apron - lander, shaping.Lander.Radius));
            anchors.Add(Make(surface, WorldAnchorIds.PlaceApron, apron, lander - apron, shaping.Apron.Radius));
            Vector2 ruins = shaping.Ruins.Center;
            anchors.Add(Make(surface, WorldAnchorIds.PlaceRuins, ruins, ruins - apron, shaping.Ruins.Radius));
            Vector2 midway = shaping.MidwayCenter;
            anchors.Add(Make(surface, WorldAnchorIds.PlaceMidway, midway, midway - wake, shaping.MidwayRadius));
        }

        /// <summary>A shaped site footprint on the basin floor, facing on from home.</summary>
        private static WorldAnchor Footprint(MoonSurface surface, string id, GroundShape shape)
        {
            return Make(surface, id, shape.Center, shape.Center, shape.Radius);
        }

        private static WorldAnchor Make(MoonSurface surface, string id, Vector2 position, Vector2 forward,
            float radius)
        {
            Vector2 flat = forward.normalized;
            var ground = new Vector3(position.x, surface.SampleHeight(position.x, position.y), position.y);
            return new WorldAnchor(id, ground, new Vector3(flat.x, 0f, flat.y), radius);
        }
    }
}
