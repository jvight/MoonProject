using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The packing note of a crew crate that was just cut open (docs/features/M3-25 T4): a calm glass card low on the
    /// screen with the note as its crew member wrote it, resting as long as it takes to read and then fading by itself.
    /// It never takes the player's input or the view, so driving goes on under it; notes that arrive together wait
    /// their turn, and the card waits while a bigger card (a memory, a reward) holds the screen.
    /// </summary>
    internal sealed class CrateNote
    {
        private readonly CrateNoteSettings _settings;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private readonly Label _caption;
        private readonly Label _text;
        private readonly Queue<string> _waiting = new Queue<string>();
        private string _key;
        private bool _showing;
        private bool _leaving;
        private float _timer;
        private float _hold;

        public CrateNote(UiLayout layout, CrateNoteSettings settings, ILocalization localization)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _reveal = new Reveal(layout.CrateNote, settings.Reveal);
            _reveal.Snap(false);
            _caption = layout.CrateNote.Q<Label>("crate-note-caption")
                       ?? throw new InvalidOperationException("GameUI.uxml has no crate-note-caption.");
            _text = layout.CrateNote.Q<Label>("crate-note-text")
                    ?? throw new InvalidOperationException("GameUI.uxml has no crate-note-text.");
        }

        /// <summary>True while a note is on screen (or easing).</summary>
        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True while a note waits its turn.</summary>
        public bool IsWaiting => _waiting.Count > 0;

        /// <summary>The words on the card now (tests and captures).</summary>
        public string Text => _text.text;

        /// <summary>A crate at <paramref name="siteId"/> was opened: its note will show.</summary>
        public void Opened(string siteId)
        {
            _waiting.Enqueue(CrewCrateKeys.Note(siteId));
        }

        /// <summary>Re-reads the words in the new language (a note on screen changes in place).</summary>
        public void Relocalize()
        {
            if (_showing || _leaving)
            {
                Write();
            }
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="open">False while a bigger card holds the screen.</param>
        public void Tick(float deltaTime, bool open)
        {
            if (!_showing && !_leaving && open && _waiting.Count > 0)
            {
                _key = _waiting.Dequeue();
                Write();
                _hold = _settings.HoldSeconds(_text.text.Length);
                _timer = 0f;
                _showing = true;
                _reveal.Show();
            }

            if (_showing)
            {
                _timer += deltaTime;
                if (_timer >= _hold)
                {
                    _showing = false;
                    _leaving = true;
                    _reveal.Hide();
                }
            }

            _reveal.Tick(deltaTime);
            if (_leaving && _reveal.IsHidden)
            {
                _leaving = false;
            }
        }

        private void Write()
        {
            _caption.text = _localization.Get(UiKeys.CrateCaption);
            _text.text = _localization.Get(_key);
        }
    }
}
