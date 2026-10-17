using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The reward beat of a scan (docs/design/opening-v2.md §3, §5): when 07 learns a blueprint a glass card stays up
    /// for a few seconds and says "Blueprint learned: Charging Pad". Cards wait their turn behind one another and
    /// behind whatever else holds the guidance slot; it is a card, so it is part of the one guidance slot.
    /// </summary>
    internal sealed class UnlockCard
    {
        private readonly UnlockCardSettings _settings;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private readonly Label _verb;
        private readonly Queue<string> _waiting = new Queue<string>();
        private string _shown = string.Empty;
        private bool _showing;
        private bool _leaving;
        private float _timer;

        public UnlockCard(UiLayout layout, UnlockCardSettings settings, ILocalization localization)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            VisualElement root = layout.UnlockCard;
            _reveal = new Reveal(root, settings.Reveal);
            _reveal.Snap(false);
            _verb = Require<Label>(root, "unlock-card-verb");
        }

        /// <summary>True while a card is on screen (or easing).</summary>
        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True while a card waits its turn.</summary>
        public bool IsWaiting => _waiting.Count > 0;

        /// <summary>The structure of the card on screen, or empty (tests and captures).</summary>
        public string Shown => _shown;

        /// <summary>Queues the card of the structure <paramref name="structureId"/> that 07 just learned.</summary>
        public void Learned(string structureId)
        {
            if (string.IsNullOrEmpty(structureId))
            {
                throw new ArgumentException("A learned blueprint needs a structure id.", nameof(structureId));
            }

            _waiting.Enqueue(structureId);
        }

        /// <summary>Re-reads the words in the new language.</summary>
        public void Relocalize()
        {
            if (_showing || _leaving)
            {
                Write();
            }
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        /// <param name="open">False while another block holds the screen.</param>
        public void Tick(float deltaTime, bool open)
        {
            if (!_showing && !_leaving && open && _waiting.Count > 0)
            {
                _shown = _waiting.Dequeue();
                Write();
                _showing = true;
                _timer = 0f;
                _reveal.Show();
            }

            if (_showing)
            {
                _timer += deltaTime;
                if (_timer >= _settings.HoldSeconds)
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
                _shown = string.Empty;
            }
        }

        private void Write()
        {
            _verb.text = string.Format(_localization.Get(UiKeys.BlueprintLearned),
                _localization.Get(StructureIds.NameKey(_shown)));
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"GameUI.uxml has no {typeof(T).Name} named '{name}' under '{root.name}'.");
        }
    }
}
