using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// relay.1's lamp once the call is answered (opening v3 R8, <see cref="RelayAnswerLamp"/>): dark as the dead mast
    /// it is until relay.1 answers 07, then a warm amber halo at the mast's top that keys three short and one long with
    /// the call's own sign. Seen across the night basin, so it never shrinks below a small angle.
    /// </summary>
    [Serializable]
    public sealed class RelayLampSettings
    {
        [Tooltip("Brightness of the lamp's halo at the top of a key. Its amber peak stays under the bloom threshold " +
            "(checked by WorldSettings), so it is a lamp, never a sun.")]
        [Range(0f, 8f)]
        [SerializeField] private float _haloIntensity = 1.2f;

        [Tooltip("Radius of the lamp's halo up close, metres: a lamp's own tight glow round its lens, never a ball of " +
            "light (from afar the smallest angle below keeps it a point).")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _haloRadius = 0.2f;

        [Tooltip("Smallest angular size of the halo, degrees: the first lure after the answer, so a warm point that " +
            "still stands out in the whole night frame from the base, 300 m off.")]
        [Range(0f, 2f)]
        [SerializeField] private float _haloMinAngle = 0.9f;

        [Tooltip("Distance (m) from which the halo's minimum size starts to grow.")]
        [Range(1f, 200f)]
        [SerializeField] private float _haloMinFrom = 40f;

        [Tooltip("Distance (m) at which the halo's minimum size is full.")]
        [Range(10f, 1500f)]
        [SerializeField] private float _haloMinFull = 500f;

        [Tooltip("Metres the halo floats above the mast's lamp node.")]
        [Range(0f, 2f)]
        [SerializeField] private float _haloLift = 0.2f;

        [Header("Relit (opening v3 5.1)")]
        [Tooltip("Share of the keyed peak the lamp holds once relit: a calm steady glow.")]
        [Range(0f, 1f)]
        [SerializeField] private float _relitLevel = 0.85f;

        [Tooltip("Seconds the lamp flickers as it warms up on relighting.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _warmUpSeconds = 1.2f;

        [Tooltip("Seconds each flicker step of the warm-up lasts.")]
        [Range(0.02f, 0.3f)]
        [SerializeField] private float _flickerStepSeconds = 0.07f;

        [Tooltip("Chance a flicker step is on as the warm-up begins.")]
        [Range(0f, 1f)]
        [SerializeField] private float _flickerOnFrom = 0.35f;

        [Tooltip("Chance a flicker step is on as the warm-up ends.")]
        [Range(0f, 1f)]
        [SerializeField] private float _flickerOnTo = 0.95f;

        [Tooltip("Share of the steady glow an on step reaches as the warm-up begins (it rises to all of it).")]
        [Range(0f, 1f)]
        [SerializeField] private float _flickerRiseFrom = 0.5f;

        [Tooltip("Share of the steady glow an off step keeps (a filament never quite cold).")]
        [Range(0f, 1f)]
        [SerializeField] private float _flickerOffShare = 0.08f;

        public float HaloIntensity => _haloIntensity;
        [Header("The network's reply (the repair's Reply stage)")]
        [Tooltip("Short blinks before the long one in the network's reply: 3 short, 1 long.")]
        [Range(1, 6)]
        [SerializeField] private int _replyShorts = 3;

        [Tooltip("Seconds a short blink of the reply is lit.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _replyShortSeconds = 0.16f;

        [Tooltip("Seconds the long blink of the reply is lit.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _replyLongSeconds = 0.6f;

        [Tooltip("Seconds dark between the reply's blinks.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _replyGapSeconds = 0.2f;

        [Tooltip("Share of the steady glow the lamp keeps between the reply's blinks: low, never fully out.")]
        [Range(0f, 1f)]
        [SerializeField] private float _replyLowShare = 0.12f;

        public float RelitLevel => _relitLevel;
        public int ReplyShorts => _replyShorts;
        public float ReplyShortSeconds => _replyShortSeconds;
        public float ReplyLongSeconds => _replyLongSeconds;
        public float ReplyGapSeconds => _replyGapSeconds;
        public float ReplyLowShare => _replyLowShare;
        public float WarmUpSeconds => _warmUpSeconds;
        public float FlickerStepSeconds => _flickerStepSeconds;
        public float FlickerOnFrom => _flickerOnFrom;
        public float FlickerOnTo => _flickerOnTo;
        public float FlickerRiseFrom => _flickerRiseFrom;
        public float FlickerOffShare => _flickerOffShare;
        public float HaloRadius => _haloRadius;
        public float HaloMinAngle => _haloMinAngle;
        public float HaloMinFrom => _haloMinFrom;
        public float HaloMinFull => Mathf.Max(_haloMinFull, _haloMinFrom + 1f);
        public float HaloLift => _haloLift;
    }
}
