using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The tether reticle: a soft centre ring shown only while aiming. It appears dim while the tether button is held
    /// with nothing in the aim cone, brightens to tether-cyan and gently opens when a relic is hovered, and leaves
    /// once something is on the tether (the beam says it all). No permanent crosshair.
    /// </summary>
    internal sealed class TetherReticle
    {
        private const float WriteEpsilon = 1e-4f;

        private readonly ReticleSettings _settings;
        private readonly Reveal _reveal;
        private readonly VisualElement _rest;
        private readonly VisualElement _hover;
        private readonly SoftSpring _expansion = new SoftSpring(1f);
        private float _hoverProgress;
        private float _writtenHover = -1f;
        private float _writtenScale = -1f;

        public TetherReticle(VisualElement reticle, VisualElement rest, VisualElement hover, ReticleSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _rest = rest ?? throw new ArgumentNullException(nameof(rest));
            _hover = hover ?? throw new ArgumentNullException(nameof(hover));
            _reveal = new Reveal(reticle, settings.Reveal);
            _reveal.Snap(false);
            new ReticlePainter(rest);
            new ReticlePainter(hover);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True when the hovered look is (fading towards) on.</summary>
        public bool IsHighlighted => _hoverProgress > 0.5f;

        /// <param name="deltaTime">Unscaled seconds since the last tick.</param>
        /// <param name="state">The tether's state this frame.</param>
        /// <param name="tetherHeld">True while the player holds the tether button.</param>
        public void Tick(float deltaTime, TetherAimState state, bool tetherHeld)
        {
            bool hovering = state == TetherAimState.Hovering;
            _reveal.Set(hovering || (state == TetherAimState.Idle && tetherHeld));
            _reveal.Tick(deltaTime);
            if (_reveal.IsHidden)
            {
                _hoverProgress = 0f;
                _expansion.Snap(1f);
                return;
            }

            float step = _settings.HoverFadeSeconds <= 0f ? 1f : deltaTime / _settings.HoverFadeSeconds;
            _hoverProgress = Mathf.Clamp01(_hoverProgress + (hovering ? step : -step));
            _expansion.Step(hovering ? _settings.HoverScale : 1f, deltaTime, _settings.HoverSpringFrequency,
                _settings.HoverSpringDamping);

            float hover = UiEase.InOutSine(_hoverProgress);
            if (Mathf.Abs(hover - _writtenHover) > WriteEpsilon)
            {
                _hover.style.opacity = hover;
                _rest.style.opacity = 1f - hover;
                _writtenHover = hover;
            }

            float scale = _expansion.Value;
            if (Mathf.Abs(scale - _writtenScale) > WriteEpsilon)
            {
                var styleScale = new StyleScale(new Scale(new Vector2(scale, scale)));
                _rest.style.scale = styleScale;
                _hover.style.scale = styleScale;
                _writtenScale = scale;
            }
        }
    }
}
