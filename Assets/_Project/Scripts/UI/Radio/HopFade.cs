using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Gameplay;
using Object = UnityEngine.Object;

namespace MoonProject.UI
{
    /// <summary>
    /// The radio-hop's screen fade (docs/features/M3-06): over the whole HUD, under the pause menu, a soft indigo dark
    /// with a faint crawling static eases in and out with <see cref="IRadioHop.Fade"/> (<see cref="HopVeil"/>). Never
    /// a hard cut, never pure black. Fully clear, it leaves the layout. Owns its grain texture: dispose it with the UI.
    /// </summary>
    internal sealed class HopFade : IDisposable
    {
        private const float WriteEpsilon = 1e-3f;

        private readonly IRadioHop _hop;
        private readonly HopVeil _veil;
        private readonly VisualElement _root;
        private readonly VisualElement _dark;
        private readonly VisualElement _grain;
        private readonly Texture2D _texture;
        private bool _writtenShown = true;
        private float _writtenDark = -1f;
        private float _writtenGrain = -1f;
        private int _writtenGrainRevision = -1;

        public HopFade(UiLayout layout, RelaySettings settings, IRadioHop hop)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _hop = hop ?? throw new ArgumentNullException(nameof(hop));
            _veil = new HopVeil(settings);
            _root = layout.HopVeil;
            _dark = layout.HopVeilDark;
            _grain = layout.HopVeilGrain;
            _texture = GrainTexture.Create(settings.GrainTexels, settings.GrainDensity);
            float tile = settings.GrainTilePixels;
            _grain.style.backgroundImage = new StyleBackground(_texture);
            _grain.style.backgroundRepeat =
                new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat));
            _grain.style.backgroundSize =
                new StyleBackgroundSize(new BackgroundSize(new Length(tile), new Length(tile)));
            _grain.style.left = -tile;
            _grain.style.top = -tile;
            _grain.style.right = -tile;
            _grain.style.bottom = -tile;
            Write();
        }

        /// <summary>True while any of the dark is on screen.</summary>
        public bool IsVisible => !_veil.IsClear;

        /// <summary>The veil's current darkness (0..1 of its own scale), for tests and captures.</summary>
        public float Level => _veil.Level;

        /// <param name="deltaTime">Unscaled seconds since the last tick.</param>
        public void Tick(float deltaTime)
        {
            _veil.Step(_hop.Fade, deltaTime);
            Write();
        }

        public void Dispose()
        {
            _grain.style.backgroundImage = StyleKeyword.None;
            if (Application.isPlaying)
            {
                Object.Destroy(_texture);
            }
            else
            {
                Object.DestroyImmediate(_texture);
            }
        }

        private void Write()
        {
            bool shown = !_veil.IsClear;
            if (shown != _writtenShown)
            {
                _root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                _writtenShown = shown;
            }

            if (!shown)
            {
                return;
            }

            if (Mathf.Abs(_veil.Darkness - _writtenDark) > WriteEpsilon)
            {
                _dark.style.opacity = _veil.Darkness;
                _writtenDark = _veil.Darkness;
            }

            if (Mathf.Abs(_veil.Grain - _writtenGrain) > WriteEpsilon)
            {
                _grain.style.opacity = _veil.Grain;
                _writtenGrain = _veil.Grain;
            }

            if (_veil.GrainRevision != _writtenGrainRevision)
            {
                Vector2 offset = _veil.GrainOffset;
                _grain.style.translate = new StyleTranslate(new Translate(offset.x, offset.y));
                _writtenGrainRevision = _veil.GrainRevision;
            }
        }
    }
}
