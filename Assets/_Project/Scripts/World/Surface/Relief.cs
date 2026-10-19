using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The basin floor's gentle relief (<see cref="ReliefSettings"/>): broad swells and swales and long wrinkle ridges,
    /// faded to level over the opening's sites and lanes and round every crater, and the shallow furrows of rolled
    /// boulders. Every shape is
    /// C1 and broad against 07's size, so the floor stays drivable. Immutable and thread-safe.
    /// </summary>
    public sealed class Relief
    {
        private const uint SwellSalt = 0x3C6EF372u;
        private const uint RidgeSalt = 0xA54FF53Au;

        // The crest rides this share of the arch's half width off the ridge's line, as a mare ridge's crest does: near
        // the arch's top, so the two flanks' slopes do not stack.
        private const float CrestShift = 0.1f;

        // Share of a furrow's length over which it fades in at the top and out under the boulder.
        private const float TrackFade = 0.2f;

        private readonly float _swellHeight;
        private readonly float _invSwellWavelength;
        private readonly GradientNoise _swellNoise;
        private readonly GradientNoise _ridgeNoise;
        private readonly WrinkleRidge[] _ridges;
        private readonly Vector2[] _ridgeAlong;
        private readonly BoulderTrack[] _tracks;
        private readonly Vector2[] _keepFrom;
        private readonly Vector2[] _keepTo;
        private readonly float[] _keepLevel;
        private readonly float[] _keepReachSq;
        private readonly float _fade;

        // The keep-level lists hold the opening's sites first, then its protected lanes, then the craters.
        private readonly int _openingSites;
        private readonly int _openingKeeps;

        /// <param name="craters">The floor's craters: the relief gives way round each, so their walls stay as gentle
        /// as they were.</param>
        public Relief(ReliefSettings settings, SiteSettings sites, float padRadius, IReadOnlyList<Crater> craters,
            uint seed)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (sites == null)
            {
                throw new ArgumentNullException(nameof(sites));
            }

            if (craters == null)
            {
                throw new ArgumentNullException(nameof(craters));
            }

            _swellHeight = settings.SwellHeight;
            _invSwellWavelength = 1f / settings.SwellWavelength;
            _swellNoise = new GradientNoise(Hashing.Mix(seed ^ SwellSalt));
            _ridgeNoise = new GradientNoise(Hashing.Mix(seed ^ RidgeSalt));
            _ridges = settings.Ridges ?? Array.Empty<WrinkleRidge>();
            _ridgeAlong = new Vector2[_ridges.Length];
            for (int i = 0; i < _ridges.Length; i++)
            {
                _ridgeAlong[i] = MoonSurface.BearingToDirection(_ridges[i].Bearing);
            }

            _tracks = settings.Tracks ?? Array.Empty<BoulderTrack>();
            _fade = settings.ProtectFade;

            // The opening's sites (their footprints plus a margin) and the protected lanes, as lanes of no length.
            float margin = settings.SiteMargin;
            ProtectedLane[] lanes = settings.ProtectedLanes ?? Array.Empty<ProtectedLane>();
            var spots = new[]
            {
                (Vector2.zero, padRadius),
                (sites.ApronCenter, sites.ApronRadius),
                (sites.LanderCenter, sites.LanderRadius),
                (sites.RuinsCenter, sites.RuinsRadius + sites.RuinsBlend),
                (sites.ScoutCraterCenter, sites.ScoutCraterRadius),
                (sites.MidwayCenter, sites.MidwayRadius),
            };
            int count = spots.Length + lanes.Length + craters.Count;
            _openingSites = spots.Length;
            _openingKeeps = spots.Length + lanes.Length;
            _keepFrom = new Vector2[count];
            _keepTo = new Vector2[count];
            _keepLevel = new float[count];
            _keepReachSq = new float[count];
            for (int i = 0; i < spots.Length; i++)
            {
                _keepFrom[i] = spots[i].Item1;
                _keepTo[i] = spots[i].Item1;
                _keepLevel[i] = spots[i].Item2 + margin;
            }

            for (int i = 0; i < lanes.Length; i++)
            {
                _keepFrom[spots.Length + i] = lanes[i].From;
                _keepTo[spots.Length + i] = lanes[i].To;
                _keepLevel[spots.Length + i] = lanes[i].HalfWidth;
            }

            for (int i = 0; i < craters.Count; i++)
            {
                int at = spots.Length + lanes.Length + i;
                _keepFrom[at] = craters[i].Center;
                _keepTo[at] = craters[i].Center;
                _keepLevel[at] = craters[i].Radius;
            }

            for (int i = 0; i < count; i++)
            {
                float reach = _keepLevel[i] + _fade;
                _keepReachSq[i] = reach * reach;
            }
        }

        /// <summary>
        /// The swells' and wrinkle ridges' height at (x, z), metres, faded to level over the protected sites and
        /// lanes; the floor calms it on crater walls, as it does the dunes.
        /// </summary>
        public float Rise(float x, float z)
        {
            var point = new Vector2(x, z);
            float level = Level(point);
            if (level <= 0f)
            {
                return 0f;
            }

            float rise = _swellHeight * _swellNoise.Fractal(x * _invSwellWavelength, z * _invSwellWavelength, 2, 2f,
                0.45f);
            for (int i = 0; i < _ridges.Length; i++)
            {
                rise += Ridge(i, point);
            }

            return level * rise;
        }

        /// <summary>How deep the rolled boulders' furrows cut the ground at (x, z), metres (0 off them).</summary>
        public float Furrow(float x, float z)
        {
            return Furrows(new Vector2(x, z));
        }

        /// <summary>How much relief stands at <paramref name="point"/>: 0 over a protected site or lane, 1 clear of all.
        /// </summary>
        public float Level(Vector2 point)
        {
            float level = 1f;
            for (int i = 0; i < _keepFrom.Length && level > 0f; i++)
            {
                float distanceSq = DistanceToSegmentSq(point, _keepFrom[i], _keepTo[i]);
                if (distanceSq < _keepReachSq[i])
                {
                    level *= SmoothMath.Smootherstep(_keepLevel[i], _keepLevel[i] + _fade,
                        (float)Math.Sqrt(distanceSq));
                }
            }

            return level;
        }

        /// <summary>
        /// True where <paramref name="point"/> lies on one of the opening's sites (the pad, apron, lander, ruins, Scout
        /// crater and midway, each with its margin), short of the relief's fade round them: ground kept smooth to build
        /// on.
        /// </summary>
        public bool OnOpeningSite(Vector2 point)
        {
            for (int i = 0; i < _openingSites; i++)
            {
                if (DistanceToSegmentSq(point, _keepFrom[i], _keepTo[i]) < _keepLevel[i] * _keepLevel[i])
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Distance from <paramref name="point"/> to the nearest protected lane's centre line, metres.</summary>
        public float DistanceToProtectedLanes(Vector2 point)
        {
            float best = float.MaxValue;
            for (int i = _openingSites; i < _openingKeeps; i++)
            {
                best = Mathf.Min(best, DistanceToSegmentSq(point, _keepFrom[i], _keepTo[i]));
            }

            return Mathf.Sqrt(best);
        }

        private float Ridge(int index, Vector2 point)
        {
            WrinkleRidge ridge = _ridges[index];
            Vector2 along = _ridgeAlong[index];
            Vector2 local = point - ridge.Start;
            float s = Vector2.Dot(local, along);
            if (s <= 0f || s >= ridge.Length)
            {
                return 0f;
            }

            float lateral = along.x * local.y - along.y * local.x;
            float reach = ridge.Sinuosity + ridge.ArchHalfWidth + ridge.ArchHalfWidth * CrestShift;
            if (Mathf.Abs(lateral) >= reach)
            {
                return 0f;
            }

            float wander = ridge.Sinuosity * _ridgeNoise.Sample(s / ridge.SinuosityWavelength, index * 7.31f);
            float off = lateral - wander;
            float ends = SmoothMath.Smootherstep(0f, ridge.Taper, s)
                         * SmoothMath.Smootherstep(0f, ridge.Taper, ridge.Length - s);
            float arch = ridge.ArchHeight * SmoothMath.Bump(off / ridge.ArchHalfWidth);
            float fromCrest = off - ridge.ArchHalfWidth * CrestShift;
            bool lee = ridge.SteepToLeft ? fromCrest > 0f : fromCrest < 0f;
            float crest = ridge.CrestHeight
                          * SmoothMath.Bump(fromCrest / (lee ? ridge.CrestSteepHalfWidth : ridge.CrestHalfWidth));
            return ends * (arch + crest);
        }

        private float Furrows(Vector2 point)
        {
            float depth = 0f;
            for (int i = 0; i < _tracks.Length; i++)
            {
                BoulderTrack track = _tracks[i];
                Vector2 run = track.End - track.Start;
                float lengthSq = run.sqrMagnitude;
                if (lengthSq <= 0f)
                {
                    continue;
                }

                float t = Vector2.Dot(point - track.Start, run) / lengthSq;
                if (t <= 0f || t >= 1f)
                {
                    continue;
                }

                float across = Vector2.Distance(point, track.Start + run * t);
                float half = track.Width * 0.5f;
                if (across >= half)
                {
                    continue;
                }

                float ends = SmoothMath.Smootherstep(0f, TrackFade, t) * SmoothMath.Smootherstep(0f, TrackFade, 1f - t);
                depth = Mathf.Max(depth, track.Depth * ends * SmoothMath.Bump(across / half));
            }

            return depth;
        }

        private static float DistanceToSegmentSq(Vector2 point, Vector2 from, Vector2 to)
        {
            float runX = to.x - from.x;
            float runZ = to.y - from.y;
            float offX = point.x - from.x;
            float offZ = point.y - from.y;
            float lengthSq = runX * runX + runZ * runZ;
            float t = lengthSq > 0f ? Mathf.Clamp01((offX * runX + offZ * runZ) / lengthSq) : 0f;
            float dx = offX - runX * t;
            float dz = offZ - runZ * t;
            return dx * dx + dz * dz;
        }
    }
}
