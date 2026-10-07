using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Whispering Canyon carved into the moon surface: the main corridor (mouth, take-off ramp, chasm, landing apron,
    /// a winding climb to the terminus, alcoves and the glinting ledge) and the one-way exit corridor (a shelf ending
    /// in a step down to the basin). Each corridor is a trench (floor plus sheer walls) unioned with the world by a
    /// smooth minimum, and banks that raise the walls where the rim is low, by a smooth maximum; both fade in over
    /// the mouths, so the surface stays C1. Immutable and thread-safe.
    /// </summary>
    public sealed class Canyon
    {
        /// <summary>
        /// Half the depth (along the canyon) of the rock slabs that make the gate faces vertical: wide enough to
        /// swallow every mesh triangle the terrain step can produce (2 m cells, jitter, any heading).
        /// </summary>
        public const float SlabHalfDepth = 3.6f;

        private const float Lead = 26f;
        private const float MouthBlend = 24f;
        private const int MouthSamples = 9;

        // Walls start rising just after the floor has switched in, and reach full height this far into the corridor.
        private const float MainWallRamp = 14f;
        private const float ExitWallRamp = 6f;
        private const float StepLength = 1f;
        private const float Rounding = 1f;
        private const float WallOvershoot = 40f;
        private const float FlankDrop = 60f;
        private const float FarAway = 2000f;
        private const float ReachMargin = 4f;
        private const float EdgeRise = 0.4f;
        private const float EdgeMargin = 1f;
        private const float ClimbEase = 20f;
        private const float NarrowLength = 20f;
        private const float BayWidth = 7f;
        private const float BayHalfLength = 14f;
        private const float LedgeRun = 12f;

        // The relay ledge is taller than the glinting one, so its slopes run longer to stay a gentle climb (<= 20 deg).
        private const float RelayLedgeRun = 28f;
        private const float ExitMouthHalfWidth = 9f;
        private const float ExitEase = 4f;
        private const float RoughnessWavelength = 18f;
        private const uint RoughnessSalt = 0x4A7C15F3u;

        // Gate slabs: one piece per gate (seams between pieces read as masonry), buried this far below the low side,
        // reaching this far into the walls, its top a hair above the high side so the terrain never pokes through.
        private const float SlabBury = 1.5f;
        private const float SlabWallReach = 1f;
        private const float SlabTopLift = 0.02f;

        // Gate posts flank the exit step on the basin side: blocks standing this far into the corridor from each
        // wall foot, reaching this far out in front of the step's slab and this far above the shelf. A lunar run-up
        // carries 07 a couple of metres up the rounded foot of a wall; the posts' sheer faces leave nothing to ride
        // up onto the shelf beside the step.
        private const float PostInset = 1f;
        private const float PostLength = 6f;
        private const float PostRise = 2f;

        private readonly CanyonSettings _settings;
        private readonly CanyonPath _main;
        private readonly CanyonPath _exit;
        private readonly GradientNoise _roughness;
        private readonly float _mainBase;
        private readonly float _exitBase;
        private readonly float _lip;
        private readonly float _far;
        private readonly float _apronEnd;
        private readonly float _climbEnd;
        private readonly float _sideLength;
        private readonly float _exitLength;
        private readonly float _exitFoot;
        private readonly float _ledgeArc;
        private readonly float _ledgeSide;
        private readonly Vector2 _ledgeCenter;
        private readonly Vector2 _relayLedgeCenter;
        private readonly float[] _alcoveArcs;
        private readonly float[] _alcoveSides;
        private readonly float _mainReach;
        private readonly float _exitReach;

        /// <param name="baseHeight">The world height without the canyon (sets the floor height at the mouths).</param>
        public Canyon(CanyonSettings settings, uint seed, Func<float, float, float> baseHeight)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (baseHeight == null)
            {
                throw new ArgumentNullException(nameof(baseHeight));
            }

            _roughness = new GradientNoise(Hashing.Mix(seed ^ RoughnessSalt));
            _lip = settings.ApproachLength + settings.RampLength;
            _far = _lip + settings.Gap;
            _apronEnd = _far + settings.ApronLength;
            _climbEnd = settings.Length - settings.TerminusRadius;
            _sideLength = settings.TroughDepth / Mathf.Tan(settings.TroughSideSlope * Mathf.Deg2Rad)
                + CanyonSettings.TroughSideEase;

            float widest = Mathf.Max(settings.MouthHalfWidth, settings.ChasmHalfWidth,
                settings.CanyonHalfWidth + Mathf.Max(BayWidth, settings.AlcoveDepth), settings.TerminusRadius);
            _mainReach = widest + settings.WallRun + settings.FlankRun + ReachMargin;
            _exitReach = ExitMouthHalfWidth + settings.WallRun + settings.FlankRun + ReachMargin;

            Vector2 outward = MoonSurface.BearingToDirection(settings.Bearing);
            Vector2 mouth = outward * settings.MouthDistance;
            _main = new CanyonPath(MainLine(settings, mouth), -Lead, _mainReach);
            _mainBase = MouthHeight(_main, settings.MouthHalfWidth, baseHeight);

            List<Vector2> exitLine = ExitLine(settings, mouth, outward, _far + settings.ExitBranch);
            _exit = new CanyonPath(exitLine, -Lead, _exitReach);
            _exitLength = _exit.EndArc;
            _exitBase = MouthHeight(_exit, ExitMouthHalfWidth, baseHeight);
            _exitFoot = settings.ExitFootLength;

            _ledgeSide = settings.BendAngle >= 0f ? -1f : 1f;
            _ledgeArc = SightArc(outward);
            _ledgeCenter = _main.PointAt(_ledgeArc)
                + _main.RightAt(_ledgeArc) * (_ledgeSide * (settings.CanyonHalfWidth + BayWidth * 0.5f));
            _relayLedgeCenter = _main.PointAt(settings.RelayLedgeArc)
                + _main.RightAt(settings.RelayLedgeArc) * settings.RelayLedgeLateral;
            _alcoveArcs = new float[settings.AlcoveCount];
            _alcoveSides = new float[settings.AlcoveCount];
            float first = _ledgeArc + BayHalfLength + settings.AlcoveLength;
            float span = _climbEnd - settings.TerminusRadius - first;
            for (int i = 0; i < settings.AlcoveCount; i++)
            {
                _alcoveArcs[i] = first + span * (i + 0.5f) / settings.AlcoveCount;
                _alcoveSides[i] = i % 2 == 0 ? -_ledgeSide : _ledgeSide;
            }

            float shelf = _exitBase + settings.ExitStepHeight;
            Slabs = new[]
            {
                Slab(_main, _far, settings.ChasmHalfWidth, TroughHeight, ApronHeight),
                Slab(_exit, _exitFoot, settings.ExitHalfWidth, _exitBase, shelf),
                Post(_exit, _exitFoot, -1f, settings.ExitHalfWidth, _exitBase, shelf + PostRise),
                Post(_exit, _exitFoot, 1f, settings.ExitHalfWidth, _exitBase, shelf + PostRise),
            };

            Rect a = _main.Bounds;
            Rect b = _exit.Bounds;
            Bounds = Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax),
                Mathf.Max(a.yMax, b.yMax));
        }

        /// <summary>The tuning this canyon was carved from.</summary>
        public CanyonSettings Settings => _settings;

        /// <summary>The canyon changes the surface only inside this rectangle.</summary>
        public Rect Bounds { get; }

        /// <summary>Height of the lip, the landing apron and the exit shelf, metres.</summary>
        public float LipHeight => _mainBase + _settings.RampHeight;

        public float ApronHeight => LipHeight + _settings.ApronRaise;

        public float TroughHeight => LipHeight - _settings.TroughDepth;

        /// <summary>Horizontal distance from the lip to the front of the far face's rock slab.</summary>
        public float GapToFarFace => _settings.Gap - SlabHalfDepth;

        public CanyonPath MainPath => _main;

        public CanyonPath ExitPath => _exit;

        /// <summary>Arc length of the lip crest along the main path.</summary>
        public float LipArc => _lip;

        /// <summary>Arc length of the far face's centre along the main path.</summary>
        public float FarFaceArc => _far;

        public float ApronEndArc => _apronEnd;

        /// <summary>Arc length of the exit step's face along the exit path.</summary>
        public float ExitStepArc => _exitFoot;

        public float ExitLength => _exitLength;

        /// <summary>Where the step's foot meets the basin side: a crew trail marker can stand here.</summary>
        public Vector2 ExitFoot => _exit.PointAt(_exitFoot - SlabHalfDepth - 3f);

        /// <summary>
        /// Arc length of the glinting ledge: where the base pad's line of sight straight down the canyon meets the
        /// bay in the bend's outer wall, so its warm light reads from the base.
        /// </summary>
        public float LedgeArc => _ledgeArc;

        /// <summary>Which wall the ledge's bay is cut into: -1 left, +1 right (the bend's outer side).</summary>
        public float LedgeSide => _ledgeSide;

        public Vector2 LedgeCenter => _ledgeCenter;

        /// <summary>Centre (XZ) of the relay mast's ledge at the mouth of the terminus chamber (M3-06).</summary>
        public Vector2 RelayLedgeCenter => _relayLedgeCenter;

        /// <summary>Where the faint warm light hangs (XZ): over the glinting ledge, near the bay's back wall.</summary>
        public Vector2 GlowPoint => _ledgeCenter + _main.RightAt(_ledgeArc) * (_ledgeSide * _settings.LedgeRadius);

        public IReadOnlyList<float> AlcoveArcs => _alcoveArcs;

        public IReadOnlyList<float> AlcoveSides => _alcoveSides;

        /// <summary>
        /// The rock slabs that make both gate faces vertical (the chasm's far face, the exit step), and the posts
        /// flanking the exit step.
        /// </summary>
        public IReadOnlyList<CanyonSlab> Slabs { get; }

        /// <summary>
        /// Height at (x, z) given <paramref name="worldHeight"/> there, plus region weights: 1 on a corridor floor,
        /// and 1 in the chasm trough.
        /// </summary>
        public float Apply(float x, float z, float worldHeight, out float floorWeight, out float chasmWeight)
        {
            floorWeight = 0f;
            chasmWeight = 0f;
            float trench = float.MaxValue;
            float bank = float.MinValue;
            float rounding = 0f;
            if (_main.TryProject(x, z, out float arc, out float lateral))
            {
                Corridor(true, x, z, arc, lateral, worldHeight, ref trench, ref bank, ref rounding, out float floor);
                floorWeight = floor;
                chasmWeight = floor * SmoothMath.Smootherstep(_lip + 1f, _lip + 4f, arc)
                    * (1f - SmoothMath.Smootherstep(_far - SlabHalfDepth - 1f, _far - SlabHalfDepth, arc));
            }

            if (_exit.TryProject(x, z, out arc, out lateral))
            {
                Corridor(false, x, z, arc, lateral, worldHeight, ref trench, ref bank, ref rounding, out float floor);
                floorWeight = Mathf.Max(floorWeight, floor);
            }

            if (trench == float.MaxValue)
            {
                return worldHeight;
            }

            if (rounding <= 0f)
            {
                return Mathf.Min(Mathf.Max(worldHeight, bank), trench);
            }

            return SmoothMath.SmoothMin(SmoothMath.SmoothMax(worldHeight, bank, rounding), trench, rounding);
        }

        /// <summary>
        /// True on a corridor floor (inside the mouths, away from the gate faces), false on the canyon's walls and
        /// banks, null where the canyon has no say (the world's own rule applies).
        /// </summary>
        public bool? IsDrivable(float x, float z)
        {
            bool? result = null;
            if (_main.TryProject(x, z, out float arc, out float lateral))
            {
                result = Decide(arc, Mathf.Abs(lateral), HalfWidth(true, arc, lateral), _settings.WallRun,
                    Mathf.Abs(arc - _far) <= SlabHalfDepth + 0.5f);
            }

            if (result != true && _exit.TryProject(x, z, out arc, out lateral))
            {
                bool? exit = Decide(arc, Mathf.Abs(lateral), HalfWidth(false, arc, lateral), _settings.WallRun,
                    Mathf.Abs(arc - _exitFoot) <= SlabHalfDepth + 0.5f);
                result = exit == true ? true : result ?? exit;
            }

            return result;
        }

        /// <summary>
        /// True when (x, z) lies on a corridor's floor; <paramref name="mainCorridor"/> tells which corridor (the main
        /// one wins where they meet), <paramref name="arc"/> how far along it, and <paramref name="centre"/> how close
        /// to its centre line (0 on the line, 1 at the foot of the walls).
        /// </summary>
        public bool TryFloor(float x, float z, out bool mainCorridor, out float arc, out float centre)
        {
            mainCorridor = true;
            if (_main.TryProject(x, z, out arc, out float lateral))
            {
                float halfWidth = HalfWidth(true, arc, lateral);
                if (Mathf.Abs(lateral) < halfWidth)
                {
                    centre = Mathf.Abs(lateral) / halfWidth;
                    return true;
                }
            }

            mainCorridor = false;
            if (_exit.TryProject(x, z, out arc, out lateral))
            {
                float halfWidth = HalfWidth(false, arc, lateral);
                if (Mathf.Abs(lateral) < halfWidth)
                {
                    centre = Mathf.Abs(lateral) / halfWidth;
                    return true;
                }
            }

            centre = 1f;
            return false;
        }

        /// <summary>
        /// True when a canyon floor or wall may cross <paramref name="area"/> (they need fine mesh cells).
        /// </summary>
        public bool Touches(Rect area)
        {
            float wallReach = _settings.WallRun + ReachMargin;
            return Touches(_main, _mainReach - _settings.FlankRun - ReachMargin + wallReach, area)
                || Touches(_exit, _exitReach - _settings.FlankRun - ReachMargin + wallReach, area);
        }

        /// <summary>
        /// Floor height of the main corridor at arc length <paramref name="arc"/> (before the ledge).
        /// </summary>
        public float MainFloor(float arc)
        {
            float floor = _mainBase
                + _settings.RampHeight * SmoothMath.Smootherstep(_settings.ApproachLength, _lip, arc)
                - _settings.TroughDepth * SmoothMath.SmoothRamp(arc - _lip, _sideLength, CanyonSettings.TroughSideEase)
                + (_settings.TroughDepth + _settings.ApronRaise) * SmoothMath.Smootherstep(
                    _far - _settings.FaceRun * 0.5f, _far + _settings.FaceRun * 0.5f, arc);
            return floor + _settings.Climb * SmoothMath.SmoothRamp(arc - _apronEnd, _climbEnd - _apronEnd, ClimbEase);
        }

        /// <summary>Floor height of the exit corridor at arc length <paramref name="arc"/> from its mouth.</summary>
        public float ExitFloor(float arc)
        {
            float shelf = _exitBase + _settings.ExitStepHeight;
            return _exitBase + _settings.ExitStepHeight * SmoothMath.Smootherstep(
                    _exitFoot - _settings.FaceRun * 0.5f, _exitFoot + _settings.FaceRun * 0.5f, arc)
                + (ApronHeight - shelf) * SmoothMath.SmoothRamp(arc - _exitFoot - SlabHalfDepth,
                    _exitLength - _exitFoot - SlabHalfDepth, ExitEase);
        }

        private bool? Decide(float arc, float distance, float halfWidth, float wallRun, bool onFace)
        {
            if (arc < -MouthBlend || distance >= halfWidth + wallRun + _settings.FlankRun)
            {
                return null;
            }

            bool onFloor = distance < halfWidth - EdgeMargin;
            if (arc < 0f)
            {
                return onFloor ? (bool?)null : false;
            }

            return onFloor && !onFace;
        }

        /// <summary>
        /// Adds one corridor's trench and bank. Over the mouth everything fades with the mouth weight, the rounding
        /// included: at weight 0 trench, bank and world are equal and the union returns the world exactly. The bank
        /// always stands two roundings above the floor, so the rounding never dents the floor.
        /// </summary>
        private void Corridor(bool main, float x, float z, float arc, float lateral, float worldHeight,
            ref float trench, ref float bank, ref float rounding, out float floorWeight)
        {
            float distance = Mathf.Abs(lateral);
            float halfWidth = HalfWidth(main, arc, lateral);
            float mouth = SmoothMath.Smootherstep(-MouthBlend, 0f, arc);
            float ownRounding = Rounding * mouth;
            float floor = main ? MainFloor(arc) + Ledge(x, z) : ExitFloor(arc);
            float flooring = worldHeight + (floor - worldHeight) * mouth;
            float rising = (_settings.WallHeight + _settings.WallRoughness
                    * _roughness.Fractal(x / RoughnessWavelength, z / RoughnessWavelength, 2, 2f, 0.5f))
                * SmoothMath.Smootherstep(-1f, main ? MainWallRamp : ExitWallRamp, arc);
            float walls = ownRounding > 0f
                ? SmoothMath.SmoothMax(rising, 2f * ownRounding, ownRounding)
                : Mathf.Max(rising, 0f);
            float wallEnd = halfWidth + _settings.WallRun;
            float flankEnd = wallEnd + _settings.FlankRun;
            float reach = main ? _mainReach : _exitReach;
            float rise = SmoothMath.Smootherstep(halfWidth, wallEnd, distance);
            float edge = distance < halfWidth ? EdgeRise * Mathf.Pow(distance / halfWidth, 4f) : EdgeRise;

            float ownTrench = flooring + edge * mouth + (walls + WallOvershoot) * rise
                + FarAway * SmoothMath.Smootherstep(wallEnd, reach, distance);
            float ownBank = flooring + walls
                - (walls + FlankDrop) * SmoothMath.Smootherstep(wallEnd, flankEnd, distance)
                - FarAway * SmoothMath.Smootherstep(flankEnd, reach, distance);
            if (ownTrench < trench)
            {
                trench = ownTrench;
                rounding = ownRounding;
            }

            bank = Mathf.Max(bank, ownBank);
            floorWeight = mouth * (1f - rise);
        }

        private float HalfWidth(bool main, float arc, float lateral)
        {
            if (!main)
            {
                return _settings.ExitHalfWidth + (ExitMouthHalfWidth - _settings.ExitHalfWidth)
                    * (1f - SmoothMath.Smootherstep(-MouthBlend, _exitFoot - SlabHalfDepth, arc));
            }

            CanyonSettings s = _settings;
            float flare = 1f - SmoothMath.Smootherstep(-6f, s.ApproachLength + 4f, arc);
            float narrowing = SmoothMath.Smootherstep(_apronEnd, _apronEnd + NarrowLength, arc);
            float width = s.ChasmHalfWidth
                + (s.MouthHalfWidth - s.ChasmHalfWidth) * flare
                + (s.CanyonHalfWidth - s.ChasmHalfWidth) * narrowing
                + (s.TerminusRadius - s.CanyonHalfWidth) * SmoothMath.Smootherstep(_climbEnd - 25f, s.Length, arc);
            float side = lateral >= 0f ? 1f : -1f;
            if (side == _ledgeSide)
            {
                width += BayWidth * SmoothMath.Bump((arc - _ledgeArc) / BayHalfLength);
            }

            for (int i = 0; i < _alcoveArcs.Length; i++)
            {
                if (_alcoveSides[i] == side)
                {
                    width += s.AlcoveDepth * SmoothMath.Bump((arc - _alcoveArcs[i]) / (s.AlcoveLength * 0.5f));
                }
            }

            return width;
        }

        /// <summary>Height the main floor's two mesas add at (x, z): the glinting and the relay ledge.</summary>
        private float Ledge(float x, float z)
        {
            var point = new Vector2(x, z);
            float glinting = _settings.LedgeHeight * (1f - SmoothMath.Smootherstep(_settings.LedgeRadius,
                _settings.LedgeRadius + LedgeRun, Vector2.Distance(point, _ledgeCenter)));
            float relay = _settings.RelayLedgeHeight * (1f - SmoothMath.Smootherstep(_settings.RelayLedgeRadius,
                _settings.RelayLedgeRadius + RelayLedgeRun, Vector2.Distance(point, _relayLedgeCenter)));
            return glinting + relay;
        }

        private CanyonSlab Slab(CanyonPath path, float arc, float halfWidth, float low, float high)
        {
            Vector2 centre = path.PointAt(arc);
            Vector2 tangent = path.TangentAt(arc);
            float width = 2f * (halfWidth + _settings.WallRun + SlabWallReach);
            float bottom = low - SlabBury;
            float top = high + SlabTopLift;
            float yaw = Mathf.Atan2(tangent.x, tangent.y) * Mathf.Rad2Deg;
            return new CanyonSlab(new Vector3(centre.x, (bottom + top) * 0.5f, centre.y), yaw,
                new Vector3(width, top - bottom, 2f * SlabHalfDepth));
        }

        /// <summary>
        /// A gate post on <paramref name="side"/> of the corridor (-1 left, +1 right): from just inside the wall foot
        /// out into the wall, and from in front of the step's slab to its far end.
        /// </summary>
        private CanyonSlab Post(CanyonPath path, float faceArc, float side, float halfWidth, float low, float top)
        {
            float centreArc = faceArc - PostLength * 0.5f;
            float width = PostInset + _settings.WallRun + SlabWallReach;
            float lateral = side * (halfWidth - PostInset + width * 0.5f);
            Vector2 centre = path.PointAt(centreArc) + path.RightAt(centreArc) * lateral;
            Vector2 tangent = path.TangentAt(faceArc);
            float bottom = low - SlabBury;
            float yaw = Mathf.Atan2(tangent.x, tangent.y) * Mathf.Rad2Deg;
            return new CanyonSlab(new Vector3(centre.x, (bottom + top) * 0.5f, centre.y), yaw,
                new Vector3(width, top - bottom, 2f * SlabHalfDepth + PostLength));
        }

        /// <summary>
        /// Walks the straight line of sight from the base along the canyon's bearing until it has drifted, past the
        /// bend's start, to the middle of the ledge's bay on the outer wall, and returns the arc there.
        /// </summary>
        private float SightArc(Vector2 outward)
        {
            float bay = _settings.CanyonHalfWidth + BayWidth * 0.5f;
            float bendEnd = _settings.BendStart + _settings.BendLength;
            for (float r = _settings.MouthDistance; r < _settings.MouthDistance + bendEnd; r += StepLength)
            {
                Vector2 sight = outward * r;
                if (_main.TryProject(sight.x, sight.y, out float arc, out float lateral)
                    && arc > _settings.BendStart && lateral * _ledgeSide >= bay)
                {
                    return arc;
                }
            }

            return bendEnd;
        }

        /// <summary>
        /// Mean world height across a mouth: the corridor's floor starts level with the ground around it.
        /// </summary>
        private static float MouthHeight(CanyonPath path, float halfWidth, Func<float, float, float> baseHeight)
        {
            float sum = 0f;
            for (int i = 0; i < MouthSamples; i++)
            {
                float lateral = halfWidth * (2f * i / (MouthSamples - 1) - 1f);
                Vector2 p = path.PointAt(0f) + path.RightAt(0f) * lateral;
                sum += baseHeight(p.x, p.y);
            }

            return sum / MouthSamples;
        }

        private static bool Touches(CanyonPath path, float reach, Rect area)
        {
            for (float arc = path.StartArc; arc <= path.EndArc; arc += StepLength)
            {
                Vector2 p = path.PointAt(arc);
                float dx = Mathf.Max(area.xMin - p.x, 0f, p.x - area.xMax);
                float dz = Mathf.Max(area.yMin - p.y, 0f, p.y - area.yMax);
                if (dx * dx + dz * dz <= reach * reach)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<Vector2> MainLine(CanyonSettings settings, Vector2 mouth)
        {
            // Integrated in double precision so the straight run before the bend stays exactly straight.
            double x = mouth.x - Math.Sin(settings.Bearing * Mathf.Deg2Rad) * Lead;
            double z = mouth.y - Math.Cos(settings.Bearing * Mathf.Deg2Rad) * Lead;
            var points = new List<Vector2>();
            float bendEnd = settings.BendStart + settings.BendLength;
            int steps = Mathf.RoundToInt((settings.Length + Lead) / StepLength);
            for (int step = 0; step < steps; step++)
            {
                points.Add(new Vector2((float)x, (float)z));
                float middle = -Lead + (step + 0.5f) * StepLength;
                float wind = settings.WindAmplitude
                    * Mathf.Sin((middle - bendEnd) * Mathf.PI * 2f / settings.WindWavelength)
                    * SmoothMath.Smootherstep(bendEnd, bendEnd + settings.WindWavelength * 0.5f, middle);
                double heading = (settings.Bearing
                    + settings.BendAngle * SmoothMath.Smootherstep(settings.BendStart, bendEnd, middle) + wind)
                    * (Math.PI / 180.0);
                x += Math.Sin(heading) * StepLength;
                z += Math.Cos(heading) * StepLength;
            }

            points.Add(new Vector2((float)x, (float)z));
            return points;
        }

        /// <summary>
        /// The exit's centre line, from its mouth on the basin floor (to the left of the canyon's mouth) straight out
        /// alongside the chasm, then a quarter turn to the right that ends on the landing apron at
        /// <paramref name="joinArc"/> along the main corridor (which is straight there).
        /// </summary>
        private static List<Vector2> ExitLine(CanyonSettings settings, Vector2 mainMouth, Vector2 outward,
            float joinArc)
        {
            var left = new Vector2(-outward.y, outward.x);
            float radius = settings.ExitTurnRadius;
            float joinLateral = settings.ChasmHalfWidth - settings.ExitHalfWidth;
            float turnArc = joinArc - radius;
            Vector2 mouth = mainMouth + left * settings.ExitOffset;
            Vector2 turnCentre = mainMouth + outward * turnArc + left * (settings.ExitOffset - radius);
            var points = new List<Vector2>();
            for (float arc = -Lead; arc < turnArc; arc += StepLength)
            {
                points.Add(mouth + outward * arc);
            }

            int turnSteps = Mathf.CeilToInt(radius * Mathf.PI * 0.5f / StepLength);
            for (int i = 0; i < turnSteps; i++)
            {
                float angle = Mathf.PI * 0.5f * i / turnSteps;
                points.Add(turnCentre + left * (radius * Mathf.Cos(angle)) + outward * (radius * Mathf.Sin(angle)));
            }

            Vector2 turnEnd = turnCentre + outward * radius;
            Vector2 join = mainMouth + outward * joinArc + left * joinLateral;
            float straight = Vector2.Distance(turnEnd, join);
            for (float along = 0f; along < straight; along += StepLength)
            {
                points.Add(turnEnd - left * along);
            }

            points.Add(join);
            return points;
        }
    }
}
