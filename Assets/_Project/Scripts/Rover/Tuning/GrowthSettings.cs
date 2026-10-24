using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The growth parts 07 gains in the opening (docs/design/opening-v3-plan.md 2.6, 5.2a): the antenna's second
    /// segment slides out of its mast at the first charge, the igniter cartridge seats into the cutter turret when it
    /// is fitted, and the SSR-3 headlamp pods (and flank cell pods) unfold and clamp home when fitted at the rack.
    /// Slow, mechanical and soft: nothing snaps and nothing rattles.
    /// </summary>
    [Serializable]
    public sealed class GrowthSettings
    {
        [Tooltip("Metres the antenna's second segment slides out along the mast's local up (Art's rest pose is retracted).")]
        [Range(0.05f, 0.8f)]
        [SerializeField] private float _antennaExtendMetres = 0.35f;

        [Tooltip("Seconds the segment takes to slide out, eased in and out.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _antennaSeconds = 1.2f;

        [Tooltip("Seconds the segment takes to settle after it has arrived: a soft mechanical give, then still.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _antennaSettleSeconds = 0.5f;

        [Tooltip("Metres of that give at its first swing (it sinks back a little before it rests).")]
        [Range(0f, 0.05f)]
        [SerializeField] private float _antennaSettleMetres = 0.012f;

        [Tooltip("Metres the igniter cartridge starts out of its socket before it seats.")]
        [Range(0.01f, 0.15f)]
        [SerializeField] private float _igniterSeatMetres = 0.05f;

        [Tooltip("Where the cartridge comes from, in the cutter turret's local space (the way out of the socket).")]
        [SerializeField] private Vector3 _igniterPullOut = Vector3.forward;

        [Tooltip("Seconds the cartridge takes to slide into its socket, eased out.")]
        [Range(0.1f, 1.5f)]
        [SerializeField] private float _igniterSeatSeconds = 0.35f;

        [Tooltip("Metres the cartridge presses past home at the click before it rests.")]
        [Range(0f, 0.02f)]
        [SerializeField] private float _igniterClickMetres = 0.006f;

        [Tooltip("Seconds of the click: the press and the release back to rest.")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float _igniterClickSeconds = 0.16f;

        [Tooltip("Seconds the SSR-3 headlamp pods (and flank cell pods) take to unfold and clamp home when fitted.")]
        [Range(0.2f, 1.5f)]
        [SerializeField] private float _headlampFitSeconds = 0.6f;

        [Tooltip("Seconds after a live headlamp fit before 07's small happy chirp: once the pods have clamped home.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _fitChirpSeconds = 0.7f;

        [Tooltip("Seconds after the fit before 07 starts trying its new eyes: the head looks out across the ground.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _fitLookStartSeconds = 0.9f;

        [Tooltip("Seconds the head looks to each side in turn, so the lamp's first beam sweeps once across the ground.")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _fitLookSeconds = 0.6f;

        [Tooltip("Degrees to each side of straight ahead the head looks while it tries its new eyes.")]
        [Range(10f, 80f)]
        [SerializeField] private float _fitLookDegrees = 40f;

        [Tooltip("Metres ahead of 07 the ground the head looks at lies.")]
        [Range(2f, 20f)]
        [SerializeField] private float _fitLookMetres = 6f;

        [Tooltip("Gaze priority of the head's look (0 passing, 1 interest, 2 hands-on).")]
        [Range(0, 3)]
        [SerializeField] private int _fitLookPriority = 2;

        [Tooltip("Metres each headlamp pod housing (or flank cell pod) starts outboard of its bracket before clamping in.")]
        [Range(0.01f, 0.2f)]
        [SerializeField] private float _headlampClampMetres = 0.06f;

        [Tooltip("Degrees each headlamp pod housing starts unfolded outward on its bracket before clamping shut.")]
        [Range(5f, 60f)]
        [SerializeField] private float _headlampUnfoldDegrees = 28f;

        [Tooltip("Metres each pod presses past home at the final clamp click before resting.")]
        [Range(0f, 0.02f)]
        [SerializeField] private float _headlampClickMetres = 0.005f;

        public float AntennaExtendMetres => _antennaExtendMetres;
        public float AntennaSeconds => _antennaSeconds;
        public float AntennaSettleSeconds => _antennaSettleSeconds;
        public float AntennaSettleMetres => _antennaSettleMetres;
        public float IgniterSeatMetres => _igniterSeatMetres;
        public Vector3 IgniterPullOut => _igniterPullOut.sqrMagnitude > 1e-6f ? _igniterPullOut.normalized : Vector3.forward;
        public float IgniterSeatSeconds => _igniterSeatSeconds;
        public float IgniterClickMetres => _igniterClickMetres;
        public float IgniterClickSeconds => _igniterClickSeconds;
        public float HeadlampFitSeconds => _headlampFitSeconds;
        public float FitChirpSeconds => _fitChirpSeconds;
        public float FitLookStartSeconds => _fitLookStartSeconds;
        public float FitLookSeconds => _fitLookSeconds;
        public float FitLookDegrees => _fitLookDegrees;
        public float FitLookMetres => _fitLookMetres;
        public int FitLookPriority => _fitLookPriority;
        public float HeadlampClampMetres => _headlampClampMetres;
        public float HeadlampUnfoldDegrees => _headlampUnfoldDegrees;
        public float HeadlampClickMetres => _headlampClickMetres;
    }
}
