using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// How 07's lid darkens the picture (docs/design/cutscenes.md §1): the curved soft lid, the lens' shallow focus
    /// and the eye's gain on the first light out of the black. Rover drives how open the lid is through
    /// <see cref="MoonProject.Core.ILensView"/>; these say how it looks.
    /// </summary>
    [Serializable]
    public sealed class LensViewSettings
    {
        /// <summary>Smallest lid span the eye's gain fades over, so the ramp never divides by 0.</summary>
        private const float MinGainSpan = 0.01f;

        [Tooltip("Width of the lid's soft edge, in viewport heights. Wide and soft: the lid is far too near the " +
            "lens to ever be sharp.")]
        [Range(0.01f, 0.4f)]
        [SerializeField] private float _lidSoftness = 0.14f;

        [Tooltip("How much higher the lid's edge sits in the middle of the frame than at its sides, in viewport " +
            "heights: the brow-shutter's arc seen from inside.")]
        [Range(0f, 0.4f)]
        [SerializeField] private float _lidCurve = 0.12f;

        [Tooltip("How the lid's openness maps to its edge: below 1 the first stages already show more of the world "
            + "(1 = linear).")]
        [Range(0.3f, 1f)]
        [SerializeField] private float _lidOpenGamma = 0.75f;

        [Tooltip("Lens focal length in mm for the depth of field: longer = shallower focus.")]
        [Range(10f, 300f)]
        [SerializeField] private float _focalLength = 60f;

        [Tooltip("Lens aperture (f-stop) for the depth of field: lower = shallower focus, softer blur.")]
        [Range(1f, 32f)]
        [SerializeField] private float _aperture = 1.6f;

        [Tooltip("Nearest focus the lens can hunt to (m).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _nearestFocus = 0.1f;

        [Header("Eye gain (the first light out of the black, in the eclipse)")]
        [Tooltip("Exposure (EV) 07's eye adds while its lid is barely open: the eclipse is near black, so the first " +
            "light out of the black would show nothing; this lifts it to dim but readable.")]
        [Range(0f, 4f)]
        [SerializeField] private float _wakeGain = 1.5f;

        [Tooltip("The full gain holds until the lid has opened this far (0..1), so the near-black first light gets " +
            "all of it.")]
        [Range(0f, 1f)]
        [SerializeField] private float _gainHoldLid = 0.2f;

        [Tooltip("By the time the lid has opened this far (0..1) the gain is gone, so the world's own exposure comes " +
            "back without a jump. A partial close never brings it back; a shut lid starts it again.")]
        [Range(0f, 1f)]
        [SerializeField] private float _gainGoneLid = 0.65f;

        [Tooltip("Priority of the lens' own volume: above the world's look, below the pause menu's backdrop " +
            "blur (100), so a menu still blurs over the opening.")]
        [Range(1f, 99f)]
        [SerializeField] private float _volumePriority = 90f;

        public float LidSoftness => _lidSoftness;
        public float LidCurve => _lidCurve;
        public float LidOpenGamma => _lidOpenGamma;
        public float FocalLength => _focalLength;
        public float Aperture => _aperture;
        public float NearestFocus => _nearestFocus;
        public float VolumePriority => _volumePriority;
        public float WakeGain => _wakeGain;
        public float GainHoldLid => _gainHoldLid;
        public float GainGoneLid => Mathf.Max(_gainGoneLid, _gainHoldLid + MinGainSpan);
    }
}
