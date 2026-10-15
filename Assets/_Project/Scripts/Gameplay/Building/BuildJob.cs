using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Pure state machine for welding a placed structure into service and deconstructing it (0.5.7). Advances toward
    /// <see cref="BuildSeconds"/> while 07 is within <c>weldRange</c> and welding; pauses without losing progress when
    /// 07 leaves range or stops welding. Deconstruction returns a tuned share of the structure's <see cref="Recipe"/>
    /// build cost.
    /// </summary>
    public sealed class BuildJob
    {
        private const float ShareRoundingEpsilon = 1e-5f;

        private float _elapsedSeconds;

        public BuildJob(Recipe buildCost, float buildSeconds, float initialProgress01 = 0f)
        {
            if (buildSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(buildSeconds), buildSeconds,
                    "BuildSeconds must be positive.");
            }

            BuildCost = buildCost;
            BuildSeconds = buildSeconds;
            float clamped = Mathf.Clamp01(initialProgress01);
            _elapsedSeconds = clamped * buildSeconds;
            IsPaused = clamped < 1f;
        }

        public Recipe BuildCost { get; }

        public float BuildSeconds { get; }

        public float ElapsedSeconds => _elapsedSeconds;

        /// <summary>Normalized build progress in [0, 1].</summary>
        public float Progress01 => Mathf.Clamp01(_elapsedSeconds / BuildSeconds);

        /// <summary>True once weld progress has reached 100%.</summary>
        public bool IsDone => _elapsedSeconds >= BuildSeconds;

        /// <summary>True when the structure is not yet finished and is not currently being welded.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>True during a step where 07 is in weld range and actively welding an unfinished structure.</summary>
        public bool IsWeldingActive { get; private set; }

        /// <summary>
        /// Steps the build job by <paramref name="deltaTime"/> seconds given 07's world position and weld state.
        /// Returns true on the step that transitions the structure from unfinished to <see cref="IsDone"/>.
        /// </summary>
        public bool Step(
            float deltaTime,
            Vector3 structurePosition,
            Vector3 roverPosition,
            float weldRange,
            bool isWelding)
        {
            float distance = SurfaceRules.HorizontalDistance(structurePosition, roverPosition);
            return Step(deltaTime, distance, weldRange, isWelding);
        }

        /// <summary>
        /// Steps the build job by <paramref name="deltaTime"/> seconds given horizontal <paramref name="distanceTo07"/>.
        /// Returns true on the step that transitions the structure from unfinished to <see cref="IsDone"/>.
        /// </summary>
        public bool Step(float deltaTime, float distanceTo07, float weldRange, bool isWelding)
        {
            if (IsDone)
            {
                IsPaused = false;
                IsWeldingActive = false;
                return false;
            }

            bool inRange = distanceTo07 <= Mathf.Max(0f, weldRange);
            if (!inRange || !isWelding || deltaTime <= 0f)
            {
                IsPaused = true;
                IsWeldingActive = false;
                return false;
            }

            IsPaused = false;
            IsWeldingActive = true;
            _elapsedSeconds = Mathf.Min(BuildSeconds, _elapsedSeconds + deltaTime);

            if (IsDone)
            {
                IsWeldingActive = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the material refund when removing this structure: an unfinished frame (<c>!IsDone</c>) returns the
        /// full <see cref="BuildCost"/>; a finished structure (<see cref="IsDone"/>) returns <paramref name="refundShare"/>
        /// (0..1) of <see cref="BuildCost"/>.
        /// </summary>
        public Recipe Deconstruct(float refundShare)
        {
            return IsDone ? CalculateRefund(BuildCost, refundShare) : BuildCost;
        }

        /// <summary>Calculates the tuned share (0..1) of <paramref name="buildCost"/> returned on deconstruction.</summary>
        public static Recipe CalculateRefund(Recipe buildCost, float refundShare)
        {
            float share = Mathf.Clamp01(refundShare);
            return new Recipe(
                ScaleAmount(buildCost.Metal, share),
                ScaleAmount(buildCost.Wiring, share),
                ScaleAmount(buildCost.Optics, share));
        }

        private static int ScaleAmount(int cost, float share)
        {
            if (cost <= 0 || share <= 0f)
            {
                return 0;
            }

            return Mathf.FloorToInt(cost * share + ShareRoundingEpsilon);
        }
    }
}
