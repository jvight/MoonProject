using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Calm Variant C terminal + telex/paper card (`#letter-card`) for the long story-v4 letters
    /// (`letter.mast_1`…`10`, `letter.after_1`…`10`, and `ticker.ending.answer`). Instead of squeezing 250-character
    /// letters into the 460 px radio ticker toast, letters open in a dedicated card with a header stamp (`LOG 0311` /
    /// `L4 ARK · R.`), readable line length, a gentle typewriter reveal that publishes rate-limited
    /// <see cref="UiTypewriterTick"/> (`UiTypeKind.Card`) cues, and a calm hold until read or dismissed.
    /// </summary>
    internal sealed class LetterCard
    {
        public const string EndingAnswerKey = "ticker.ending.answer";
        public const string LetterPrefix = "letter.";
        public const string MastPrefix = "letter.mast_";
        public const string AfterPrefix = "letter.after_";
        public const string DefaultArkStamp = "L4 ARK";
        public const string ArkLinkClass = "letter-card--ark";

        private const string MidDotSeparator = " · ";
        private const string EmDashSeparator = " — ";
        private const int MaxStampSearchIndex = 36;
        private const int MaxTrailingSignoffLength = 24;

        private readonly LetterCardSettings _settings;
        private readonly ILocalization _localization;
        private readonly EventBus _events;
        private readonly TickerText _formatter;
        private readonly Reveal _reveal;
        private readonly VisualElement _card;
        private readonly Label _caption;
        private readonly Label _stamp;
        private readonly Label _text;
        private readonly Label _sender;
        private readonly GlyphView _glyph;
        private readonly Label _close;
        private readonly Queue<TickerLine> _pending = new Queue<TickerLine>();

        private Phase _phase;
        private float _timer;
        private float _revealTimer;
        private float _sinceLastTick;
        private float _holdSeconds;
        private TickerLine _shown;
        private string _fullBody = string.Empty;
        private int _revealedChars;

        public LetterCard(UiLayout layout, LetterCardSettings settings, ILocalization localization, EventBus events)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _formatter = new TickerText(localization);
            _reveal = new Reveal(layout.LetterCard, settings.Reveal);
            _reveal.Snap(false);
            _card = layout.LetterCard;
            _caption = layout.LetterCardCaption;
            _stamp = layout.LetterCardStamp;
            _text = layout.LetterCardText;
            _sender = layout.LetterCardSender;
            _glyph = new GlyphView(layout.LetterCardGlyph, layout.LetterCardGlyphLabel);
            _close = layout.LetterCardClose;
            _close.text = localization.Get(UiKeys.CardClose);
            new ShadowPainter(layout.LetterCardShadow);
            new CassetteIconPainter(layout.LetterCardIcon);
        }

        private enum Phase
        {
            Idle,
            Waiting,
            Reading,
            Leaving,
        }

        /// <summary>True while a letter card is on screen (or fading).</summary>
        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True while a letter card is on screen, about to appear, or waiting its turn.</summary>
        public bool IsBusy => _phase != Phase.Idle || _pending.Count > 0;

        /// <summary>True while a letter card is up and can be closed with the cancel/pause button.</summary>
        public bool CanDismiss => _phase == Phase.Reading;

        /// <summary>True once the current letter's typewriter pass has revealed every character.</summary>
        public bool IsFullyRevealed => _phase == Phase.Reading && _revealedChars >= _fullBody.Length;

        /// <summary>The localization key of the letter currently on screen (null when none).</summary>
        public string Current => _shown.Key;

        /// <summary>Total hold time (reveal + rest) for the currently shown letter.</summary>
        public float HoldSeconds => _holdSeconds;

        /// <summary>Number of letters waiting in the queue.</summary>
        public int Waiting => _pending.Count;

        /// <summary>
        /// True when <paramref name="key"/> belongs to the long story-v4 letters (`letter.mast_1`…`10`,
        /// `letter.after_1`…`10`, or `ticker.ending.answer`) rather than the short radio ticker toast.
        /// </summary>
        public static bool IsLetterKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return key.StartsWith(LetterPrefix, StringComparison.Ordinal) ||
                   string.Equals(key, EndingAnswerKey, StringComparison.Ordinal);
        }

        /// <summary>True for the pre-launch crew mast letters (`letter.mast_*`); false for live Ark replies.</summary>
        public static bool IsCrewLetterKey(string key)
        {
            return !string.IsNullOrEmpty(key) && key.StartsWith(MastPrefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Splits a localized letter string into its header stamp (e.g. `LOG 0311`, `L4 ARK · S.`), main body text,
        /// and optional signoff (`— R.`).
        /// </summary>
        public static void ParseLetter(string key, string raw, out string stamp, out string body, out string sender)
        {
            stamp = DefaultArkStamp;
            sender = string.Empty;
            if (string.IsNullOrEmpty(raw))
            {
                body = string.Empty;
                return;
            }

            string working = raw;
            int searchLimit = Mathf.Min(working.Length, MaxStampSearchIndex);
            int firstDot = working.IndexOf(MidDotSeparator, 0, searchLimit, StringComparison.Ordinal);
            if (firstDot > 0)
            {
                int splitAt = firstDot;
                int afterFirst = firstDot + MidDotSeparator.Length;
                if (afterFirst < searchLimit)
                {
                    int secondDot = working.IndexOf(MidDotSeparator, afterFirst, searchLimit - afterFirst,
                        StringComparison.Ordinal);
                    if (secondDot > firstDot)
                    {
                        splitAt = secondDot;
                    }
                }

                stamp = working.Substring(0, splitAt);
                working = working.Substring(splitAt + MidDotSeparator.Length);
            }

            int lastDash = working.LastIndexOf(EmDashSeparator, StringComparison.Ordinal);
            if (lastDash > 0 && working.Length - lastDash <= MaxTrailingSignoffLength)
            {
                sender = working.Substring(lastDash + 1);
                working = working.Substring(0, lastDash);
            }

            body = working;
        }

        public void Enqueue(TickerLine line)
        {
            if (string.IsNullOrEmpty(line.Key))
            {
                return;
            }

            if (!_localization.TryGet(line.Key, out string _))
            {
                Debug.LogError($"{nameof(LetterCard)}: letter '{line.Key}' has no localized text; no card shown.");
                return;
            }

            _pending.Enqueue(line);
        }

        /// <summary>Extends the current letter's hold when a voiced recording is longer than the text hold.</summary>
        public void HoldAtLeast(string key, float seconds)
        {
            if (_phase == Phase.Reading && string.Equals(_shown.Key, key, StringComparison.Ordinal) &&
                seconds > _holdSeconds)
            {
                _holdSeconds = seconds;
            }
        }

        /// <summary>Closes the letter card early when the player presses Cancel or Pause.</summary>
        public void Dismiss()
        {
            if (_phase == Phase.Reading)
            {
                _events.Publish(new UiCue(UiCueKind.Back));
                Leave();
            }
        }

        /// <summary>Reveals the full letter text immediately (used when language changes after reveal or in captures).</summary>
        public void CompleteRevealImmediate()
        {
            if (_phase != Phase.Reading)
            {
                return;
            }

            _revealedChars = _fullBody.Length;
            _text.text = _fullBody;
            _timer = 0f;
            _holdSeconds = Mathf.Max(_holdSeconds, _settings.HoldAfterRevealSeconds);
        }

        public void Tick(float deltaTime, string closeGlyph, InputDeviceKind device, bool stageClear)
        {
            _glyph.Set(closeGlyph, device);
            switch (_phase)
            {
                case Phase.Idle:
                    if (_pending.Count > 0)
                    {
                        _phase = Phase.Waiting;
                        _timer = 0f;
                    }

                    break;
                case Phase.Waiting:
                    _timer += deltaTime;
                    if (_timer >= _settings.AppearDelay && stageClear)
                    {
                        Present(_pending.Dequeue());
                    }

                    break;
                case Phase.Reading:
                    if (_reveal.IsShown || _revealedChars < _fullBody.Length)
                    {
                        _timer += deltaTime;
                        StepTypewriter(deltaTime);
                    }

                    if (_revealedChars >= _fullBody.Length && _timer >= _holdSeconds)
                    {
                        Leave();
                    }

                    break;
                case Phase.Leaving:
                    if (_reveal.IsHidden)
                    {
                        _shown = default;
                        _fullBody = string.Empty;
                        _revealedChars = 0;
                        _phase = Phase.Idle;
                    }

                    break;
            }

            _reveal.Tick(deltaTime);
        }

        public void Relocalize()
        {
            _close.text = _localization.Get(UiKeys.CardClose);
            if (!string.IsNullOrEmpty(_shown.Key))
            {
                bool wasComplete = _revealedChars >= _fullBody.Length;
                Write(_shown, wasComplete ? int.MaxValue : _revealedChars);
            }
        }

        private void Present(TickerLine line)
        {
            _shown = line;
            bool isCrewLetter = IsCrewLetterKey(line.Key);
            _card.EnableInClassList(ArkLinkClass, !isCrewLetter);
            Write(line, 0);
            _holdSeconds = _settings.TotalHoldSeconds(_fullBody.Length);
            _timer = 0f;
            _revealTimer = 0f;
            _sinceLastTick = _settings.TickIntervalSeconds;
            _phase = Phase.Reading;
            _reveal.Show();
            _events.Publish(new UiCue(UiCueKind.CardShown));
            _events.Publish(new TickerLineShown(line.Key));
        }

        private void Write(TickerLine line, int initialRevealedChars)
        {
            bool isCrewLetter = IsCrewLetterKey(line.Key);
            _caption.text = _localization.Get(
                isCrewLetter ? UiKeys.CrewLogCaption : UiKeys.StationName(UpgradeStationKind.RadioTower));
            string formatted = _formatter.Format(line);
            ParseLetter(line.Key, formatted, out string stamp, out string body, out string sender);
            _stamp.text = stamp;
            _fullBody = body;
            _revealedChars = Mathf.Clamp(initialRevealedChars, 0, _fullBody.Length);
            _text.text = _revealedChars >= _fullBody.Length
                ? _fullBody
                : (_revealedChars == 0 ? string.Empty : _fullBody.Substring(0, _revealedChars));
            bool hasSender = !string.IsNullOrEmpty(sender);
            _sender.text = sender;
            _sender.style.display = hasSender ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void StepTypewriter(float deltaTime)
        {
            if (_revealedChars >= _fullBody.Length || deltaTime <= 0f)
            {
                return;
            }

            _revealTimer += deltaTime;
            _sinceLastTick += deltaTime;
            int target = Mathf.Min(_fullBody.Length, Mathf.FloorToInt(_revealTimer * _settings.CharactersPerSecond));
            if (target <= _revealedChars)
            {
                return;
            }

            bool spokeChar = false;
            for (int i = _revealedChars; i < target; i++)
            {
                if (!char.IsWhiteSpace(_fullBody[i]))
                {
                    spokeChar = true;
                    break;
                }
            }

            _revealedChars = target;
            _text.text = _revealedChars >= _fullBody.Length ? _fullBody : _fullBody.Substring(0, _revealedChars);
            if (spokeChar && _sinceLastTick >= _settings.TickIntervalSeconds)
            {
                _sinceLastTick = 0f;
                _events.Publish(new UiTypewriterTick(UiTypeKind.Card));
            }
        }

        private void Leave()
        {
            _phase = Phase.Leaving;
            _reveal.Hide();
        }
    }
}
