using System;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// Which of 07's wake cues each <see cref="WakeBeat"/> plays (docs/design/cutscenes.md §1.2), plain C# so the sheet
    /// is testable: the O1 thump and the faint ghost chord, a lid tick per stage, the lens whine and the tick as focus
    /// lands, the hum's dropout, the neck servo on each look with the ghost chord again at the pilot and at Earth, the
    /// blink's lid click and "hm?", and a soft tick as the short wake stirs. Hurried, every cue is softer and the ghost
    /// chords are left out.
    /// </summary>
    public sealed class WakeCueSheet
    {
        /// <summary>Most cues one beat plays.</summary>
        public const int MaxCalls = 2;

        public const int GhostFaint = 0;
        public const int GhostClearer = 1;
        public const int GhostLong = 2;
        public const int TickA2 = 0;
        public const int TickD3 = 1;
        public const int TickA4 = 2;
        public const int TickD5 = 3;

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
                    count = Ghost(calls, count, hurried, GhostFaint, _settings.CrackleGhostDelay, _settings.GhostFaint);
                    break;
                case WakeBeat.LidCracked:
                    calls[count++] = new WakeCueCall(WakeCueKind.Tick, TickA2, 0f, _settings.Tick * scale);
                    break;
                case WakeBeat.LidHalf:
                    calls[count++] = new WakeCueCall(WakeCueKind.Tick, TickD3, 0f, _settings.Tick * scale);
                    break;
                case WakeBeat.FocusHunting:
                    calls[count++] = new WakeCueCall(WakeCueKind.Whine, 0, 0f, _settings.Whine * scale);
                    break;
                case WakeBeat.FocusSettled:
                case WakeBeat.FarFocusSettled:
                    calls[count++] = new WakeCueCall(WakeCueKind.Tick, TickA4, 0f, _settings.Tick * scale);
                    break;
                case WakeBeat.Flicker:
                    calls[count++] = new WakeCueCall(WakeCueKind.Hum, 0, 0f, _settings.Hum * scale);
                    break;
                case WakeBeat.GazeHome:
                case WakeBeat.GazePeak:
                    calls[count++] = new WakeCueCall(WakeCueKind.Servo, 0, 0f, _settings.Servo * scale);
                    break;
                case WakeBeat.GazePilot:
                    calls[count++] = new WakeCueCall(WakeCueKind.Servo, 0, 0f, _settings.Servo * scale);
                    count = Ghost(calls, count, hurried, GhostClearer, _settings.PilotGhostDelay, _settings.GhostClearer);
                    break;
                case WakeBeat.GazeEarth:
                    calls[count++] = new WakeCueCall(WakeCueKind.Servo, 0, 0f, _settings.Servo * scale);
                    count = Ghost(calls, count, hurried, GhostLong, 0f, _settings.GhostLong);
                    break;
                case WakeBeat.Blinked:
                    calls[count++] = new WakeCueCall(WakeCueKind.Tick, TickD5, 0f, _settings.Tick * scale);
                    calls[count++] = new WakeCueCall(WakeCueKind.Chirp, 0, _settings.ChirpDelay, _settings.Chirp * scale);
                    break;
                case WakeBeat.Stirred:
                    calls[count++] = new WakeCueCall(WakeCueKind.Tick, TickD5, 0f, _settings.Stir);
                    break;
            }

            return count;
        }

        private static int Ghost(WakeCueCall[] calls, int count, bool hurried, int variant, float delay, float volume)
        {
            if (!hurried)
            {
                calls[count++] = new WakeCueCall(WakeCueKind.Ghost, variant, delay, volume);
            }

            return count;
        }
    }
}
