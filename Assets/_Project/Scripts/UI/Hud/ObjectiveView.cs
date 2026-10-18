using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The objective line on screen (docs/design/opening-v2.md §3): one plain line, high on the left, bright for its
    /// rest and a small dim line after. The low-battery hint and the build step take the same line when the guidance
    /// slot gives them the turn; the weld step adds a thin bar that fills with the weld. A change of source or of step
    /// fades the line out before the new words fade in. The words are looked up only when the key or the language
    /// changes.
    /// </summary>
    internal sealed class ObjectiveView
    {
        private const string DimClass = "next-line--dim";
        private const float FullPercent = 100f;

        private readonly UiLayout _layout;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private string _key = string.Empty;
        private LineSource _source;
        private BuildStep _step;
        private string _stepText;
        private int _shownPercent = -1;

        public ObjectiveView(UiLayout layout, GuidanceSettings settings, ILocalization localization)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _reveal = new Reveal(layout.NextLine, settings.Line);
            _reveal.Snap(false);
            _layout.NextLineProgress.style.display = DisplayStyle.None;
            new ShadowPainter(layout.NextLineShadow);
        }

        private enum LineSource
        {
            None,
            Objective,
            Battery,
            Build,
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True while the line on screen (or easing) is the low-battery hint.</summary>
        public bool ShowsBattery => !_reveal.IsHidden && _source == LineSource.Battery;

        /// <summary>True while the line on screen (or easing) is the objective.</summary>
        public bool ShowsObjective => !_reveal.IsHidden && _source == LineSource.Objective;

        /// <summary>True while the line on screen (or easing) explains a build step.</summary>
        public bool ShowsBuildStep => !_reveal.IsHidden && _source == LineSource.Build;

        /// <summary>The build step the line explains while <see cref="ShowsBuildStep"/>.</summary>
        public BuildStep ShownStep => _source == LineSource.Build ? _step : BuildStep.None;

        /// <summary>True while the objective is shown in its small dim form (tests and captures).</summary>
        public bool IsDim => _layout.NextLine.ClassListContains(DimClass);

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="line">The objective's rule: its key, whether it wants to show and whether it is bright.</param>
        /// <param name="lineOpen">False while something else needs the screen (pause, a card, the title).</param>
        /// <param name="battery">The low-battery hint, which takes this same line when the slot gives it the turn.</param>
        /// <param name="batteryOpen">False while the hint may not show.</param>
        /// <param name="build">The build step's words and the weld's progress.</param>
        /// <param name="buildOpen">False while the build step may not show.</param>
        public void Tick(float deltaTime, ObjectiveLine line, bool lineOpen, LowBatteryHint battery, bool batteryOpen,
            BuildStripView build, bool buildOpen)
        {
            LineSource want = buildOpen && build != null && build.Step != BuildStep.None ? LineSource.Build
                : batteryOpen && battery.WantsShown ? LineSource.Battery
                : lineOpen && line.WantsShown ? LineSource.Objective
                : LineSource.None;
            bool same = want == _source &&
                        (want != LineSource.Objective || _key.Length == 0 ||
                         string.Equals(_key, line.Key, StringComparison.Ordinal)) &&
                        (want != LineSource.Build || build.Step == _step);
            bool shown = want != LineSource.None && (same || _reveal.IsHidden);
            if (shown && !same)
            {
                _source = want;
                _key = want == LineSource.Objective ? line.Key : string.Empty;
                _step = want == LineSource.Build ? build.Step : BuildStep.None;
                _stepText = null;
                Relocalize(battery);
            }
            else if (shown && want == LineSource.Objective && _key.Length == 0)
            {
                _key = line.Key;
                Relocalize(battery);
            }
            else if (shown && want == LineSource.Battery)
            {
                _layout.NextLineText.text = battery.Text;
            }

            if (shown && want == LineSource.Build)
            {
                WriteBuildStep(build);
            }

            _layout.NextLine.EnableInClassList(DimClass, shown && want == LineSource.Objective && !line.Bright);
            _reveal.Set(shown);
            _reveal.Tick(deltaTime);
        }

        /// <summary>A new language: the line on screen is written again.</summary>
        public void Relocalize(LowBatteryHint battery)
        {
            _stepText = null;
            switch (_source)
            {
                case LineSource.Battery:
                    _layout.NextLineText.text = battery.Text;
                    break;
                case LineSource.Objective when _key.Length > 0:
                    _layout.NextLineText.text = _localization.Get(_key);
                    break;
            }

            bool progress = _source == LineSource.Build && BuildSteps.ShowsProgress(_step);
            _layout.NextLineProgress.style.display = progress ? DisplayStyle.Flex : DisplayStyle.None;
            _shownPercent = -1;
        }

        /// <summary>The step's words (cached by the build view per key and device) and the weld bar, on change only.</summary>
        private void WriteBuildStep(BuildStripView build)
        {
            string text = build.StepText;
            if (!ReferenceEquals(text, _stepText))
            {
                _stepText = text;
                _layout.NextLineText.text = text;
            }

            if (!BuildSteps.ShowsProgress(_step))
            {
                return;
            }

            int percent = Mathf.RoundToInt(Mathf.Clamp01(build.WeldProgress01) * FullPercent);
            if (percent != _shownPercent)
            {
                _shownPercent = percent;
                _layout.NextLineProgressFill.style.width = Length.Percent(percent);
            }
        }
    }
}
