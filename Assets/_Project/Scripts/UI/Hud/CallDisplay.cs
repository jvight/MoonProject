using System;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// When 07's face shows the call (pure, EditMode-tested; docs/design/opening-v3-plan.md 2.4): once 07 has stood
    /// still long enough, and for a few seconds after the call's state changes or a letter decodes. A replayed
    /// (restored) change is the session starting, so it shows nothing. The change's seconds count only while it is on
    /// screen, so a change that waits for the guidance slot is still seen in full.
    /// </summary>
    internal sealed class CallDisplay
    {
        private const char Unknown = '?';
        private const char Space = ' ';

        private readonly CallDisplaySettings _settings;
        private float _still;
        private float _changeLeft;
        private bool _heard;

        public CallDisplay(CallDisplaySettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The sender's full identity, once known (null before).</summary>
        public string Sender { get; private set; }

        public CallState State { get; private set; }

        /// <summary>Characters of <see cref="Sender"/> decoded so far, its spaces not counted.</summary>
        public int Decoded { get; private set; }

        /// <summary>The sender as 07's face shows it: decoded characters, a question mark for each one still hidden.</summary>
        public string Letters { get; private set; } = string.Empty;

        /// <summary>The call has been heard and its sender is known, so there is something to show.</summary>
        public bool Known => _heard && Sender != null;

        /// <summary>A live change wants its seconds on the face.</summary>
        public bool WantsChange => Known && _changeLeft > 0f;

        /// <summary>07 has stood still long enough for its face to show the call.</summary>
        public bool WantsStill => Known && _still >= _settings.StillSeconds;

        public void SetSender(string sender)
        {
            if (string.IsNullOrEmpty(sender))
            {
                throw new ArgumentException("The call needs a sender.", nameof(sender));
            }

            Sender = sender;
            Letters = Mask(Sender, Decoded);
        }

        public void StateChanged(CallState state, bool restored)
        {
            bool changed = !_heard || state != State;
            State = state;
            Heard(restored, changed);
        }

        public void Decode(int decoded, bool restored)
        {
            bool changed = !_heard || decoded != Decoded;
            Decoded = Math.Max(0, decoded);
            if (Sender != null)
            {
                Letters = Mask(Sender, Decoded);
            }

            Heard(restored, changed);
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        /// <param name="still">07 is under the still speed now.</param>
        /// <param name="changeOnScreen">The change is on 07's face now: its seconds count only then.</param>
        public void Step(float deltaTime, bool still, bool changeOnScreen)
        {
            _still = still ? _still + deltaTime : 0f;
            if (changeOnScreen)
            {
                _changeLeft = Math.Max(0f, _changeLeft - deltaTime);
            }
        }

        /// <summary>
        /// <paramref name="sender"/> with every character after the first <paramref name="decoded"/> (spaces not
        /// counted) hidden: <c>A?? ??</c> for one of <c>ARK IV</c>.
        /// </summary>
        public static string Mask(string sender, int decoded)
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            var letters = new char[sender.Length];
            int shown = 0;
            for (int i = 0; i < sender.Length; i++)
            {
                char c = sender[i];
                if (c == Space)
                {
                    letters[i] = Space;
                    continue;
                }

                letters[i] = shown < decoded ? c : Unknown;
                shown++;
            }

            return new string(letters);
        }

        private void Heard(bool restored, bool changed)
        {
            _heard = true;
            if (restored)
            {
                _changeLeft = 0f;
            }
            else if (changed)
            {
                _changeLeft = _settings.ChangeSeconds;
            }
        }
    }
}
