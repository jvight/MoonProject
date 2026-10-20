using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// The call on 07's CRT face (docs/design/opening-v3-plan.md 2.4), in look A's grammar: a quiet word, the moving
    /// waveform and the sender's letters, where the decoded ones are the one accent and the hidden ones stay muted
    /// question marks. It eases in and out and builds its letter cells once, when the sender is first shown.
    /// </summary>
    internal sealed class CallDisplayView
    {
        public const string RootClass = "call-display";
        public const string WordClass = "call-display__word";
        public const string WaveClass = "call-display__wave";
        public const string LettersClass = "call-display__letters";
        public const string LetterClass = "call-display__letter";
        public const string DecodedClass = "call-display__letter--decoded";

        private readonly VisualElement _root;
        private readonly Label _word;
        private readonly VisualElement _letterRow;
        private readonly CallWavePainter _wave;
        private readonly CallDisplaySettings _settings;
        private readonly ILocalization _localization;
        private Label[] _letters = Array.Empty<Label>();
        private string _sender;
        private int _decoded = -1;
        private float _openness;
        private float _clock;

        public CallDisplayView(VisualElement parent, CallDisplaySettings settings, ILocalization localization)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _root = new VisualElement { name = "call-display", pickingMode = PickingMode.Ignore };
            _root.AddToClassList(RootClass);
            _word = new Label { name = "call-display-word", pickingMode = PickingMode.Ignore };
            _word.AddToClassList(WordClass);
            var wave = new VisualElement { name = "call-display-wave", pickingMode = PickingMode.Ignore };
            wave.AddToClassList(WaveClass);
            _letterRow = new VisualElement { name = "call-display-letters", pickingMode = PickingMode.Ignore };
            _letterRow.AddToClassList(LettersClass);
            _root.Add(_word);
            _root.Add(wave);
            _root.Add(_letterRow);
            parent.Add(_root);
            _wave = new CallWavePainter(wave, settings);
            Relocalize();
            Apply();
        }

        /// <summary>On screen, also while it eases out (the guidance slot waits for it).</summary>
        public bool IsVisible => _openness > 0f;

        /// <summary>The letters as shown (tests): decoded characters and question marks.</summary>
        public string Letters { get; private set; } = string.Empty;

        /// <summary>Letter cells drawn in the accent (tests).</summary>
        public int AccentLetters { get; private set; }

        public void Relocalize()
        {
            _word.text = _localization.Get(UiKeys.FaceCall);
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused, which also stills the wave.</param>
        /// <param name="shown">The display has the face now.</param>
        public void Tick(float deltaTime, bool shown, CallDisplay call)
        {
            float step = deltaTime / _settings.FadeSeconds;
            float openness = Mathf.MoveTowards(_openness, shown ? 1f : 0f, step);
            if (openness <= 0f && _openness <= 0f)
            {
                return;
            }

            _openness = openness;
            _clock += deltaTime;
            SetLetters(call.Sender, call.Decoded, call.Letters);
            _wave.Set(_clock, call.State);
            Apply();
        }

        /// <summary>Something else takes the face: the call leaves at once rather than fading under it.</summary>
        public void HideNow()
        {
            if (_openness <= 0f)
            {
                return;
            }

            _openness = 0f;
            Apply();
        }

        private void SetLetters(string sender, int decoded, string letters)
        {
            if (sender == null || (ReferenceEquals(sender, _sender) && decoded == _decoded))
            {
                return;
            }

            if (!ReferenceEquals(sender, _sender))
            {
                _sender = sender;
                _letterRow.Clear();
                _letters = new Label[sender.Length];
                for (int i = 0; i < sender.Length; i++)
                {
                    _letters[i] = new Label { pickingMode = PickingMode.Ignore };
                    _letters[i].AddToClassList(LetterClass);
                    _letterRow.Add(_letters[i]);
                }
            }

            _decoded = decoded;
            Letters = letters;
            int accent = 0;
            for (int i = 0; i < _letters.Length; i++)
            {
                bool known = letters[i] == sender[i] && sender[i] != ' ';
                _letters[i].text = letters[i].ToString();
                _letters[i].EnableInClassList(DecodedClass, known);
                accent += known ? 1 : 0;
            }

            AccentLetters = accent;
        }

        private void Apply()
        {
            // Smoothstep: the face's light eases on and off rather than ramping.
            float eased = _openness * _openness * (3f - 2f * _openness);
            _root.style.opacity = eased;
            _root.style.display = _openness > 0f ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
