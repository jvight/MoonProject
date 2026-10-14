using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// The first time 07 comes near a place (docs/features/M3-16 "Each place and its one job"), its name and its one
    /// job drift in high on screen on the floating names' soft shadow, rest, and fade away. Each place once; places
    /// met while a card is up wait their turn, and a card waits while the screen is busy (a kit title, a story card).
    /// A blueprint found in a wreck gets the same quiet card: the kit's name over "Blueprint found". The radio tower's
    /// card waits a few seconds while its cables preview what it powers (docs/features/M3-24 B).
    /// </summary>
    internal sealed class PlaceCard
    {
        private const string Prefix = "place.";
        private const string TowerPlace = "place.tower";

        // A place card is its name alone; only the dock also says its job, which nothing else teaches
        // (docs/design/text-audit-0.5.5.md §3.3).
        private static readonly HashSet<string> JobPlaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "place.dock",
        };

        private readonly UiLayout _layout;
        private readonly GuidanceSettings _settings;
        private readonly ILocalization _localization;
        private readonly EventBus _events;
        private readonly Reveal _reveal;
        private readonly Queue<string> _waiting = new Queue<string>();
        private readonly Queue<string> _waitingLines = new Queue<string>();
        private readonly Queue<int> _waitingCounts = new Queue<int>();
        private readonly Queue<float> _readyAt = new Queue<float>();
        private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);
        private float _rest;
        private float _clock;
        private string _lineKey;
        private int _lineCount;

        public PlaceCard(UiLayout layout, GuidanceSettings settings, ILocalization localization, EventBus events)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _reveal = new Reveal(layout.PlaceCard, settings.Place);
            _reveal.Snap(false);
            new ShadowPainter(layout.PlaceCardShadow);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>A card is queued and waits for the screen to clear.</summary>
        public bool IsWaiting => Current == null && _waiting.Count > 0;

        /// <summary>The title key of the card that is up (or easing), or null.</summary>
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
                _waiting.Enqueue(KeyBase(placeId) + ".name");
                _waitingLines.Enqueue(JobPlaces.Contains(KeyBase(placeId)) ? KeyBase(placeId) + ".job" : null);
                _waitingCounts.Enqueue(0);
                _readyAt.Enqueue(_clock + (KeyBase(placeId) == TowerPlace ? _settings.TowerCardDelay : 0f));
            }
        }

        /// <summary>Queues the scan card for <paramref name="reached"/>, once per place.</summary>
        public void Reached(ScanSiteReached reached)
        {
            Reached(reached.PlaceId, reached.Left);
        }

        /// <summary>Queues the scan card for <paramref name="placeId"/> with <paramref name="left"/> fragments, once.</summary>
        public void Reached(string placeId, int left)
        {
            if (string.IsNullOrEmpty(placeId) || left <= 0)
            {
                return;
            }

            string nameKey = UiKeys.ScanPlaceName(placeId);
            if (_seen.Add(nameKey))
            {
                _waiting.Enqueue(nameKey);
                _waitingLines.Enqueue(UiKeys.ScanSiteCount);
                _waitingCounts.Enqueue(left);
                _readyAt.Enqueue(_clock);
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
            _clock += deltaTime;
            if (!open && _reveal.IsShown)
            {
                _reveal.Hide();
            }

            if (Current == null && open && _waiting.Count > 0 && _clock >= _readyAt.Peek())
            {
                Current = _waiting.Dequeue();
                _lineKey = _waitingLines.Dequeue();
                _lineCount = _waitingCounts.Dequeue();
                _readyAt.Dequeue();
                Write();
                _rest = _settings.PlaceHoldSeconds;
                _reveal.Show();
                _events.Publish(new UiCue(UiCueKind.CardShown));
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
            _layout.PlaceCardName.text = _localization.Get(Current);
            bool job = _lineKey != null;
            _layout.PlaceCardJob.style.display = job ? DisplayStyle.Flex : DisplayStyle.None;
            if (job)
            {
                string line = _localization.Get(_lineKey);
                _layout.PlaceCardJob.text = _lineCount > 0 ? string.Format(line, _lineCount) : line;
            }
        }
    }
}
