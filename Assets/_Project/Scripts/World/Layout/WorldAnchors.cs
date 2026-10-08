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
    /// sites (M3-13: site.depot, site.kestrel, site.drill, site.garage, site.lander). Every radius is flat,
    /// drivable, uncluttered ground.
    /// <para>
    /// Forward is the way 07 travels when arriving into the space, so content faces -Forward to greet it: into the
    /// canyon at the mouth and the landing, across the chasm at the lip, into the bay at the ledge and into each
    /// alcove from the corridor, into the chamber at the terminus, and down the step, out to the basin, at the exit.
    /// </para>
    /// <para>
    /// The salvage sites on the basin floor face on from home, the way 07 drives out to them; Kestrel-3's crater
    /// faces along its fall line, the way its debris trail leads 07 in (the furrow trails back from the crater along
    /// -Forward, see <see cref="MoonSurface.Kestrel"/>); the crashed lander faces into its bay off the canyon apron.
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
            anchors.Add(Footprint(surface, Drill, shaping.Drill));
            anchors.Add(Footprint(surface, Garage, shaping.Garage));
            float bayArc = settings.LanderBayArc;
            Vector2 intoBay = main.RightAt(bayArc) * canyon.LanderSide;
            float bayLateral = settings.ChasmHalfWidth + settings.LanderBayDepth - LanderRadius - LanderWallGap;
            anchors.Add(Make(surface, Lander, main.PointAt(bayArc) + intoBay * bayLateral, intoBay, LanderRadius));

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
