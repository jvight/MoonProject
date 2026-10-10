using System;
using UnityEngine;

namespace MoonProject.Gameplay.Lift
{
    /// <summary>
    /// The crew's lift as plain logic (docs/features/M3-16 step 8). Dormant until the tower reaches its third level;
    /// then 07 rests on the feed pad to feed the winch, stitches it (the platform eases level and down to the bottom
    /// stop), and from then on rests on the platform to ride up to the deck lookout, any input bringing it down.
    /// <see cref="Level"/> takes the platform from its jammed pose to level at the bottom; <see cref="Height"/> from
    /// the bottom stop to the top one. After a ride down 07 must leave the platform before it can board again.
    /// </summary>
    public sealed class LiftCycle
    {
        private readonly LiftTuning _tuning;
        private float _elapsed;
        private bool _mustLeave;

        public LiftCycle(LiftTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        public LiftPhase Phase { get; private set; }

        /// <summary>0 = the jammed pose, 1 = level at the bottom stop with the cable taut (eased).</summary>
        public float Level { get; private set; }

        /// <summary>0 = the bottom stop, 1 = the top stop (eased at both ends).</summary>
        public float Height { get; private set; }

        /// <summary>Whether the winch works (the cable is restored and the lamp lit).</summary>
        public bool IsRestored => Phase >= LiftPhase.Ready;

        /// <summary>Whether 07 is held still on the platform (riding or at the lookout).</summary>
        public bool Holds07 => Phase >= LiftPhase.Rising;

        /// <summary>Starts from a save: restored at the bottom stop, or dormant (the power decides on the next step).</summary>
        public void Restore(bool restored)
        {
            Phase = restored ? LiftPhase.Ready : LiftPhase.Dormant;
            Level = restored ? 1f : 0f;
            Height = 0f;
            _elapsed = 0f;
            _mustLeave = false;
        }

        public LiftCue Step(LiftInput input, float deltaTime)
        {
            _elapsed += deltaTime;
            switch (Phase)
            {
                case LiftPhase.Dormant:
                    return input.Powered ? Enter(LiftPhase.Jammed, LiftCue.Powered) : LiftCue.None;
                case LiftPhase.Jammed:
                    bool rested = input.OnFeedPad && input.StillSeconds >= _tuning.FeedDelay;
                    return rested && input.Affordable ? Enter(LiftPhase.Feeding, LiftCue.FeedStarted) : LiftCue.None;
                case LiftPhase.Feeding:
                    return input.FeedDone ? Enter(LiftPhase.Stitching, LiftCue.None) : LiftCue.None;
                case LiftPhase.Stitching:
                    Level = Ease(_elapsed, _tuning.StitchSeconds);
                    return Level >= 1f ? Enter(LiftPhase.Ready, LiftCue.Restored) : LiftCue.None;
                case LiftPhase.Ready:
                    _mustLeave &= input.OnPlatform;
                    bool boarded = !_mustLeave && input.OnPlatform && input.StillSeconds >= _tuning.BoardDelay;
                    return boarded ? Enter(LiftPhase.Rising, LiftCue.RideStarted) : LiftCue.None;
                case LiftPhase.Rising:
                    Height = Ease(_elapsed, _tuning.RideSeconds);
                    return Settled() ? Enter(LiftPhase.Top, LiftCue.ReachedTop) : LiftCue.None;
                case LiftPhase.Top:
                    return input.AnyInput ? Enter(LiftPhase.Lowering, LiftCue.Descending) : LiftCue.None;
                case LiftPhase.Lowering:
                    Height = 1f - Ease(_elapsed, _tuning.RideSeconds);
                    if (!Settled())
                    {
                        return LiftCue.None;
                    }

                    _mustLeave = true;
                    return Enter(LiftPhase.Ready, LiftCue.ReachedBottom);
                default:
                    throw new InvalidOperationException($"Unknown lift phase {Phase}.");
            }
        }

        private bool Settled()
        {
            return _elapsed >= _tuning.RideSeconds + _tuning.SettleSeconds;
        }

        private LiftCue Enter(LiftPhase phase, LiftCue cue)
        {
            Phase = phase;
            _elapsed = 0f;
            return cue;
        }

        private static float Ease(float elapsed, float duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            return t * t * (3f - 2f * t);
        }
    }
}
