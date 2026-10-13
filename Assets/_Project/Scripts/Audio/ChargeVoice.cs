using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The cut's weighted charge-up in sound (docs/design/0.5.5-owner-feedback.md §5.5; pure, EditMode-tested): a
    /// capacitor whine that glides up through each stage and lands on D, A, then D′ at the locks, over a low hum on D
    /// that swells with the stages. A fresh charge waits out the brace before the whine rises; a warm emitter starts
    /// at once, already at the hum's level. The whine holds through the breath after the full lock, stops at the
    /// ignition, and on a release glides back down to D and fades. Allocation-free.
    /// </summary>
    public sealed class ChargeVoice
    {
        /// <summary>The whine's pitch at the locks, as a ratio of its D5 loop: D5, A5, D6.</summary>
        private static readonly float[] LockPitches = { 1f, 1.4983f, 2f };

        private const float StartPitch = 0.7491f;
        private const float DrainPitch = 0.5f;
        private const float GlideShare = 0.3f;

        private readonly EasedValue _pitch = new EasedValue(StartPitch);
        private readonly EasedValue _whine = new EasedValue(0f);
        private readonly EasedValue _hum = new EasedValue(0f);
        private Phase _phase;
        private float _wait;
        private float _stageSeconds;
        private float _pitchTarget = StartPitch;
        private float _humTarget;
        private float _drainSeconds;

        private enum Phase
        {
            Silent,
            Brace,
            Building,
            Held,
            Draining,
        }

        /// <summary>The whine's pitch factor on its D5 loop.</summary>
        public float Pitch => _pitch.Value;

        /// <summary>The whine's level, 0..1.</summary>
        public float WhineLevel => _whine.Value;

        /// <summary>The hum's level, 0..1.</summary>
        public float HumLevel => _hum.Value;

        /// <summary>True from the charge's start until its whine and hum have died away.</summary>
        public bool IsSounding => _phase != Phase.Silent;

        /// <summary>
        /// The charge starts (stage 0): after <paramref name="braceSeconds"/> on a fresh charge, at once on a warm
        /// one; the first stage then builds over <paramref name="stageSeconds"/>.
        /// </summary>
        public void Begin(bool warm, float braceSeconds, float stageSeconds, float humStart)
        {
            _stageSeconds = Mathf.Max(0.01f, stageSeconds);
            _pitchTarget = LockPitches[0];
            _humTarget = humStart;
            if (warm)
            {
                _phase = Phase.Building;
                _pitch.Snap(StartPitch);
                _whine.Snap(humStart);
                _hum.Snap(humStart);
                return;
            }

            _phase = Phase.Brace;
            _wait = Mathf.Max(0f, braceSeconds);
            _pitch.Snap(StartPitch);
            _whine.Snap(0f);
            _hum.Snap(0f);
        }

        /// <summary>Stage <paramref name="stage"/> of <paramref name="needed"/> locked: the whine sits on its note and
        /// glides on toward the next; the full lock holds it.</summary>
        public void Lock(int stage, int needed, float humLevel)
        {
            if (_phase == Phase.Silent || _phase == Phase.Draining)
            {
                return;
            }

            int index = Mathf.Clamp(stage - 1, 0, LockPitches.Length - 1);
            _pitch.Snap(LockPitches[index]);
            _humTarget = humLevel;
            if (stage >= needed)
            {
                _phase = Phase.Held;
                _pitchTarget = LockPitches[index];
                return;
            }

            _phase = Phase.Building;
            _pitchTarget = LockPitches[Mathf.Min(index + 1, LockPitches.Length - 1)];
        }

        /// <summary>The beam fires: the charge's voice stops (the ignition takes over).</summary>
        public void Fire()
        {
            _phase = Phase.Silent;
            _whine.Snap(0f);
            _hum.Snap(0f);
        }

        /// <summary>Released before firing: the whine glides back down to D and fades over
        /// <paramref name="seconds"/>.</summary>
        public void Drain(float seconds)
        {
            if (_phase == Phase.Silent)
            {
                return;
            }

            _phase = Phase.Draining;
            _drainSeconds = Mathf.Max(0.01f, seconds);
            _wait = _drainSeconds;
            _pitchTarget = DrainPitch;
        }

        public void Step(float deltaTime, float whineLevel)
        {
            switch (_phase)
            {
                case Phase.Brace:
                    _wait -= deltaTime;
                    if (_wait <= 0f)
                    {
                        _phase = Phase.Building;
                    }

                    _hum.Step(_humTarget, deltaTime, _stageSeconds);
                    break;
                case Phase.Building:
                    _pitch.Step(_pitchTarget, deltaTime, _stageSeconds * GlideShare);
                    _whine.Step(whineLevel, deltaTime, _stageSeconds * GlideShare);
                    _hum.Step(_humTarget, deltaTime, _stageSeconds);
                    break;
                case Phase.Held:
                    _whine.Step(whineLevel, deltaTime, _stageSeconds * GlideShare);
                    _hum.Step(_humTarget, deltaTime, _stageSeconds);
                    break;
                case Phase.Draining:
                    _wait -= deltaTime;
                    _pitch.Step(_pitchTarget, deltaTime, _drainSeconds * GlideShare);
                    _whine.Step(0f, deltaTime, _drainSeconds * GlideShare);
                    _hum.Step(0f, deltaTime, _drainSeconds * GlideShare);
                    if (_wait <= 0f)
                    {
                        Fire();
                    }

                    break;
            }
        }
    }
}
