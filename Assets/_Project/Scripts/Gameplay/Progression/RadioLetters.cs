using System;
using System.Collections.Generic;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The radio's fragments (docs/design/text/radio-letters.md). As tower level 1 clears the radio, the station's
    /// automated weather loop plays once. Each mast 07 lights then plays back its share of the letters in time order,
    /// whichever mast it is, so the story always reads in order: one letter per mast for the first few (the relay's
    /// official messages), then more per mast (Ro's own letters), until the letters run out. Each goes to the ticker
    /// (<see cref="TickerLine"/>), which shows them one at a time, on the <see cref="Tick"/> after the mast's own
    /// "online" line. A load replays nothing: its announcements are restored, and only a mast lit now plays.
    /// </summary>
    public sealed class RadioLetters : IDisposable
    {
        private readonly EventBus _events;
        private readonly int _singleLetterMasts;
        private readonly int _lettersAfter;
        private readonly int _letterCount;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private int _played;
        private int _due;

        /// <param name="singleLetterMasts">The first this many masts play one letter each...</param>
        /// <param name="lettersAfter">...and every later mast this many.</param>
        /// <param name="letterCount">Letters there are (letter.mast_1 .. letter.mast_n).</param>
        public RadioLetters(EventBus events, int singleLetterMasts, int lettersAfter, int letterCount)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _singleLetterMasts = singleLetterMasts;
            _lettersAfter = lettersAfter;
            _letterCount = letterCount;
            _subscriptions.Add(events.Subscribe<BasePowerChanged>(OnPower));
            _subscriptions.Add(events.Subscribe<RelayRestored>(OnRestored));
        }

        /// <summary>How many letters the first <paramref name="litMasts"/> masts have played between them.</summary>
        public int LettersBy(int litMasts)
        {
            int masts = Math.Max(0, litMasts);
            int single = Math.Min(masts, _singleLetterMasts);
            int letters = single + (masts - single) * _lettersAfter;
            return Math.Min(letters, _letterCount);
        }

        /// <summary>Plays the letters the masts lit since the last tick, in order.</summary>
        public void Tick()
        {
            while (_played < _due)
            {
                _played++;
                _events.Publish(new TickerLine(ProgressionKeys.LetterPrefix + _played));
            }
        }

        public void Dispose()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        private void OnPower(BasePowerChanged change)
        {
            if (!change.Restored && change.Stage == BasePowerStage.Home)
            {
                _events.Publish(new TickerLine(ProgressionKeys.RadioWeather));
            }
        }

        /// <summary>Mast number <c>LitCount</c> just came online: its share of the letters is due.</summary>
        private void OnRestored(RelayRestored restored)
        {
            _played = Math.Max(_played, LettersBy(restored.LitCount - 1));
            _due = Math.Max(_due, LettersBy(restored.LitCount));
        }
    }
}
