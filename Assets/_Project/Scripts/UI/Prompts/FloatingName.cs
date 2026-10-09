using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// A name that drifts up over a point in the world, rests a moment and fades away (a friend waking, a site
    /// answering the sonar). It sits on the kit title's soft dusk of shadow (USS .name-tag, .soft-shadow--name), with
    /// no plate, so it stays legible over the bright Earth or a lit sky; the shadow fades with it. Hidden while its
    /// point is off screen. Which name shows, and when, is the owner's call.
    /// </summary>
    internal sealed class FloatingName
    {
        private readonly VisualElement _tag;
        private readonly Label _text;
        private readonly Reveal _reveal;
        private readonly WorldAnchor _anchor;
        private float _rest;
        private bool _onScreen = true;

        /// <param name="anchor">The zero-size world anchor the tag hangs from.</param>
        /// <param name="tag">The tag (.name-tag) that fades, holding the shadow and the text.</param>
        /// <param name="shadow">The tag's soft shadow layer (.soft-shadow--name).</param>
        /// <param name="text">The name itself.</param>
        /// <param name="reveal">How the name eases in and out.</param>
        /// <param name="anchoring">How world-anchored labels follow their point.</param>
        /// <param name="view">The player's camera.</param>
        public FloatingName(VisualElement anchor, VisualElement tag, VisualElement shadow, Label text,
            RevealSettings reveal, PromptSettings anchoring, IViewCamera view)
        {
            if (anchoring == null)
            {
                throw new ArgumentNullException(nameof(anchoring));
            }

            _tag = tag ?? throw new ArgumentNullException(nameof(tag));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _reveal = new Reveal(tag, reveal);
            _reveal.Snap(false);
            _anchor = new WorldAnchor(anchor, view, anchoring.ScreenMargin, anchoring.FollowHalfLife, false);
            new ShadowPainter(shadow);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True from <see cref="Show"/> until the name has rested and faded fully away.</summary>
        public bool IsUp => _reveal.Target || !_reveal.IsHidden;

        /// <summary>Shows <paramref name="name"/>, resting <paramref name="holdSeconds"/> once it is fully in.</summary>
        public void Show(string name, float holdSeconds)
        {
            _text.text = name;
            _rest = holdSeconds;
            _reveal.Show();
        }

        /// <summary>Changes the words in place (a new language), keeping the name's time on screen.</summary>
        public void Rename(string name)
        {
            _text.text = name;
        }

        /// <param name="point">The world point the name floats at.</param>
        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused so the name waits too.</param>
        /// <param name="panelSize">Size of the UI panel.</param>
        public void Tick(Vector3 point, float deltaTime, Vector2 panelSize)
        {
            if (_reveal.IsShown)
            {
                _rest -= deltaTime;
                if (_rest <= 0f)
                {
                    _reveal.Hide();
                }
            }

            bool onScreen = _anchor.Track(point, panelSize, deltaTime, _reveal.IsHidden);
            if (onScreen != _onScreen)
            {
                _tag.visible = onScreen;
                _onScreen = onScreen;
            }

            _reveal.Tick(deltaTime);
        }
    }
}
