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
    /// With a title in the game the wake is not its own: it is held asleep while the title is up, and the opening (or
    /// the short wake) drives it beat by beat (<see cref="Hold"/>, <see cref="BeginScripted"/>,
    /// <see cref="SetScriptedSleep"/>, <see cref="FinishScripted"/>).
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
        private bool _held;
        private bool _scripted;
        private bool _scriptBegan;

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

        /// <summary>True while a script drives the wake.</summary>
        public bool IsScripted => _scripted;

        /// <summary>Keeps 07 asleep: neither the settle delay nor the player's input wakes it until a script begins.</summary>
        public void Hold()
        {
            _held = Phase == WakePhase.Asleep;
        }

        /// <summary>Lets go of <see cref="Hold"/>: 07 wakes on its own again (settle delay or the player).</summary>
        public void Release()
        {
            _held = false;
        }

        /// <summary>
        /// A script takes the wake over. From asleep, the next <see cref="Step"/> reports
        /// <see cref="MoodTransition.BeganWaking"/> (the radio crackles on); <see cref="Sleep"/> then follows
        /// <see cref="SetScriptedSleep"/>.
        /// </summary>
        public void BeginScripted()
        {
            _held = false;
            _scripted = true;
            if (Phase == WakePhase.Asleep)
            {
                Phase = WakePhase.Waking;
                _scriptBegan = true;
            }
        }

        /// <summary>The script's sleep this frame (1 fast asleep .. 0 awake); ignored without a script.</summary>
        public void SetScriptedSleep(float sleep)
        {
            if (_scripted)
            {
                Sleep = Mathf.Clamp01(sleep);
            }
        }

        /// <summary>The script is done: 07 is awake, quietly (the script did its own blink).</summary>
        public void FinishScripted()
        {
            if (!_scripted)
            {
                return;
            }

            _scripted = false;
            _scriptBegan = false;
            Sleep = 0f;
            EarthLook = 0f;
            Phase = WakePhase.Awake;
        }

        /// <summary>Advances the sequence; returns the transition that happened this frame, if any.</summary>
        public MoodTransition Step(bool playerActive, float deltaTime)
        {
            if (_scripted)
            {
                bool began = _scriptBegan;
                _scriptBegan = false;
                return began ? MoodTransition.BeganWaking : MoodTransition.None;
            }

            switch (Phase)
            {
                case WakePhase.Asleep:
                    if (_held)
                    {
                        return MoodTransition.None;
                    }

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
