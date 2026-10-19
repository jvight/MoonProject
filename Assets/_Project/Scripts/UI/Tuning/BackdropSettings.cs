using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The live frame behind a menu (pause now; the title and the map when they come): softly blurred and a little
    /// greyed, so the menu sits inside the world instead of on a flat dim (M3-18 V11).
    /// </summary>
    [Serializable]
    public sealed class BackdropSettings
    {
        [Tooltip("Metres from the camera where the blur begins. 0 blurs 07 too, so the menu has the focus.")]
        [Min(0f)]
        [SerializeField] private float _blurStartMetres;

        [Tooltip("Metres from the camera where the blur reaches full softness.")]
        [Range(0.1f, 50f)]
        [SerializeField] private float _blurEndMetres = 4f;

        [Tooltip("How soft the blur is at full strength (URP's Gaussian depth of field radius).")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float _blurRadius = 1.5f;

        [Tooltip("Colour taken out of the frame at full strength: a slight grey, never black and white.")]
        [Range(-100f, 0f)]
        [SerializeField] private float _saturation = -30f;

        [Header("Face-Inspect Backdrop (behind 07's head)")]
        [Tooltip("Metres from the camera where the blur begins during face inspect so 07's head stays razor-sharp.")]
        [Min(0.5f)]
        [SerializeField] private float _faceBlurStartMetres = 1.35f;

        [Tooltip("Metres from the camera where the blur reaches full softness behind 07 during face inspect.")]
        [Range(1f, 50f)]
        [SerializeField] private float _faceBlurEndMetres = 6.5f;

        [Tooltip("Film grain while 07's face or a menu is up (the world's grain speckles small text on the CRT).")]
        [Range(0f, 1f)]
        [SerializeField] private float _faceGrain;

        [Tooltip("Saturation offset during face inspect (0 keeps 07's amber and cyan phosphors at full colour).")]
        [Range(-100f, 0f)]
        [SerializeField] private float _faceSaturation;

        public float BlurStartMetres => _blurStartMetres;

        public float BlurEndMetres => Mathf.Max(_blurEndMetres, _blurStartMetres + 0.1f);

        public float BlurRadius => _blurRadius;

        public float Saturation => _saturation;

        public float FaceBlurStartMetres => _faceBlurStartMetres;

        public float FaceBlurEndMetres => Mathf.Max(_faceBlurEndMetres, _faceBlurStartMetres + 0.5f);

        public float FaceSaturation => _faceSaturation;

        public float FaceGrain => _faceGrain;
    }
}
