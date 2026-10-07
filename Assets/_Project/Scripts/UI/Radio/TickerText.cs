using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// The words of a ticker line in the current language: its localized key, with its argument (localized first when
    /// <see cref="TickerLine.ArgumentIsKey"/>) formatted into the {0}. A line whose text and argument disagree (an
    /// argument with nowhere to go, or a {0} with nothing to fill it) is a wiring bug: it is logged, and the text
    /// still shows as it is so the bug stays visible. Allocates; call it when a line starts, never per frame.
    /// </summary>
    internal sealed class TickerText
    {
        private const string Placeholder = "{0}";

        private readonly ILocalization _localization;

        public TickerText(ILocalization localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        public string Format(TickerLine line)
        {
            string text = _localization.Get(line.Key);
            bool hasPlaceholder = text.IndexOf(Placeholder, StringComparison.Ordinal) >= 0;
            if (string.IsNullOrEmpty(line.Argument))
            {
                if (hasPlaceholder)
                {
                    Debug.LogError($"{nameof(TickerText)}: '{line.Key}' has a {Placeholder} but its line carries no "
                                   + "argument.");
                }

                return text;
            }

            if (!hasPlaceholder)
            {
                Debug.LogError($"{nameof(TickerText)}: '{line.Key}' has no {Placeholder} for the argument "
                               + $"'{line.Argument}'.");
                return text;
            }

            string argument = line.ArgumentIsKey ? _localization.Get(line.Argument) : line.Argument;
            return string.Format(text, argument);
        }
    }
}
