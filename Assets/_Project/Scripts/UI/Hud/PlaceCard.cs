using System;
using System.Collections.Generic;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The first time 07 comes near a place (docs/features/M3-16 "Each place and its one job"), its name and its one
    /// job drift in high on screen on the floating names' soft shadow, rest, and fade away. Each place once; places
    /// met while a card is up wait their turn, and a card waits while the screen is busy (a kit title, a story card).
    /// </summary>
    internal sealed class PlaceCard
    {
        private const string Prefix = "place.";

        private readonly UiLayout _layout;
        private readonly GuidanceSettings _settings;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private readonly Queue<string> _waiting = new Queue<string>();
        private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);
        private float _rest;

        public PlaceCard(UiLayout layout, GuidanceSettings settings, ILocalization localization)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _reveal = new Reveal(layout.PlaceCard, settings.Place);
            _reveal.Snap(false);
            new ShadowPainter(layout.PlaceCardShadow);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The place whose card is up (or easing), or null.</summary>
        public string Current { get; private set; }

        /// <summary>"place.dock" (or "dock") → "place.dock".</summary>
        public static string KeyBase(string placeId)
        {
            return placeId.StartsWith(Prefix, StringComparison.Ordinal) ? placeId : Prefix + placeId;
        }

        /// <summary>Queues the card for <paramref name="placeId"/>, once per place.</summary>
        public void Approached(string placeId)
        {
            if (!string.IsNullOrEmpty(placeId) && _seen.Add(KeyBase(placeId)))
            {
                _waiting.Enqueue(KeyBase(placeId));
            }
        }

        public void Relocalize()
        {
            if (Current != null)
            {
                Write();
            }
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused so the card waits too.</param>
        /// <param name="open">False while something else holds the screen: a waiting card does not start.</param>
        public void Tick(float deltaTime, bool open)
        {
            if (Current == null && open && _waiting.Count > 0)
            {
                Current = _waiting.Dequeue();
                Write();
                _rest = _settings.PlaceHoldSeconds;
                _reveal.Show();
            }

            if (_reveal.IsShown)
            {
                _rest -= deltaTime;
                if (_rest <= 0f)
                {
                    _reveal.Hide();
                }
            }

            _reveal.Tick(deltaTime);
            if (Current != null && _reveal.IsHidden && !_reveal.Target)
            {
                Current = null;
            }
        }

        private void Write()
        {
            _layout.PlaceCardName.text = _localization.Get(Current + ".name");
            _layout.PlaceCardJob.text = _localization.Get(Current + ".job");
        }
    }
}
