using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The credits screen (docs: the licence compliance task): a Variant C CRT panel opened from the title's
    /// Credits entry, filled once from <see cref="CreditsData"/> and the localization tables, drifting up slowly by
    /// itself. The player can scroll it with the mouse wheel, the stick or the up and down keys, which holds the drift
    /// for a few seconds; Back (or Esc, B) closes it. A language change rewrites the text in place.
    /// </summary>
    internal sealed class CreditsView
    {
        private const string HeadingClass = "credits__heading";
        private const string NameClass = "credits__name";
        private const string DetailClass = "credits__detail";
        private const string NoteClass = "credits__note";
        private const string VersionClass = "credits__version";
        private const float EndEpsilon = 0.5f;
        private const float LeadIn = 0.6f;
        private const float LeadOut = 0.5f;

        private readonly CreditsData _data;
        private readonly CreditsSettings _settings;
        private readonly ILocalization _localization;
        private readonly UiLayout _layout;
        private readonly Reveal _reveal;
        private readonly ScrollView _scroll;
        private readonly Label _version;
        private float _delay;
        private float _hold;
        private float _written;
        private float _padded;
        private bool _built;

        public CreditsView(UiLayout layout, CreditsSettings settings, ILocalization localization, string json)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _data = JsonUtility.FromJson<CreditsData>(json ?? throw new ArgumentNullException(nameof(json)));
            if (_data == null || _data.sections == null || _data.sections.Length == 0)
            {
                throw new ArgumentException("The credits asset has no sections.", nameof(json));
            }

            _reveal = new Reveal(layout.Credits, settings.Reveal);
            _reveal.Snap(false);
            _scroll = layout.CreditsScroll;
            _version = new Label { pickingMode = PickingMode.Ignore };
            _layout.CreditsBack.RegisterCallback<NavigationMoveEvent>(OnNavigate);
        }

        public bool IsOpen => _reveal.Target;

        /// <summary>0..1 how far the panel has eased in.</summary>
        public float Visibility => _reveal.Visibility;

        /// <summary>The panel's scroll offset (tests and captures).</summary>
        public float Offset => _scroll.scrollOffset.y;

        /// <summary>The credits come up from the top and begin to drift after a short rest.</summary>
        public void Open()
        {
            if (!_built)
            {
                Build();
            }

            _scroll.scrollOffset = Vector2.zero;
            _written = 0f;
            _delay = _settings.StartDelaySeconds;
            _hold = 0f;
            _reveal.Show();
        }

        public void Close()
        {
            _reveal.Hide();
        }

        /// <summary>Writes the credits again in the language now in use.</summary>
        public void Relocalize()
        {
            if (_built)
            {
                Build();
            }

            _layout.CreditsTitle.text = _localization.Get(UiKeys.CreditsTitle);
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        public void Tick(float deltaTime)
        {
            _reveal.Tick(deltaTime);
            if (!_reveal.Target)
            {
                return;
            }

            Pad();
            float offset = _scroll.scrollOffset.y;
            if (Mathf.Abs(offset - _written) > EndEpsilon)
            {
                _hold = _settings.ResumeAfterSeconds;
                _written = offset;
                return;
            }

            if (_hold > 0f)
            {
                _hold -= deltaTime;
                return;
            }

            if (_delay > 0f)
            {
                _delay -= deltaTime;
                return;
            }

            float end = Mathf.Max(0f, _scroll.contentContainer.layout.height - _scroll.contentViewport.layout.height);
            if (_padded <= 0f || offset >= end)
            {
                return;
            }

            _written = Mathf.Min(end, offset + _settings.ScrollSpeed * deltaTime);
            _scroll.scrollOffset = new Vector2(0f, _written);
        }

        /// <summary>
        /// Film credits: the text starts a little below the middle of the panel and rises through it, and ends with its
        /// last line half way up, so there is always something to drift, at any text size.
        /// </summary>
        private void Pad()
        {
            float viewport = _scroll.contentViewport.layout.height;
            if (float.IsNaN(viewport) || viewport <= 0f || Mathf.Abs(viewport - _padded) <= 1f)
            {
                return;
            }

            _padded = viewport;
            _scroll.contentContainer.style.paddingTop = viewport * LeadIn;
            _scroll.contentContainer.style.paddingBottom = viewport * LeadOut;
        }

        private void OnNavigate(NavigationMoveEvent evt)
        {
            float step = evt.direction == NavigationMoveEvent.Direction.Down ? _settings.LineStepPixels
                : evt.direction == NavigationMoveEvent.Direction.Up ? -_settings.LineStepPixels : 0f;
            if (step == 0f)
            {
                return;
            }

            float end = Mathf.Max(0f, _scroll.contentContainer.layout.height - _scroll.contentViewport.layout.height);
            _scroll.scrollOffset = new Vector2(0f, Mathf.Clamp(_scroll.scrollOffset.y + step, 0f, end));
            evt.StopPropagation();
        }

        /// <summary>Builds the labels from the data (allocates; once, and again on a language change).</summary>
        private void Build()
        {
            _layout.CreditsTitle.text = _localization.Get(UiKeys.CreditsTitle);
            VisualElement content = _scroll.contentContainer;
            content.Clear();
            for (int s = 0; s < _data.sections.Length; s++)
            {
                CreditsSection section = _data.sections[s];
                content.Add(Line(_localization.Get(section.headingKey), HeadingClass));
                if (s == 0)
                {
                    _version.text = string.Format(_localization.Get(UiKeys.CreditsVersion), Application.version);
                    _version.ClearClassList();
                    _version.AddToClassList(VersionClass);
                    content.Add(_version);
                }

                foreach (CreditsEntry entry in section.entries)
                {
                    string name = string.IsNullOrEmpty(entry.nameKey) ? entry.name : _localization.Get(entry.nameKey);
                    content.Add(Line(name, NameClass));
                    string detail = string.IsNullOrEmpty(entry.detail) && !string.IsNullOrEmpty(entry.detailKey)
                        ? _localization.Get(entry.detailKey)
                        : entry.detail;
                    if (!string.IsNullOrEmpty(detail))
                    {
                        content.Add(Line(detail, DetailClass));
                    }

                    if (!string.IsNullOrEmpty(entry.note))
                    {
                        content.Add(Line(entry.note, NoteClass));
                    }
                }
            }

            _built = true;
        }

        private static Label Line(string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            return label;
        }
    }
}
