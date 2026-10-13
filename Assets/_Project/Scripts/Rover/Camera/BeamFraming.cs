using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Which beam framing runs and which beats end it, as plain logic (the camera rig only applies
    /// <see cref="Framing"/>):
    /// <list type="bullet">
    /// <item>the cut's push-in (docs/design/0.5.5-owner-feedback.md §5.6): its Anticipate says where the beam will
    /// run, and the charge's stage 0 begins the push-in only when <see cref="CutFraming.Wanted"/>;</item>
    /// <item>the dig's shoulder framing (feel-pass §3.1, as in 0.5.4) begins on its own Anticipate, slower with the
    /// Gentle camera;</item>
    /// <item>each ends only on its own kind's fold (Follow at 1) or Cancel.</item>
    /// </list>
    /// </summary>
    public sealed class BeamFraming
    {
        private readonly RoverCameraTuning _tuning;
        private Vector3 _chargePoint;

        public BeamFraming(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>The framing's side, eye and weight.</summary>
        public CutFraming Framing { get; } = new CutFraming();

        /// <summary>True while the framing running (or easing back) is the dig's.</summary>
        public bool IsDig { get; private set; }

        /// <param name="rover">07's position.</param>
        /// <param name="forward">07's facing.</param>
        /// <param name="gentle">The player's Gentle camera.</param>
        public void OnStaged(CutChargeStaged staged, Vector3 rover, Vector3 forward, bool gentle)
        {
            if (staged.Stage != 0
                || !CutFraming.Wanted(staged.Warm, staged.StagesNeeded, _tuning.CutFrameMinStages, gentle))
            {
                return;
            }

            IsDig = false;
            Framing.Begin(_chargePoint, CutFraming.ChooseSide(rover, forward, _chargePoint),
                _tuning.CutFrameInSeconds);
        }

        /// <param name="rover">07's position.</param>
        /// <param name="forward">07's facing.</param>
        /// <param name="gentle">The player's Gentle camera.</param>
        public void OnBeat(FeelBeat beat, Vector3 rover, Vector3 forward, bool gentle)
        {
            if (beat.Kind != FeelKind.Cut && beat.Kind != FeelKind.Dig)
            {
                return;
            }

            bool dig = beat.Kind == FeelKind.Dig;
            switch (beat.Phase)
            {
                case FeelPhase.Anticipate when !dig:
                    _chargePoint = beat.Position;
                    break;
                case FeelPhase.Anticipate:
                    IsDig = true;
                    Framing.Begin(beat.Position, CutFraming.ChooseSide(rover, forward, beat.Position),
                        gentle ? _tuning.DigFrameGentleSeconds : _tuning.DigFrameInSeconds);
                    break;
                case FeelPhase.Follow when beat.Strength >= 1f:
                case FeelPhase.Cancel:
                    if (Framing.IsActive && IsDig == dig)
                    {
                        Framing.End(dig ? _tuning.DigFrameOutSeconds : _tuning.CutFrameOutSeconds);
                    }

                    break;
            }
        }
    }
}
