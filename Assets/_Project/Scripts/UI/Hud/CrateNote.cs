using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// A short note on a calm glass card low on the screen: the packing note of a crew crate that was just cut open
    /// (docs/features/M3-25 T4, as its crew member wrote it) or what a scan found (docs/design/0.5.5-owner-feedback.md
    /// §1), resting as long as it takes to read and then fading by itself.
    /// It never takes the player's input or the view, so driving goes on under it; notes that arrive together wait
    /// their turn, and the card waits while a bigger card (a memory, a reward) holds the screen.
    /// </summary>
    internal sealed class CrateNote
    {
        public const string ScanClass = "crate-note--scan";
        public const string PaperClass = "crate-note--paper";

        private readonly CrateNoteSettings _settings;
        private readonly ILocalization _localization;
        private readonly VisualElement _root;
        private readonly Reveal _reveal;
        private readonly Label _caption;
        private readonly Label _text;
        private readonly Queue<Note> _waiting = new Queue<Note>();
        private Note _note;
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
            _root = layout.CrateNote;
            _reveal = new Reveal(_root, settings.Reveal);
            _reveal.Snap(false);
            _caption = _root.Q<Label>("crate-note-caption")
                       ?? throw new InvalidOperationException("GameUI.uxml has no crate-note-caption.");
            _text = _root.Q<Label>("crate-note-text")
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
            _waiting.Enqueue(new Note(UiKeys.CrateCaption, CrewCrateKeys.Note(siteId), null));
        }

        /// <summary>A scan found something: <paramref name="text"/> (already worded) shows under the Scan caption.</summary>
        public void Scanned(string text)
        {
            _waiting.Enqueue(new Note(UiKeys.HintScan, null, text));
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
                _note = _waiting.Dequeue();
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
            bool isScan = _note.CaptionKey == UiKeys.HintScan;
            _root.EnableInClassList(ScanClass, isScan);
            _root.EnableInClassList(PaperClass, !isScan);
            _caption.text = _localization.Get(_note.CaptionKey);
            _text.text = _note.Text ?? _localization.Get(_note.Key);
        }

        private readonly struct Note
        {
            public Note(string captionKey, string key, string text)
            {
                CaptionKey = captionKey;
                Key = key;
                Text = text;
            }

            public string CaptionKey { get; }

            /// <summary>The localization key of the note, or null when <see cref="Text"/> is already worded.</summary>
            public string Key { get; }

            public string Text { get; }
        }
    }
}
