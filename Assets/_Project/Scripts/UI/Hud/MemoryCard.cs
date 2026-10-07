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
    /// friend remembers ("friend.&lt;id&gt;.name" and "friend.&lt;id&gt;.repair_log"); a crew log found in the world
    /// shows its "log.&lt;id&gt;" text; a collected cassette turns it into a smaller, warmer liner-note card with the
    /// tape's title, Ro's note ("cassette.&lt;id&gt;.title" and ".note") and how many tapes 07 now owns. It fades in a
    /// moment later, stays long enough to read at a calm pace (longer texts stay longer), then fades away. It never
    /// takes input focus, so driving is never blocked; the cancel button (shown on the card) closes it early. Every
    /// kind shares one queue, so cards never overlap, and a card never appears while the radio ticker's line is still
    /// on screen.
    /// </summary>
    internal sealed class MemoryCard
    {
        /// <summary>The liner-note look (USS): narrower and amber-edged, with the cassette icon and the count.</summary>
        public const string LinerClass = "memory-card--liner";

        /// <summary>A card without a name line (a crew log: its caption says what it is).</summary>
        public const string UntitledClass = "memory-card--untitled";

        private readonly MemoryCardSettings _settings;
        private readonly ILocalization _localization;
        private readonly EventBus _events;
        private readonly RelicCatalog _catalog;
        private readonly IFriendStatuses _friends;
        private readonly Reveal _reveal;
        private readonly VisualElement _card;
        private readonly Label _caption;
        private readonly Label _count;
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
            _card = layout.MemoryCard;
            _caption = layout.MemoryCardCaption;
            _count = layout.MemoryCardCount;
            _name = layout.MemoryCardName;
            _memory = layout.MemoryCardText;
            _glyph = new GlyphView(layout.MemoryCardGlyph, layout.MemoryCardGlyphLabel);
            _close = layout.MemoryCardClose;
            _close.text = localization.Get(UiKeys.CardClose);
            new ShadowPainter(layout.MemoryCardShadow);
            new CassetteIconPainter(layout.MemoryCardIcon);
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

        /// <summary>The id of the relic, friend, crew log or cassette whose card is up (null when none).</summary>
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

        /// <summary>
        /// Queues crew log <paramref name="logId"/> (its "log.&lt;id&gt;" text); a log without text is a wiring bug and
        /// is logged.
        /// </summary>
        public void EnqueueCrewLog(string logId)
        {
            string key = UiKeys.CrewLog(logId);
            if (!_localization.TryGet(key, out string _))
            {
                Debug.LogError($"{nameof(MemoryCard)}: crew log '{logId}' has no '{key}' text; no card.");
                return;
            }

            _pending.Enqueue(Memory.CrewLog(logId, key));
        }

        /// <summary>
        /// Queues the liner notes of tape <paramref name="cassetteId"/>, which made <paramref name="collected"/> of
        /// <paramref name="total"/>; a tape without its title and note is a wiring bug and is logged.
        /// </summary>
        public void EnqueueCassette(string cassetteId, int collected, int total)
        {
            string title = UiKeys.CassetteTitle(cassetteId);
            string note = UiKeys.CassetteNote(cassetteId);
            if (!_localization.TryGet(title, out string _) || !_localization.TryGet(note, out string _))
            {
                Debug.LogError(
                    $"{nameof(MemoryCard)}: cassette '{cassetteId}' needs '{title}' and '{note}' texts; no card.");
                return;
            }

            _pending.Enqueue(Memory.Liner(cassetteId, collected, total, title, note));
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
            _card.EnableInClassList(LinerClass, memory.Kind == MemoryKind.Liner);
            _card.EnableInClassList(UntitledClass, memory.Kind == MemoryKind.CrewLog);
            Write(memory);
            _readSeconds = _settings.ReadSeconds(_name.text.Length + _memory.text.Length);
            _timer = 0f;
            _phase = Phase.Reading;
            _reveal.Show();
            _events.Publish(new UiCue(UiCueKind.CardShown));
        }

        private void Write(Memory memory)
        {
            string name = memory.NameKey != null ? _localization.Get(memory.NameKey) : string.Empty;
            switch (memory.Kind)
            {
                case MemoryKind.Relic:
                    _caption.text = string.Format(_localization.Get(UiKeys.CardCaption), memory.Count,
                        _catalog.Relics.Count);
                    break;
                case MemoryKind.FriendLog:
                    _caption.text = string.Format(_localization.Get(UiKeys.LogCaption), name);
                    break;
                case MemoryKind.CrewLog:
                    _caption.text = _localization.Get(UiKeys.CrewLogCaption);
                    break;
                case MemoryKind.Liner:
                    _caption.text = _localization.Get(UiKeys.LinerCaption);
                    _count.text = string.Format(_localization.Get(UiKeys.TapeCount), memory.Count, memory.Total);
                    break;
            }

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

        private enum MemoryKind
        {
            /// <summary>A relic's memory of Earth, numbered among the museum's relics.</summary>
            Relic,

            /// <summary>The crew log a friend remembers when it wakes.</summary>
            FriendLog,

            /// <summary>A crew log found in the world (a cache).</summary>
            CrewLog,

            /// <summary>A cassette's liner notes, with the tape count.</summary>
            Liner,
        }

        private readonly struct Memory
        {
            private Memory(string id, MemoryKind kind, int count, int total, string nameKey, string bodyKey)
            {
                Id = id;
                Kind = kind;
                Count = count;
                Total = total;
                NameKey = nameKey;
                BodyKey = bodyKey;
            }

            public string Id { get; }

            public MemoryKind Kind { get; }

            /// <summary>The relic's place on the shelf, or the tapes owned after this one.</summary>
            public int Count { get; }

            /// <summary>Tapes in the game (liner notes only).</summary>
            public int Total { get; }

            /// <summary>The name line's key; null for a card without one.</summary>
            public string NameKey { get; }

            public string BodyKey { get; }

            public static Memory Relic(string relicId, int displayedCount)
            {
                return new Memory(relicId, MemoryKind.Relic, displayedCount, 0, UiKeys.RelicName(relicId),
                    UiKeys.RelicMemory(relicId));
            }

            public static Memory Log(string friendId)
            {
                return new Memory(friendId, MemoryKind.FriendLog, 0, 0, UiKeys.FriendName(friendId),
                    UiKeys.FriendRepairLog(friendId));
            }

            public static Memory CrewLog(string logId, string textKey)
            {
                return new Memory(logId, MemoryKind.CrewLog, 0, 0, null, textKey);
            }

            public static Memory Liner(string cassetteId, int collected, int total, string titleKey, string noteKey)
            {
                return new Memory(cassetteId, MemoryKind.Liner, collected, total, titleKey, noteKey);
            }
        }
    }
}
