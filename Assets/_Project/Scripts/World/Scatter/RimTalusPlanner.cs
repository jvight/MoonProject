using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Plans the talus of <see cref="RimTalusSettings"/> (Art's rim talus kit, small to large): round the rim, every
    /// so often a big block where the wall's slope breaks onto the floor, with middle blocks and small stones fanning
    /// out from it, more of them under steep ground; and along the canyon's and the slot's floors, only the smallest
    /// stones, tight against the walls, never in the drive line. Keeps clear of the world's anchors, the rille's pass,
    /// the Peak road and the canyon's rim, at random yaws, scales and sinkings, never in rows. Pure and deterministic.
    /// </summary>
    public sealed class RimTalusPlanner
    {
        private const uint TalusSalt = 0x1B873593u;

        // The wall's foot is read along each bearing at this step; a full turn round the rim, in radians.
        private const float FootStep = 1f;
        private const float FullTurn = 6.28318531f;

        private readonly MoonSurface _surface;
        private readonly RimTalusSettings _settings;
        private readonly IReadOnlyList<Vector2> _pieces;
        private readonly Vector3[] _clearings;
        private readonly uint _seed;

        /// <param name="pieces">Each kit piece's horizontal extent (x) and height (y), metres, small to large.</param>
        public RimTalusPlanner(MoonSurface surface, RimTalusSettings settings, IWorldAnchors anchors,
            IReadOnlyList<Vector2> pieces)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            string error = settings.Validate();
            if (error != null)
            {
                throw new ArgumentException(error, nameof(settings));
            }

            if (pieces.Count <= settings.FirstBig)
            {
                throw new ArgumentException("The rim talus kit has no big blocks.", nameof(pieces));
            }

            _clearings = new Vector3[anchors.Count];
            for (int i = 0; i < anchors.Count; i++)
            {
                WorldAnchor anchor = anchors.Get(i);
                _clearings[i] = new Vector3(anchor.Position.x, anchor.Position.z, anchor.Radius);
            }

            _seed = Hashing.Mix((uint)surface.Seed ^ TalusSalt);
        }

        public List<ScatterInstance> Plan()
        {
            var instances = new List<ScatterInstance>();
            var random = new SeededRandom(_seed);
            PlanRim(instances, ref random);
            PlanCorridor(instances, _surface.Canyon.MainPath, ref random);
            PlanCorridor(instances, _surface.Canyon.ExitPath, ref random);
            PlanCorridor(instances, _surface.Canyon.SlotPath, ref random);
            return instances;
        }

        /// <summary>Distance from the base, metres, of the wall's foot along <paramref name="direction"/>.</summary>
        public float FootDistance(Vector2 direction)
        {
            RimTalusSettings s = _settings;
            float from = _surface.FloorRadius - s.FootSearchInside;
            float to = _surface.FloorRadius + s.FootSearchOut;
            float slope = Mathf.Tan(s.FootSlope * Mathf.Deg2Rad) * FootStep;
            float previous = Height(direction, from);
            for (float r = from + FootStep; r <= to; r += FootStep)
            {
                float height = Height(direction, r);
                if (height - previous >= slope)
                {
                    return r - FootStep;
                }

                previous = height;
            }

            return to;
        }

        private void PlanRim(List<ScatterInstance> instances, ref SeededRandom random)
        {
            RimTalusSettings s = _settings;
            float bearing = random.Range(0f, FullTurn);
            float end = bearing + FullTurn;
            while (bearing < end)
            {
                var direction = new Vector2(Mathf.Sin(bearing), Mathf.Cos(bearing));
                float foot = FootDistance(direction);
                Vector2 footSpot = direction * foot;
                float spacing = random.Range(s.AnchorSpacing.x, s.AnchorSpacing.y);
                if (OnTheRim(footSpot))
                {
                    var along = new Vector2(direction.y, -direction.x);
                    float steep = Steep(direction, foot);
                    Place(instances, ref random, direction, along, foot, spacing, s.AnchorBand, s.FirstBig,
                        _pieces.Count, 1f);
                    Place(instances, ref random, direction, along, foot, spacing, s.MidBand, s.FirstMid, s.FirstBig,
                        random.Range(s.MidsPerAnchor.x, s.MidsPerAnchor.y) * steep);
                    Place(instances, ref random, direction, along, foot, spacing, s.SmallBand, 0, s.FirstMid,
                        random.Range(s.SmallsPerAnchor.x, s.SmallsPerAnchor.y) * steep);
                }

                bearing += spacing / Mathf.Max(foot, 1f);
            }
        }

        // count pieces between first (inclusive) and last (exclusive) out from the foot by band, spread along the rim
        // over spacing; a fractional count places its last piece by chance.
        private void Place(List<ScatterInstance> instances, ref SeededRandom random, Vector2 direction, Vector2 along,
            float foot, float spacing, Vector2 band, int first, int last, float count)
        {
            int whole = Mathf.FloorToInt(count);
            int pieces = whole + (random.NextFloat() < count - whole ? 1 : 0);
            for (int i = 0; i < pieces; i++)
            {
                float outFromFoot = random.Range(band.x, band.y);
                float sideways = random.Range(-0.5f, 0.5f) * spacing;
                Vector2 spot = direction * (foot - outFromFoot) + along * sideways;
                if (OnTheRim(spot))
                {
                    instances.Add(Piece(ref random, spot, random.Range(first, last)));
                }
            }
        }

        private void PlanCorridor(List<ScatterInstance> instances, CanyonPath path, ref SeededRandom random)
        {
            RimTalusSettings s = _settings;
            Canyon canyon = _surface.Canyon;
            for (float arc = 0f; arc <= path.EndArc; arc += s.CorridorSpacing * random.Range(0.6f, 1.4f))
            {
                Vector2 centre = path.PointAt(arc);
                Vector2 right = path.RightAt(arc);
                float side = random.NextFloat() < 0.5f ? -1f : 1f;
                if (!canyon.TryFloor(centre.x + right.x, centre.y + right.y, out CanyonCorridor _, out float _,
                        out float share) || share <= 0f)
                {
                    continue;
                }

                // TryFloor's share is the distance from the centre line over the half width there.
                float halfWidth = 1f / share;
                float lateral = halfWidth - random.Range(0f, s.CorridorReach);
                Vector2 spot = centre + right * (side * lateral);
                if (canyon.TryFloor(spot.x, spot.y, out CanyonCorridor _, out float _, out float _) && Clear(spot))
                {
                    instances.Add(Piece(ref random, spot, random.Range(0, s.CorridorLast + 1)));
                }
            }
        }

        private ScatterInstance Piece(ref SeededRandom random, Vector2 spot, int variant)
        {
            RimTalusSettings s = _settings;
            float scale = random.Range(s.Scale.x, s.Scale.y);
            Vector2 piece = _pieces[variant] * scale;
            var position = new Vector3(spot.x, _surface.SampleHeight(spot.x, spot.y), spot.y);
            return new ScatterInstance(ScatterKind.Talus, position, _surface.SampleNormal(spot.x, spot.y),
                random.Range(0f, 360f), piece.x, variant, default, piece.y * random.Range(s.Sink.x, s.Sink.y));
        }

        // How much denser the small talus heaps here: the steep boost under a steep wall, else 1.
        private float Steep(Vector2 direction, float foot)
        {
            RimTalusSettings s = _settings;
            float rise = Height(direction, foot + s.SteepProbe + FootStep) - Height(direction, foot + s.SteepProbe);
            float slope = Mathf.Atan2(rise, FootStep) * Mathf.Rad2Deg;
            return Mathf.Lerp(1f, s.SteepBoost, SmoothMath.Smootherstep(s.FootSlope, s.SteepSlope, slope));
        }

        // Round the rim: clear of the anchors, the canyon, the rille's pass and the Peak road.
        private bool OnTheRim(Vector2 spot)
        {
            RimTalusSettings s = _settings;
            if (_surface.Canyon.Bounds.Contains(spot) || !Clear(spot))
            {
                return false;
            }

            Vector2 rille = _surface.Rille.Direction;
            bool inRille = Vector2.Dot(spot, rille) > 0f
                && Mathf.Abs(spot.x * rille.y - spot.y * rille.x) < s.ExitClear;
            return !inRille && Vector2.Distance(spot, _surface.PeakRoadFoot) > s.ExitClear;
        }

        private bool Clear(Vector2 spot)
        {
            RimTalusSettings s = _settings;
            foreach (Vector3 clearing in _clearings)
            {
                float reach = clearing.z + s.AnchorClear;
                if ((spot - new Vector2(clearing.x, clearing.y)).sqrMagnitude < reach * reach)
                {
                    return false;
                }
            }

            return true;
        }

        private float Height(Vector2 direction, float r)
        {
            return _surface.SampleHeight(direction.x * r, direction.y * r);
        }
    }
}
