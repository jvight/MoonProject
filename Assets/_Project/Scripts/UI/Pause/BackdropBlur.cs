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

        private readonly GameObject _host;
        private readonly VolumeProfile _profile;
        private readonly Volume _volume;

        public BackdropBlur(Transform parent, BackdropSettings settings)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "MenuBackdrop";
            var blur = _profile.Add<DepthOfField>(true);
            blur.mode.Override(DepthOfFieldMode.Gaussian);
            blur.gaussianStart.Override(settings.BlurStartMetres);
            blur.gaussianEnd.Override(settings.BlurEndMetres);
            blur.gaussianMaxRadius.Override(settings.BlurRadius);
            blur.highQualitySampling.Override(true);
            var colour = _profile.Add<ColorAdjustments>(true);
            colour.saturation.Override(settings.Saturation);

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

        /// <param name="openness">The most open menu's veil (0..1).</param>
        public void Set(float openness)
        {
            float weight = Mathf.Clamp01(openness);
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
