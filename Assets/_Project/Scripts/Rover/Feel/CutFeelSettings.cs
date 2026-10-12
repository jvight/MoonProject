using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// How the cut feels on 07's chassis and in the gamepad (docs/design/feel-pass.md §3.1, Body and Rumble columns).
    /// The dig answers the same beats the same way.
    /// </summary>
    [Serializable]
    public sealed class CutFeelSettings
    {
        [Header("Rumble")]
        [Tooltip("Charge-up: ramps over the beat's charge length (0 -> 0.08), held until ignition.")]
        [SerializeField] private RumbleProfile _charge = new RumbleProfile(0f, 0.08f, 0f, 0.04f, 0.65f, true);

        [Tooltip("Ignition: the kick (0.25 for 90 ms).")]
        [SerializeField] private RumbleProfile _ignition = new RumbleProfile(0.25f, 0.25f, 0.25f, 0.25f, 0.09f, false);

        [Tooltip("The beam held: a steady level the Act beats retune (scaled by the sustain levels below).")]
        [SerializeField] private RumbleProfile _sustain = new RumbleProfile(1f, 1f, 0.6f, 0.6f, 0f, true);

        [Tooltip("Sustain level right after ignition, before any progress.")]
        [Range(0f, 0.35f)]
        [SerializeField] private float _sustainIgnited = 0.12f;

        [Tooltip("Sustain level at the first cut step (progress 0).")]
        [Range(0f, 0.35f)]
        [SerializeField] private float _sustainStart = 0.1f;

        [Tooltip("Sustain level as the piece is about to break (progress 1).")]
        [Range(0f, 0.35f)]
        [SerializeField] private float _sustainEnd = 0.18f;

        [Tooltip("Seconds to fade to silence when the player releases (cancel or pause).")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _releaseFade = 0.15f;

        [Tooltip("The crack as the piece breaks free: the strongest pulse in normal play (0.35 for 120 ms).")]
        [SerializeField] private RumbleProfile _crack = new RumbleProfile(0.35f, 0.35f, 0.3f, 0.3f, 0.12f, false);

        [Tooltip("The fold into a bundle (0.05 for 40 ms).")]
        [SerializeField] private RumbleProfile _fold = new RumbleProfile(0.05f, 0.05f, 0.05f, 0.05f, 0.04f, false);

        [Tooltip("The bundle received (and, once UI counts units, each unit): 0.05 tick.")]
        [SerializeField] private RumbleProfile _receivedTick =
            new RumbleProfile(0.05f, 0.05f, 0.05f, 0.05f, 0.04f, false);

        [Header("Body")]
        [Tooltip("Charge-up: 07 leans slightly into the line (deg forward, held while cutting).")]
        [SerializeField] private NudgeSettings _lean = new NudgeSettings(0f, 1f, 2f, 1f);

        [Tooltip("Ignition recoil: 1.5 cm back, recovering in ~250 ms.")]
        [SerializeField] private NudgeSettings _recoil = new NudgeSettings(0.015f, 0f, 4f, 1f);

        [Tooltip("Sustain micro-motions: peak steadying wobble (deg) while the beam is on.")]
        [Range(0f, 2f)]
        [SerializeField] private float _sustainJitter = 0.3f;

        [Tooltip("How fast (Hz) the sustain micro-motions wander.")]
        [Range(0.2f, 10f)]
        [SerializeField] private float _sustainJitterRate = 2.5f;

        [Tooltip("Crack: the suspension dips 2 cm as the beam's load lets go.")]
        [SerializeField] private NudgeSettings _crackDip = new NudgeSettings(0.02f, 0f, 3f, 0.6f);

        [Tooltip("Received: the rear dips (deg of nose-up tilt, ~1.5 cm at the tail) with one rebound.")]
        [SerializeField] private NudgeSettings _rearDip = new NudgeSettings(0f, 1f, 2.5f, 0.45f);

        [Tooltip("Received: the antenna's wiggle (deg/s kick).")]
        [Range(0f, 400f)]
        [SerializeField] private float _antennaWiggle = 120f;

        public RumbleProfile Charge => _charge;

        public RumbleProfile Ignition => _ignition;

        public RumbleProfile Sustain => _sustain;

        public float SustainIgnited => _sustainIgnited;

        /// <summary>The sustained level at cut progress <paramref name="progress"/> (0..1).</summary>
        public float SustainAt(float progress)
        {
            return Mathf.Lerp(_sustainStart, _sustainEnd, progress);
        }

        public float ReleaseFade => _releaseFade;

        public RumbleProfile Crack => _crack;

        public RumbleProfile Fold => _fold;

        public RumbleProfile ReceivedTick => _receivedTick;

        public NudgeSettings Lean => _lean;

        public NudgeSettings Recoil => _recoil;

        public float SustainJitter => _sustainJitter;

        public float SustainJitterRate => _sustainJitterRate;

        public NudgeSettings CrackDip => _crackDip;

        public NudgeSettings RearDip => _rearDip;

        public float AntennaWiggle => _antennaWiggle;
    }
}
