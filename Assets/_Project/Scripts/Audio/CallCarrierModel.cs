using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The call's carrier as numbers (docs/design/opening-v3-purpose.md §2): its gain from how squarely 07 faces the
    /// light (<see cref="FacingGain"/>, a pure monotonic function), which loop and how much static each
    /// <see cref="CallState"/> uses, and, while Broken, the dropouts and the wow, stepped with a seeded
    /// <see cref="AudioRandom"/>. Plain logic, applied by the call's audio component. Allocation-free.
    /// </summary>
    public sealed class CallCarrierModel
    {
        private readonly CallCarrierSettings _settings;
        private readonly AudioRandom _random;
        private float _facingGain;
        private float _dropoutLeft;
        private float _holdLeft;
        private float _dropout01;
        private float _breakUp01;
        private float _static;
        private float _wowPhase;

        public CallCarrierModel(CallCarrierSettings settings, uint seed)
        {
            _settings = settings;
            _random = new AudioRandom(seed);
            _holdLeft = _random.Range(settings.HoldMin, settings.HoldMax);
            _facingGain = settings.AwayFloor;
        }

        /// <summary>The call's state the carrier follows.</summary>
        public CallState State { get; private set; } = CallState.Broken;

        /// <summary>True once relay.1 answers: the call-and-response loop plays instead of the call alone.</summary>
        public bool Answered => State == CallState.Answered;

        /// <summary>The carrier's volume scale now (level x facing x the dropouts).</summary>
        public float Gain { get; private set; }

        /// <summary>The carrier's pitch scale now (1 except for the wow while broken).</summary>
        public float Pitch { get; private set; } = 1f;

        /// <summary>The static's volume scale now (share of its cue volume).</summary>
        public float Static => _static;

        /// <summary>True while a dropout is cutting the carrier (tests and tooling).</summary>
        public bool InDropout => _dropoutLeft > 0f;

        /// <summary>The length of the dropout that began last, in seconds (tests).</summary>
        public float LastDropoutLength { get; private set; }

        /// <summary>
        /// The share of the carrier's level heard at a facing of <paramref name="facingCos"/> (the cosine between 07's
        /// gaze and the light's bearing, -1 away to 1 squarely on): from <paramref name="awayFloor"/> up to 1,
        /// never decreasing as 07 turns toward it, gathering toward the end like tuning a radio.
        /// </summary>
        public static float FacingGain(float facingCos, float awayFloor, float sharpness)
        {
            float toward = Mathf.Clamp01((facingCos + 1f) * 0.5f);
            return Mathf.Lerp(Mathf.Clamp01(awayFloor), 1f, Mathf.Pow(toward, Mathf.Max(0.01f, sharpness)));
        }

        /// <summary>The static under the carrier in <paramref name="state"/>: some while broken, a little once locked,
        /// none when the night clears it or relay.1 answers.</summary>
        public static float StaticFor(CallState state, CallCarrierSettings settings)
        {
            switch (state)
            {
                case CallState.Broken:
                    return settings.BrokenStatic;
                case CallState.Locked:
                    return settings.LockedStatic;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// The note step for the <paramref name="decoded"/>-th character of <paramref name="total"/> (from 1): the climb
        /// is spread over <paramref name="steps"/> notes so it rises as the name completes and the last character
        /// always lands on the top step (D).
        /// </summary>
        public static int DecodeStep(int decoded, int total, int steps)
        {
            if (steps <= 1 || total <= 1)
            {
                return Mathf.Max(0, steps - 1);
            }

            float share = Mathf.Clamp01((decoded - 1f) / (total - 1f));
            return Mathf.RoundToInt(share * (steps - 1));
        }

        /// <summary>The call changed state (or was restored): the carrier eases into its new mode.</summary>
        public void SetState(CallState state)
        {
            State = state;
        }

        /// <summary>Advances by <paramref name="deltaTime"/> with 07 facing the light at <paramref name="facingCos"/>.
        /// </summary>
        public void Step(float deltaTime, float facingCos)
        {
            float dt = Mathf.Max(0f, deltaTime);
            float target = FacingGain(facingCos, _settings.AwayFloor, _settings.FacingSharpness);
            _facingGain += (target - _facingGain) * (1f - Mathf.Exp(-dt / _settings.FacingSmoothing));

            float settle = dt / _settings.StateFade;
            _breakUp01 = Mathf.MoveTowards(_breakUp01, State == CallState.Broken ? 1f : 0f, settle);
            _static = Mathf.MoveTowards(_static, StaticFor(State, _settings), settle * _settings.BrokenStatic);
            StepDropouts(dt);

            float cut = Mathf.Lerp(1f, _settings.DropoutGain, _dropout01 * _breakUp01);
            Gain = _settings.Level * _facingGain * cut;
            _wowPhase = Mathf.Repeat(_wowPhase + dt * _settings.WowRate, 1f);
            Pitch = 1f + _settings.WowDepth * _breakUp01 * Mathf.Sin(2f * Mathf.PI * _wowPhase);
        }

        private void StepDropouts(float dt)
        {
            if (_dropoutLeft > 0f)
            {
                _dropoutLeft -= dt;
                if (_dropoutLeft <= 0f)
                {
                    _dropoutLeft = 0f;
                    _holdLeft = _random.Range(_settings.HoldMin, _settings.HoldMax);
                }
            }
            else if (State == CallState.Broken)
            {
                _holdLeft -= dt;
                if (_holdLeft <= 0f)
                {
                    LastDropoutLength = _random.Range(_settings.DropoutMin, _settings.DropoutMax);
                    _dropoutLeft = LastDropoutLength;
                }
            }

            float ease = dt / _settings.DropoutEase;
            _dropout01 = Mathf.MoveTowards(_dropout01, _dropoutLeft > 0f ? 1f : 0f, ease);
        }
    }
}
