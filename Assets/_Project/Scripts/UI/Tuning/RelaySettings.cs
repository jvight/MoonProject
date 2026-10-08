using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The relay network UI (docs/features/M3-06): the soft dark of a radio-hop, the tiny list of lit nodes, and the
    /// price tag at a dark mast.
    /// </summary>
    [Serializable]
    public sealed class RelaySettings
    {
        [Tooltip("Fastest the veil may darken or lighten, per second. Gameplay's eased hop fade changes more slowly "
            + "and is followed exactly; anything faster (a hop cut short) still eases over a moment, never a cut.")]
        [Range(1f, 20f)]
        [SerializeField] private float _veilMaxChangePerSecond = 3f;

        [Tooltip("Opacity of the indigo veil at the darkest moment of a hop: a soft dark, never pure black.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _veilMaxDarkness = 0.94f;

        [Tooltip("Opacity of the static grain over the veil at the darkest moment (it rises and resolves with it).")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _grainOpacity = 0.05f;

        [Tooltip("Texels per side of the generated grain tile.")]
        [Range(32, 512)]
        [SerializeField] private int _grainTexels = 256;

        [Tooltip("Pixels (at 1080p) one grain tile covers on screen: larger is a softer, coarser grain.")]
        [Range(32f, 512f)]
        [SerializeField] private float _grainTilePixels = 256f;

        [Tooltip("Share (0..1) of the grain texels that carry a speck of static.")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _grainDensity = 0.5f;

        [Tooltip("Times per second the static shifts to a new place: it crawls, it never strobes.")]
        [Range(1f, 30f)]
        [SerializeField] private float _grainStepsPerSecond = 12f;

        [Tooltip("The list of lit nodes easing in and out on 07's pad.")]
        [SerializeField] private RevealSettings _list = new RevealSettings(0.3f, 0.35f, 8f, 0.96f, 1.8f, 0.7f);

        [Tooltip("The price tag easing in and out at a dark mast.")]
        [SerializeField] private RevealSettings _tag = new RevealSettings(0.4f, 0.4f, 8f, 0.9f, 1.4f, 0.6f);

        [Tooltip("Metres above the mast's part socket where the price tag floats (the Restore prompt's point: when the "
            + "prompt is up, the tag rests on top of it).")]
        [Range(0f, 4f)]
        [SerializeField] private float _tagLiftMetres = 0.9f;

        [Tooltip("Pixels (at 1080p) between the Restore prompt and the price tag resting on top of it.")]
        [Range(0f, 40f)]
        [SerializeField] private float _tagStackGap = 8f;

        public float VeilMaxChangePerSecond => _veilMaxChangePerSecond;

        public float VeilMaxDarkness => _veilMaxDarkness;

        public float GrainOpacity => _grainOpacity;

        public int GrainTexels => _grainTexels;

        public float GrainTilePixels => _grainTilePixels;

        public float GrainDensity => _grainDensity;

        public float GrainStepsPerSecond => _grainStepsPerSecond;

        public RevealSettings List => _list;

        public RevealSettings Tag => _tag;

        public float TagLiftMetres => _tagLiftMetres;

        public float TagStackGap => _tagStackGap;
    }
}
