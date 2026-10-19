using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// Counts the guidance elements on screen (the objective line, a chip, the call display, the controls card) every
    /// frame and fails the run when two show together for longer than the cross-fade a hand-over takes: the game's
    /// rule is one element at a time (opening-v3 §0), and the bot watches that rule the way a player would see it
    /// broken. Pure: the frame's shown names come in, the verdict comes out.
    /// </summary>
    public sealed class GuidanceCounter
    {
        private readonly float _graceSeconds;
        private float _together;
        private int _mostAtOnce;

        /// <param name="graceSeconds">Seconds two elements may show together while one fades over the other.</param>
        public GuidanceCounter(float graceSeconds)
        {
            _graceSeconds = graceSeconds;
        }

        /// <summary>The run broke the one-element rule; <see cref="Message"/> says how.</summary>
        public bool Failed { get; private set; }

        /// <summary>What broke the rule: the elements, and for how long, or empty.</summary>
        public string Message { get; private set; } = string.Empty;

        /// <summary>The most elements ever on screen together in one frame.</summary>
        public int MostAtOnce => _mostAtOnce;

        /// <summary>One frame: <paramref name="shown"/> are the names of the guidance elements on screen now.</summary>
        public void Step(float deltaSeconds, IReadOnlyList<string> shown)
        {
            if (shown.Count > _mostAtOnce)
            {
                _mostAtOnce = shown.Count;
            }

            if (shown.Count < 2)
            {
                _together = 0f;
                return;
            }

            _together += deltaSeconds;
            if (Failed || _together <= _graceSeconds)
            {
                return;
            }

            Failed = true;
            var text = new StringBuilder("two guidance elements were on screen together for ");
            text.Append(_together.ToString("0.0", CultureInfo.InvariantCulture)).Append(" s: ");
            for (int i = 0; i < shown.Count; i++)
            {
                text.Append(i > 0 ? ", " : string.Empty).Append(shown[i]);
            }

            Message = text.Append(" (the rule is one at a time).").ToString();
        }
    }
}
