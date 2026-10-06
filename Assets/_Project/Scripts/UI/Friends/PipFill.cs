using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// How full each pip of the parts readout is (pure logic, EditMode-tested): the shown amount walks towards the
    /// number of parts back at a gentle pace, so a newly returned part fills its pip instead of popping it on, and
    /// pips fill one after another in order.
    /// </summary>
    internal sealed class PipFill
    {
        private readonly FriendUiSettings _settings;

        public PipFill(FriendUiSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Parts shown, fractional while a pip fills.</summary>
        public float Shown { get; private set; }

        public int Target { get; private set; }

        /// <summary>Shows <paramref name="collected"/> at once (a different friend came into focus).</summary>
        public void Snap(int collected)
        {
            Target = collected;
            Shown = collected;
        }

        /// <summary>Moves towards <paramref name="collected"/>; true when <see cref="Shown"/> changed.</summary>
        public bool Step(int collected, float deltaTime)
        {
            Target = collected;
            float before = Shown;
            float step = _settings.PipFillSeconds <= 0f ? float.MaxValue : deltaTime / _settings.PipFillSeconds;
            Shown = Shown < Target ? Mathf.Min(Target, Shown + step) : Mathf.Max(Target, Shown - step);
            return before != Shown;
        }

        /// <summary>0..1, eased: how full pip <paramref name="index"/> is.</summary>
        public float Of(int index)
        {
            return UiEase.InOutSine(Shown - index);
        }
    }
}
