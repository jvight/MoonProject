using System;
using UnityEngine;

namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// The crew's belly lift as plain logic (docs/features/M3-19 §4). Dormant until home has power; then jammed and
    /// silent until 07 first comes home carrying something for the lab, when it asks for the stitch (its pad ring
    /// lights): 07 rests by the platform and stitches, and the platform eases level. From then on 07 rests on the
    /// platform to ride up into the lab (at least a breath after the stitch), where it stays until Back brings it
    /// down. <see cref="Level"/> takes the platform from its jammed pose to level;
    /// <see cref="Height"/> from the bottom stop to the lab. After a ride down 07 must leave the platform before it can
    /// board again.
    /// </summary>
    public sealed class LiftCycle
    {
        private readonly LiftTuning _tuning;
        private float _elapsed;
        private bool _mustLeave;
        private bool _arrived;

        public LiftCycle(LiftTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        public LiftPhase Phase { get; private set; }

        /// <summary>0 = the jammed pose, 1 = level with the cable taut (eased).</summary>
        public float Level { get; private set; }

        /// <summary>0 = the bottom stop, 1 = settled in the lab (eased at both ends).</summary>
        public float Height { get; private set; }

        /// <summary>How far through the ride down, 0..1 (eased like the height; 0 outside it).</summary>
        public float Descent => Phase == LiftPhase.Lowering ? 1f - Height : 0f;

        /// <summary>Whether the cable is restored.</summary>
        public bool IsRestored => Phase >= LiftPhase.Ready;

        /// <summary>Whether 07 is held on the platform (riding or in the lab).</summary>
        public bool Holds07 => Phase >= LiftPhase.Rising;

        /// <summary>Starts from a save: restored at the bottom stop, or dormant (power decides next step).</summary>
        public void Restore(bool restored)
        {
            Phase = restored ? LiftPhase.Ready : LiftPhase.Dormant;
            Level = restored ? 1f : 0f;
            Height = 0f;
            _elapsed = 0f;
            _mustLeave = false;
            _arrived = false;
        }

        public LiftStep Step(LiftInput input, float deltaTime)
        {
            _elapsed += deltaTime;
            switch (Phase)
            {
                case LiftPhase.Dormant:
                    return input.Powered ? Enter(LiftPhase.Jammed, LiftStep.Powered) : LiftStep.None;
                case LiftPhase.Jammed:
                    return input.CarryingHome ? Enter(LiftPhase.Called, LiftStep.Called) : LiftStep.None;
                case LiftPhase.Called:
                    bool rested = input.AtStitchSpot && input.StillSeconds >= _tuning.StitchDelay;
                    return rested ? Enter(LiftPhase.Stitching, LiftStep.StitchStarted) : LiftStep.None;
                case LiftPhase.Stitching:
                    Level = Ease(_elapsed, _tuning.StitchSeconds);
                    return Level >= 1f ? Enter(LiftPhase.Ready, LiftStep.Restored) : LiftStep.None;
                case LiftPhase.Ready:
                    _mustLeave &= input.OnPlatform;
                    bool boarded = !_mustLeave && input.OnPlatform && input.StillSeconds >= _tuning.BoardDelay
                        && _elapsed >= _tuning.BoardDelay;
                    return boarded ? Enter(LiftPhase.Rising, LiftStep.RideStarted) : LiftStep.None;
                case LiftPhase.Rising:
                    Height = Ease(_elapsed, _tuning.RideSeconds);
                    return Travel(LiftPhase.Inside, LiftStep.ReachedInside);
                case LiftPhase.Inside:
                    return input.Down ? Enter(LiftPhase.Lowering, LiftStep.Descending) : LiftStep.None;
                case LiftPhase.Lowering:
                    Height = 1f - Ease(_elapsed, _tuning.RideSeconds);
                    LiftStep lowered = Travel(LiftPhase.Ready, LiftStep.ReachedBottom);
                    _mustLeave |= lowered == LiftStep.ReachedBottom;
                    return lowered;
                default:
                    throw new InvalidOperationException($"Unknown lift phase {Phase}.");
            }
        }

        /// <summary>
        /// Arrives at the stop once the ride's time is up, then settles there before entering <paramref name="next"/>.
        /// </summary>
        private LiftStep Travel(LiftPhase next, LiftStep settled)
        {
            if (!_arrived && _elapsed >= _tuning.RideSeconds)
            {
                _arrived = true;
                return LiftStep.Arrived;
            }

            return _arrived && _elapsed >= _tuning.RideSeconds + _tuning.SettleSeconds
                ? Enter(next, settled)
                : LiftStep.None;
        }

        private LiftStep Enter(LiftPhase phase, LiftStep step)
        {
            Phase = phase;
            _elapsed = 0f;
            _arrived = false;
            return step;
        }

        private static float Ease(float elapsed, float duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            return t * t * (3f - 2f * t);
        }
    }
}
