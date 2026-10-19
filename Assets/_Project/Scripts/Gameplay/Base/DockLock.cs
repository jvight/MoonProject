using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The dock locks 07 in and lets it go when full (docs/design/feel-pass.md §7.1), as numbers the dock shows:
    /// <list type="bullet">
    /// <item>Roll-on: while 07 rolls onto the pad the rim lamps chase toward the centre (<see cref="RimLevel"/>).</item>
    /// <item>Lock: when 07 docks to charge, four walls rise staggered with a small overshoot, then the clamp.</item>
    /// <item>Fill: each whole cell lights its share of the strips (4 cells = one wall each; more subdivide them).</item>
    /// <item>Full: the strips sweep from cyan to gold (<see cref="Sweep"/>), then the walls lower, staggered.</item>
    /// <item>Drive-off before full: the walls drop at once; nothing is lost (the battery keeps its partial cells).</item>
    /// <item>The rest 07 wakes into (a loaded game, or a fresh one on its dock) shows the state silently.</item>
    /// </list>
    /// Each beat is a <see cref="FeelBeat"/> of <see cref="FeelKind.Dock"/>: Anticipate as the walls start rising
    /// (Seconds = until the clamp; the rim chase on roll-on is the dock's own), Impact at the
    /// clamp (strength = how empty 07 arrived), Act per cell (strength = charge), Reward when full, Cancel on an early
    /// drive-off. Plain logic, stepped by <see cref="ChargingDock"/>.
    /// </summary>
    public sealed class DockLock
    {
        /// <summary>One wall per side of the pad.</summary>
        public const int Walls = 4;

        /// <summary>Rings of rim lamps, outer to inner.</summary>
        public const int RimRings = 3;

        /// <summary>Segments of each wall's lamp band: twelve round the ring, an equal block per cell.</summary>
        public const int SegmentsPerWall = 3;

        /// <summary>Segments round the whole ring.</summary>
        public const int Segments = Walls * SegmentsPerWall;

        /// <summary>The middle segment of a wall's band, which lights first.</summary>
        public const int CentreSegment = SegmentsPerWall / 2;

        private enum Stage
        {
            Open,
            Locking,
            Locked,
            Releasing,
            Dropping,
        }

        private readonly EventBus _events;
        private readonly BaseTuning _tuning;
        private readonly float[] _heights = new float[Walls];
        private readonly float[] _dropFrom = new float[Walls];
        private readonly float[] _strips = new float[Walls];
        private readonly float[] _rims = new float[RimRings];
        private Stage _stage;
        private float _time;
        private float _chase;
        private float _arrival;
        private int _litCells;
        private int _cells = 1;
        private bool _wasDocked;
        private bool _rolledOn;
        private bool _bootRest = true;

        public DockLock(EventBus events, BaseTuning tuning)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>True from the walls starting to rise until they are down again.</summary>
        public bool Raised => _stage != Stage.Open;

        /// <summary>The lock stands as 07 woke into it (a loaded game): shown at once, with no beats and no motion.
        /// </summary>
        public bool Silent { get; private set; }

        /// <summary>
        /// The session starts: 07 wakes on the dock's pad (a loaded game: the lock then shows at once, silently) or
        /// off it (the first roll-on then locks with its beats).
        /// </summary>
        public void Wake(bool onPad)
        {
            _bootRest = onPad;
        }

        /// <summary>True while 07 is held between the risen walls, charging.</summary>
        public bool Locked => _stage == Stage.Locked;

        /// <summary>How far the strips have swept from the charging tint to the full one, 0..1.</summary>
        public float Sweep { get; private set; }

        /// <summary>Wall <paramref name="wall"/>'s rise, 0 (sunk in its slot) to 1 (up; a little over while rising).</summary>
        public float WallHeight(int wall)
        {
            return _heights[wall];
        }

        /// <summary>How much of wall <paramref name="wall"/>'s strip its charged cells light, 0..1.</summary>
        public float StripFill(int wall)
        {
            return _strips[wall];
        }

        /// <summary>
        /// How far the ring has filled, 0..1: the whole charged cells over the battery's cells. Every wall's band shows
        /// it at once, growing from the wall's centre outward.
        /// </summary>
        public float RingFill => (float)_litCells / Mathf.Max(1, _cells);

        /// <summary>Rim ring <paramref name="ring"/>'s share of the roll-on chase, 0..1.</summary>
        public float RimLevel(int ring)
        {
            return _rims[ring];
        }

        /// <param name="onPad">07 is on the dock's pad.</param>
        /// <param name="docked">07 rests on the dock (it charges there).</param>
        /// <param name="charge">The battery's charge in cells.</param>
        /// <param name="cells">The battery's capacity in cells.</param>
        public void Step(bool onPad, bool docked, float charge, int cells, Vector3 dock, float deltaTime)
        {
            _time += deltaTime;
            bool full = charge >= cells;
            if (!onPad)
            {
                _bootRest = false;
                _rolledOn = false;
            }

            StepRim(onPad && !docked, dock, deltaTime);
            if (docked && !_wasDocked)
            {
                OnDocked(charge, cells, full, dock);
            }
            else if (!docked && _wasDocked)
            {
                OnUndocked(dock);
            }

            _wasDocked = docked;
            switch (_stage)
            {
                case Stage.Locking:
                    StepLocking(charge, cells, dock);
                    break;
                case Stage.Locked:
                    StepLocked(charge, cells, full, dock);
                    break;
                case Stage.Releasing:
                    StepReleasing();
                    break;
                case Stage.Dropping:
                    StepDropping();
                    break;
            }
        }

        private void OnDocked(float charge, int cells, bool full, Vector3 dock)
        {
            if (full)
            {
                return;
            }

            _arrival = Mathf.Clamp01(charge / Mathf.Max(1, cells));
            _litCells = Mathf.FloorToInt(charge);
            Sweep = 0f;
            FillStrips(_litCells, cells);
            if (_bootRest)
            {
                // The rest 07 wakes into: the walls already stand round it, lit as its cells say, without a beat.
                _bootRest = false;
                SetAll(_heights, 1f);
                Enter(Stage.Locked);
                Silent = true;
                return;
            }

            Enter(Stage.Locking);
            _events.Publish(new FeelBeat(FeelKind.Dock, FeelPhase.Anticipate, dock, Vector3.up, 1f - _arrival,
                (Walls - 1) * _tuning.WallStagger + _tuning.WallRiseSeconds));
        }

        private void OnUndocked(Vector3 dock)
        {
            if (_stage != Stage.Locking && _stage != Stage.Locked)
            {
                return;
            }

            Array.Copy(_heights, _dropFrom, Walls);
            Enter(Stage.Dropping);
            Publish(FeelPhase.Cancel, dock, _arrival);
        }

        private void StepRim(bool rolling, Vector3 dock, float deltaTime)
        {
            if (rolling && !_rolledOn && _stage == Stage.Open)
            {
                _rolledOn = true;
                _chase = 0f;
            }

            _chase += deltaTime;
            float head = rolling ? _chase / _tuning.RimChaseSeconds * RimRings % RimRings : -RimRings;
            for (int ring = 0; ring < RimRings; ring++)
            {
                _rims[ring] = Mathf.Clamp01(1f - Mathf.Abs(head - ring));
            }
        }

        private void StepLocking(float charge, int cells, Vector3 dock)
        {
            float rise = _tuning.WallRiseSeconds;
            for (int wall = 0; wall < Walls; wall++)
            {
                float t = Mathf.Clamp01((_time - wall * _tuning.WallStagger) / rise);
                _heights[wall] = Ease.OutBack(t, _tuning.WallOvershoot);
            }

            if (_time < (Walls - 1) * _tuning.WallStagger + rise)
            {
                return;
            }

            SetAll(_heights, 1f);
            Enter(Stage.Locked);
            Publish(FeelPhase.Impact, dock, 1f - _arrival);
            StepLocked(charge, cells, charge >= cells, dock);
        }

        private void StepLocked(float charge, int cells, bool full, Vector3 dock)
        {
            int whole = Mathf.Min(cells, Mathf.FloorToInt(charge));
            while (_litCells < whole)
            {
                _litCells++;
                Publish(FeelPhase.Act, dock, Mathf.Clamp01((float)_litCells / Mathf.Max(1, cells)));
            }

            FillStrips(_litCells, cells);
            if (!full)
            {
                return;
            }

            Enter(Stage.Releasing);
            Publish(FeelPhase.Reward, dock, 1f);
        }

        private void StepReleasing()
        {
            float sweep = _tuning.FullSweepSeconds;
            Sweep = Mathf.Clamp01(_time / sweep);
            float lower = _tuning.WallLowerSeconds;
            for (int wall = 0; wall < Walls; wall++)
            {
                float t = Mathf.Clamp01((_time - sweep - wall * _tuning.WallStagger) / lower);
                _heights[wall] = 1f - t * t;
            }

            if (_time >= sweep + (Walls - 1) * _tuning.WallStagger + lower)
            {
                SetAll(_heights, 0f);
                Enter(Stage.Open);
            }
        }

        private void StepDropping()
        {
            float t = Mathf.Clamp01(_time / _tuning.WallDropSeconds);
            for (int wall = 0; wall < Walls; wall++)
            {
                _heights[wall] = _dropFrom[wall] * (1f - t * t);
            }

            if (t >= 1f)
            {
                Enter(Stage.Open);
            }
        }

        /// <summary>Each whole cell lights its share of the strips, wall by wall round the pad.</summary>
        private void FillStrips(int lit, int cells)
        {
            _cells = Mathf.Max(1, cells);
            float perWall = Mathf.Max(1, cells) / (float)Walls;
            for (int wall = 0; wall < Walls; wall++)
            {
                _strips[wall] = Mathf.Clamp01((lit - wall * perWall) / perWall);
            }
        }

        private void Enter(Stage stage)
        {
            _stage = stage;
            _time = 0f;
            Silent = false;
        }

        private void Publish(FeelPhase phase, Vector3 dock, float strength)
        {
            _events.Publish(new FeelBeat(FeelKind.Dock, phase, dock, Vector3.up, strength, 0f));
        }

        private static void SetAll(float[] values, float value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = value;
            }
        }
    }
}
