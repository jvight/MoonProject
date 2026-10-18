using System;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// Which of 07's wake cues each <see cref="WakeBeat"/> plays (docs/design/cutscenes.md §1.2), plain C# so the sheet
    /// is testable: the thump in the black and the faint ghost chord, a lid tick as the picture opens and as the eyes
    /// open, and a soft tick as the short wake stirs. Hurried, every cue is softer and the ghost chord is left out.
    /// </summary>
    public sealed class WakeCueSheet
    {
        /// <summary>Most cues one beat plays.</summary>
        public const int MaxCalls = 2;

        public const int TickA2 = 0;
        public const int TickD3 = 1;
        public const int TickD5 = 2;

        private readonly WakeAudioSettings _settings;

        public WakeCueSheet(WakeAudioSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Writes the cues <paramref name="beat"/> plays into <paramref name="calls"/>; returns how many.</summary>
        public int CuesFor(WakeBeat beat, bool hurried, WakeCueCall[] calls)
        {
            if (calls == null || calls.Length < MaxCalls)
            {
                throw new ArgumentException($"needs room for {MaxCalls} calls", nameof(calls));
            }

            int count = 0;
            float scale = hurried ? _settings.Hurried : 1f;
            switch (beat)
            {
                case WakeBeat.Crackle:
                    calls[count++] = new WakeCueCall(WakeCueKind.Thump, 0, 0f, _settings.Thump * scale);
                    if (!hurried)
                    {
                        calls[count++] = new WakeCueCall(WakeCueKind.Ghost, 0, _settings.CrackleGhostDelay,
                            _settings.GhostFaint);
                    }

                    break;
                case WakeBeat.PictureOpens:
                    calls[count++] = new WakeCueCall(WakeCueKind.Tick, TickA2, 0f, _settings.Tick * scale);
                    break;
                case WakeBeat.EyesOpen:
                    calls[count++] = new WakeCueCall(WakeCueKind.Tick, TickD3, 0f, _settings.Tick * scale);
                    break;
                case WakeBeat.Stirred:
                    calls[count++] = new WakeCueCall(WakeCueKind.Tick, TickD5, 0f, _settings.Stir);
                    break;
            }

            return count;
        }
    }
}
