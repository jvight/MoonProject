using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// "Waking 07" (design ruling 8): the session opens with 07 asleep (lid shut, eye dark, head bowed). Once the scene
    /// has settled for WakeDelay seconds it wakes slowly: the eye opens and glows up over the first part of the
    /// sequence, the head lifts toward Earth in the middle and returns to normal at the end. Any drive input wakes it
    /// at once (a short eased hurry, never a snap), so control is never blocked.
    /// <list type="bullet">
    /// <item>Asleep -> Waking: after the delay, or on player input (<see cref="WokenByPlayer"/>).</item>
    /// <item>Waking -> Awake: when the slow sequence completes, or when the hurried fade has finished.</item>
    /// </list>
    /// </summary>
    public sealed class WakeUpSequence
    {
        /// <summary>Longest frame counted toward the delay, so a loading hitch cannot skip the quiet moment.</summary>
        private const float MaxSettleStep = 0.1f;

        private const float AwakeEpsilon = 1e-3f;

        private readonly RoverCharacterTuning _tuning;
        private float _settled;
        private float _progress;
        private bool _hurried;

        public WakeUpSequence(RoverCharacterTuning tuning, bool startAsleep)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            Phase = startAsleep ? WakePhase.Asleep : WakePhase.Awake;
            Sleep = startAsleep ? 1f : 0f;
        }

        public WakePhase Phase { get; private set; }

        /// <summary>1 = fast asleep (lid shut, eye dark, head bowed), 0 = awake.</summary>
        public float Sleep { get; private set; }

        /// <summary>How much the waking 07 looks up toward Earth, 0..1.</summary>
        public float EarthLook { get; private set; }

        /// <summary>True when the player's input woke 07 before it woke on its own.</summary>
        public bool WokenByPlayer { get; private set; }

        /// <summary>Advances the sequence; returns the transition that happened this frame, if any.</summary>
        public MoodTransition Step(bool playerActive, float deltaTime)
        {
            switch (Phase)
            {
                case WakePhase.Asleep:
                    _settled += Mathf.Min(deltaTime, MaxSettleStep);
                    if (!playerActive && _settled < _tuning.WakeDelay)
                    {
                        return MoodTransition.None;
                    }

                    Phase = WakePhase.Waking;
                    WokenByPlayer = playerActive;
                    _hurried = playerActive;
                    Advance(deltaTime);
                    return MoodTransition.BeganWaking;

                case WakePhase.Waking:
                    _hurried |= playerActive;
                    return Advance(deltaTime) ? MoodTransition.FinishedWaking : MoodTransition.None;

                default:
                    return MoodTransition.None;
            }
        }

        /// <summary>Returns true when 07 is fully awake after this step.</summary>
        private bool Advance(float deltaTime)
        {
            if (_hurried)
            {
                Sleep = Smoothing.Damp(Sleep, 0f, _tuning.HurriedWakeHalfLife, deltaTime);
                EarthLook = Smoothing.Damp(EarthLook, 0f, _tuning.HurriedWakeHalfLife, deltaTime);
                if (Sleep > AwakeEpsilon || EarthLook > AwakeEpsilon)
                {
                    return false;
                }
            }
            else
            {
                _progress = Mathf.Min(1f, _progress + deltaTime / _tuning.WakeDuration);
                Sleep = 1f - Smoothing.SmoothStep(0f, _tuning.WakeEyesOpenBy, _progress);
                EarthLook = Smoothing.SmoothStep(_tuning.WakeGlanceFrom, _tuning.WakeEyesOpenBy, _progress)
                    * (1f - Smoothing.SmoothStep(_tuning.WakeGlanceUntil, 1f, _progress));
                if (_progress < 1f)
                {
                    return false;
                }
            }

            Sleep = 0f;
            EarthLook = 0f;
            Phase = WakePhase.Awake;
            return true;
        }
    }
}
