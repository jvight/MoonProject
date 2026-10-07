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
    /// The story card. When a relic settles on the museum shelf it tells its memory of Earth (the localized
    /// "relic.&lt;id&gt;.name" and "relic.&lt;id&gt;.memory" strings); when a friend wakes it shows the crew log the
    /// friend remembers ("friend.&lt;id&gt;.name" and "friend.&lt;id&gt;.repair_log"). It fades in a moment later,
    /// stays long enough to read at a calm pace (longer texts stay longer), then fades away. It never takes input
    /// focus, so driving is never blocked; the cancel button (shown on the card) closes it early. Cards that arrive
    /// while one is up wait their turn, and a card never appears while the radio ticker's line is still on screen.
    /// </summary>
    internal sealed class MemoryCard
    {
        private readonly MemoryCardSettings _settings;
        private readonly ILocalization _localization;
        private readonly EventBus _events;
        private readonly RelicCatalog _catalog;
        private readonly IFriendStatuses _friends;
        private readonly Reveal _reveal;
        private readonly Label _caption;
        private readonly Label _name;
        private readonly Label _memory;
        private readonly GlyphView _glyph;
        private readonly Label _close;
        private readonly Queue<Memory> _pending = new Queue<Memory>();
        private Phase _phase;
        private float _timer;
        private float _readSeconds;
        private Memory _shown;

        public MemoryCard(UiLayout layout, MemoryCardSettings settings, ILocalization localization, EventBus events,
            RelicCatalog catalog, IFriendStatuses friends)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _catalog = catalog != null ? catalog : throw new ArgumentNullException(nameof(catalog));
            _friends = friends ?? throw new ArgumentNullException(nameof(friends));
            _reveal = new Reveal(layout.MemoryCard, settings.Reveal);
            _reveal.Snap(false);
            _caption = layout.MemoryCardCaption;
            _name = layout.MemoryCardName;
            _memory = layout.MemoryCardText;
            _glyph = new GlyphView(layout.MemoryCardGlyph, layout.MemoryCardGlyphLabel);
            _close = layout.MemoryCardClose;
            _close.text = localization.Get(UiKeys.CardClose);
            new ShadowPainter(layout.MemoryCardShadow);
        }

        private enum Phase
        {
            Idle,
            Waiting,
            Reading,
            Leaving,
        }

        /// <summary>True while a card is on screen (or fading).</summary>
        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True while a card is on screen, about to appear, or waiting its turn.</summary>
        public bool IsBusy => _phase != Phase.Idle || _pending.Count > 0;

        /// <summary>True while a card is up and can be closed with the cancel button.</summary>
        public bool CanDismiss => _phase == Phase.Reading;

        /// <summary>The id of the relic or friend whose card is up (null when none).</summary>
        public string Current => _shown.Id;

        /// <summary>
        /// Queues the memory of <paramref name="relicId"/>; an unknown id is a content bug and is logged.
        /// </summary>
        public void Enqueue(string relicId, int displayedCount)
        {
            if (!InCatalog(relicId))
            {
                Debug.LogError($"{nameof(MemoryCard)}: relic '{relicId}' is not in the RelicCatalog; no card shown.");
                return;
            }

            _pending.Enqueue(Memory.Relic(relicId, displayedCount));
        }

        /// <summary>
        /// Queues the log friend <paramref name="friendId"/> remembers on waking; an unknown id is a wiring bug and is
        /// logged.
        /// </summary>
        public void EnqueueLog(string friendId)
        {
            if (!IsFriend(friendId))
            {
                Debug.LogError($"{nameof(MemoryCard)}: friend '{friendId}' is not in the friend roster; no card.");
                return;
            }

            _pending.Enqueue(Memory.Log(friendId));
        }

        /// <summary>Closes the card early (the cancel or pause button).</summary>
        public void Dismiss()
        {
            if (_phase == Phase.Reading)
            {
                _events.Publish(new UiCue(UiCueKind.Back));
                Leave();
            }
        }

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused so reading time waits too.</param>
        /// <param name="closeGlyph">The cancel control's label for the active device.</param>
        /// <param name="device">The active device (key cap or round glyph).</param>
        /// <param name="stageClear">False while the radio ticker's line is still easing away: the card waits.</param>
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
                    if (_reveal.IsShown)
                    {
                        _timer += deltaTime;
                    }

                    if (_timer >= _readSeconds)
                    {
                        Leave();
                    }

                    break;
                case Phase.Leaving:
                    if (_reveal.IsHidden)
                    {
                        _shown = default;
                        _phase = Phase.Idle;
                    }

                    break;
            }

            _reveal.Tick(deltaTime);
        }

        /// <summary>Re-reads every string in the new language (the card on screen changes in place).</summary>
        public void Relocalize()
        {
            _close.text = _localization.Get(UiKeys.CardClose);
            if (_shown.Id != null)
            {
                Write(_shown);
            }
        }

        private void Present(Memory memory)
        {
            _shown = memory;
            Write(memory);
            _readSeconds = _settings.ReadSeconds(_name.text.Length + _memory.text.Length);
            _timer = 0f;
            _phase = Phase.Reading;
            _reveal.Show();
            _events.Publish(new UiCue(UiCueKind.CardShown));
        }

        private void Write(Memory memory)
        {
            string name = _localization.Get(memory.NameKey);
            _caption.text = memory.IsLog
                ? string.Format(_localization.Get(UiKeys.LogCaption), name)
                : string.Format(_localization.Get(UiKeys.CardCaption), memory.DisplayedCount, _catalog.Relics.Count);
            _name.text = name;
            _memory.text = _localization.Get(memory.BodyKey);
        }

        private void Leave()
        {
            _phase = Phase.Leaving;
            _reveal.Hide();
        }

        private bool InCatalog(string relicId)
        {
            IReadOnlyList<RelicDefinition> relics = _catalog.Relics;
            for (int i = 0; i < relics.Count; i++)
            {
                if (relics[i] != null && string.Equals(relics[i].Id, relicId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFriend(string friendId)
        {
            for (int i = 0; i < _friends.Count; i++)
            {
                if (string.Equals(_friends.Definition(i).Id, friendId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct Memory
        {
            private Memory(string id, bool isLog, int displayedCount, string nameKey, string bodyKey)
            {
                Id = id;
                IsLog = isLog;
                DisplayedCount = displayedCount;
                NameKey = nameKey;
                BodyKey = bodyKey;
            }

            public string Id { get; }

            /// <summary>A friend's crew log rather than a relic's memory.</summary>
            public bool IsLog { get; }

            public int DisplayedCount { get; }

            public string NameKey { get; }

            public string BodyKey { get; }

            public static Memory Relic(string relicId, int displayedCount)
            {
                return new Memory(relicId, false, displayedCount, UiKeys.RelicName(relicId),
                    UiKeys.RelicMemory(relicId));
            }

            public static Memory Log(string friendId)
            {
                return new Memory(friendId, true, 0, UiKeys.FriendName(friendId), UiKeys.FriendRepairLog(friendId));
            }
        }
    }
}
