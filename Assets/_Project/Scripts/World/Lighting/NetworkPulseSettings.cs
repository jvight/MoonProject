using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>
    /// The network's handshake (north-star §4a beat 7): when a mast lights, a warm bead runs lamp to lamp along the
    /// network's links, out from the new mast to every lit mast and back to home, and each lamp flashes softly as it
    /// passes, so the player sees how far the network now reaches. Calm: a steady hop rhythm that reads from a
    /// distance, soft flashes under the bloom threshold, gone in a few breaths.
    /// </summary>
    [Serializable]
    public sealed class NetworkPulseSettings
    {
        [Header("Rhythm")]
        [Tooltip("Seconds the bead takes to run one link, lamp to lamp, whatever its length: a steady rhythm the eye " +
            "can follow across the basin.")]
        [Range(0.3f, 5f)]
        [SerializeField] private float _hopSeconds = 1.4f;

        [Tooltip("Seconds a lamp's flash takes to brighten as the bead arrives.")]
        [Range(0.02f, 2f)]
        [SerializeField] private float _flashRise = 0.2f;

        [Tooltip("Seconds a lamp's flash takes to ease away after its peak.")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _flashFall = 1.6f;

        [Tooltip("How much stronger the newly lit mast's own flash is than the others' (times): the handshake starts " +
            "there.")]
        [Range(1f, 3f)]
        [SerializeField] private float _originBoost = 1.4f;

        [Tooltip("Share of a hop over which the bead fades in leaving a lamp and fades out reaching the next (0..0.5).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _beadFade = 0.2f;

        [Tooltip("How high the bead's path arcs above the straight line between two lamps, metres per metre of link: " +
            "a radio hop, clear of the ridges between.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _arcPerMetre = 0.06f;

        [Header("Look")]
        [Tooltip("Colour of the flashes and the bead: the lamps' warm.")]
        [SerializeField] private Color _color = Palette.Get(PaletteSwatch.WarmLamp);

        [Tooltip("Glow of a lamp's flash halo at its peak (LofiBeaconHalo intensity).")]
        [Range(0f, 6f)]
        [SerializeField] private float _flashIntensity = 1.5f;

        [Tooltip("Radius of a lamp's flash halo up close, metres.")]
        [Range(0.05f, 4f)]
        [SerializeField] private float _flashRadius = 0.6f;

        [Tooltip("Glow of the travelling bead's halo (LofiBeaconHalo intensity).")]
        [Range(0f, 6f)]
        [SerializeField] private float _beadIntensity = 1.1f;

        [Tooltip("Radius of the travelling bead's halo up close, metres.")]
        [Range(0.05f, 4f)]
        [SerializeField] private float _beadRadius = 0.35f;

        [Tooltip("Smallest apparent radius of the flashes and the bead, degrees, so the chain still reads as warm " +
            "dots from across the basin.")]
        [Range(0f, 2f)]
        [SerializeField] private float _minAngle = 0.3f;

        [Tooltip("Distance (metres) from which the halos begin keeping their minimum size.")]
        [Range(1f, 400f)]
        [SerializeField] private float _minAngleFrom = 30f;

        [Tooltip("Distance (metres) from which the halos keep their full minimum size.")]
        [Range(1f, 400f)]
        [SerializeField] private float _minAngleFull = 60f;

        [Tooltip("Intensity of the warm light a lamp's flash casts on the ground round its mast, at its peak.")]
        [Range(0f, 20f)]
        [SerializeField] private float _flashLight = 4f;

        [Tooltip("Range of a lamp's flash light, metres.")]
        [Range(1f, 60f)]
        [SerializeField] private float _flashLightRange = 14f;

        [Tooltip("Intensity of the warm light riding the bead.")]
        [Range(0f, 20f)]
        [SerializeField] private float _beadLight = 2.5f;

        [Tooltip("Range of the bead's light, metres.")]
        [Range(1f, 60f)]
        [SerializeField] private float _beadLightRange = 8f;

        public float HopSeconds => _hopSeconds;
        public float FlashRise => _flashRise;
        public float FlashFall => _flashFall;
        public float OriginBoost => _originBoost;
        public float BeadFade => _beadFade;
        public float ArcPerMetre => _arcPerMetre;
        public Color Color => _color;
        public float FlashIntensity => _flashIntensity;
        public float FlashRadius => _flashRadius;
        public float BeadIntensity => _beadIntensity;
        public float BeadRadius => _beadRadius;
        public float MinAngle => _minAngle;
        public float MinAngleFrom => _minAngleFrom;
        public float MinAngleFull => _minAngleFull;
        public float FlashLight => _flashLight;
        public float FlashLightRange => _flashLightRange;
        public float BeadLight => _beadLight;
        public float BeadLightRange => _beadLightRange;
    }
}
