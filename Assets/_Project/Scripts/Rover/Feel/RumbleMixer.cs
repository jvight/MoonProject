using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Mixes gamepad rumble as plain logic (docs/design/feel-pass.md §4): at most two profiles play at once (a third
    /// replaces the one nearest its end, held ones last), mixed by max per motor, scaled by the player's strength and
    /// capped at <see cref="HardCap"/>. Off means silent at once. A fade takes everything down to silence over its
    /// seconds (a cancelled cut). Fixed memory, no allocations.
    /// </summary>
    public sealed class RumbleMixer
    {
        /// <summary>The motors never run harder than this, whatever the profiles and strength (owner,
        /// 2026-10-12).</summary>
        public const float HardCap = 0.35f;

        private const int Voices = 2;

        private readonly RumbleProfile[] _profiles = new RumbleProfile[Voices];
        private readonly float[] _elapsed = new float[Voices];
        private readonly float[] _seconds = new float[Voices];
        private readonly float[] _gains = new float[Voices];
        private float _fadeSeconds;
        private float _fade = 1f;

        /// <summary>Whether the player lets beats rumble (IFeelSettings.RumbleEnabled).</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>The player's strength 0..1 (IFeelSettings.RumbleStrength).</summary>
        public float Strength { get; set; } = 1f;

        /// <summary>This step's low motor speed, ready for the gamepad.</summary>
        public float Low { get; private set; }

        /// <summary>This step's high motor speed, ready for the gamepad.</summary>
        public float High { get; private set; }

        /// <summary>Whether any profile is playing.</summary>
        public bool IsPlaying
        {
            get
            {
                for (int i = 0; i < Voices; i++)
                {
                    if (_profiles[i] != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Starts <paramref name="profile"/> scaled by <paramref name="gain"/>, over <paramref name="seconds"/> (or the
        /// profile's own when negative). Returns its voice, to retune a held one with <see cref="SetGain"/>.
        /// </summary>
        public int Play(RumbleProfile profile, float gain = 1f, float seconds = -1f)
        {
            int voice = FreeVoice();
            _profiles[voice] = profile;
            _elapsed[voice] = 0f;
            _seconds[voice] = seconds >= 0f ? seconds : profile.Seconds;
            _gains[voice] = Mathf.Max(0f, gain);
            _fadeSeconds = 0f;
            _fade = 1f;
            return voice;
        }

        /// <summary>Retunes a playing voice (a sustained beam's level following its progress).</summary>
        public void SetGain(int voice, float gain)
        {
            if (voice >= 0 && voice < Voices && _profiles[voice] != null)
            {
                _gains[voice] = Mathf.Max(0f, gain);
            }
        }

        /// <summary>Stops one voice at once.</summary>
        public void Stop(int voice)
        {
            if (voice >= 0 && voice < Voices)
            {
                _profiles[voice] = null;
            }
        }

        /// <summary>Takes everything down to silence over <paramref name="seconds"/>; a new profile ends the
        /// fade.</summary>
        public void FadeOut(float seconds)
        {
            if (seconds <= 0f)
            {
                StopAll();
                return;
            }

            _fadeSeconds = seconds;
        }

        /// <summary>Silence at once (pause, focus lost, the gamepad gone).</summary>
        public void StopAll()
        {
            for (int i = 0; i < Voices; i++)
            {
                _profiles[i] = null;
            }

            _fadeSeconds = 0f;
            _fade = 1f;
            Low = 0f;
            High = 0f;
        }

        public void Step(float deltaTime)
        {
            float step = Mathf.Max(deltaTime, 0f);
            if (_fadeSeconds > 0f)
            {
                _fade -= step / _fadeSeconds;
                if (_fade <= 0f)
                {
                    StopAll();
                    return;
                }
            }

            float low = 0f;
            float high = 0f;
            for (int i = 0; i < Voices; i++)
            {
                RumbleProfile profile = _profiles[i];
                if (profile == null)
                {
                    continue;
                }

                _elapsed[i] += step;
                float seconds = _seconds[i];
                if (_elapsed[i] > seconds && !profile.HoldsEnd)
                {
                    _profiles[i] = null;
                    continue;
                }

                float share = seconds > 0f ? Mathf.Clamp01(_elapsed[i] / seconds) : 1f;
                low = Mathf.Max(low, profile.Low(share) * _gains[i]);
                high = Mathf.Max(high, profile.High(share) * _gains[i]);
            }

            float scale = Enabled ? Mathf.Clamp01(Strength) * _fade : 0f;
            Low = Mathf.Min(low * scale, HardCap);
            High = Mathf.Min(high * scale, HardCap);
        }

        /// <summary>An idle voice, else the timed one nearest its end, else the first.</summary>
        private int FreeVoice()
        {
            int best = 0;
            float bestLeft = float.PositiveInfinity;
            for (int i = 0; i < Voices; i++)
            {
                RumbleProfile profile = _profiles[i];
                if (profile == null)
                {
                    return i;
                }

                float left = profile.HoldsEnd ? float.MaxValue : _seconds[i] - _elapsed[i];
                if (left < bestLeft)
                {
                    bestLeft = left;
                    best = i;
                }
            }

            return best;
        }
    }
}
