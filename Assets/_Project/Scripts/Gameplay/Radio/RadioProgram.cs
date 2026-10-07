using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What the base radio plays (Core's <see cref="IRadioProgram"/>, docs/features/M3-05): the cassettes 07 owns in
    /// the order it found them, and, once Bell is repaired, her dial. Before that the dial does not exist and the radio
    /// plays Lumen After Dark as it always has (collected tapes still join the shuffle). One turn of the dial moves one
    /// detent along Lumen After Dark, then the Tape Deck on each owned tape in turn, then Quiet Hours, and round again.
    /// Every change publishes <see cref="RadioProgramChanged"/>, and so does <see cref="Announce"/> once the save is
    /// loaded; Audio re-reads the program and owns the playback. Also the <see cref="IHeldItems"/> source for friends
    /// that need a tape (Bell). Pure; saved through <see cref="RadioSaveData"/>; nothing collected is ever lost.
    /// </summary>
    public sealed class RadioProgram : IRadioProgram, IHeldItems
    {
        private readonly EventBus _events;
        private readonly string[] _known;
        private readonly List<string> _owned;

        /// <param name="knownTapes">Every cassette id in the game (the cassette catalog).</param>
        public RadioProgram(EventBus events, IReadOnlyList<string> knownTapes)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            if (knownTapes == null)
            {
                throw new ArgumentNullException(nameof(knownTapes));
            }

            _known = new string[knownTapes.Count];
            for (int i = 0; i < _known.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(knownTapes[i]))
                {
                    throw new ArgumentException($"Cassette {i} has no id.", nameof(knownTapes));
                }

                _known[i] = knownTapes[i];
            }

            _owned = new List<string>(_known.Length);
        }

        public bool DialUnlocked { get; private set; }

        public RadioChannel Channel { get; private set; } = RadioChannel.LumenAfterDark;

        public string SelectedTape { get; private set; } = string.Empty;

        public int OwnedTapeCount => _owned.Count;

        public int TotalTapeCount => _known.Length;

        public string GetOwnedTape(int index)
        {
            return _owned[index];
        }

        public bool Owns(string tapeId)
        {
            return IndexOf(tapeId) >= 0;
        }

        public bool Holds(string itemId)
        {
            return Owns(itemId);
        }

        /// <summary>07 collected <paramref name="tapeId"/>; false (nothing changes) when it already owned it.</summary>
        public bool AddTape(string tapeId)
        {
            if (!IsKnown(tapeId))
            {
                throw new ArgumentException($"'{tapeId}' is not a cassette of the catalog.", nameof(tapeId));
            }

            if (Owns(tapeId))
            {
                return false;
            }

            _owned.Add(tapeId);
            Changed();
            return true;
        }

        /// <summary>Bell is repaired: the dial appears. False when it already had.</summary>
        public bool UnlockDial()
        {
            if (DialUnlocked)
            {
                return false;
            }

            DialUnlocked = true;
            Changed();
            return true;
        }

        /// <summary>One detented click of Bell's dial; false (nothing changes) before the dial exists.</summary>
        public bool TurnDial()
        {
            if (!DialUnlocked)
            {
                return false;
            }

            switch (Channel)
            {
                case RadioChannel.LumenAfterDark:
                    if (_owned.Count > 0)
                    {
                        Channel = RadioChannel.TapeDeck;
                        SelectedTape = _owned[0];
                    }
                    else
                    {
                        Channel = RadioChannel.QuietHours;
                    }

                    break;
                case RadioChannel.TapeDeck:
                    int next = IndexOf(SelectedTape) + 1;
                    if (next > 0 && next < _owned.Count)
                    {
                        SelectedTape = _owned[next];
                    }
                    else
                    {
                        Channel = RadioChannel.QuietHours;
                    }

                    break;
                default:
                    Channel = RadioChannel.LumenAfterDark;
                    break;
            }

            Changed();
            return true;
        }

        public RadioSaveData Capture()
        {
            return new RadioSaveData
            {
                tapes = _owned.ToArray(),
                channel = (int)Channel,
                selectedTape = SelectedTape,
                dialUnlocked = DialUnlocked,
            };
        }

        /// <summary>
        /// Applies saved progress on top of what is already known (nothing is ever taken away): the saved tapes come
        /// first in their order, unknown ids are dropped with a warning, and the channel always agrees with the dial
        /// and the tapes. Quiet: the owner calls <see cref="Announce"/> once the whole save is loaded.
        /// </summary>
        public void Restore(RadioSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var owned = new List<string>(_known.Length);
            foreach (string tape in data.tapes ?? Array.Empty<string>())
            {
                if (!IsKnown(tape))
                {
                    Debug.LogWarning($"{nameof(RadioProgram)}: the save holds an unknown cassette '{tape}'; " +
                                     "cassette ids must never be renamed.");
                    continue;
                }

                if (!owned.Contains(tape))
                {
                    owned.Add(tape);
                }
            }

            foreach (string tape in _owned)
            {
                if (!owned.Contains(tape))
                {
                    owned.Add(tape);
                }
            }

            _owned.Clear();
            _owned.AddRange(owned);
            DialUnlocked |= data.dialUnlocked;
            var channel = (RadioChannel)data.channel;
            bool valid = channel == RadioChannel.LumenAfterDark || channel == RadioChannel.TapeDeck ||
                         channel == RadioChannel.QuietHours;
            Channel = DialUnlocked && valid ? channel : RadioChannel.LumenAfterDark;
            SelectedTape = Owns(data.selectedTape) ? data.selectedTape : string.Empty;
            if (Channel == RadioChannel.TapeDeck && SelectedTape.Length == 0)
            {
                if (_owned.Count > 0)
                {
                    SelectedTape = _owned[0];
                }
                else
                {
                    Channel = RadioChannel.LumenAfterDark;
                }
            }
        }

        /// <summary>
        /// Publishes <see cref="RadioProgramChanged"/> for the program as it stands: once after the save is loaded
        /// (restored or fresh), so listeners start from the real program.
        /// </summary>
        public void Announce()
        {
            Changed();
        }

        private int IndexOf(string tapeId)
        {
            for (int i = 0; i < _owned.Count; i++)
            {
                if (string.Equals(_owned[i], tapeId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsKnown(string tapeId)
        {
            for (int i = 0; i < _known.Length; i++)
            {
                if (string.Equals(_known[i], tapeId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void Changed()
        {
            _events.Publish(new RadioProgramChanged());
        }
    }
}
