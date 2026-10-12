using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One hold of the beam on a piece (docs/design/feel-pass.md §3.1, §6). Engaging starts a charge-up (a shorter one
    /// when the piece is already part cut); letting go during it cancels with nothing lost. At ignition the beam cuts
    /// and the battery draws (<see cref="Draws"/>), the progress climbing in steps; letting go then pauses with the
    /// progress kept, and the next hold resumes. The cut breaks through at full progress. Each beat is published as a
    /// <see cref="FeelBeat"/> of <see cref="FeelKind.Cut"/>: Anticipate on engaging (Seconds = the charge), Act at
    /// ignition (strength 0) and at each step (strength = progress), Cancel on letting go, Impact at the break. A new
    /// hold may start the moment the last one ends, whatever its piece is still doing: it never locks the player.
    /// </summary>
    public sealed class CutState
    {
        private readonly EventBus _events;
        private readonly float _chargeSeconds;
        private readonly float _resumeChargeSeconds;
        private readonly int _steps;
        private float _chargeLeft;
        private SalvageMaterial _material;

        /// <param name="steps">The cut's progress publishes an Act beat each time it crosses a 1/steps mark.</param>
        public CutState(EventBus events, float chargeSeconds, float resumeChargeSeconds, int steps)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _chargeSeconds = chargeSeconds;
            _resumeChargeSeconds = resumeChargeSeconds;
            _steps = Math.Max(1, steps);
        }

        public CutPhase Phase { get; private set; }

        /// <summary>True while the beam is lit and cutting: the only time it draws on the battery.</summary>
        public bool Draws => Phase == CutPhase.Cutting;

        /// <summary>How far the current charge-up has come, 0..1 (0 when not charging).</summary>
        public float ChargeProgress { get; private set; }

        /// <summary>The length of the current charge-up (s).</summary>
        public float ChargeSeconds { get; private set; }

        /// <summary>
        /// 07 holds the beam on a piece of <paramref name="material"/> already cut <paramref name="progress"/> of the
        /// way: the charge-up begins (the short one when resuming).
        /// </summary>
        public void Engage(float progress, SalvageMaterial material, Vector3 point, Vector3 normal)
        {
            _material = material;
            ChargeSeconds = progress > 0f ? _resumeChargeSeconds : _chargeSeconds;
            _chargeLeft = ChargeSeconds;
            ChargeProgress = 0f;
            Phase = CutPhase.Charging;
            Publish(FeelPhase.Anticipate, point, normal, progress, ChargeSeconds);
        }

        /// <summary>The hold lets go before the break: a charge-up cancels, a cut pauses, the progress kept.</summary>
        public void Release(float progress, Vector3 point, Vector3 normal)
        {
            if (Phase == CutPhase.Idle)
            {
                return;
            }

            Phase = CutPhase.Idle;
            ChargeProgress = 0f;
            Publish(FeelPhase.Cancel, point, normal, progress, 0f);
        }

        /// <summary>
        /// Advances the hold by <paramref name="deltaTime"/>: the charge-up, then the cut of a piece that takes
        /// <paramref name="cutSeconds"/> in all. True on the frame it breaks through (the state is idle again then).
        /// </summary>
        /// <param name="ignited">True on the frame the beam ignites.</param>
        public bool Step(float deltaTime, float cutSeconds, ref float progress, Vector3 point, Vector3 normal,
            out bool ignited)
        {
            ignited = false;
            if (Phase == CutPhase.Charging)
            {
                _chargeLeft -= deltaTime;
                ChargeProgress = ChargeSeconds > 0f ? Mathf.Clamp01(1f - _chargeLeft / ChargeSeconds) : 1f;
                if (_chargeLeft > 0f)
                {
                    return false;
                }

                Phase = CutPhase.Cutting;
                ChargeProgress = 0f;
                ignited = true;
                Publish(FeelPhase.Act, point, normal, 0f, 0f);
                return false;
            }

            if (Phase != CutPhase.Cutting)
            {
                return false;
            }

            float before = progress;
            progress = Mathf.Min(1f, progress + deltaTime / Mathf.Max(0.01f, cutSeconds));
            if (progress >= 1f)
            {
                Phase = CutPhase.Idle;
                Publish(FeelPhase.Impact, point, normal, 1f, 0f);
                return true;
            }

            if (Mathf.FloorToInt(progress * _steps) > Mathf.FloorToInt(before * _steps))
            {
                Publish(FeelPhase.Act, point, normal, progress, 0f);
            }

            return false;
        }

        private void Publish(FeelPhase phase, Vector3 point, Vector3 normal, float strength, float seconds)
        {
            _events.Publish(new FeelBeat(FeelKind.Cut, phase, point, normal, strength, seconds, _material));
        }
    }
}
