using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// Localizes the fixed words of the UXML: every text element with the <see cref="MarkerClass"/> class carries its
    /// localization key as its text in the UXML (e.g. text="ui.pause.resume"), and shows the localized string. The
    /// keys are collected once at bind time; <see cref="Apply"/> re-reads them after a language change.
    /// </summary>
    internal sealed class StaticTextLocalizer
    {
        public const string MarkerClass = "loc";

        private readonly ILocalization _localization;
        private readonly List<TextElement> _elements = new List<TextElement>();
        private readonly List<string> _keys = new List<string>();

        public StaticTextLocalizer(VisualElement root, ILocalization localization)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            root.Query<TextElement>(className: MarkerClass).ForEach(Collect);
            Apply();
        }

        /// <summary>Number of localized elements (for tests).</summary>
        public int Count => _elements.Count;

        public void Apply()
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                _elements[i].text = _localization.Get(_keys[i]);
            }
        }

        private void Collect(TextElement element)
        {
            if (string.IsNullOrWhiteSpace(element.text))
            {
                throw new InvalidOperationException(
                    $"GameUI.uxml element '{element.name}' is marked '{MarkerClass}' but has no key in its text.");
            }

            _elements.Add(element);
            _keys.Add(element.text);
        }
    }
}
