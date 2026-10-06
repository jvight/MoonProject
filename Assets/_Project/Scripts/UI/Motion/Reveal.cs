using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Shows and hides one element the calm way: opacity follows an eased <see cref="Fade"/>, the element drifts a few
    /// pixels into place, and its scale springs up with a soft overshoot. Fully hidden elements are taken out of the
    /// layout (display: none) so they cost nothing and can never be clicked. Styles are written only when they change.
    /// </summary>
    internal sealed class Reveal
    {
        private const float WriteEpsilon = 1e-4f;

        private readonly VisualElement _element;
        private readonly RevealSettings _settings;
        private readonly Fade _fade;
        private readonly SoftSpring _scale;
        private float _writtenOpacity = -1f;
        private float _writtenScale = -1f;
        private float _writtenSlide = float.NaN;
        private bool _writtenDisplayed = true;

        public Reveal(VisualElement element, RevealSettings settings)
        {
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _fade = new Fade(settings);
            _scale = new SoftSpring(settings.ScaleFrom);
            Apply();
        }

        public VisualElement Element => _element;

        /// <summary>Eased visibility 0..1.</summary>
        public float Visibility => _fade.Value;

        /// <summary>True when heading towards (or at) shown.</summary>
        public bool Target => _fade.Target;

        public bool IsShown => _fade.IsShown;

        public bool IsHidden => _fade.IsHidden;

        public void Set(bool shown)
        {
            if (shown && _fade.IsHidden)
            {
                _scale.Snap(_settings.ScaleFrom);
            }

            _fade.Set(shown);
        }

        public void Show()
        {
            Set(true);
        }

        public void Hide()
        {
            Set(false);
        }

        /// <summary>Jumps to the end state without motion (setup only).</summary>
        public void Snap(bool shown)
        {
            _fade.Snap(shown);
            _scale.Snap(shown ? 1f : _settings.ScaleFrom);
            Apply();
        }

        public void Tick(float deltaTime)
        {
            if (_fade.IsHidden && !_writtenDisplayed)
            {
                return;
            }

            _fade.Step(deltaTime);
            _scale.Step(_fade.Target ? 1f : _settings.ScaleFrom, deltaTime, _settings.SpringFrequency,
                _settings.SpringDamping);
            Apply();
        }

        private void Apply()
        {
            bool displayed = !_fade.IsHidden;
            if (displayed != _writtenDisplayed)
            {
                _element.style.display = displayed ? DisplayStyle.Flex : DisplayStyle.None;
                _writtenDisplayed = displayed;
            }

            if (!displayed)
            {
                return;
            }

            float visibility = _fade.Value;
            if (Mathf.Abs(visibility - _writtenOpacity) > WriteEpsilon)
            {
                _element.style.opacity = visibility;
                _writtenOpacity = visibility;
            }

            float scale = _scale.Value;
            if (Mathf.Abs(scale - _writtenScale) > WriteEpsilon)
            {
                _element.style.scale = new StyleScale(new Scale(new Vector2(scale, scale)));
                _writtenScale = scale;
            }

            float slide = (1f - visibility) * _settings.Slide;
            if (float.IsNaN(_writtenSlide) || Mathf.Abs(slide - _writtenSlide) > WriteEpsilon)
            {
                _element.style.translate = new StyleTranslate(new Translate(0f, slide));
                _writtenSlide = slide;
            }
        }
    }
}
