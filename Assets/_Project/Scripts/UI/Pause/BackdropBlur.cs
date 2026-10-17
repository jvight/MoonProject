using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MoonProject.UI
{
    /// <summary>
    /// Blurs and slightly greys the live frame behind a menu: a global URP volume of its own, above the world's look,
    /// whose weight follows the menu's veil so the blur eases in and out with it (M3-18 V11).
    /// </summary>
    internal sealed class BackdropBlur : IDisposable
    {
        // Above the world's own volume so the blur wins while a menu is open; the weight blends it back out.
        private const float Priority = 100f;

        private readonly BackdropSettings _settings;
        private readonly GameObject _host;
        private readonly VolumeProfile _profile;
        private readonly Volume _volume;
        private readonly DepthOfField _blur;
        private readonly ColorAdjustments _colour;

        public BackdropBlur(Transform parent, BackdropSettings settings)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "MenuBackdrop";
            _blur = _profile.Add<DepthOfField>(true);
            _blur.mode.Override(DepthOfFieldMode.Gaussian);
            _blur.gaussianStart.Override(settings.BlurStartMetres);
            _blur.gaussianEnd.Override(settings.BlurEndMetres);
            _blur.gaussianMaxRadius.Override(settings.BlurRadius);
            _blur.highQualitySampling.Override(true);
            _colour = _profile.Add<ColorAdjustments>(true);
            _colour.saturation.Override(settings.Saturation);

            _host = new GameObject("MenuBackdrop");
            _host.transform.SetParent(parent, false);
            _volume = _host.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = Priority;
            _volume.sharedProfile = _profile;
            _volume.weight = 0f;
        }

        /// <summary>How much of the blur is on (0..1).</summary>
        public float Weight => _volume.weight;

        /// <summary>Current Gaussian blur start distance in metres.</summary>
        public float StartMetres => _blur.gaussianStart.value;

        /// <param name="openness">The most open full-screen menu's veil (0..1).</param>
        /// <param name="faceOpenness">07's face-inspect screen openness (0..1), blurring only behind 07's head.</param>
        public void Set(float openness, float faceOpenness = 0f)
        {
            float menu = Mathf.Clamp01(openness);
            float face = Mathf.Clamp01(faceOpenness);
            float weight = Mathf.Max(menu, face);
            float menuShare = weight > 0f ? Mathf.Clamp01(menu / weight) : 1f;
            _blur.gaussianStart.value = Mathf.Lerp(_settings.FaceBlurStartMetres, _settings.BlurStartMetres, menuShare);
            _blur.gaussianEnd.value = Mathf.Lerp(_settings.FaceBlurEndMetres, _settings.BlurEndMetres, menuShare);
            _colour.saturation.value = Mathf.Lerp(_settings.FaceSaturation, _settings.Saturation, menuShare);
            _volume.weight = weight;
            _volume.enabled = weight > 0f;
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(_host);
            UnityEngine.Object.Destroy(_profile);
        }
    }
}
