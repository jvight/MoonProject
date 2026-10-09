using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// While 07 stargazes, the ambient HUD (prompts, pips, names, ticker, chips) slowly leaves the sky to the player
    /// and comes back sooner when the beat ends. Menus that are open (the tower panel, a card, the hop list, the pause
    /// menu) are not among its elements, so they stay. Its opacity multiplies with each element's own fade, and is
    /// written only when it moves.
    /// </summary>
    internal sealed class StargazeVeil
    {
        private readonly VisualElement[] _elements;
        private readonly Fade _fade;

        public StargazeVeil(RevealSettings settings, params VisualElement[] elements)
        {
            _elements = elements ?? throw new ArgumentNullException(nameof(elements));
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] == null)
                {
                    throw new ArgumentNullException(nameof(elements), $"element {i} is missing");
                }
            }

            _fade = new Fade(settings);
            _fade.Snap(true);
        }

        /// <summary>True while 07 stargazes (the HUD is leaving or gone).</summary>
        public bool IsVeiled => !_fade.Target;

        /// <summary>The ambient HUD's opacity, 0..1.</summary>
        public float Opacity => _fade.Value;

        public void SetStargazing(bool stargazing)
        {
            _fade.Set(!stargazing);
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        public void Tick(float deltaTime)
        {
            if (!_fade.Step(deltaTime))
            {
                return;
            }

            float opacity = _fade.Value;
            for (int i = 0; i < _elements.Length; i++)
            {
                _elements[i].style.opacity = opacity;
            }
        }
    }
}
